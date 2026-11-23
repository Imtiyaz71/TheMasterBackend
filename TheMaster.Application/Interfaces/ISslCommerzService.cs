using System.Threading.Tasks;
using TheMaster.Application.DTOs;

namespace TheMaster.Application.Interfaces
{
    public interface ISslCommerzService
    {
        Task<SslCommerzInitResponse> InitiatePaymentAsync(int orderId, decimal amount, string tranId, string customerName, string customerEmail, string customerPhone);
        Task<bool> ValidatePaymentAsync(string valId);
    }
}
