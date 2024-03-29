using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSLCommerz.Web.PaymentGateway;
using Stripe;
using Stripe.Checkout;
using Stripe.Issuing;
using System.Collections.Specialized;
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
        //this is sslcommerze;
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

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
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

            foreach (var cart in shoppingCartVM.ShoppingCartList)
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

            //start here ssl ecommerz
            var productName = "HP Pavilion Series Laptop";
            var price = shoppingCartVM.OrderHeader.OrderTotal;

            var baseUrl = Request.Scheme + "://" + Request.Host;

            // CREATING LIST OF POST DATA
            NameValueCollection PostData = new NameValueCollection();
            //SuccessUrl = domain + $"cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                   // CancelUrl = domain + "cart/Index",
           
            var domain = "http://localhost:5192/";
            PostData.Add("total_amount", $"{price}");
            PostData.Add("tran_id", "TESTASPNET1234");
            PostData.Add("success_url", domain + $"cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}");
            PostData.Add("fail_url", baseUrl + "/Cart/CheckoutFail");
            PostData.Add("cancel_url", baseUrl + "/Cart/CheckoutCancel");

            PostData.Add("version", "3.00");
            PostData.Add("cus_name", "ABC XY");
            PostData.Add("cus_email", "abc.xyz@mail.co");
            PostData.Add("cus_add1", "Address Line On");
            PostData.Add("cus_add2", "Address Line Tw");
            PostData.Add("cus_city", "City Nam");
            PostData.Add("cus_state", "State Nam");
            PostData.Add("cus_postcode", "Post Cod");
            PostData.Add("cus_country", "Countr");
            PostData.Add("cus_phone", "0111111111");
            PostData.Add("cus_fax", "0171111111");
            PostData.Add("ship_name", "ABC XY");
            PostData.Add("ship_add1", "Address Line On");
            PostData.Add("ship_add2", "Address Line Tw");
            PostData.Add("ship_city", "City Nam");
            PostData.Add("ship_state", "State Nam");
            PostData.Add("ship_postcode", "Post Cod");
            PostData.Add("ship_country", "Countr");
            PostData.Add("value_a", "ref00");
            PostData.Add("value_b", "ref00");
            PostData.Add("value_c", "ref00");
            PostData.Add("value_d", "ref00");
            PostData.Add("shipping_method", "NO");
            PostData.Add("num_of_item", "1");
            PostData.Add("product_name", $"{productName}");
            PostData.Add("product_profile", "general");
            PostData.Add("product_category", "Demo");

            //we can get from email notificaton
            var storeId = "ghurt6603c31a9fec4";
            var storePassword = "ghurt6603c31a9fec4@ssl";
            var isSandboxMood = true;

            SSLCommerzGatewayProcessor sslcz = new SSLCommerzGatewayProcessor(storeId, storePassword, isSandboxMood);

            string response = sslcz.InitiateTransaction(PostData);

            return Redirect(response);
        }

        //this is stripe payment getway wrok

        /*public IActionResult SummaryPost(ShoppingCartVM shoppingCartVM)
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
		}*/

        public IActionResult OrderConfirmation(int id, string sessionId, string paymentIntentId)
        {
            OrderHeader orderHeader = _shoppingCartRepository.GetOrderHeader(id);

            if (!(!String.IsNullOrEmpty(Request.Form["status"]) && Request.Form["status"] == "VALID"))
            {
                ViewBag.SuccessInfo = "There some error while processing your payment. Please try again.";
                return View();
            }


            string TrxID = Request.Form["tran_id"];
            // AMOUNT and Currency FROM DB FOR THIS TRANSACTION
            string amount = orderHeader.OrderTotal.ToString();
            string currency = "BDT";

            var storeId = "ghurt6603c31a9fec4";
            var storePassword = "ghurt6603c31a9fec4@ssl";

            SSLCommerzGatewayProcessor sslcz = new SSLCommerzGatewayProcessor(storeId, storePassword, true);
            var resonse = sslcz.OrderValidate(TrxID, amount, currency, Request);
            var successInfo = $"Validation Response: {resonse}";
            ViewBag.SuccessInfo = successInfo;

            return View(id);

            /*if (orderHeader.PaymentStatus!=SD.PaymentStatusDelayedPayment)
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
            return View(id);*/
        }


        //start for sslcommerze

        public IActionResult CheckoutConfirmation()
        {
            if (!(!String.IsNullOrEmpty(Request.Form["status"]) && Request.Form["status"] == "VALID"))
            {
                ViewBag.SuccessInfo = "There some error while processing your payment. Please try again.";
                return View();
            }

            string TrxID = Request.Form["tran_id"];
            // AMOUNT and Currency FROM DB FOR THIS TRANSACTION
            string amount = "85000";
            string currency = "BDT";

            var storeId = "rejgsggsgsgsgsgeabc28c1c8";
            var storePassword = "rfgsgejagsggsgsgsgsg8c1c8@ssl";

            SSLCommerzGatewayProcessor sslcz = new SSLCommerzGatewayProcessor(storeId, storePassword, true);
            var resonse = sslcz.OrderValidate(TrxID, amount, currency, Request);
            var successInfo = $"Validation Response: {resonse}";
            ViewBag.SuccessInfo = successInfo;

            return View();
        }
        public IActionResult CheckoutFail()
        {
            ViewBag.FailInfo = "There some error while processing your payment. Please try again.";
            return View();
        }
        public IActionResult CheckoutCancel()
        {
            ViewBag.CancelInfo = "Your payment has been cancel";
            return View();
        }

        //end
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
