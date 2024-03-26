using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Awsome.Repositories.Repository
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly ApplicationDbContext _db;
        public ShoppingCartRepository(ApplicationDbContext db)
        {
            _db = db;

        }
        public bool AddCart(ShoppingCart shoppingCart)
        {
            _db.ShoppingCarts.Add(shoppingCart);
            _db.SaveChanges();
            return true;
        }

        public List<ShoppingCart> GetAllCart(string userId)
        {
            return _db.ShoppingCarts
                 .Include(p => p.Product)
                 .Where(p => p.ApplicationUserId == userId)
                 .ToList();
        }

        public ApplicationUser GetApplicationUser(string userId)
        {
            return _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
        }

        public bool GetCart(int cartId)
        {
           var cart = _db.ShoppingCarts.FirstOrDefault(p => p.Id == cartId);
            cart.Count += 1;
            _db.ShoppingCarts.Update(cart);
            _db.SaveChanges();
            return true;
        }

		public OrderHeader GetOrderHeader(int id)
		{
            return _db.OrderHeaders
                .Include(p => p.ApplicationUser)
                .Where(p => p.Id == id)
                .FirstOrDefault();
		}

		public bool MinusCart(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(p => p.Id == cartId);
            if(cart.Count<=1)
            {
                _db.ShoppingCarts.Remove(cart);
                _db.SaveChanges();
                return true;
            }
            else
            {
                cart.Count -= 1;
                _db.ShoppingCarts.Update(cart);
                _db.SaveChanges();
                return false;
            }
            
        }

		public void RemoveAllCart(string applicationUserId)
		{
			List<ShoppingCart> shoppingCarts = _db.ShoppingCarts.Where(u=>u.ApplicationUserId== applicationUserId).ToList();
            _db.ShoppingCarts.RemoveRange(shoppingCarts);
			_db.SaveChanges();
		}

		public bool RemoveCart(int cartId)
        {
            var cart = _db.ShoppingCarts.FirstOrDefault(p => p.Id == cartId);
            _db.ShoppingCarts.Remove(cart);
            _db.SaveChanges();
            return true;
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if(orderFromDb!=null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if(!string.IsNullOrEmpty(paymentStatus))
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
                _db.OrderHeaders.Update(orderFromDb);
                _db.SaveChanges();
            }
		}

		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if(orderFromDb!=null)
            {
				if (!string.IsNullOrEmpty(sessionId))
				{
					orderFromDb.SessionId = sessionId;
				}
				if (!string.IsNullOrEmpty(paymentIntentId))
				{
					orderFromDb.PaymentIntentId = paymentIntentId;
					orderFromDb.PaymentDate = DateTime.Now;
				}
				_db.OrderHeaders.Update(orderFromDb);
				_db.SaveChanges();
			}
            
		}
	}
}
