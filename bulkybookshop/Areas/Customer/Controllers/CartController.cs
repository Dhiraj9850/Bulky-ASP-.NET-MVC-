using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Models.ViewModels;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace bulkybookshop.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitofwork _unitofwork;
        [BindProperty]
       public ShoppingCartVM ShoppingCartVM { get; set; }
       public CartController(IUnitofwork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new()
            };
			foreach (var cart in ShoppingCartVM.ListCart)
			{
				cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
			}

			return View(ShoppingCartVM); 
        }
        //Summary-GET
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitofwork.ShoppingCart.GetAll(u=>u.ApplicationUserId == claim.Value,includeProperties:"Product"),
                OrderHeader = new() 
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitofwork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


			foreach (var cart in ShoppingCartVM.ListCart)
			{
				cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
			}
			return View(ShoppingCartVM);  
        }

        [ActionName("Summary")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM.ListCart = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach(var cart in ShoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal  += (cart.Count * cart.Price);
            }
            ApplicationUser applicationUser = _unitofwork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }


            _unitofwork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitofwork.Save();

            foreach(var cart in ShoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId, 
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count, 
                };
				_unitofwork.OrderDetail.Add(orderDetail);
				_unitofwork.Save();
            }

            if(applicationUser.CompanyId.GetValueOrDefault()==0)
            {
				// stripe settings
				var domain = "https://localhost:44394/";
				var options = new SessionCreateOptions
				{
					PaymentMethodTypes = new List<string>
				{
					"card",
				},
					LineItems = new List<SessionLineItemOptions>()
					 ,
					Mode = "payment",
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "$customer/cart/index",
				};

				foreach (var item in ShoppingCartVM.ListCart)
				{

					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title,
							},
						},
						Quantity = item.Count,
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);
				_unitofwork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitofwork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
            else
            {
                return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id});
            }

	}

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitofwork.OrderHeader.UpdateStripePaymentID(id, orderHeader.SessionId, session.PaymentIntentId);
					_unitofwork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitofwork.Save();
				}
			}
			
            List<ShoppingCart> shoppingCarts = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            HttpContext.Session.Clear();
            _unitofwork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitofwork.Save();
            return View(id);  
        }
        public IActionResult Plus(int cartId)
        {
            var cart = _unitofwork.ShoppingCart.GetFirstOrDefault(u=>u.Id== cartId);
            _unitofwork.ShoppingCart.IncrementCount(cart,1);
            _unitofwork.Save();
            return RedirectToAction("Index");
        }

		public IActionResult Minus(int cartId)
		{
			var cart = _unitofwork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            if(cart.Count <= 1)
            {
                _unitofwork.ShoppingCart.Remove(cart);
                var count =  _unitofwork.ShoppingCart.GetAll(u=>u.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
     			 _unitofwork.ShoppingCart.DecrementCount(cart, 1);
            }
			_unitofwork.Save();
			return RedirectToAction("Index");
		}

		public IActionResult Remove(int cartId)
		{
			var cart = _unitofwork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
			_unitofwork.ShoppingCart.Remove(cart);
			_unitofwork.Save();
            var count = _unitofwork.ShoppingCart.GetAll(u=>u.ApplicationUserId== cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart,count);
			return RedirectToAction("Index");
		}

		private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if(quantity <= 50)
            {
                return price;
            }
            else
            {
                if(quantity <= 100)
                {
                    return price50;
                }
                return price100;   
            }
        }
    }
}
