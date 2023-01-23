using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace bulkybookshop.ViewComponents
{
    public class ShoppingCartViewComponent:ViewComponent
    {
        private readonly IUnitofwork _unitofwork;

        public ShoppingCartViewComponent(IUnitofwork unitofwork)
        {
            _unitofwork = unitofwork;   
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);    

            if (claim != null) 
            {
                if(HttpContext.Session.GetInt32(SD.SessionCart)!=null)
                {
                    return View(HttpContext.Session.GetInt32(SD.SessionCart)); 
                }
                else
                {
                    HttpContext.Session.SetInt32(SD.SessionCart,
                        _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
