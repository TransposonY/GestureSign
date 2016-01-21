using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GestureSign.Common;

namespace GestureSign.ControlPanel.Common
{
    class Net
    {
        private const string Address = "GestureSignfeedback00@outlook.com";

        public static string SendMail(string subject, string content)
        {
            MailMessage mail = new MailMessage(Address, "553078206@qq.com")
            {
                Subject = subject,
                Body = content,
                IsBodyHtml = false
            };

            SmtpClient client = new SmtpClient
            {
                // UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(Address, "GestureSign" + Int32.MaxValue),
                // Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Host = "smtp.live.com",
                EnableSsl = true,
                Timeout = 36000,
            };
            // client.Port = 587

            try
            {
                client.Send(mail);
                //client.SendAsync(mail, userState);

                return null;
            }
            catch (SmtpException ex)
            {
                Logging.LogException(ex);
                return ex.Message;
            }
        }
    }
}

