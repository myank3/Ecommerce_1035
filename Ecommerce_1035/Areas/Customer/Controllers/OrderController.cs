using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Models.Models.ViewModel;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce_1035.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitofWork _work;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(IUnitofWork work, UserManager<ApplicationUser> userManager)
        {
            _work = work;
            _userManager = userManager;
        }

        // GET: /Customer/Order
        public IActionResult Index(string? fromDate, string? toDate, string? status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Do NOT pre-filter by OrderStatus. Customers must see ALL their orders.
            var orders = _work.OrderHeader
                .GetAll(o => o.ApplicationUserId == userId,
                            includeProperties: "ApplicationUser")
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            // Optional date range filter
            if (!string.IsNullOrEmpty(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                orders = orders.Where(o => o.OrderDate.Date >= from.Date);
            }

            if (!string.IsNullOrEmpty(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                orders = orders.Where(o => o.OrderDate.Date <= to.Date);
            }

            // Optional status filter — only applied when provided.
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.OrderStatus == status);
            }

            return View(orders.ToList());
        }

        // GET: /Customer/Order/Details/5
        public IActionResult Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var order = _work.OrderHeader
                .FirstOrDefault(o => o.Id == id && o.ApplicationUserId == userId,
                                 includedProperties: "ApplicationUser");

            if (order == null)
                return NotFound();

            var vm = new OrderVM
            {
                OrderHeader = order,
                OrderDetailList = _work.OrderDetail
                    .GetAll(d => d.OrderHeaderId == id, includeProperties: "Product")
                    .ToList()
            };

            return View(vm);
        }

         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reorder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var order = _work.OrderHeader
                .FirstOrDefault(o => o.Id == id && o.ApplicationUserId == userId);

            if (order == null)
                return NotFound();

            var details = _work.OrderDetail
                .GetAll(d => d.OrderHeaderId == id, includeProperties: "Product")
                .ToList();

            if (!details.Any())
            {
                TempData["error"] = "This order has no items to reorder.";
                return RedirectToAction(nameof(Index));
            }

            var cart = _work.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId)
                .ToList();

            foreach (var d in details)
            {
                if (d.Product == null) continue;

                var existing = cart.FirstOrDefault(c => c.ProductId == d.ProductId);
                if (existing != null)
                {
                    existing.Count += d.Count;
                }
                else
                {
                    _work.ShoppingCart.Add(new ShoppingCart
                    {
                        ProductId = d.ProductId,
                        ApplicationUserId = userId,
                        Count = d.Count
                    });
                }
            }

            _work.Save();

            TempData["success"] = $"{details.Count} item(s) added to your cart.";
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }

        #region APIs
         
        [HttpGet]
        public IActionResult GetAll(string? fromDate, string? toDate, string? status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var orders = _work.OrderHeader
                .GetAll(o => o.ApplicationUserId == userId,
                            includeProperties: "ApplicationUser")
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                orders = orders.Where(o => o.OrderDate.Date >= from.Date);
            }

            if (!string.IsNullOrEmpty(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                orders = orders.Where(o => o.OrderDate.Date <= to.Date);
            }

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.OrderStatus == status);
            }

            return Json(new { data = orders.ToList() });
        }

        #endregion
    }
}