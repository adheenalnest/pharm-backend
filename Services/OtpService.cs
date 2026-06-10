using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PharmeasyAPI.Services;

public class OtpService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IConfiguration config, ILogger<OtpService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyOtp(string otp, string hash) =>
        HashOtp(otp) == hash;

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        // Always log OTP to console for easy dev testing
        _logger.LogWarning("=== OTP for {Email}: {Otp} ===", toEmail, otp);

        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"];
        var username = smtp["Username"];
        var password = smtp["Password"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("SMTP not configured — OTP only printed to console.");
            return;
        }

        var port = int.Parse(smtp["Port"] ?? "587");
        var from = smtp["From"] ?? username;

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mail = new MailMessage(from, toEmail)
            {
                Subject = "Your Pharmeasy OTP",
                Body = $"Your OTP is: {otp}\n\nThis code expires in 10 minutes. Do not share it with anyone."
            };

            await client.SendMailAsync(mail);
            _logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // Email failure must not block login — OTP is still printed to console above
            _logger.LogError(ex, "Failed to send OTP email to {Email}. Use console OTP instead.", toEmail);
        }
    }
}
