using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheMaster.Application.Common;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;

namespace TheMaster.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ISslCommerzService _sslService;

        public OrdersController(IOrderService orderService, ISslCommerzService sslService)
        {
            _orderService = orderService;
            _sslService = sslService;
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Student";
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "student@themaster.com";

            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized access.", 401));
            }

            var order = await _orderService.CreateOrderAsync(userId, dto);

            var paymentInit = await _sslService.InitiatePaymentAsync(
                order.Id,
                order.Amount,
                order.TransactionId ?? order.OrderNumber,
                userName,
                userEmail,
                order.UserPhone ?? "01700000000"
            );

            return Ok(ApiResponse<object>.Ok(new
            {
                order,
                payment = paymentInit
            }, "Checkout session created successfully."));
        }

        [Authorize]
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized access.", 401));
            }

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.Ok(orders));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound(ApiResponse<object>.Fail("Order not found.", 404));

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && order.UserId.ToString() != userIdStr)
            {
                return Forbid();
            }

            return Ok(ApiResponse<OrderResponseDto>.Ok(order));
        }
    }
}
