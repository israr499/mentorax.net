using System.Net;
using System.Net.Mail;

namespace MentoraXWebApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                client.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "MentoraX"),
                    Subject = "Your MentoraX Login Verification Code",
                    Body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='UTF-8'>
                            <style>
                                body {{ font-family: Arial, sans-serif; }}
                                .container {{ max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px; }}
                                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                                .code {{ font-size: 36px; font-weight: bold; text-align: center; padding: 20px; background: #f5f5f5; border-radius: 8px; letter-spacing: 5px; font-family: monospace; }}
                                .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                                .warning {{ color: #e74c3c; font-size: 12px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h2>🔐 MentoraX Login Verification</h2>
                                </div>
                                <div style='padding: 20px;'>
                                    <p>Hello,</p>
                                    <p>You recently tried to log in to your MentoraX account.</p>
                                    <p>Your One-Time Password (OTP) is:</p>
                                    <div class='code'>{otpCode}</div>
                                    <p>This code is valid for <strong>5 minutes</strong>.</p>
                                    <p>If you didn't request this, please ignore this email.</p>
                                    <hr />
                                    <p class='warning'>⚠️ Never share this code with anyone.</p>
                                </div>
                                <div class='footer'>
                                    <p>&copy; 2026 MentoraX - Secure Tutoring Platform</p>
                                </div>
                            </div>
                        </body>
                        </html>
                    ",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
            }
        }
    }
}