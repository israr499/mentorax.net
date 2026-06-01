using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorService.Data;
using TutorService.Models;
using System.Text.Json;

namespace TutorService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly TutorDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TutorController(TutorDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // POST: api/Tutor/CreateProfile
        [HttpPost("CreateProfile")]
        public async Task<ActionResult> CreateProfile([FromBody] TutorProfileRequest request)
        {
            // Check if tutor profile already exists
            var existingTutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserID == request.UserID);
            if (existingTutor != null)
            {
                return BadRequest(new { message = "Tutor profile already exists" });
            }

            // Create tutor
            var tutor = new Tutor
            {
                TutorID = Guid.NewGuid().ToString(),
                UserID = request.UserID,
                Specialization = request.Specialization,
                Qualification = request.Qualification,
                ExperienceYears = request.ExperienceYears,
                HourlyRate = request.HourlyRate,
                Status = "Pending",
                Bio = request.Bio,
                TeachingStyle = request.TeachingStyle,
                EducationDetails = request.EducationDetails,
                IntroVideoUrl = request.IntroVideoUrl  // ← ADD THIS LINE
            };

            _context.Tutors.Add(tutor);
            await _context.SaveChangesAsync();

            // Add subjects
            if (request.Subjects != null)
            {
                foreach (var subjectReq in request.Subjects)
                {
                    var subject = new TutorSubject
                    {
                        TutorSubjectID = Guid.NewGuid().ToString(),
                        TutorID = tutor.TutorID,
                        SubjectName = subjectReq.SubjectName,
                        GradeLevel = subjectReq.GradeLevel,
                        ExperienceInSubject = subjectReq.ExperienceInSubject
                    };
                    _context.TutorSubjects.Add(subject);
                }
            }

            // Add availability
            if (request.Availability != null)
            {
                foreach (var availReq in request.Availability)
                {
                    var availability = new TutorAvailability
                    {
                        AvailabilityID = Guid.NewGuid().ToString(),
                        TutorID = tutor.TutorID,
                        DayOfWeek = availReq.DayOfWeek,
                        StartTime = TimeSpan.Parse(availReq.StartTime),
                        EndTime = TimeSpan.Parse(availReq.EndTime),
                        IsRecurring = true
                    };
                    _context.TutorAvailabilities.Add(availability);

                    // Generate time slots for next 30 days
                    await GenerateTimeSlots(tutor.TutorID, availability);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Tutor profile created successfully. Waiting for admin approval.", tutorId = tutor.TutorID });
        }

        // Helper method to generate time slots
        private async Task GenerateTimeSlots(string tutorId, TutorAvailability availability)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(30);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if ((int)date.DayOfWeek == availability.DayOfWeek)
                {
                    var slot = new TimeSlot
                    {
                        SlotID = Guid.NewGuid().ToString(),
                        TutorID = tutorId,
                        SlotDate = date,
                        StartTime = availability.StartTime,
                        EndTime = availability.EndTime,
                        IsBooked = false
                    };
                    _context.TimeSlots.Add(slot);
                }
            }
        }

        // GET: api/Tutor/GetAll
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<Tutor>>> GetAll()
        {
            return await _context.Tutors.ToListAsync();
        }

        // GET: api/Tutor/GetPending
        [HttpGet("GetPending")]
        public async Task<ActionResult<IEnumerable<Tutor>>> GetPending()
        {
            return await _context.Tutors.Where(t => t.Status == "Pending").ToListAsync();
        }

        // GET: api/Tutor/GetApproved
        [HttpGet("GetApproved")]
        public async Task<ActionResult<IEnumerable<Tutor>>> GetApproved()
        {
            return await _context.Tutors.Where(t => t.Status == "Approved").ToListAsync();
        }

        // GET: api/Tutor/GetById/{id}
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<object>> GetById(string id)
        {
            var tutor = await _context.Tutors.FindAsync(id);
            if (tutor == null)
                return NotFound();

            var client = _httpClientFactory.CreateClient("UserService");
            string fullName = tutor.UserID;
            string email = "";

            try
            {
                var userResponse = await client.GetAsync($"api/User/GetById/{tutor.UserID}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var userJson = await userResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(userJson);
                    fullName = doc.RootElement.GetProperty("fullName").GetString();
                    email = doc.RootElement.GetProperty("email").GetString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return Ok(new
            {
                tutor.TutorID,
                tutor.UserID,
                FullName = fullName,
                Email = email,
                tutor.Specialization,
                tutor.Qualification,
                tutor.ExperienceYears,
                tutor.HourlyRate,
                tutor.Status,
                tutor.Bio,
                tutor.TeachingStyle,
                tutor.EducationDetails,
                IntroVideoUrl = tutor.IntroVideoUrl  // ← ADD THIS LINE
            });
        }

        // GET: api/Tutor/GetByUser/{userId}
        [HttpGet("GetByUser/{userId}")]
        public async Task<ActionResult<Tutor>> GetByUser(string userId)
        {
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserID == userId);
            if (tutor == null)
                return NotFound();
            return tutor;
        }

        [HttpGet("GetFullProfile/{tutorId}")]
        public async Task<ActionResult<object>> GetFullProfile(string tutorId)
        {
            // 1. Get tutor from database
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null)
            {
                return NotFound(new { message = $"Tutor with ID {tutorId} not found" });
            }

            // 2. Get user info from UserService
            // FIX: Use a custom HttpClient that ignores SSL cert errors (dev only).
            // The named 'UserService' client fails silently because ASP.NET's HttpClient
            // rejects the self-signed dev cert on https://localhost:7292.
            // Also changed fallback from tutor.UserID to "" so the MVC layer can detect
            // a missing name and re-fetch it rather than displaying a UUID.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7292/")
            };

            string fullName = "";
            string email = "";
            string profileImageUrl = "";

            try
            {
                var userResponse = await httpClient.GetAsync($"api/User/GetById/{tutor.UserID}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var userJson = await userResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(userJson);
                    fullName = doc.RootElement.TryGetProperty("fullName", out var fn)
                        ? fn.GetString() ?? ""
                        : doc.RootElement.TryGetProperty("FullName", out var fn2)
                            ? fn2.GetString() ?? ""
                            : "";
                    email = doc.RootElement.TryGetProperty("email", out var em)
                        ? em.GetString() ?? ""
                        : "";
                    profileImageUrl = doc.RootElement.TryGetProperty("profileImageUrl", out var pi)
                        ? pi.GetString() ?? ""
                        : "";
                    Console.WriteLine($"[GetFullProfile] Resolved name: {fullName} for user {tutor.UserID}");
                }
                else
                {
                    Console.WriteLine($"[GetFullProfile] UserService returned {userResponse.StatusCode} for {tutor.UserID}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFullProfile] UserService call failed: {ex.Message}");
            }

            // 3. Get subjects from database
            var subjectsFromDb = await _context.TutorSubjects
                .Where(s => s.TutorID == tutorId)
                .ToListAsync();

            var subjects = subjectsFromDb.Select(s => new
            {
                s.TutorSubjectID,
                s.SubjectName,
                s.GradeLevel,
                s.ExperienceInSubject
            }).ToList();

            // 4. Get availability from database
            var availabilityFromDb = await _context.TutorAvailabilities
                .Where(a => a.TutorID == tutorId)
                .ToListAsync();

            var availability = availabilityFromDb.Select(a => new
            {
                a.AvailabilityID,
                a.DayOfWeek,
                DayName = GetDayName(a.DayOfWeek),
                StartTime = a.StartTime.ToString(),
                EndTime = a.EndTime.ToString()
            }).ToList();

            // 5. Return complete response
            var result = new
            {
                tutor.TutorID,
                tutor.UserID,
                FullName = fullName,
                Email = email,
                ProfileImageUrl = profileImageUrl,
                tutor.Specialization,
                tutor.Qualification,
                tutor.ExperienceYears,
                tutor.HourlyRate,
                tutor.Status,
                tutor.Bio,
                tutor.TeachingStyle,
                tutor.EducationDetails,
                IntroVideoUrl = tutor.IntroVideoUrl,
                Subjects = subjects,
                Availability = availability
            };

            return Ok(result);
        }

        private string GetDayName(int dayOfWeek)
        {
            string[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            return days[dayOfWeek];
        }

        [HttpGet("GetAvailableSlots/{tutorId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableSlots(string tutorId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? start.AddDays(14);

            var slotsFromDb = await _context.TimeSlots
                .Where(s => s.TutorID == tutorId && s.SlotDate >= start && s.SlotDate <= end && !s.IsBooked)
                .OrderBy(s => s.SlotDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var slots = slotsFromDb.Select(s => new
            {
                s.SlotID,
                s.SlotDate,
                StartTime = s.StartTime.ToString(),
                EndTime = s.EndTime.ToString(),
                s.IsBooked
            }).ToList();

            return Ok(slots);
        }

        // GET: api/Tutor/SearchBySubject
        [HttpGet("SearchBySubject")]
        public async Task<ActionResult<IEnumerable<Tutor>>> SearchBySubject([FromQuery] string subject)
        {
            if (string.IsNullOrEmpty(subject))
                return await GetApproved();

            var tutorIds = await _context.TutorSubjects
                .Where(s => s.SubjectName.Contains(subject))
                .Select(s => s.TutorID)
                .Distinct()
                .ToListAsync();

            var tutors = await _context.Tutors
                .Where(t => tutorIds.Contains(t.TutorID) && t.Status == "Approved")
                .ToListAsync();

            return Ok(tutors);
        }

        // PUT: api/Tutor/Approve
        [HttpPut("Approve")]
        public async Task<ActionResult> Approve([FromBody] TutorApprovalRequest request)
        {
            var tutor = await _context.Tutors.FindAsync(request.TutorID);
            if (tutor == null)
                return NotFound();

            tutor.Status = request.IsApproved ? "Approved" : "Rejected";
            tutor.AdminApprovedBy = request.AdminID;
            tutor.AdminApprovedAt = DateTime.UtcNow;
            tutor.RejectionReason = request.RejectionReason;

            await _context.SaveChangesAsync();
            return Ok(tutor);
        }

        // PUT: api/Tutor/Update
        [HttpPut("Update")]
        public async Task<ActionResult> Update([FromBody] Tutor tutor)
        {
            _context.Entry(tutor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(tutor);
        }

        // DELETE: api/Tutor/Delete/{id}
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var tutor = await _context.Tutors.FindAsync(id);
            if (tutor == null)
                return NotFound();

            _context.Tutors.Remove(tutor);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}