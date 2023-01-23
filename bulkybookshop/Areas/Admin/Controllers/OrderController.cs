using bulkybookshop.DataAccess.Repository;
using bulkybookshop.DataAccess.Repository.IRepository;
using bulkybookshop.Models;
using bulkybookshop.Models.ViewModels;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;

namespace bulkybookshop.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitofwork _unitofwork;
		[BindProperty]
		public OrderVM OrderVM { get; set; }
		public OrderController(IUnitofwork unitofwork)
		{
			_unitofwork = unitofwork;
		}
	
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Details(int orderId)
		{
			OrderVM = new OrderVM()
			{
				OrderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = _unitofwork.OrderDetail.GetAll(u=>u.OrderId==orderId,includeProperties:"Product"),
			};
			return View(OrderVM);
		}


		public IActionResult PaymentConfirmation(int orderHeaderId)
		{
			OrderHeader orderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitofwork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitofwork.Save();
				}
			}

			return View(orderHeaderId);
		}
		[ActionName("Details")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Details_PAY_NOW()
		{
			OrderVM.OrderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
			OrderVM.OrderDetail = _unitofwork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id,includeProperties:"Product");

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
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
				CancelUrl = domain + "$admin/order/details?orderId={OrderVM.OrderHeader.Id}",
			};

			foreach (var item in OrderVM.OrderDetail)
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
			_unitofwork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitofwork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
			
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateOrderDetail()
		{
			var orderHeaderFromDb = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id,tracked:false);
			orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
			if(orderHeaderFromDb.TrackingNumber != null)
			{
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}
			if (orderHeaderFromDb.Carrier != null)
			{
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
			}
			_unitofwork.OrderHeader.Update(orderHeaderFromDb);
			_unitofwork.Save();
			TempData["success"] = "Order Details updated successfully";
			return RedirectToAction("Details", "Order", new {orderId = orderHeaderFromDb.Id});
		}

		//Order Management 
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult StartProcessing()
		{
			_unitofwork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
			_unitofwork.Save();
			TempData["success"] = "order is in Process";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}


		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult ShipOrder()
		{
			var orderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u=>u.Id==OrderVM.OrderHeader.Id,tracked:false);
			orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.OrderDate = DateTime.Now;
			_unitofwork.OrderHeader.Update(orderHeader);
			_unitofwork.Save();
			TempData["success"] = "Order shipped Successfully";
			return RedirectToAction("Details", "Order", new {orderId = OrderVM.OrderHeader.Id});
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult CancelOrder()
		{
			var orderHeader = _unitofwork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};

				var service = new RefundService();
				Refund refund = service.Create(options);

				_unitofwork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
				_unitofwork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}
			_unitofwork.Save();

			TempData["Success"] = "Order Cancelled Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;

			if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
			  orderHeaders = _unitofwork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
			}
			else
			{
				var claimsIdentiy = (ClaimsIdentity)User.Identity;
				var claim = claimsIdentiy.FindFirst(ClaimTypes.NameIdentifier);

				orderHeaders = _unitofwork.OrderHeader.GetAll(u=>u.ApplicationUserId==claim.Value,includeProperties:"ApplicationUser");
			}

            switch (status)
            {
                case "pending":
					orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;
                case "inprocess":
					orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
					break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusApproved)	;
                    break;
                default:
                   break;
            }

            return Json(new { data = orderHeaders });
		}
		
		 
		#endregion

	}
}
