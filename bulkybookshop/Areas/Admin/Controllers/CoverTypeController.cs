using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace bulkybookshop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitofwork _unitofwork;

        public CoverTypeController(IUnitofwork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverTypeList = _unitofwork.CoverType.GetAll();
            return View(objCoverTypeList);
        }
        //GET
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
           if (ModelState.IsValid)
            {
                _unitofwork.CoverType.Add(obj);
                _unitofwork.Save();
                TempData["success"] = "CoverType created Successfuly";
                return RedirectToAction("Index");
            }
            return View(obj);
        }
        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var CoverTypeFromDb = _unitofwork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (CoverTypeFromDb == null)
            {
                return NotFound();
            }

            return View(CoverTypeFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
           if (ModelState.IsValid)
            {
                _unitofwork.CoverType.Update(obj);
                _unitofwork.Save();
                TempData["success"] = "CoverType updated successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var CoverTypeFromDb = _unitofwork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (CoverTypeFromDb == null)
            {
                return NotFound();
            }

            return View(CoverTypeFromDb);
        }
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]  
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitofwork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _unitofwork.CoverType.Remove(obj);
            _unitofwork.Save();
            TempData["success"] = "CoverTye deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
