using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Models.ViewModels;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;

namespace bulkybookshop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitofwork _unitofwork;
        private readonly IWebHostEnvironment _hostEnvironment;
        public ProductController(IUnitofwork unitofwork, IWebHostEnvironment hostEnvironment)
        {
            _unitofwork = unitofwork;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new(),
                CategoryList = _unitofwork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitofwork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            if (id == 0 || id == null)
            {
                //create
                return View(productVM);
            }
            else
            {
                //GET-update
                productVM.Product = _unitofwork.Product.GetFirstOrDefault(i => i.Id == id);
                return View(productVM);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"images\products");
                    var extension = Path.GetExtension(file.FileName);


                    if (obj.Product.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);
                    }
                    obj.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }
                if (obj.Product.Id == 0)
                {
                    _unitofwork.Product.Add(obj.Product);
                    TempData["success"] = "Product created successfully";
                }
                else
                {
                    _unitofwork.Product.Update(obj.Product);
                    TempData["success"] = "Product updated successfully";
                }
                _unitofwork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitofwork.Product.GetAll(includeProperties: "Category,CoverType");
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitofwork.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitofwork.Product.Remove(obj);
            _unitofwork.Save();
            return Json(new { success = true, message = "Delete successful" });
        }
        #endregion
    }
}
