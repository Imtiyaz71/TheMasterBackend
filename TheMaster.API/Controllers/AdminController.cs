using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheMaster.Application.Common;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;
using TheMaster.Infrastructure.Data;

namespace TheMaster.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminController(IOrderService orderService, IEmailService emailService, IDbConnectionFactory connectionFactory)
        {
            _orderService = orderService;
            _emailService = emailService;
            _connectionFactory = connectionFactory;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.Ok(orders));
        }

        [HttpPut("orders/{id:int}/drive-access")]
        public async Task<IActionResult> UpdateDriveAccess(int id, [FromBody] UpdateDriveAccessDto dto)
        {
            var success = await _orderService.UpdateDriveAccessStatusAsync(id, dto);
            if (!success) return NotFound(ApiResponse<object>.Fail("Order not found.", 404));

            return Ok(ApiResponse<bool>.Ok(true, $"Drive access status updated to '{dto.Status}'."));
        }

        [HttpPost("orders/{id:int}/resend-email")]
        public async Task<IActionResult> ResendEmail(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound(ApiResponse<object>.Fail("Order not found.", 404));

            var sent = await _emailService.SendCourseEnrollmentEmailAsync(
                order.UserFullName,
                order.UserEmail,
                order.CourseTitle,
                order.DriveFolderLink
            );

            return Ok(ApiResponse<bool>.Ok(sent, sent ? "Email sent successfully." : "Failed to send email. Check SMTP settings."));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            using var db = _connectionFactory.CreateConnection();

            var totalRevenue = await db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(Amount), 0) FROM Orders WHERE PaymentStatus = 'Paid';");
            var totalOrders = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders;");
            var paidOrders = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders WHERE PaymentStatus = 'Paid';");
            var pendingDriveAccess = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders WHERE PaymentStatus = 'Paid' AND DriveAccessStatus = 'Pending';");
            var totalCourses = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Courses;");
            var totalStudents = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE Role = 'Student';");

            var stats = new
            {
                totalRevenue,
                totalOrders,
                paidOrders,
                pendingDriveAccess,
                totalCourses,
                totalStudents
            };

            return Ok(ApiResponse<object>.Ok(stats));
        }
    }
}
