using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace Automation.Library
{
    public class EmailMethod
    {
        public static void SendEmailTemplate(string comments,string emailId)
        {
            string[] address = new string[] { "<EMAIL_ID>","<EMAIL_ID>" };
            string fullName = null;
            try
            {
                fullName = UserPrincipal.Current.DisplayName;
            }
            catch(Exception ex)
            {
                LoggerUtil.LogMessageToFile("Issue while fetching Display Name is : " + ex.ToString());
            }

            foreach (string addr in address)
            {
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress("DLNAME" + "@DOMAIN.com");

                if(fullName.Contains(' '))
                {
                msg.CC.Add(fullName.Split(' ')[0] + "." + fullName.Split(' ')[1] + "@DOMAIN.com");
                }
                else if (fullName != null)
                {
                    msg.CC.Add(fullName + "@DOMAIN.com");
                }
                msg.Bcc.Add(addr);
                msg.Bcc.Add("<BCC_EMAIL_ID>");
                msg.Subject = "Test Mate Issue";
                msg.IsBodyHtml = true;
                msg.BodyEncoding = Encoding.ASCII;
                msg.Body = "Issue has been reported by the User : " + Environment.UserName + " On the machine " + Environment.MachineName +"<br>"+
                    "Comment Entered by User is : " + comments + "<br>" +
                   " Environment Details are as follows : " + "<br>" +
                   " OS Version : " + Environment.OSVersion + "<br>" +
                   " UserName : " + Environment.UserName + "<br>" +
                   " SystemDirectory : " + Environment.SystemDirectory + "<br>" +
                   " TEMP Variable : " + LoggerUtil.GetTempPath() + "<br>" +
                   " PFA the logfile : " + LoggerUtil.GetTempPath() + ".";

                if (File.Exists(LoggerUtil.GetTempPath() + "TestMateLogFile.txt"))
                {
                    string logFilePath = LoggerUtil.GetTempPath() + "TestMateLogFile.txt";
                    msg.Attachments.Add(new Attachment(logFilePath));
                }
                sendMail(msg);
                System.Threading.Thread.Sleep(2000);
            }
        }

        public static void sendMail(MailMessage msg)
        {
            SmtpClient mClient = new SmtpClient();
            mClient.Host = "<SMTP_IP>";
            mClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            mClient.Timeout = 100000;
            mClient.Send(msg);
        }
    }
}
