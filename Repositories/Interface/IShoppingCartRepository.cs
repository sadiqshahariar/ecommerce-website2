using Awsome.Models;

namespace Awsome.Repositories.Interface
{
    public interface IShoppingCartRepository
    {
        bool AddCart(ShoppingCart shoppingCart);
        List<ShoppingCart> GetAllCart(string userId);
        ApplicationUser GetApplicationUser(string userId);
        bool GetCart(int cartId);
		OrderHeader GetOrderHeader(int id);
		bool MinusCart(int cartId);
		void RemoveAllCart(string applicationUserId);
		bool RemoveCart(int cartId);
        void UpdateStatus(int id, string orderStatus, string? paymentStatus = null);
        void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId);
    }
}
