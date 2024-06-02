using BookSelling.DataAccess.Data;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using Microsoft.EntityFrameworkCore;
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
        private readonly DbSet<Product> _dbSet;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
            _dbSet = db.Set<Product>();
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
                objFromDB.FlashSalePrice = obj.FlashSalePrice;
                objFromDB.IsFlashSale = obj.IsFlashSale;
                objFromDB.FlashSaleStart = obj.FlashSaleStart;
                objFromDB.FlashSaleEnd = obj.FlashSaleEnd;
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
        public IEnumerable<Product> GetFlashSaleProducts()
        {
            return _db.Products.Where(p => p.IsFlashSale && p.FlashSaleEnd > DateTime.Now).ToList();
        }
        // Trong ProductRepository
        public IEnumerable<Product> Where(Expression<Func<Product, bool>> filter = null, Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy = null, string includeProperties = null)
        {
            IQueryable<Product> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Xử lý các thuộc tính bao gồm (include properties) nếu có
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            // Sắp xếp dữ liệu nếu có
            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }

            return query.ToList();
        }
    }
}
