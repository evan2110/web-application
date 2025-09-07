using server.DTOs;

namespace server.Services
{
    public interface IMailService
    {
        Task SendAsync(MailDataReqDTO mailData);
    }
}
