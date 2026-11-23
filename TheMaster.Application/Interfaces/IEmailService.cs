using System.Threading.Tasks;

namespace TheMaster.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendCourseEnrollmentEmailAsync(string studentName, string studentEmail, string courseTitle, string driveFolderLink);
    }
}
