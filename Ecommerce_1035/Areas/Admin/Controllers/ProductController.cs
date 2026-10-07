using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Models.Models.ViewModel;
using Ecommerce_1035.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce_1035.Areas.Admin.Controllers
{
    [Area("Admin")]

    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductController : Controller
    {
        private readonly IUnitofWork _work;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitofWork work, IWebHostEnvironment web)
        {
            _work = work;
            _webHostEnvironment = web;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new Product(),
                CategoryList = _work.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                CoverTypeList = _work.CoverTypes.GetAll().Select(ct => new SelectListItem
                {
                    Text = ct.Name,
                    Value = ct.Id.ToString()
                })
            };

            if (id == null || id == 0)
            {
                return View(productVM);
            }

            productVM.Product = _work.Product.Get(id.Value);
            if (productVM.Product == null)
            {
                return NotFound();
            }

            return View(productVM);
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM)
        {
            if(ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if(files.Count > 0)
                {
                    var filename = Guid.NewGuid().ToString();
                    var extension = Path.GetExtension(files[0].FileName);
                    var uploads = Path.Combine(webRootPath, @"images\Product");
                    if(productVM.Product.Id != 0)
                    {
                        var imageExists = _work.Product.Get(productVM.Product.Id).ImageUrl;
                        productVM.Product.ImageUrl = imageExists;
                    }
                    if(productVM.Product.ImageUrl != null)
                    {
                        var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.Trim('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(uploads, filename + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }
                    productVM.Product.ImageUrl = @"\images\Product\" + filename + extension;
                }
                else
                {
                    if(productVM.Product.Id != 0)
                    {
                         var imageExists = _work.Product.Get(productVM.Product.Id).ImageUrl;
                         productVM.Product.ImageUrl = imageExists;
                    }
                }
                if(productVM.Product.Id == 0)
                {
                    _work.Product.Add(productVM.Product);
                }
                else
                {
                    _work.Product.Update(productVM.Product);
                }
                _work.Save();
                return RedirectToAction("Index");
            }
            else
            {
                productVM = new ProductVM()
                {
                    Product = new Product(),
                    CategoryList = _work.Category.GetAll().Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    }),
                    CoverTypeList = _work.CoverTypes.GetAll().Select(ct => new SelectListItem
                    {
                        Text = ct.Name,
                        Value = ct.Id.ToString()
                    })
                };
                if(productVM.Product.Id != 0)
                {
                    productVM.Product = _work.Product.Get(productVM.Product.Id);
                }
                return RedirectToAction("Index");
            }

        }
      
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var objList = _work.Product.GetAll();
            return Json(new { data = objList });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _work.Product.Get(id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting: Product not found." });
            }

            if (!string.IsNullOrEmpty(objFromDb.ImageUrl))
            {
                var wwwRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(wwwRootPath, objFromDb.ImageUrl.TrimStart('\\', '/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _work.Product.Remove(objFromDb);
            _work.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
