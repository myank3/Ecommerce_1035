using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Models.Models.ViewModel;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks.Dataflow;

namespace Ecommerce_1035.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitofWork _work;
        public HomeController(IUnitofWork works)
        {
            _work = works;
        }

        public IActionResult Index(string sortOrder = null)
        {
            var claimsId = (ClaimsIdentity)User.Identity;
            var claim = claimsId.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _work.ShoppingCart.GetAll(sc => sc.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }
            IEnumerable<Product> productList = _work.Product.GetAll(includeProperties: "Category,CoverTypes");
            if(sortOrder == "bestseller")
            {
                var orderDetail = _work.OrderDetail.GetAll();
                var bestSeller = orderDetail.GroupBy(x => x.ProductId).ToDictionary
                    (group => group.Key,
                    group => group.Sum(od => od.Count)
                    );
                productList = productList.OrderByDescending(p => bestSeller.ContainsKey(p.Id) ? bestSeller[p.Id] : 0).ToList();
                ViewBag.BestsellerQuantities = bestSeller;
            }
            ViewBag.CurrentSort = sortOrder;
            return View(productList);
        }
        [HttpGet]
        public IActionResult Details(int id)

        {
            var claimsId = (ClaimsIdentity)User.Identity;
            var claim = claimsId.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _work.ShoppingCart.GetAll(sc => sc.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }

            var productInDb = _work.Product.FirstOrDefault(p => p.Id == id, includedProperties: "Category,CoverTypes");
            if (productInDb == null) return NotFound();
            var shoppingCartEdit = new ShoppingCart()
            {
                Product = productInDb,
                ProductId = id
            };
            return View(shoppingCartEdit);

        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            if (ModelState.IsValid)
            {
                var claimIdentity = (ClaimsIdentity)User.Identity;
                var claims = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claims == null) return NotFound();
                shoppingCart.ApplicationUserId = claims.Value;
                var shoppingCartInDB = _work.ShoppingCart.FirstOrDefault(sc => sc.ApplicationUserId == claims.Value && sc.ProductId == shoppingCart.ProductId);
                if (shoppingCartInDB == null)
                    _work.ShoppingCart.Add(shoppingCart);
                else
                    shoppingCartInDB.Count += shoppingCart.Count;
                _work.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var productInDb = _work.Product.FirstOrDefault(p => p.Id == shoppingCart.ProductId, includedProperties: "Category,CoverTypes");
                if (productInDb == null) return NotFound();
                var shoppingCartEdit = new ShoppingCart()
                {
                    Product = productInDb,
                    ProductId = shoppingCart.ProductId
                };
                return View(shoppingCartEdit);
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

}