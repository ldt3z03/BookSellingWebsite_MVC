using BookSelling.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSellingWebsite.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class MailController : Controller
    {
        private readonly IMailService _mailService;
        //injecting IMailService vào constructor
        public MailController(IMailService _MailService)
        {
            _mailService = _MailService;
        }
        public IActionResult CreateMail()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SendMail(MailData mailData)
        {
            _mailService.SendMail(mailData);
            return View();
        }
    }
}
