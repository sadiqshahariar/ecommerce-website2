using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Awsome.Repositories.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db)
        {
            _db = db;
           
        }

        public bool DeleteData(NewProduct obj)
        {
           _db.Products.Remove(obj);
            _db.SaveChanges();
            return true;
        }

        public NewProduct Find(int id)
        {
              return  _db.Products
               .Include(p => p.Category)
               .Where(p => p.Id == id)
               .Select(p => new NewProduct
               {
                   Id = p.Id,
                   Title = p.Title,
                   Description = p.Description,
                   ISBN = p.ISBN,
                   Author = p.Author,
                   ListPrice = p.ListPrice,
                   Price = p.Price,
                   Price50 = p.Price50,
                   Price100 = p.Price100,
                   CategoryId = p.CategoryId,
                   Category = p.Category,
                   ImageUrl = p.ImageUrl
               })
               .FirstOrDefault();

        }

        public NewProduct GetProductDetails(int productId)
        {
            return _db.Products
             .Include(p => p.Category)
             .Where(p => p.Id == productId)
             .Select(p => new NewProduct
             {
                 Id = p.Id,
                 Title = p.Title,
                 Description = p.Description,
                 ISBN = p.ISBN,
                 Author = p.Author,
                 ListPrice = p.ListPrice,
                 Price = p.Price,
                 Price50 = p.Price50,
                 Price100 = p.Price100,
                 CategoryId = p.CategoryId,
                 Category = p.Category,
                 ImageUrl = p.ImageUrl
             })
             .FirstOrDefault();

        }

        public List<Category> GetCategory()
        {
            return _db.Categories.ToList();
        }

        public List<NewProduct> getProductList()
        {
            return _db.Products
               .Include(p => p.Category) // Include the Category navigation property
                .OrderByDescending(p => p.Id)
               .Select(p => new NewProduct
               {
                   Id = p.Id,
                   Title = p.Title,
                   Description = p.Description,
                   ISBN = p.ISBN,
                   Author = p.Author,
                   ListPrice = p.ListPrice,
                   Price = p.Price,
                   Price50 = p.Price50,
                   Price100 = p.Price100,
                   CategoryId = p.CategoryId,
                   Category = p.Category, // Assign the Category navigation property
                   ImageUrl = p.ImageUrl
               })
               .ToList();
        }

        public bool SaveData(NewProduct product)
        {
           _db.Products.Add(product);
            _db.SaveChanges();
            return true;
        }

        public bool SaveUpdate(NewProduct obj)
        {
            _db.Products.Update(obj);
            _db.SaveChanges();
            return true;
        }
    }
}
