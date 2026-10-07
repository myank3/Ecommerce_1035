using AspNetCoreGeneratedDocument;
using brevo_csharp.Model;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Models.Models.ViewModel;
using Ecommerce_1035.Utilities;
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Models;
using System.Security.Claims;
using System.Text;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Ecommerce_1035.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitofWork _work;
        private readonly PaypalServerSdkClient _paypalClient;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<CartController> _logger;
        private readonly IConfiguration _config;

        public CartController(
            IUnitofWork work,
            PaypalServerSdkClient pal,
            ISmsSender smsSender,
            ILogger<CartController> logger,
            IConfiguration config)
        {
            _work = work;
            _paypalClient = pal;
            _smsSender = smsSender;
            _logger = logger;
            _config = config;
        }

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null)
            {
                ShoppingCartVM = new ShoppingCartVM()
                {
                    ListCart = new List<ShoppingCart>()
                };
                return View(ShoppingCartVM);
            }

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _work.ShoppingCart.GetAll(sc => sc.ApplicationUserId == claims.Value, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = _work.ApplicationUser.FirstOrDefault(au => au.Id == claims.Value);

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                if (list.Product.Description != null && list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }

            return View(ShoppingCartVM);
        }

        public IActionResult plus(int id)
        {
            var cart = _work.ShoppingCart.Get(id);
            if (cart != null)
            {
                cart.Count += 1;
                _work.Save();
            }
            return RedirectToAction("Index");
        }

        public IActionResult minus(int id)
        {
            var cart = _work.ShoppingCart.Get(id);
            if (cart != null)
            {
                if (cart.Count <= 1)
                    cart.Count = 1;
                else
                    cart.Count -= 1;

                _work.Save();
            }
            return RedirectToAction("Index");
        }

        public IActionResult delete(int id)
        {
            var cart = _work.ShoppingCart.Get(id);
            if (cart != null)
            {
                _work.ShoppingCart.Remove(id);
                _work.Save();
            }
            return RedirectToAction("Index");
        }

        public IActionResult summary([FromQuery] int[] selectedItem)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (selectedItem == null || selectedItem.Length == 0)
            {
                return RedirectToAction("Index");
            }

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _work.ShoppingCart.GetAll(sc => sc.ApplicationUserId == claims.Value && selectedItem.Contains(sc.Id), includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _work.ApplicationUser.FirstOrDefault(au => au.Id == claims.Value);

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                if (list.Product.Description != null && list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> summaryPost(int[]? selectedItem, string? paymentMethod)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity!;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims == null) return NotFound();

            // 1. Resolve Cart Items (check selectedItem array first; fall back to all items in cart)
            if (selectedItem != null && selectedItem.Length > 0)
            {
                ShoppingCartVM.ListCart = _work.ShoppingCart.GetAll(
                    sc => sc.ApplicationUserId == claims.Value && selectedItem.Contains(sc.Id),
                    includeProperties: "Product").ToList();
            }
            else
            {
                ShoppingCartVM.ListCart = _work.ShoppingCart.GetAll(
                    sc => sc.ApplicationUserId == claims.Value,
                    includeProperties: "Product").ToList();
            }

            if (!ShoppingCartVM.ListCart.Any())
            {
                TempData["Error"] = "Your cart is empty or no valid items were selected.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Calculate Item Prices and Total Order Amount FIRST
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
            }

            // 3. Ensure minimum transaction limit (Razorpay minimum is 100 paise = 1 INR)
            if (ShoppingCartVM.OrderHeader.OrderTotal < 1.0)
            {
                TempData["Error"] = "Order total must be at least ₹1.00 to process payment.";
                return RedirectToAction(nameof(Index));
            }

            // 4. Populate and Save OrderHeader
            ShoppingCartVM.OrderHeader.ApplicationUser = _work.ApplicationUser.FirstOrDefault(au => au.Id == claims.Value);
            ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claims.Value;

            _work.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _work.Save();

            // 5. Save OrderDetails line items
            foreach (var list in ShoppingCartVM.ListCart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = list.ProductId,
                    Price = list.Price,
                    Count = list.Count
                };
                _work.OrderDetail.Add(orderDetail);
            }
            _work.Save();

            // 6. Remove purchased items from ShoppingCart & reset session counter
            _work.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
            _work.Save();
            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, 0);

            // 7. Resolve Selected Gateway
            var selectedGateway = !string.IsNullOrEmpty(paymentMethod)
                ? paymentMethod
                : (ShoppingCartVM?.PaymentMethod ?? "Razorpay");

            // =========================================================
            // OPTION A: RAZORPAY GATEWAY
            // =========================================================
            if (string.Equals(selectedGateway, "Razorpay", StringComparison.OrdinalIgnoreCase))
            {
                var keyId = _config["Razorpay:KeyId"]?.Trim();
                var keySecret = _config["Razorpay:KeySecret"]?.Trim();

                if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret))
                {
                    TempData["Error"] = "Razorpay credentials are not configured properly.";
                    return RedirectToAction(nameof(Index));
                }

                int amountInPaise = (int)Math.Round(ShoppingCartVM.OrderHeader.OrderTotal * 100);

                var client = new Razorpay.Api.RazorpayClient(keyId, keySecret);
                var rzpOptions = new Dictionary<string, object>
        {
            { "amount", amountInPaise },
            { "currency", "INR" },
            { "receipt", $"order_{ShoppingCartVM.OrderHeader.Id}" },
            { "payment_capture", 1 }
        };

                var razorOrder = client.Order.Create(rzpOptions);
                string razorpayOrderId = razorOrder["id"].ToString();

                // Pass credentials and parameters to view
                ViewBag.RazorpayKeyId = keyId;
                ViewBag.RazorpayOrderId = razorpayOrderId;
                ViewBag.AmountInPaise = amountInPaise;
                ViewBag.OrderHeaderId = ShoppingCartVM.OrderHeader.Id;
                ViewBag.PrefillName = ShoppingCartVM.OrderHeader.Name;
                ViewBag.PrefillEmail = ShoppingCartVM.OrderHeader.ApplicationUser?.Email ?? "";
                ViewBag.PrefillContact = ShoppingCartVM.OrderHeader.PhoneNumber;

                // Re-render Summary view to automatically trigger the Razorpay modal
                return View(ShoppingCartVM);
            }

            // =========================================================
            // OPTION B: PAYPAL GATEWAY
            // =========================================================
            var orderRequest = new OrderRequest
            {
                Intent = CheckoutPaymentIntent.Capture,
                PurchaseUnits = new List<PurchaseUnitRequest>
        {
            new PurchaseUnitRequest
            {
                Amount = new AmountWithBreakdown
                {
                    CurrencyCode = "USD",
                    MValue = ShoppingCartVM.OrderHeader.OrderTotal.ToString(
                        "0.00", System.Globalization.CultureInfo.InvariantCulture)
                },
                CustomId = ShoppingCartVM.OrderHeader.Id.ToString()
            }
        },
                ApplicationContext = new OrderApplicationContext
                {
                    ReturnUrl = Url.Action("PayPalReturn", "Cart",
                        new { id = ShoppingCartVM.OrderHeader.Id }, Request.Scheme),
                    CancelUrl = Url.Action("Index", "Cart", null, Request.Scheme),
                    UserAction = OrderApplicationContextUserAction.PayNow
                }
            };

            var createOrderInput = new CreateOrderInput { Body = orderRequest };
            var result = await _paypalClient.OrdersController.CreateOrderAsync(createOrderInput);

            var approvalLink = result.Data.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            if (string.IsNullOrEmpty(approvalLink))
            {
                TempData["Error"] = "Failed to create PayPal order.";
                return RedirectToAction(nameof(Index));
            }

            return Redirect(approvalLink);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RazorpayReturn(
            string razorpay_payment_id,
            string razorpay_order_id,
            string razorpay_signature,
            int orderHeaderId)
        {
            var orderHeader = _work.OrderHeader.Get(orderHeaderId);
            if (orderHeader == null) return NotFound();

            var keySecret = _config["Razorpay:KeySecret"]?.Trim();
            string payload = $"{razorpay_order_id}|{razorpay_payment_id}";

            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(keySecret!));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            string generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            bool isValid = string.Equals(generatedSignature, razorpay_signature, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                orderHeader.OrderStatus = SD.OrderStatusApproved;
                orderHeader.TransactionId = razorpay_payment_id;
                orderHeader.PaymentDate = DateTime.Now;
                _work.Save();

                return RedirectToAction(nameof(OrderConfirmation), new { id = orderHeader.Id });
            }
            else
            {
                orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                orderHeader.OrderStatus = SD.OrderStatusCancelled;
                _work.Save();

                TempData["Error"] = "Payment verification failed. Your card or account was not charged.";
                return RedirectToAction(nameof(Index));
            }
        }
        public async Task<IActionResult> PayPalReturn(int id, string token)
        {
            var orderHeader = _work.OrderHeader.Get(id);
            if (orderHeader == null) return NotFound();

            var captureInput = new CaptureOrderInput { Id = token };

            try
            {
                var result = await _paypalClient.OrdersController.CaptureOrderAsync(captureInput);
                var order = result.Data;

                if (order.Status == OrderStatus.Completed)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    orderHeader.OrderStatus = SD.OrderStatusApproved;
                    orderHeader.TransactionId = order.Id;
                    orderHeader.PaymentDate = DateTime.Now;
                    _work.Save();
                }
                else
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                    _work.Save();
                    TempData["Error"] = "Payment was not completed.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                _work.Save();
                TempData["Error"] = "Payment capture failed: " + ex.Message;
                return RedirectToAction("Index");
            }

            return RedirectToAction("OrderConfirmation", "Cart", new { id = orderHeader.Id });
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = _work.OrderHeader.GetAll(o => o.Id == id, includeProperties: "ApplicationUser").FirstOrDefault();
            if (order == null) return NotFound();

            var orderDetails = _work.OrderDetail
                .GetAll(od => od.OrderHeaderId == id, includeProperties: "Product")
                .ToList();

            if (order.ApplicationUser != null)
            {
                var user = order.ApplicationUser;

                var smsMessage = $"Book-Shelf: Order #{order.Id} confirmed. Total Rs.{order.OrderTotal:0.00}. Thanks for shopping!";
                var voiceMessage = $"Hello {order.Name}. Your Book-Shelf order number {order.Id} has been confirmed. Total amount is {order.OrderTotal:0.00} rupees. Thank you for shopping with us.";
                var emailSubject = $"Order Confirmation #{order.Id} — Book-Shelf";
                var emailBody = BuildOrderEmailBody(order, orderDetails);

                try
                {
                    await _smsSender.SendAllChannelsAsync(
                        number: user.PhoneNumber,
                        email: user.Email,
                        smsMessage: smsMessage,
                        voiceMessage: voiceMessage,
                        emailSubject: emailSubject,
                        emailBody: emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order confirmation notifications for order {OrderId}", order.Id);
                }
            }

            return View(id);
        }

        private static string BuildOrderEmailBody(OrderHeader order, List<OrderDetail> details)
        {
            var rows = string.Join("", details.Select(d =>
                $"""
                <tr>
                    <td style="padding:8px;border-bottom:1px solid #eee;">{d.Product?.Title ?? "Item"}</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;text-align:center;">{d.Count}</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;text-align:right;">₹{d.Price:0.00}</td>
                </tr>
                """));

            return $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                    <h1 style="color:#1a73e8;">Thank you for your order!</h1>
                    <p>Hi {order.Name},</p>
                    <p>Your order <strong>#{order.Id}</strong> was placed on {order.OrderDate:dd MMM yyyy HH:mm}.</p>

                    <h3>Order Items</h3>
                    <table style="width:100%;border-collapse:collapse;">
                        <thead>
                            <tr>
                                <th style="text-align:left;padding:8px;border-bottom:2px solid #eee;">Product</th>
                                <th style="text-align:center;padding:8px;border-bottom:2px solid #eee;">Qty</th>
                                <th style="text-align:right;padding:8px;border-bottom:2px solid #eee;">Price</th>
                            </tr>
                        </thead>
                        <tbody>
                            {rows}
                        </tbody>
                    </table>

                    <p style="text-align:right;font-size:18px;"><strong>Total: ₹{order.OrderTotal:0.00}</strong></p>
                    <p>Payment Status: <strong>{order.PaymentStatus}</strong></p>
                    <p>Order Status: <strong>{order.OrderStatus}</strong></p>

                    <h3>Shipping Address</h3>
                    <p>
                        {order.Name}<br/>
                        {order.StreetAddress}<br/>
                        {order.City}, {order.State} {order.PostalCode}<br/>
                        {order.PhoneNumber}
                    </p>

                    <p style="color:#666;font-size:12px;margin-top:30px;">This is an automated confirmation.</p>
                </div>
                """;
        }
    }
}