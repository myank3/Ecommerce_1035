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
                return Challenge(); // not signed in → trigger auth challenge

            // Only this user's orders
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

            // Optional status filter
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

            // If not found OR it belongs to someone else → 404
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

        #region APIs

        // GET: /Customer/Order/GetAll?fromDate=&toDate=&status=
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