using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_1035.Areas.Admin.Controllers
{
    [Area("Admin")]

    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CategoryController : Controller
    {
      
        private readonly IUnitofWork _work;
        public CategoryController(IUnitofWork work)
        {
            _work = work;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        #region APIs
       
        public IActionResult GetAll()
        {
            var get = _work.Category.GetAll();
            return Json(new { data = get });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var get = _work.Category.Get(id);
            if (get == null) return Json(new { success = false, message = "no delete" });
            _work.Category.Remove(get);
            _work.Save();
            return Json(new { success = true, message = "Deleted" });
        }

        #endregion

        public IActionResult Upsert(int? id)
        {
            Category cat = new Category();
            if (id == null) return View(cat);
            cat = _work.Category.Get(id.GetValueOrDefault());
            if (cat == null) return NotFound();
            return View(cat);

        }
        [HttpPost]
        public IActionResult Upsert(Category cat)
        {
            if (cat == null) return BadRequest();
            if (ModelState.IsValid)
            {
                if (cat.Id == 0)
                {
                    _work.Category.Add(cat);

                }
                else
                {
                    _work.Category.Update(cat);
                }
                _work.Save();
            }

            return RedirectToAction("Index");
        }
    }
}