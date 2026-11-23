using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;
using TheMaster.Domain.Entities;
using TheMaster.Infrastructure.Data;

namespace TheMaster.Infrastructure.Services
{
    public class CourseService : ICourseService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CourseService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync(bool publicOnly = true)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = publicOnly 
                ? "SELECT * FROM Courses WHERE IsPublished = 1 ORDER BY CreatedAt DESC"
                : "SELECT * FROM Courses ORDER BY CreatedAt DESC";

            var courses = (await db.QueryAsync<Course>(sql)).ToList();
            if (!courses.Any()) return Enumerable.Empty<CourseResponseDto>();

            var courseIds = courses.Select(c => c.Id).ToArray();
            var lessonsSql = "SELECT * FROM Lessons WHERE CourseId IN @Ids ORDER BY LessonNumber ASC";
            var lessons = (await db.QueryAsync<Lesson>(lessonsSql, new { Ids = courseIds })).ToList();

            var result = new List<CourseResponseDto>();
            foreach (var c in courses)
            {
                var dto = MapToDto(c);
                dto.Lessons = lessons.Where(l => l.CourseId == c.Id).Select(MapLessonToDto).ToList();
                result.Add(dto);
            }

            return result;
        }

        public async Task<CourseResponseDto?> GetCourseByIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();

            var course = await db.QueryFirstOrDefaultAsync<Course>("SELECT * FROM Courses WHERE Id = @Id", new { Id = id });
            if (course == null) return null;

            var lessons = await db.QueryAsync<Lesson>("SELECT * FROM Lessons WHERE CourseId = @CourseId ORDER BY LessonNumber ASC", new { CourseId = id });

            var dto = MapToDto(course);
            dto.Lessons = lessons.Select(MapLessonToDto).ToList();
            return dto;
        }

        public async Task<CourseResponseDto?> GetCourseBySlugAsync(string slug)
        {
            using var db = _connectionFactory.CreateConnection();

            var course = await db.QueryFirstOrDefaultAsync<Course>("SELECT * FROM Courses WHERE Slug = @Slug", new { Slug = slug });
            if (course == null) return null;

            var lessons = await db.QueryAsync<Lesson>("SELECT * FROM Lessons WHERE CourseId = @CourseId ORDER BY LessonNumber ASC", new { CourseId = course.Id });

            var dto = MapToDto(course);
            dto.Lessons = lessons.Select(MapLessonToDto).ToList();
            return dto;
        }

        public async Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Course" : dto.Title.Trim();
            var baseSlug = GenerateSlug(title);
            var slug = baseSlug;
            var counter = 1;

            while (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Courses WHERE Slug = @Slug", new { Slug = slug }) > 0)
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var driveLink = dto.DriveFolderLink?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(driveLink) && !driveLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !driveLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                driveLink = "https://" + driveLink;
            }

            var sql = @"
                INSERT INTO Courses (Title, Slug, Subject, Description, ThumbnailUrl, Price, DiscountPrice, ValidityDays, TotalVideos, DriveFolderLink, IsPublished, CreatedAt)
                VALUES (@Title, @Slug, @Subject, @Description, @ThumbnailUrl, @Price, @DiscountPrice, @ValidityDays, @TotalVideos, @DriveFolderLink, @IsPublished, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var courseId = await db.ExecuteScalarAsync<int>(sql, new
            {
                Title = title,
                Slug = slug,
                Subject = string.IsNullOrWhiteSpace(dto.Subject) ? "General" : dto.Subject.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                ThumbnailUrl = string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ? null : dto.ThumbnailUrl.Trim(),
                Price = dto.Price < 0 ? 0 : dto.Price,
                DiscountPrice = dto.DiscountPrice.HasValue && dto.DiscountPrice.Value >= 0 ? dto.DiscountPrice : null,
                ValidityDays = dto.ValidityDays <= 0 ? 365 : dto.ValidityDays,
                TotalVideos = dto.TotalVideos < 0 ? 0 : dto.TotalVideos,
                DriveFolderLink = driveLink,
                IsPublished = dto.IsPublished ? 1 : 0
            });

            return (await GetCourseByIdAsync(courseId))!;
        }

        public async Task<CourseResponseDto?> UpdateCourseAsync(int id, UpdateCourseDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var driveLink = dto.DriveFolderLink?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(driveLink) && !driveLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !driveLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                driveLink = "https://" + driveLink;
            }

            var sql = @"
                UPDATE Courses 
                SET Title = @Title,
                    Subject = @Subject,
                    Description = @Description,
                    ThumbnailUrl = @ThumbnailUrl,
                    Price = @Price,
                    DiscountPrice = @DiscountPrice,
                    ValidityDays = @ValidityDays,
                    TotalVideos = @TotalVideos,
                    DriveFolderLink = @DriveFolderLink,
                    IsPublished = @IsPublished,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id";

            var rows = await db.ExecuteAsync(sql, new
            {
                Id = id,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Course" : dto.Title.Trim(),
                Subject = string.IsNullOrWhiteSpace(dto.Subject) ? "General" : dto.Subject.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                ThumbnailUrl = string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ? null : dto.ThumbnailUrl.Trim(),
                Price = dto.Price < 0 ? 0 : dto.Price,
                DiscountPrice = dto.DiscountPrice.HasValue && dto.DiscountPrice.Value >= 0 ? dto.DiscountPrice : null,
                ValidityDays = dto.ValidityDays <= 0 ? 365 : dto.ValidityDays,
                TotalVideos = dto.TotalVideos < 0 ? 0 : dto.TotalVideos,
                DriveFolderLink = driveLink,
                IsPublished = dto.IsPublished ? 1 : 0
            });

            if (rows == 0) return null;
            return await GetCourseByIdAsync(id);
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            
            // Check if there are orders or enrollments attached to this course
            var orderCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders WHERE CourseId = @Id", new { Id = id });
            if (orderCount > 0)
            {
                // Soft-delete to avoid foreign key violation and preserve student purchase history
                var rows = await db.ExecuteAsync("UPDATE Courses SET IsPublished = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id", new { Id = id });
                return rows > 0;
            }

            // If no orders, hard delete lessons and course cleanly
            await db.ExecuteAsync("DELETE FROM Lessons WHERE CourseId = @Id", new { Id = id });
            var deleted = await db.ExecuteAsync("DELETE FROM Courses WHERE Id = @Id", new { Id = id });
            return deleted > 0;
        }

        public async Task<LessonResponseDto> AddLessonAsync(int courseId, CreateLessonDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var courseExists = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Courses WHERE Id = @Id", new { Id = courseId }) > 0;
            if (!courseExists)
            {
                throw new KeyNotFoundException($"Course with Id {courseId} not found.");
            }

            var sql = @"
                INSERT INTO Lessons (CourseId, LessonNumber, Title, Description, DurationMinutes, DriveVideoLink, IsFreePreview, CreatedAt)
                VALUES (@CourseId, @LessonNumber, @Title, @Description, @DurationMinutes, @DriveVideoLink, @IsFreePreview, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var lessonId = await db.ExecuteScalarAsync<int>(sql, new
            {
                CourseId = courseId,
                LessonNumber = dto.LessonNumber,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DurationMinutes = dto.DurationMinutes,
                DriveVideoLink = dto.DriveVideoLink?.Trim(),
                IsFreePreview = dto.IsFreePreview ? 1 : 0
            });

            // Update total video count
            await db.ExecuteAsync(@"
                UPDATE Courses 
                SET TotalVideos = (SELECT COUNT(*) FROM Lessons WHERE CourseId = @CourseId)
                WHERE Id = @CourseId", new { CourseId = courseId });

            return new LessonResponseDto
            {
                Id = lessonId,
                CourseId = courseId,
                LessonNumber = dto.LessonNumber,
                Title = dto.Title,
                Description = dto.Description,
                DurationMinutes = dto.DurationMinutes,
                DriveVideoLink = dto.DriveVideoLink,
                IsFreePreview = dto.IsFreePreview,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<LessonResponseDto?> UpdateLessonAsync(int lessonId, CreateLessonDto dto)
        {
            using var db = _connectionFactory.CreateConnection();

            var existing = await db.QueryFirstOrDefaultAsync<Lesson>("SELECT * FROM Lessons WHERE Id = @Id", new { Id = lessonId });
            if (existing == null) return null;

            var sql = @"
                UPDATE Lessons 
                SET LessonNumber = @LessonNumber,
                    Title = @Title,
                    Description = @Description,
                    DurationMinutes = @DurationMinutes,
                    DriveVideoLink = @DriveVideoLink,
                    IsFreePreview = @IsFreePreview
                WHERE Id = @Id";

            await db.ExecuteAsync(sql, new
            {
                Id = lessonId,
                LessonNumber = dto.LessonNumber,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DurationMinutes = dto.DurationMinutes,
                DriveVideoLink = dto.DriveVideoLink?.Trim(),
                IsFreePreview = dto.IsFreePreview ? 1 : 0
            });

            return new LessonResponseDto
            {
                Id = lessonId,
                CourseId = existing.CourseId,
                LessonNumber = dto.LessonNumber,
                Title = dto.Title,
                Description = dto.Description,
                DurationMinutes = dto.DurationMinutes,
                DriveVideoLink = dto.DriveVideoLink,
                IsFreePreview = dto.IsFreePreview,
                CreatedAt = existing.CreatedAt
            };
        }

        public async Task<bool> DeleteLessonAsync(int lessonId)
        {
            using var db = _connectionFactory.CreateConnection();
            var lesson = await db.QueryFirstOrDefaultAsync<Lesson>("SELECT CourseId FROM Lessons WHERE Id = @Id", new { Id = lessonId });
            var rows = await db.ExecuteAsync("DELETE FROM Lessons WHERE Id = @Id", new { Id = lessonId });

            if (rows > 0 && lesson != null)
            {
                await db.ExecuteAsync(@"
                    UPDATE Courses 
                    SET TotalVideos = (SELECT COUNT(*) FROM Lessons WHERE CourseId = @CourseId)
                    WHERE Id = @CourseId", new { CourseId = lesson.CourseId });
            }

            return rows > 0;
        }

        private static CourseResponseDto MapToDto(Course c)
        {
            return new CourseResponseDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Subject = c.Subject,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                ValidityDays = c.ValidityDays,
                TotalVideos = c.TotalVideos,
                DriveFolderLink = c.DriveFolderLink,
                IsPublished = c.IsPublished,
                CreatedAt = c.CreatedAt,
                Lessons = new List<LessonResponseDto>()
            };
        }

        private static LessonResponseDto MapLessonToDto(Lesson l)
        {
            return new LessonResponseDto
            {
                Id = l.Id,
                CourseId = l.CourseId,
                LessonNumber = l.LessonNumber,
                Title = l.Title,
                Description = l.Description,
                DurationMinutes = l.DurationMinutes,
                DriveVideoLink = l.DriveVideoLink,
                IsFreePreview = l.IsFreePreview,
                CreatedAt = l.CreatedAt
            };
        }

        private static string GenerateSlug(string title)
        {
            var slug = title.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }
    }
}
