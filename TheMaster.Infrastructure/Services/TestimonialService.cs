using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;
using TheMaster.Domain.Entities;
using TheMaster.Infrastructure.Data;

namespace TheMaster.Infrastructure.Services
{
    public class TestimonialService : ITestimonialService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TestimonialService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<TestimonialResponseDto>> GetAllTestimonialsAsync(bool activeOnly = true)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = activeOnly
                ? "SELECT * FROM Testimonials WHERE IsActive = 1 ORDER BY DisplayOrder ASC, CreatedAt DESC"
                : "SELECT * FROM Testimonials ORDER BY DisplayOrder ASC, CreatedAt DESC";

            var items = await db.QueryAsync<Testimonial>(sql);
            return items.Select(MapToDto);
        }

        public async Task<TestimonialResponseDto?> GetTestimonialByIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            var item = await db.QueryFirstOrDefaultAsync<Testimonial>("SELECT * FROM Testimonials WHERE Id = @Id", new { Id = id });
            return item != null ? MapToDto(item) : null;
        }

        public async Task<TestimonialResponseDto> CreateTestimonialAsync(CreateTestimonialDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var insertSql = @"
                INSERT INTO Testimonials (StudentName, Designation, Speech, AvatarUrl, Rating, DisplayOrder, IsActive, CreatedAt)
                VALUES (@StudentName, @Designation, @Speech, @AvatarUrl, @Rating, @DisplayOrder, @IsActive, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await db.ExecuteScalarAsync<int>(insertSql, new
            {
                dto.StudentName,
                dto.Designation,
                dto.Speech,
                dto.AvatarUrl,
                dto.Rating,
                dto.DisplayOrder,
                dto.IsActive
            });

            var created = await GetTestimonialByIdAsync(id);
            return created!;
        }

        public async Task<TestimonialResponseDto?> UpdateTestimonialAsync(int id, UpdateTestimonialDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var updateSql = @"
                UPDATE Testimonials
                SET StudentName = @StudentName,
                    Designation = @Designation,
                    Speech = @Speech,
                    AvatarUrl = @AvatarUrl,
                    Rating = @Rating,
                    DisplayOrder = @DisplayOrder,
                    IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id;";

            var affected = await db.ExecuteAsync(updateSql, new
            {
                Id = id,
                dto.StudentName,
                dto.Designation,
                dto.Speech,
                dto.AvatarUrl,
                dto.Rating,
                dto.DisplayOrder,
                dto.IsActive
            });

            if (affected == 0) return null;
            return await GetTestimonialByIdAsync(id);
        }

        public async Task<bool> DeleteTestimonialAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            var affected = await db.ExecuteAsync("DELETE FROM Testimonials WHERE Id = @Id", new { Id = id });
            return affected > 0;
        }

        private static TestimonialResponseDto MapToDto(Testimonial t)
        {
            return new TestimonialResponseDto
            {
                Id = t.Id,
                StudentName = t.StudentName,
                Designation = t.Designation,
                Speech = t.Speech,
                AvatarUrl = t.AvatarUrl,
                Rating = t.Rating,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            };
        }
    }
}
