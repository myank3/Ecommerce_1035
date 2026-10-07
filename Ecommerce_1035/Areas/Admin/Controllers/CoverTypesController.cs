using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Identity.Client;

namespace Ecommerce_1035.Areas.Admin.Controllers
{
    [Area("Admin")]

    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CoverTypesController : Controller
    {
        private readonly IUnitofWork _work;
        public CoverTypesController(IUnitofWork work)
        {
            _work = work;
        }
        public IActionResult Index()
        {
            return View();
        }
        #region APIs

        public IActionResult GetAll()
        {
            var get =  _work.CoverTypes.GetAll();
            return Json(new { data = get });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var get = _work.CoverTypes.Get(id);
            if (get == null) return Json(new { success = false, message = "Can't be deleted" });
            _work.CoverTypes.Remove(get);
            _work.Save();
            return Json(new { success = true, message = "Deleted" });
        }

        #endregion
        [HttpGet]
        public IActionResult Upsert (int? id)
        {
            CoverTypes cvr = new CoverTypes();
            if (id == null) return View(cvr);
            cvr =  _work.CoverTypes.Get(id.GetValueOrDefault());
            if (cvr == null) return BadRequest();
            return View(cvr);
        }
        [HttpPost]
        public IActionResult Upsert (CoverTypes cvr)
        {
            
            if (cvr == null) return BadRequest();
            if (ModelState.IsValid)
            {
                if(cvr.Id == 0)
                {
                    _work.CoverTypes.Add(cvr);

                }
                else
                {
                    _work.CoverTypes.Update(cvr);
                }
                _work.Save();
            }
            return RedirectToAction("Index");


        }
        


        
    }
}
