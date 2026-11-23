using System.Collections.Generic;
using System.Threading.Tasks;
using TheMaster.Application.DTOs;

namespace TheMaster.Application.Interfaces
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync(bool publicOnly = true);
        Task<CourseResponseDto?> GetCourseByIdAsync(int id);
        Task<CourseResponseDto?> GetCourseBySlugAsync(string slug);
        Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseResponseDto?> UpdateCourseAsync(int id, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int id);
        Task<LessonResponseDto> AddLessonAsync(int courseId, CreateLessonDto dto);
        Task<LessonResponseDto?> UpdateLessonAsync(int lessonId, CreateLessonDto dto);
        Task<bool> DeleteLessonAsync(int lessonId);
    }
}
