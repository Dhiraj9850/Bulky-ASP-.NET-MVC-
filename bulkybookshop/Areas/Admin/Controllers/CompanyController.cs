using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bulkybookshop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitofwork _unitofwork;
        public CompanyController(IUnitofwork unitofwork)
        {
            _unitofwork = unitofwork;
        }
    
        public IActionResult Index()
        {
            IEnumerable<Company> CompanyList = _unitofwork.Company.GetAll();
            return View(CompanyList);   
        }
        //Upsert-GET
        public IActionResult Upsert(int? id)
        {
            Company company = new();
            if(id == null || id == 0)
            { 
              return View(company);  
            }
            else
            {
                company = _unitofwork.Company.GetFirstOrDefault (u => u.Id == id);
                return View(company);   
            }
        }

        //Upsert-POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj)
        {
           if(ModelState.IsValid)
            { 
               if(obj.Id == 0)
                {
                    _unitofwork.Company.Add(obj);
                    TempData["success"] = "Company Added Successfully";
                }
                else
                {
                    _unitofwork.Company.Update(obj);
                    TempData["success"] = "Company Updated Successfully";
                }
                _unitofwork.Save();
                return RedirectToAction("Index");   
            }
            return View(obj);
        }

        #region API CALLS
        public IActionResult GetAll()
        {
            var CompanyList = _unitofwork.Company.GetAll();
            return Json(new { data = CompanyList });
        }
        public IActionResult Delete(int? id)
        {
            var obj = _unitofwork.Company.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "error while deleting" });
            }
            _unitofwork.Company.Remove(obj);
            _unitofwork.Save();
            return Json(new { success = true, message = "deleted" });
        }
        #endregion
    }
}
