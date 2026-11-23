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
    public class TestimonialsController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;
        private readonly IWebHostEnvironment _env;

        public TestimonialsController(ITestimonialService testimonialService, IWebHostEnvironment env)
        {
            _testimonialService = testimonialService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
        {
            var items = await _testimonialService.GetAllTestimonialsAsync(activeOnly);
            return Ok(ApiResponse<IEnumerable<TestimonialResponseDto>>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _testimonialService.GetTestimonialByIdAsync(id);
            if (item == null) return NotFound(ApiResponse<object>.Fail("Testimonial not found.", 404));
            return Ok(ApiResponse<TestimonialResponseDto>.Ok(item));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTestimonialDto dto)
        {
            var created = await _testimonialService.CreateTestimonialAsync(dto);
            return StatusCode(201, ApiResponse<TestimonialResponseDto>.Ok(created, "Testimonial created successfully.", 201));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTestimonialDto dto)
        {
            var updated = await _testimonialService.UpdateTestimonialAsync(id, dto);
            if (updated == null) return NotFound(ApiResponse<object>.Fail("Testimonial not found.", 404));
            return Ok(ApiResponse<TestimonialResponseDto>.Ok(updated, "Testimonial updated successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _testimonialService.DeleteTestimonialAsync(id);
            if (!success) return NotFound(ApiResponse<object>.Fail("Testimonial not found.", 404));
            return Ok(ApiResponse<bool>.Ok(true, "Testimonial deleted successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("No file was uploaded.", 400));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid image format. Allowed formats: JPG, PNG, WEBP.", 400));
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<object>.Fail("File size exceeds 5MB limit.", 400));
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueName = $"avatar_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var publicUrl = $"{baseUrl}/uploads/avatars/{uniqueName}";

            return Ok(ApiResponse<object>.Ok(new { url = publicUrl, relativeUrl = $"/uploads/avatars/{uniqueName}" }, "Avatar uploaded successfully."));
        }
    }
}
