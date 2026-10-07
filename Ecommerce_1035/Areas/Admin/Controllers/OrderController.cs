using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models.ViewModel;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_1035.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitofWork _work;

        public OrderController(IUnitofWork work)
        {
            _work = work;
        }

        public IActionResult Index(string? fromDate, string? toDate)
        {
            var orders = _work.OrderHeader
                .GetAll(includeProperties: "ApplicationUser")
                .ToList();

            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var vm = new OrderVM
            {
                OrderHeader = _work.OrderHeader
                    .FirstOrDefault(u => u.Id == id, includedProperties: "ApplicationUser"),

                OrderDetailList = _work.OrderDetail
                    .GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(vm);
        }
        #region APIs
        [HttpGet]
        public IActionResult GetAll(string? fromDate, string? toDate)
        {
            var orders = _work.OrderHeader
                .GetAll(includeProperties: "ApplicationUser")
                .ToList();

            if (!string.IsNullOrEmpty(fromDate))
                orders = orders.Where(o => o.OrderDate.Date >= Convert.ToDateTime(fromDate).Date).ToList();

            if (!string.IsNullOrEmpty(toDate))
                orders = orders.Where(o => o.OrderDate.Date <= Convert.ToDateTime(toDate).Date).ToList();

            return Json(new { data = orders });
        }
        #endregion
    }
}