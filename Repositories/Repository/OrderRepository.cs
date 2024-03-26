using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Awsome.Repositories.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderRepository(ApplicationDbContext db)
        {
            _db = db;

        }

        public List<OrderHeader> GetAllOrderHeader()
        {
            return _db.OrderHeaders
               .Include(p => p.ApplicationUser) // Include the Category navigation property
                .OrderByDescending(p => p.Id)
               .ToList();
        }

        public IEnumerable<OrderHeader> GetJustcustomer(string userId)
        {
            return _db.OrderHeaders
                 .Include(p => p.ApplicationUser)
                 .Where(p => p.ApplicationUserId == userId)
                 .ToList();
        }

        public IEnumerable<OrderDetail> GetOrderDetails(int orderId)
        {
            return _db.OrderDetails
                .Include(p=>p.Product)
                .OrderByDescending(p => p.OrderHeaderId == orderId)
                .ToList();
        }

        public OrderHeader GetOrderHeader(int orderId)
        {
            return _db.OrderHeaders
                .Include(p => p.ApplicationUser)
                .FirstOrDefault(p => p.Id == orderId);
        }

        public void UpdateOrderHeader(OrderHeader orderHeaderFromDb)
        {
            _db.OrderHeaders.Update(orderHeaderFromDb);
            _db.SaveChanges();
        }
    }
}
