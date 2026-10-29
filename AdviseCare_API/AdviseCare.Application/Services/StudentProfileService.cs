using AdviseCare.Application.Contracts.Requests.StudentProfile;
using AdviseCare.Application.Contracts.Responses.StudentProfile;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // A student's profile: the directory data every staff role may read at any time, with no consent and no access log
    // (email, full name, username, faculty from ACCOUNTS + student code, academic status from STUDENTPROFILES).
    // Profiles are created together with the Student account (AccountService); only the admin edits them
    public class StudentProfileService : IStudentProfileService
    {
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IActivityLogService _activityLogService;

        public StudentProfileService(
            IGenericRepository<Studentprofile> studentProfileRepository,
            IActivityLogService activityLogService)
        {
            _studentProfileRepository = studentProfileRepository;
            _activityLogService = activityLogService;
        }

        // Get all Student profiles
        public async Task<IEnumerable<GetAllStudentProfileResponse>> GetAll()
        {
            var profiles = await GetProfilesAsync(p => true);

            return profiles.Select(p => new GetAllStudentProfileResponse
            {
                StudentProfileId = p.StudentProfileId,
                StudentId = p.StudentId,
                FacultyId = p.Student.FacultyId,
                Email = p.Student.Email,
                FacultyName = p.Student.Faculty?.FacultyName ?? string.Empty,
                FullName = p.Student.FullName,
                StudentCode = p.StudentCode,
                UserName = p.Student.UserName,
                AcademicStatus = p.AcademicStatus
            });
        }

        // Get all Student profiles with pagination
        public async Task<IEnumerable<GetAllStudentProfileWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var profiles = await GetProfilesAsync(p => true);

            return profiles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetAllStudentProfileWithPaginationResponse
                {
                    StudentProfileId = p.StudentProfileId,
                    StudentId = p.StudentId,
                    FacultyId = p.Student.FacultyId,
                    Email = p.Student.Email,
                    FacultyName = p.Student.Faculty?.FacultyName ?? string.Empty,
                    FullName = p.Student.FullName,
                    StudentCode = p.StudentCode,
                    UserName = p.Student.UserName,
                    AcademicStatus = p.AcademicStatus
                });
        }

        // Get Student profile by the student's account Id
        public async Task<GetStudentProfileByIdResponse> GetByIdAsync(Guid StudentId)
        {
            var profile = (await GetProfilesAsync(p => p.StudentId == StudentId)).FirstOrDefault()
                ?? throw new NotFoundException("Student profile not found.");

            return new GetStudentProfileByIdResponse
            {
                StudentProfileId = profile.StudentProfileId,
                StudentId = profile.StudentId,
                FacultyId = profile.Student.FacultyId,
                Email = profile.Student.Email,
                FacultyName = profile.Student.Faculty?.FacultyName ?? string.Empty,
                FullName = profile.Student.FullName,
                StudentCode = profile.StudentCode,
                UserName = profile.Student.UserName,
                AcademicStatus = profile.AcademicStatus
            };
        }

        // Search Student profiles by faculty, full name or student code (contains) and academic status
        public async Task<List<SearchStudentProfileResponse>> SearchAsync(SearchStudentProfileRequest request)
        {
            var profiles = await GetProfilesAsync(p =>
                (request.FacultyId == null || p.Student.FacultyId == request.FacultyId) &&
                (string.IsNullOrWhiteSpace(request.FullName) || p.Student.FullName.Contains(request.FullName)) &&
                (string.IsNullOrWhiteSpace(request.StudentCode) || p.StudentCode.Contains(request.StudentCode)) &&
                (string.IsNullOrWhiteSpace(request.AcademicStatus) || p.AcademicStatus == request.AcademicStatus));

            return profiles.Select(p => new SearchStudentProfileResponse
            {
                StudentProfileId = p.StudentProfileId,
                StudentId = p.StudentId,
                FacultyId = p.Student.FacultyId,
                Email = p.Student.Email,
                FacultyName = p.Student.Faculty?.FacultyName ?? string.Empty,
                FullName = p.Student.FullName,
                StudentCode = p.StudentCode,
                UserName = p.Student.UserName,
                AcademicStatus = p.AcademicStatus
            }).ToList();
        }

        // Update Student profile (student code and academic status), found by the student's account Id
        public async Task<UpdateStudentProfileResponse> UpdateAsync(Guid StudentId, UpdateStudentProfileRequest request)
        {
            var profile = (await GetProfilesAsync(p => p.StudentId == StudentId)).FirstOrDefault()
                ?? throw new NotFoundException("This account has no student profile (only Student accounts have one).");

            var studentCode = request.StudentCode.Trim();
            if (await _studentProfileRepository.ExistsAsync(p => p.StudentCode == studentCode && p.StudentProfileId != profile.StudentProfileId))
                throw new ConflictException($"A student with student code '{studentCode}' already exists.");

            var changes = new List<string>();
            if (profile.StudentCode != studentCode)
                changes.Add($"student code {profile.StudentCode} -> {studentCode}");
            if (profile.AcademicStatus != request.AcademicStatus)
                changes.Add($"academic status {profile.AcademicStatus} -> {request.AcademicStatus}");

            profile.StudentCode = studentCode;
            profile.AcademicStatus = request.AcademicStatus;
            await _studentProfileRepository.SaveAsync();

            if (changes.Count > 0)
                await _activityLogService.LogCurrentAccountAsync("studentProfileUpdated", $"Updated the student profile of {profile.Student.Email}: {string.Join(", ", changes)}");

            return new UpdateStudentProfileResponse
            {
                StudentProfileId = profile.StudentProfileId,
                StudentId = profile.StudentId,
                FacultyId = profile.Student.FacultyId,
                Email = profile.Student.Email,
                FacultyName = profile.Student.Faculty?.FacultyName ?? string.Empty,
                FullName = profile.Student.FullName,
                StudentCode = profile.StudentCode,
                UserName = profile.Student.UserName,
                AcademicStatus = profile.AcademicStatus
            };
        }

        // Get the signed-in student's own profile
        public async Task<GetMyStudentProfileResponse> GetMyProfileAsync(Guid StudentId)
        {
            var profile = (await GetProfilesAsync(p => p.StudentId == StudentId)).FirstOrDefault()
                ?? throw new NotFoundException("Your student profile has not been set up yet.");

            return new GetMyStudentProfileResponse
            {
                StudentProfileId = profile.StudentProfileId,
                StudentId = profile.StudentId,
                FacultyId = profile.Student.FacultyId,
                Email = profile.Student.Email,
                FacultyName = profile.Student.Faculty?.FacultyName ?? string.Empty,
                FullName = profile.Student.FullName,
                StudentCode = profile.StudentCode,
                UserName = profile.Student.UserName,
                AcademicStatus = profile.AcademicStatus
            };
        }

        // Helper: load Student profiles with the student's account and faculty, ordered by student code
        private async Task<List<Studentprofile>> GetProfilesAsync(Expression<Func<Studentprofile, bool>> predicate)
        {
            return await _studentProfileRepository.SearchAsyncIncludeOrderBy(predicate, p => p.StudentCode, true,
                p => p.Student, p => p.Student.Faculty);
        }
    }
}
