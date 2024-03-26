using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Issuing;
using System.Security.Claims;

namespace Awsome.Controllers
{
    public class CartController : Controller
    {
        private readonly IShoppingCartRepository _shoppingCartRepository;
		private ApplicationDbContext _db;
		public CartController(IShoppingCartRepository shoppingCartRepository,ApplicationDbContext db)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _db = db;
        }
        [Authorize]
        public IActionResult Index()
        {
     
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            List<ShoppingCart> shoppingCartList = _shoppingCartRepository.GetAllCart(userId);
            ShoppingCartVM shoppingCartVM = new()
            {
                ShoppingCartList = shoppingCartList,
                OrderHeader = new()

            };

            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart); 
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(shoppingCartVM);
        }

        public IActionResult Plus(int cartId)
        {
            bool ans  = _shoppingCartRepository.GetCart(cartId);
            return RedirectToAction(nameof(Index));
            

        }
        public IActionResult Minus(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(p => p.Id == cartId);
            if (cart.Count <= 1)
            {
                _db.ShoppingCarts.Remove(cart);
                HttpContext.Session.SetInt32(SD.SessionCart,
                  _db.ShoppingCarts.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count()-1);
                _db.SaveChanges();
            }
            else
            {
                cart.Count -= 1;
                _db.ShoppingCarts.Update(cart);
                _db.SaveChanges();
            }
           // bool ans = _shoppingCartRepository.MinusCart(cartId);
           
            return RedirectToAction(nameof(Index));


        }
        public IActionResult Remove(int cartId)
        {

            var cart = _db.ShoppingCarts.FirstOrDefault(p => p.Id == cartId);
            _db.ShoppingCarts.Remove(cart);

            HttpContext.Session.SetInt32(SD.SessionCart,
                  _db.ShoppingCarts.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count() - 1);
           
            _db.SaveChanges();
           // bool ans = _shoppingCartRepository.RemoveCart(cartId);
            return RedirectToAction(nameof(Index));


        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            List<ShoppingCart> shoppingCartList = _shoppingCartRepository.GetAllCart(userId);

            ShoppingCartVM shoppingCartVM = new()
            {
                ShoppingCartList = shoppingCartList,
                OrderHeader = new()

            };

            shoppingCartVM.OrderHeader.ApplicationUser = _shoppingCartRepository.GetApplicationUser(userId);

            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(shoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPost(ShoppingCartVM shoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			List<ShoppingCart> shoppingCartList = _shoppingCartRepository.GetAllCart(userId);

            shoppingCartVM.ShoppingCartList = shoppingCartList;
            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _shoppingCartRepository.GetApplicationUser(userId);

			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if(applicationUser.CompanyId.GetValueOrDefault()==0)
            {
                //regular customer
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //company
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _db.OrderHeaders.Add(shoppingCartVM.OrderHeader);
            _db.SaveChanges();

            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _db.OrderDetails.Add(orderDetail);
                _db.SaveChanges();
            }

           // ekhane problem
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
                //regular customer
                var domain = "http://localhost:5192/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain+$"cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain+"cart/Index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};
                foreach(var item in shoppingCartVM.ShoppingCartList)
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

                _shoppingCartRepository.UpdateStripePaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);

                //end
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
				//redirectforcustomer
			}

            //forcompany
			return RedirectToAction(nameof(OrderConfirmation), new {id=shoppingCartVM.OrderHeader.Id});
		}

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _shoppingCartRepository.GetOrderHeader(id);
            if(orderHeader.PaymentStatus!=SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _shoppingCartRepository.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _shoppingCartRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                }
            }
            _shoppingCartRepository.RemoveAllCart(orderHeader.ApplicationUserId);
            HttpContext.Session.SetInt32(SD.SessionCart,
                 0);
            return View(id);
        }


		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count<=50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else return shoppingCart.Product.Price100;
            }
        }
    }
}
