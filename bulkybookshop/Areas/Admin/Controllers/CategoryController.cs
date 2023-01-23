using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace bulkybookshop.Areas.Admin.Controllers

{
	[Area("Admin")]
	[Authorize(Roles =SD.Role_Admin)]
 	public class CategoryController : Controller
	{
		private readonly IUnitofwork _unitofwork;
		public CategoryController(IUnitofwork unitofwork)
		{
			_unitofwork = unitofwork;
		}

		public IActionResult Index()
		{
			var objCategoryList = _unitofwork.Category.GetAll();
			return View(objCategoryList);
		}
		//Create-GET
		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(Category obj)
		{
			if (obj.Name == obj.DisplayOrder.ToString())
			{
				ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
			}
			if (ModelState.IsValid)
			{
				_unitofwork.Category.Add(obj);
				_unitofwork.Save();
				TempData["success"] = "Category Created Successfully";
				return RedirectToAction("Index","Category");
			}
			return View(obj);
		}

		//GET
		public IActionResult Edit(int? id) 
		{
			if(id==0 || id == null)
			{
				return NotFound();
			}
			var CategoryFromdb = _unitofwork.Category.GetFirstOrDefault(u => u.Id == id);
			if (CategoryFromdb == null)
			{
				return NotFound();
			}
			return View(CategoryFromdb);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(Category obj)
		{
			if(obj.Name == obj.DisplayOrder.ToString())
			{
				ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
			}
			if(ModelState.IsValid)
			{
				_unitofwork.Category.Update(obj);
				_unitofwork.Save();
				TempData["success"] = "Category Updated Successfully";
				return RedirectToAction("Index");
			}
			return View(obj);
		}

		//Delete-GET
		public IActionResult Delete(int? id)
		{
			if (id == 0 || id == null)
			{
				return NotFound();
			}
			var CategoryFromdb = _unitofwork.Category.GetFirstOrDefault(u => u.Id == id);
			if (CategoryFromdb == null)
			{
				return NotFound();
			}
			return View(CategoryFromdb);
		}

		[HttpPost]
		[ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public IActionResult DeletePOST(int? id)
		{
			var obj = _unitofwork.Category.GetFirstOrDefault(u => u.Id == id);
			if (obj == null)
			{
				 return NotFound();	
			}
			_unitofwork.Category.Remove(obj);
			_unitofwork.Save();
			TempData["success"] = "Category deleted Successfully";
			return RedirectToAction("Index");
		}

	}
}
