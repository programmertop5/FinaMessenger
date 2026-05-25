using System;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace ServerMsg
{
    internal class EmailService
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "it598135@gmail.com";
        private const string SenderPassword = "hezn uuiw khcy owgq";

        public void SendVerificationEmailAsync(string targetEmail, string verificationCode)
        {
            Thread emailThread = new Thread(() => Send(targetEmail, verificationCode));
            emailThread.Start();
        }

        private void Send(string targetEmail, string verificationCode)
        {
            try
            {
                string htmlBody = $@"<!DOCTYPE html>
<html>
<body style='font-family:Arial;background:#f4f4f4;padding:40px;'>
    <div style='max-width:600px;margin:auto;background:white;padding:30px;border-radius:10px;'>
        <h2 style='color:#2563eb;'>Підтвердження входу</h2>
        <p>Ваш код підтвердження:</p>
        <div style='font-size:32px; font-weight:bold; letter-spacing:5px; color:#2563eb; margin:20px 0;'>
            {verificationCode}
        </div>
        <p>Код дійсний 10 хвилин.</p>
    </div>
</body>
</html>";

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(SenderEmail, "Чат-Сервер"),
                    Subject = "Код підтвердження входу",
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(targetEmail);

                using SmtpClient smtpServer = new SmtpClient(SmtpHost);
                smtpServer.Port = SmtpPort;
                smtpServer.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                smtpServer.EnableSsl = true;

                smtpServer.Send(mail);
                Console.WriteLine($"[SMTP] HTML-код {verificationCode} успішно надіслано на почту {targetEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP Помилка]: Не вдалося надіслати HTML-лист. Деталі: {ex.Message}");
            }
        }
    }
}