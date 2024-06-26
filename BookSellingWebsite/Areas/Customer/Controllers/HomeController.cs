﻿using BookSelling.DataAccess.Data;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using BookSelling.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using X.PagedList;

namespace BookSellingWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private ApplicationDbContext _db;
        //Sử dụng @HttpContextAccessor.HttpContext.Request.Query để lấy các giá trị của tham số hiện tại từ URL và truyền chúng vào liên kết của các nút lọc giá
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ApplicationDbContext db,IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index(string category, string searchString, string sortOrder, decimal? minPrice, decimal? maxPrice, string author ,int? page)
        {
            ViewBag.PriceSortParm = string.IsNullOrEmpty(sortOrder) ? "price_desc" : "";
            ViewBag.CurrentSort = sortOrder;

            IEnumerable<Product> productList;

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = _unitOfWork.Product.GetAll(p => p.Title.Contains(searchString), includeProperties: "Category,ProductImages");
            }
            else if (!string.IsNullOrEmpty(category))
            {
                productList = _unitOfWork.Product.GetAll(p => p.Category.Name == category, includeProperties: "Category,ProductImages");
            }
            else if (!string.IsNullOrEmpty(author)) // Thêm điều kiện lọc theo tác giả
            {
                productList = _unitOfWork.Product.GetAll(p => p.Author.Contains(author), includeProperties: "Category,ProductImages");
            }
            else
            {
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            }

            if (minPrice.HasValue)
            {
                productList = productList.Where(p => (decimal)p.Price100 >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productList = productList.Where(p => (decimal)p.Price100 <= maxPrice.Value);
            }

            switch (sortOrder)
            {
                case "price_desc":
                    productList = productList.OrderByDescending(p => p.Price100);
                    break;
                default:
                    productList = productList.OrderBy(p => p.Price100);
                    break;
            }

            if (productList == null || !productList.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Empty");
            }

            var categories = _unitOfWork.Category.GetAll().Select(c => c.Name).ToList();

            if (categories != null)
            {
                ViewBag.Categories = categories;
            }
            // Lấy các sản phẩm flash sale
            var flashSaleProducts = _unitOfWork.Product.GetFlashSaleProducts();
            // Lọc ra các sản phẩm thông thường
            var regularProducts = productList.Where(p => !flashSaleProducts.Contains(p)).ToList();

            // Phân trang
            int pageSize = 12;
            int pageNumber = page ?? 1;
            //Sử dụng các biến ViewBag để lưu trữ các tham số tìm kiếm hiện tại
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentCategory = category;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.FlashSaleProducts = flashSaleProducts;

            return View(productList.ToPagedList(pageNumber, pageSize));
        }

        public IActionResult Details(int productId)
        {
            var product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category,ProductImages");

            if (product == null)
            {
                return NotFound();
            }

            ShoppingCart cart = new()
            {
                Product = product,
                Count = 1,
                ProductId = productId
            };  

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Check user roles
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            shoppingCart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId &&
            u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                //shopping cart exists
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                //add cart record
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
            }
            TempData["success"] = "Cart updated successfully";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult AutoComplete(string search)
        {
            var Result = _db.Products.Where(x => x.Title.Contains(search))
                                        .Select(x => new {
                                            label = x.Title,
                                            value = x.Title
                                        }).ToList();
            return Json(Result);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Empty()
        {
            return View();
        }
        public IActionResult DieuKhoan()
        {
            return View();
        }
        public IActionResult ChinhSach()
        {
            return View();
        }
        public IActionResult BaoMatThanhToan()
        {
            return View();
        }
        public IActionResult GioiThieu()
        {
            return View();
        }

        public IActionResult DoiTraHang()
        {
            return View();
        }
        public IActionResult BaoHanh()
        {
            return View();
        }
        public IActionResult VanChuyen()
        {
            return View();
        }
        public IActionResult ChinhSachKhachSi()
        {
            return View();
        }
        public IActionResult HuongDanThanhToan()
        {
            return View();
        }

    }
}
