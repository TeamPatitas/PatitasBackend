using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;
public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string htmlBody);
}