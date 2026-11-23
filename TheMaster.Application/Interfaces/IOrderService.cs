using System.Collections.Generic;
using System.Threading.Tasks;
using TheMaster.Application.DTOs;

namespace TheMaster.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto dto);
        Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(int orderId);
        Task<OrderResponseDto?> GetOrderByTransactionIdAsync(string transactionId);
        Task<bool> UpdateDriveAccessStatusAsync(int orderId, UpdateDriveAccessDto dto);
        Task<bool> CompletePaymentAsync(string transactionId, string valId, string rawResponse);
        Task<bool> FailPaymentAsync(string transactionId, string rawResponse);
    }
}
