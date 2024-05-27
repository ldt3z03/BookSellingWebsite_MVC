using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSelling.Models.ViewModels
{
    public class ProductSalesVM
    {
        public Product Product { get; set; }
        public int OrderCount { get; set; }
        public double TotalSales { get; set; }
    }

    public class BestSellingProductsVM
    {
        public List<ProductSalesVM> BestSellingProducts { get; set; }
    }
}
