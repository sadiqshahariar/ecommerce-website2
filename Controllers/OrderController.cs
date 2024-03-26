using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Repositories.Repository;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace Awsome.Controllers
{
	public class OrderController : Controller
	{
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOrderRepository _orderRepository;
        private readonly IShoppingCartRepository _shoppingCartRepository;

        public OrderController(IWebHostEnvironment webHostEnvironment,IOrderRepository orderRepository, IShoppingCartRepository shoppingCartRepository)
        {
            _webHostEnvironment = webHostEnvironment;
            _orderRepository = orderRepository;
            _shoppingCartRepository = shoppingCartRepository;
        }
        [Authorize]
        public IActionResult Index(string? status)
		{
            IEnumerable<OrderHeader> objOrderHeader;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeader = _orderRepository.GetAllOrderHeader();
            }
            else
            {
                var cliamsIdentity = (ClaimsIdentity)User.Identity;
                var userId = cliamsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeader = _orderRepository.GetJustcustomer(userId);
            }
            switch (status)
            {
                case "pending":
                    objOrderHeader = objOrderHeader.Where(u => u.PaymentStatus == SD.PaymentStatusPending);
                    break;
                case "inprocess":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            var orderHeaderList = objOrderHeader.ToList();
            return View(orderHeaderList);
		}
        public IActionResult Details(int orderId)
        {
            OrderVM orderVM = new()
            {
                OrderHeader = _orderRepository.GetOrderHeader(orderId),
                OrderDetails = _orderRepository.GetOrderDetails(orderId)
            };
            return View(orderVM);
        }
        [HttpPost]
        [Authorize]
        public IActionResult UpdateOrderDetails(OrderVM orderVM)
        {
            var orderHeaderFromDb = _orderRepository.GetOrderHeader(orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber= orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress= orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City= orderVM.OrderHeader.City;
            orderHeaderFromDb.State= orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode= orderVM.OrderHeader.PostalCode;
            if(!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if(!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }
            //update
            _orderRepository.UpdateOrderHeader(orderHeaderFromDb);

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});
        }
        [HttpPost]
        [Authorize]
        public IActionResult StartProcessing(OrderVM orderVM)
        {
            var orderHeaderFromDb = _orderRepository.GetOrderHeader(orderVM.OrderHeader.Id);

            orderHeaderFromDb.OrderStatus = SD.StatusInProcess;

            _orderRepository.UpdateOrderHeader(orderHeaderFromDb);

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }
        [HttpPost]
        [Authorize]
        public IActionResult ShipOrder(OrderVM orderVM)
        {
            var orderHeaderFromDb = _orderRepository.GetOrderHeader(orderVM.OrderHeader.Id);

            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            orderHeaderFromDb.TrackingNumber=orderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.ShoppingDate = DateTime.Now;

            if(orderVM.OrderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _orderRepository.UpdateOrderHeader(orderHeaderFromDb);

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }

        [HttpPost]
        [Authorize]
        public IActionResult CancelOrder(OrderVM orderVM)
        {
            var orderHeaderFromDb = _orderRepository.GetOrderHeader(orderVM.OrderHeader.Id);

            //refund when order canceled
            if(orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);

                orderHeaderFromDb.OrderStatus = SD.StatusCancelled;
                orderHeaderFromDb.PaymentStatus = SD.StatusRefunded;
            }
            else
            {
                orderHeaderFromDb.OrderStatus = SD.StatusCancelled;
                orderHeaderFromDb.PaymentStatus = SD.StatusCancelled;
            }
            _orderRepository.UpdateOrderHeader(orderHeaderFromDb);

            TempData["Success"] = "Order Cancelled Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }
        [HttpPost]
        public IActionResult PayNow(OrderVM orderVM)
        {
            orderVM.OrderHeader = _orderRepository.GetOrderHeader(orderVM.OrderHeader.Id);
            orderVM.OrderDetails = _orderRepository.GetOrderDetails(orderVM.OrderHeader.Id);


            var domain = "http://localhost:5192/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"cart/details?orderId={orderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };
            foreach (var item in orderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",

                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            //update

            _shoppingCartRepository.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _shoppingCartRepository.GetOrderHeader(orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _shoppingCartRepository.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _shoppingCartRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                }
            }
            return View(orderHeaderId);
        }

    }
}
