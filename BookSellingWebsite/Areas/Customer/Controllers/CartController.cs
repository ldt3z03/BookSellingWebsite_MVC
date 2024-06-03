using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using BookSelling.Models.ViewModels;
using BookSelling.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using X.PagedList;
using static BookSelling.Models.ViewModels.BestSellingProductsVM;


namespace BookSellingWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMailService _mailService;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IMailService mailService)
        {
            _unitOfWork = unitOfWork;
            _mailService = mailService;
        }

        private bool IsUserInRole(string role)
        {
            return User.IsInRole(role);
        }

        private IActionResult AccessDenied()
        {
            return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
        }

        // Method to display the best-selling products


        public IActionResult Index()
        {
            if (IsUserInRole(SD.Role_Admin) || IsUserInRole(SD.Role_Employee))
            {
                return AccessDenied();
            }
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            if (IsUserInRole(SD.Role_Admin) || IsUserInRole(SD.Role_Employee))
            {
                return AccessDenied();
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            if (IsUserInRole(SD.Role_Admin) || IsUserInRole(SD.Role_Employee))
            {
                return AccessDenied();
            }
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);


            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer 
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //it is a company user
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer account and need a capture payment
                //stripe logic
                var domain = "https://localhost:44353/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // $20.5 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }


                var service = new Stripe.Checkout.SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

            // Truy xuất danh sách các cuốn sách đã mua từ đơn hàng
            List<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(o => o.OrderHeaderId == id, includeProperties: "Product").ToList();

            // Tạo một danh sách các tên cuốn sách
            List<string> bookNames = orderDetails.Select(od => od.Product.Title).ToList();

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // Xác định trạng thái thanh toán và cập nhật trạng thái đơn hàng nếu cần
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
                HttpContext.Session.Clear();
            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            // Prepare email content
            string emailSubject = "Xác nhận đơn hàng #" + orderHeader.Id;
            string emailBody = $"Xin chào {orderHeader.ApplicationUser.Name}." +
                $"Đơn hàng của bạn đã được xác nhận thành công. Dưới đây là các chi tiết:" +
                $"ID đơn hàng: {orderHeader.Id}." +
                $"Thời gian đặt hàng: {orderHeader.OrderDate}." +
                $"Số điện thoại liên hệ: {orderHeader.PhoneNumber}." +
                $"Địa chỉ email: {orderHeader.ApplicationUser.Email}." +
                $".Địa chỉ nhận hàng: {orderHeader.StreetAddress}, {orderHeader.City}, {orderHeader.State}, {orderHeader.PostalCode}" +
                $"Các cuốn sách đã mua:";

            // Thêm tên các cuốn sách vào nội dung email
            emailBody += "";
            foreach (var bookName in bookNames)
            {
                emailBody += $"{bookName}";
            }
            emailBody += "";

            emailBody += $"Tổng tiền đơn hàng: {orderHeader.OrderTotal}$." +
                $"Xin cảm ơn bạn đã mua hàng của chúng tôi!" +
                $"Trân trọng, Đội ngũ hỗ trợ khách hàng của chúng tôi.";

            // Gửi email thông báo
            var mailData = new MailData
            {
                ReceiverEmail = orderHeader.ApplicationUser.Email,
                ReceiverName = orderHeader.ApplicationUser.Name,
                Title = emailSubject,
                Body = emailBody
            };
            _mailService.SendMail(mailData);

            return View(id);
        }
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            if (cartFromDb.Count <= 1)
            {
                //remove from cart
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Product.FlashSalePrice.HasValue && shoppingCart.Product.FlashSalePrice > 0)
            {
                return (double)shoppingCart.Product.FlashSalePrice.Value;
            }
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
        public IActionResult BestSellingProducts(int? page)
        {
            int pageSize = 10; // Số lượng sản phẩm trên mỗi trang
            int pageNumber = page ?? 1;

            // Lấy danh sách sản phẩm
            var products = _unitOfWork.Product.GetAll();

            // Tính toán số lượng đặt hàng và tổng doanh số bán hàng cho mỗi sản phẩm
            var bestSellingProducts = products
                .Select(p => new
                {
                    Product = p,
                    OrderCount = _unitOfWork.OrderDetail.GetAll(od => od.ProductId == p.Id).Sum(od => od.Count),
                    TotalSales = _unitOfWork.OrderDetail.GetAll(od => od.ProductId == p.Id).Sum(od => od.Count * od.Price)
                })
                .OrderByDescending(p => p.OrderCount)
                .ToList();

            // Tạo ViewModel để hiển thị
            var bestSellingProductsVM = new BestSellingProductsVM
            {
                BestSellingProducts = bestSellingProducts
                    .Select(b => new ProductSalesVM
                    {
                        Product = b.Product,
                        OrderCount = b.OrderCount,
                        TotalSales = b.TotalSales
                    }).ToPagedList(pageNumber, pageSize)
            };

            return View(bestSellingProductsVM);
        }
    }
}
