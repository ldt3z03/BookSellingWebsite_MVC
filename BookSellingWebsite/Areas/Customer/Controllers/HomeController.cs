using BookSelling.DataAccess.Data;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using BookSelling.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookSellingWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ApplicationDbContext db)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public async Task<IActionResult> Index(string category, string searchString)
        {
            IEnumerable<Product> productList;

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = _unitOfWork.Product.GetAll(p => p.Title.Contains(searchString), includeProperties: "Category,ProductImages");
            }
            else if (!string.IsNullOrEmpty(category))
            {
                productList = _unitOfWork.Product.GetAll(p => p.Category.Name == category, includeProperties: "Category,ProductImages");
            }
            else
            {
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
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

            ViewBag.CurrentFilter = searchString;

            return View(productList);
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
