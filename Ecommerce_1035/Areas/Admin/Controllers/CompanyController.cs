using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_1035.Areas.Admin.Controllers
{
    [Area("Admin")]

    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        
            private readonly IUnitofWork _work;
            public CompanyController(IUnitofWork work)
            {
                _work = work;
            }

            public IActionResult Index()
            {
                return View();
            }
            #region APIs
            [HttpGet]
            public IActionResult GetAll()
            {
                var get = _work.Company.GetAll();
                return Json(new { data = get });
            }
            [HttpDelete]
            public IActionResult Delete(int id)
            {
                var get = _work.Company.Get(id);
                if (get == null) return Json(new { success = false, message = "Unable to delete!!!" });
                _work.Company.Remove(get);
                _work.Save();
                return Json(new { success = true, message = "Deleted!!" });
            }
            #endregion

            public async Task<IActionResult> Upsert(int? id)
            {
                Company company = new Company();
                if (id == null) return View(company);
                company = _work.Company.Get(id.GetValueOrDefault());
                if (company == null) return View(company);
                return View(company);

            }
            [HttpPost]
            public async Task<IActionResult> Upsert(Company company)
            {
                if (company == null) return BadRequest();
                if (ModelState.IsValid)
                {
                    if (company.Id == 0)
                    {
                        _work.Company.Add(company);

                    }
                    else
                    {
                        _work.Company.Update(company);

                    }
                    _work.Save();

                }
                return RedirectToAction("Index");

            }

        }
    }


