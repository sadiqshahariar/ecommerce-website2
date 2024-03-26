using Awsome.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Awsome.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { } 
        public DbSet<Category>Categories { get; set; }
        public DbSet<NewProduct> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }


        public DbSet<ApplicationUser>ApplicationUsers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1,Name="Burger", DisplayOrder=1},
                new Category { Id = 2, Name = "Pizza", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Banana", DisplayOrder = 5 }
                );

            modelBuilder.Entity<NewProduct>().HasData(
                new NewProduct
                {
                    Id = 1,
                    Title = "Title",
                    Author = "Author",
                    Description = "Description",
                    ISBN = "ISBN",
                    ListPrice=100,
                    Price=300,
                    Price50=200,
                    Price100=100,
                    CategoryId=1,
                    ImageUrl=""
                }
                );


        }
    }
}  
