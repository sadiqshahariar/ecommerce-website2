using Awsome.Models;

namespace Awsome.Repositories.Interface
{
    public interface IProductRepository
    {
        List<NewProduct> getProductList();
        bool SaveData(NewProduct product);

        bool SaveUpdate(NewProduct obj);
        bool DeleteData(NewProduct obj);
        List<Category> GetCategory();
        NewProduct Find(int id);

        NewProduct GetProductDetails(int productId);
    }
}
