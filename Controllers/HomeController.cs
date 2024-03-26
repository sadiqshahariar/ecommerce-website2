using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Security.Claims;

namespace Awsome.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger,IProductRepository productRepository, IShoppingCartRepository shoppingCartRepository,ApplicationDbContext db)
        {
            _logger = logger;
            _productRepository = productRepository;
            _shoppingCartRepository = shoppingCartRepository;
            _db = db;
        }

        public IActionResult Index()
        {
            List<NewProduct> objProductList = _productRepository.getProductList();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var cliam = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(cliam != null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart)==null)
                {
                    HttpContext.Session.SetInt32(SD.SessionCart,
                   _db.ShoppingCarts.Where(u => u.ApplicationUserId == cliam.Value).ToList().Count());
                }
            }

            return View(objProductList);
        }
        public IActionResult Details(int productId)
        {
            //List<NewProduct> objProductList = _productRepository.getProductList();

            ShoppingCart cart = new ShoppingCart()
            {
                Product = _productRepository.GetProductDetails(productId),
                Count = 1,
                ProductId = productId
            };
            //NewProduct objProduct = _productRepository.GetProductDetails(productId);
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Create an instance of ApplicationUser and assign the userId
            shoppingCart.ApplicationUserId = userId;
            ShoppingCart? cartFromDb = _db.ShoppingCarts
              .FirstOrDefault(cart => cart.ApplicationUserId == userId && cart.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                cartFromDb.Count += shoppingCart.Count;
                _db.ShoppingCarts.Update(cartFromDb);
                _db.SaveChanges();
            }
            else
            {
                bool ans = _shoppingCartRepository.AddCart(shoppingCart);
                HttpContext.Session.SetInt32(SD.SessionCart, 
                    _db.ShoppingCarts.Where(u => u.ApplicationUserId == userId).ToList().Count());
            }
           

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
