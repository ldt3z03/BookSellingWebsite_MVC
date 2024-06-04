using System;
using System.Linq;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using BookSelling.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSellingWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class VoucherController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public VoucherController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var voucherList = _unitOfWork.Voucher.GetAll();
            return View(voucherList);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Voucher obj)
        {

            if (ModelState.IsValid)
            {
                _unitOfWork.Voucher.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Voucher created successfully";
                return RedirectToAction("Index");
            }
            return View();
        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Voucher? VoucherFromDb = _unitOfWork.Voucher.Get(u => u.Id == id);
           
            if (VoucherFromDb == null)
            {
                return NotFound();
            }
            return View(VoucherFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Voucher obj)
        {
            if (ModelState.IsValid)
            {
                var voucherFromDb = _unitOfWork.Voucher.Get(u => u.Id == obj.Id);
                if (voucherFromDb == null)
                {
                    return NotFound();
                }

                // Log the original and new values for debugging

                voucherFromDb.Code = obj.Code;
                voucherFromDb.Description = obj.Description;
                voucherFromDb.DiscountAmount = obj.DiscountAmount;
                voucherFromDb.ExpiryDate = obj.ExpiryDate;

                _unitOfWork.Voucher.Update(voucherFromDb);
                _unitOfWork.Save();

                // Log after update
                var updatedVoucher = _unitOfWork.Voucher.Get(u => u.Id == obj.Id);

                TempData["success"] = "Voucher updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }
            return View(obj);
        }

        // Delete Voucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Voucher obj)
        {
            var voucher = _unitOfWork.Voucher.Get(v => v.Id == obj.Id);
            if (voucher == null)
            {
                return NotFound();
            }
                
            _unitOfWork.Voucher.Remove(voucher);
            _unitOfWork.Save();

            TempData["success"] = "Voucher deleted successfully";
            return RedirectToAction(nameof(Index));
        }

        // Generate a unique voucher code
        private string GenerateVoucherCode()
        {
            // Logic to generate a unique voucher code
            // You can customize this according to your requirements
            return Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }
    }
}
