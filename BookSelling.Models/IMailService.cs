using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSelling.Models
{
    public interface IMailService
    {
        bool SendMail(MailData mailData);
    }
}
