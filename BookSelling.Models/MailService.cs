using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
namespace BookSelling.Models
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;

        //injection MailSetting vào lớp này để dùng
        public MailService(IOptions<MailSettings> mailSettingsOptions)
        {
            _mailSettings = mailSettingsOptions.Value;
        }
        // xử lý gửi mail
        public bool SendMail(MailData mailData)
        {
            //Tạo 1 đối tượng MimeMessage dùng xong thì tự hủy
            using (MimeMessage emailMessage = new MimeMessage())
            {
                //Tạo 1 địa chỉ mail người gửi
                MailboxAddress emailFrom = new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail);
                //add địa chỉ mail người gửi vào mimemessage
                emailMessage.From.Add(emailFrom);
                //tạo 1 địa chỉ mail người nhận
                MailboxAddress emailTo = new MailboxAddress(mailData.ReceiverName, mailData.ReceiverEmail);
                //add địa chỉ mail người nhận vào mimemessage
                emailMessage.To.Add(emailTo);
                //thêm mail CC và BCC nếu cần
                //emailMessage.Cc.Add(new MailboxAddress("Cc Receiver", "cc@example.com"));
                //emailMessage.Bcc.Add(new MailboxAddress("Bcc Receiver", "bcc@example.com"));
                //Gán tiêu đề mail
                emailMessage.Subject = mailData.Title;
                //Tạo đối tượng chứa nội dung mail
                BodyBuilder emailBodyBuilder = new BodyBuilder()
                {
                    TextBody = mailData.Body
                };
                emailBodyBuilder.TextBody = mailData.Body;
                //Gán nội dung mail vào mimemessage
                emailMessage.Body = emailBodyBuilder.ToMessageBody();
                //tạo đối tượng SmtpClient từ Mailkit.Net.Smtp namespace, không dùng  System.Net.Mail nhé
                using (SmtpClient mailClient = new SmtpClient())
                {
                    try
                    {
                        //Kết nối tới server smtp.gmail
                        mailClient.Connect(_mailSettings.Server, _mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                        //đăng nhập
                        mailClient.Authenticate(_mailSettings.SenderEmail, _mailSettings.Password);
                        //gửi mail
                        mailClient.Send(emailMessage);
                        //ngắt kết nối
                        mailClient.Disconnect(true);
                    }
                    catch(Exception ex)
                    {
                        // Ghi log hoặc xử lý ngoại lệ nếu cần
                        throw new InvalidOperationException("Gửi email thất bại", ex);
                    }
                    
                }
            }
            return true;
        }
    }
}
