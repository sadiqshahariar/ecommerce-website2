using Awsome.Models;

namespace Awsome.Repositories.Interface
{
    public interface IOrderRepository
    {
        List<OrderHeader> GetAllOrderHeader();
        IEnumerable<OrderHeader> GetJustcustomer(string userId);
        IEnumerable<OrderDetail> GetOrderDetails(int orderId);
        OrderHeader GetOrderHeader(int orderId);
        void UpdateOrderHeader(OrderHeader orderHeaderFromDb);
    }
}
