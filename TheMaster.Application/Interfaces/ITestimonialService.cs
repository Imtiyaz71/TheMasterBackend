using System.Collections.Generic;
using System.Threading.Tasks;
using TheMaster.Application.DTOs;

namespace TheMaster.Application.Interfaces
{
    public interface ITestimonialService
    {
        Task<IEnumerable<TestimonialResponseDto>> GetAllTestimonialsAsync(bool activeOnly = true);
        Task<TestimonialResponseDto?> GetTestimonialByIdAsync(int id);
        Task<TestimonialResponseDto> CreateTestimonialAsync(CreateTestimonialDto dto);
        Task<TestimonialResponseDto?> UpdateTestimonialAsync(int id, UpdateTestimonialDto dto);
        Task<bool> DeleteTestimonialAsync(int id);
    }
}
