using BookSelling.DataAccess.Data;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookSelling.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product obj)
        {
            var objFromDB = _db.Products.FirstOrDefault(u => u.Id == obj.Id);
            if (objFromDB != null)
            {
                objFromDB.Title = obj.Title;
                objFromDB.ISBN = obj.ISBN;
                objFromDB.Price = obj.Price;
                objFromDB.Price50 = obj.Price50;
                objFromDB.ListPrice = obj.ListPrice;
                objFromDB.Price100 = obj.Price100;
                objFromDB.Description = obj.Description;
                objFromDB.Category = obj.Category;
                objFromDB.Author = obj.Author;
                objFromDB.ProductImages = obj.ProductImages;
                //if(obj.ImageUrl != null) 
                //{
                //    objFromDB.ImageUrl = obj.ImageUrl;
                //}
            }
        }

        public IEnumerable<Product> GetBestSellingProducts(int topN)
        {
            var bestSellingProducts = _db.OrderDetails
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalCount = g.Sum(od => (int?)od.Count) ?? 0
                })
                .OrderByDescending(g => g.TotalCount)
                .Take(topN)
                .ToList()
                .Select(g => _db.Products.FirstOrDefault(p => p.Id == g.ProductId))
                .Where(p => p != null)
                .ToList();

            return bestSellingProducts;
        }
    }
}
