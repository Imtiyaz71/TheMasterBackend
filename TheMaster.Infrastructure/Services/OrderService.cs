using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;
using TheMaster.Domain.Entities;
using TheMaster.Infrastructure.Data;

namespace TheMaster.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;

        public OrderService(IDbConnectionFactory connectionFactory, IEmailService emailService)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var user = await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = userId });
            if (user == null) throw new KeyNotFoundException("User not found.");

            var course = await db.QueryFirstOrDefaultAsync<Course>("SELECT * FROM Courses WHERE Id = @Id", new { Id = dto.CourseId });
            if (course == null) throw new KeyNotFoundException("Course not found.");

            // If already purchased and paid, return existing order to avoid double payment
            var existingPaidOrder = await db.QueryFirstOrDefaultAsync<Order>(
                "SELECT * FROM Orders WHERE UserId = @UserId AND CourseId = @CourseId AND PaymentStatus = 'Paid'", 
                new { UserId = userId, CourseId = dto.CourseId });
            if (existingPaidOrder != null)
            {
                return (await GetOrderByIdAsync(existingPaidOrder.Id))!;
            }

            var amount = course.DiscountPrice.HasValue && course.DiscountPrice.Value > 0
                ? course.DiscountPrice.Value
                : course.Price;

            var tranId = $"TM{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var sqlOrder = @"
                INSERT INTO Orders (OrderNumber, UserId, CourseId, Amount, PaymentStatus, DriveAccessStatus, TransactionId, CreatedAt)
                VALUES (@OrderNumber, @UserId, @CourseId, @Amount, 'Pending', 'Pending', @TransactionId, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var orderId = await db.ExecuteScalarAsync<int>(sqlOrder, new
            {
                OrderNumber = orderNumber,
                UserId = userId,
                CourseId = dto.CourseId,
                Amount = amount,
                TransactionId = tranId
            });

            var sqlTx = @"
                INSERT INTO PaymentTransactions (OrderId, TranId, Amount, Status, CreatedAt)
                VALUES (@OrderId, @TranId, @Amount, 'PENDING', SYSUTCDATETIME());";

            await db.ExecuteAsync(sqlTx, new
            {
                OrderId = orderId,
                TranId = tranId,
                Amount = amount
            });

            return (await GetOrderByIdAsync(orderId))!;
        }

        public async Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(int userId)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT o.Id, o.OrderNumber, o.UserId, 
                       COALESCE(u.FullName, 'Student') AS UserFullName, 
                       COALESCE(u.Email, '') AS UserEmail, 
                       u.PhoneNumber AS UserPhone,
                       o.CourseId, 
                       COALESCE(c.Title, 'Course') AS CourseTitle, 
                       COALESCE(c.DriveFolderLink, '') AS DriveFolderLink, 
                       o.Amount, o.PaymentStatus, o.DriveAccessStatus,
                       o.DriveAccessGrantedAt, o.PaymentMethod, o.TransactionId, o.CreatedAt
                FROM Orders o
                LEFT JOIN Users u ON o.UserId = u.Id
                LEFT JOIN Courses c ON o.CourseId = c.Id
                WHERE o.UserId = @UserId
                ORDER BY o.CreatedAt DESC;";

            return await db.QueryAsync<OrderResponseDto>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT o.Id, o.OrderNumber, o.UserId, 
                       COALESCE(u.FullName, 'Student') AS UserFullName, 
                       COALESCE(u.Email, '') AS UserEmail, 
                       u.PhoneNumber AS UserPhone,
                       o.CourseId, 
                       COALESCE(c.Title, 'Course') AS CourseTitle, 
                       COALESCE(c.DriveFolderLink, '') AS DriveFolderLink, 
                       o.Amount, o.PaymentStatus, o.DriveAccessStatus,
                       o.DriveAccessGrantedAt, o.PaymentMethod, o.TransactionId, o.CreatedAt
                FROM Orders o
                LEFT JOIN Users u ON o.UserId = u.Id
                LEFT JOIN Courses c ON o.CourseId = c.Id
                ORDER BY o.CreatedAt DESC;";

            return await db.QueryAsync<OrderResponseDto>(sql);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT o.Id, o.OrderNumber, o.UserId, 
                       COALESCE(u.FullName, 'Student') AS UserFullName, 
                       COALESCE(u.Email, '') AS UserEmail, 
                       u.PhoneNumber AS UserPhone,
                       o.CourseId, 
                       COALESCE(c.Title, 'Course') AS CourseTitle, 
                       COALESCE(c.DriveFolderLink, '') AS DriveFolderLink, 
                       o.Amount, o.PaymentStatus, o.DriveAccessStatus,
                       o.DriveAccessGrantedAt, o.PaymentMethod, o.TransactionId, o.CreatedAt
                FROM Orders o
                LEFT JOIN Users u ON o.UserId = u.Id
                LEFT JOIN Courses c ON o.CourseId = c.Id
                WHERE o.Id = @Id;";

            return await db.QueryFirstOrDefaultAsync<OrderResponseDto>(sql, new { Id = orderId });
        }

        public async Task<OrderResponseDto?> GetOrderByTransactionIdAsync(string transactionId)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT o.Id, o.OrderNumber, o.UserId, 
                       COALESCE(u.FullName, 'Student') AS UserFullName, 
                       COALESCE(u.Email, '') AS UserEmail, 
                       u.PhoneNumber AS UserPhone,
                       o.CourseId, 
                       COALESCE(c.Title, 'Course') AS CourseTitle, 
                       COALESCE(c.DriveFolderLink, '') AS DriveFolderLink, 
                       o.Amount, o.PaymentStatus, o.DriveAccessStatus,
                       o.DriveAccessGrantedAt, o.PaymentMethod, o.TransactionId, o.CreatedAt
                FROM Orders o
                LEFT JOIN Users u ON o.UserId = u.Id
                LEFT JOIN Courses c ON o.CourseId = c.Id
                WHERE o.TransactionId = @TransactionId;";

            return await db.QueryFirstOrDefaultAsync<OrderResponseDto>(sql, new { TransactionId = transactionId });
        }

        public async Task<bool> CompletePaymentAsync(string transactionId, string valId, string rawResponse)
        {
            using var db = _connectionFactory.CreateConnection();

            var order = await GetOrderByTransactionIdAsync(transactionId);
            if (order == null) return false;

            var sqlUpdateOrder = @"
                UPDATE Orders 
                SET PaymentStatus = 'Paid', UpdatedAt = SYSUTCDATETIME() 
                WHERE TransactionId = @TransactionId;";

            await db.ExecuteAsync(sqlUpdateOrder, new { TransactionId = transactionId });

            var sqlUpdateTx = @"
                UPDATE PaymentTransactions 
                SET ValId = @ValId, Status = 'VALID', TranDate = SYSUTCDATETIME(), RawResponse = @RawResponse 
                WHERE TranId = @TranId;";

            await db.ExecuteAsync(sqlUpdateTx, new
            {
                ValId = valId,
                RawResponse = rawResponse,
                TranId = transactionId
            });

            // Check & Insert Enrollment
            var checkEnrollment = @"SELECT COUNT(*) FROM Enrollments WHERE UserId = @UserId AND CourseId = @CourseId;";
            var exists = await db.ExecuteScalarAsync<int>(checkEnrollment, new { UserId = order.UserId, CourseId = order.CourseId }) > 0;

            if (!exists)
            {
                var sqlEnrollment = @"
                    INSERT INTO Enrollments (UserId, CourseId, OrderId, EnrolledAt, ExpiresAt, DriveAccessGranted, Notes)
                    VALUES (@UserId, @CourseId, @OrderId, SYSUTCDATETIME(), DATEADD(day, 365, SYSUTCDATETIME()), 0, 'Enrolled via SSLCommerz payment.');";

                await db.ExecuteAsync(sqlEnrollment, new
                {
                    UserId = order.UserId,
                    CourseId = order.CourseId,
                    OrderId = order.Id
                });
            }

            // Auto-send Course Google Drive Link Email to student
            _ = _emailService.SendCourseEnrollmentEmailAsync(
                order.UserFullName,
                order.UserEmail,
                order.CourseTitle,
                order.DriveFolderLink
            );

            return true;
        }

        public async Task<bool> FailPaymentAsync(string transactionId, string rawResponse)
        {
            using var db = _connectionFactory.CreateConnection();

            var sqlUpdateOrder = @"
                UPDATE Orders 
                SET PaymentStatus = 'Failed', UpdatedAt = SYSUTCDATETIME() 
                WHERE TransactionId = @TransactionId;";

            await db.ExecuteAsync(sqlUpdateOrder, new { TransactionId = transactionId });

            var sqlUpdateTx = @"
                UPDATE PaymentTransactions 
                SET Status = 'FAILED', RawResponse = @RawResponse 
                WHERE TranId = @TranId;";

            await db.ExecuteAsync(sqlUpdateTx, new
            {
                RawResponse = rawResponse,
                TranId = transactionId
            });

            return true;
        }

        public async Task<bool> UpdateDriveAccessStatusAsync(int orderId, UpdateDriveAccessDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var sqlUpdateOrder = @"
                UPDATE Orders 
                SET DriveAccessStatus = @Status,
                    DriveAccessGrantedAt = CASE WHEN @Status = 'Granted' THEN SYSUTCDATETIME() ELSE NULL END,
                    UpdatedAt = SYSUTCDATETIME() 
                WHERE Id = @Id;";

            var rows = await db.ExecuteAsync(sqlUpdateOrder, new { Status = dto.Status, Id = orderId });
            if (rows == 0) return false;

            var enrollmentExists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Enrollments WHERE OrderId = @OrderId", new { OrderId = orderId }) > 0;

            if (enrollmentExists)
            {
                var sqlUpdateEnrollment = @"
                    UPDATE Enrollments 
                    SET DriveAccessGranted = CASE WHEN @Status = 'Granted' THEN 1 ELSE 0 END,
                        DriveAccessGrantedAt = CASE WHEN @Status = 'Granted' THEN SYSUTCDATETIME() ELSE NULL END,
                        Notes = COALESCE(@Notes, Notes)
                    WHERE OrderId = @OrderId;";

                await db.ExecuteAsync(sqlUpdateEnrollment, new { Status = dto.Status, Notes = dto.Notes, OrderId = orderId });
            }
            else
            {
                var order = await GetOrderByIdAsync(orderId);
                if (order != null)
                {
                    var sqlInsertEnrollment = @"
                        INSERT INTO Enrollments (UserId, CourseId, OrderId, EnrolledAt, ExpiresAt, DriveAccessGranted, DriveAccessGrantedAt, Notes)
                        VALUES (@UserId, @CourseId, @OrderId, SYSUTCDATETIME(), DATEADD(day, 365, SYSUTCDATETIME()), 
                                CASE WHEN @Status = 'Granted' THEN 1 ELSE 0 END,
                                CASE WHEN @Status = 'Granted' THEN SYSUTCDATETIME() ELSE NULL END,
                                @Notes);";
                    await db.ExecuteAsync(sqlInsertEnrollment, new
                    {
                        UserId = order.UserId,
                        CourseId = order.CourseId,
                        OrderId = orderId,
                        Status = dto.Status,
                        Notes = dto.Notes
                    });
                }
            }

            return true;
        }
    }
}
