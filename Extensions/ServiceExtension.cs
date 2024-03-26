using Awsome.Repositories.Interface;
using Awsome.Repositories.Repository;

namespace Awsome.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection APIModuleServices(this IServiceCollection services)
        {
            try
            {
                //services.AddScoped<IAccountRepository, AccountRepository>();
                services.AddScoped<IProductRepository, ProductRepository>();
                services.AddScoped<ICompanyRepository, CompanyRepository>();
                services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
                services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
                services.AddScoped<IOrderHeaderRepository, OrderHeaderRepository>();
                services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
                services.AddScoped<IOrderRepository, OrderRepository>();
                return services;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
