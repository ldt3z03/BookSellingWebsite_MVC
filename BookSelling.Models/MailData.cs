using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSelling.Models
{
    public class MailData
    {
        [DisplayName("Địa chỉ email người nhận")]
        public string ReceiverEmail { get; set; }
        [DisplayName("Tên người nhận")]
        public string ReceiverName { get; set; }
        [DisplayName("Tiêu đề")]
        public string Title { get; set; }
        [DisplayName("Nội dung")]
        public string Body { get; set; }
    }
}
