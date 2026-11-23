using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheMaster.Application.Common;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;

namespace TheMaster.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IWebHostEnvironment _env;

        public CoursesController(ICourseService courseService, IWebHostEnvironment env)
        {
            _courseService = courseService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool publicOnly = true)
        {
            var courses = await _courseService.GetAllCoursesAsync(publicOnly);
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.Ok(courses));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null) return NotFound(ApiResponse<object>.Fail("Course not found.", 404));
            return Ok(ApiResponse<CourseResponseDto>.Ok(course));
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var course = await _courseService.GetCourseBySlugAsync(slug);
            if (course == null) return NotFound(ApiResponse<object>.Fail("Course not found.", 404));
            return Ok(ApiResponse<CourseResponseDto>.Ok(course));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            var created = await _courseService.CreateCourseAsync(dto);
            return StatusCode(201, ApiResponse<CourseResponseDto>.Ok(created, "Course created successfully.", 201));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseDto dto)
        {
            var updated = await _courseService.UpdateCourseAsync(id, dto);
            if (updated == null) return NotFound(ApiResponse<object>.Fail("Course not found.", 404));
            return Ok(ApiResponse<CourseResponseDto>.Ok(updated, "Course updated successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _courseService.DeleteCourseAsync(id);
            if (!success) return NotFound(ApiResponse<object>.Fail("Course not found.", 404));
            return Ok(ApiResponse<bool>.Ok(true, "Course deleted successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/lessons")]
        public async Task<IActionResult> AddLesson(int id, [FromBody] CreateLessonDto dto)
        {
            var lesson = await _courseService.AddLessonAsync(id, dto);
            return Ok(ApiResponse<LessonResponseDto>.Ok(lesson, "Lesson added successfully.", 201));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("lessons/{lessonId:int}")]
        public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] CreateLessonDto dto)
        {
            var updated = await _courseService.UpdateLessonAsync(lessonId, dto);
            if (updated == null) return NotFound(ApiResponse<object>.Fail("Lesson not found.", 404));
            return Ok(ApiResponse<LessonResponseDto>.Ok(updated, "Lesson updated successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("lessons/{lessonId:int}")]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var success = await _courseService.DeleteLessonAsync(lessonId);
            if (!success) return NotFound(ApiResponse<object>.Fail("Lesson not found.", 404));
            return Ok(ApiResponse<bool>.Ok(true, "Lesson deleted successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadThumbnail(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("No file was uploaded.", 400));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid image format. Allowed formats: JPG, PNG, WEBP, GIF.", 400));
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<object>.Fail("File size exceeds 10MB limit.", 400));
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "thumbnails");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueName = $"thumb_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var publicUrl = $"{baseUrl}/uploads/thumbnails/{uniqueName}";

            return Ok(ApiResponse<object>.Ok(new { url = publicUrl, relativeUrl = $"/uploads/thumbnails/{uniqueName}" }, "Thumbnail uploaded successfully."));
        }
    }
}
