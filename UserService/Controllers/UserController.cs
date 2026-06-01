using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UserService.Data;
using UserService.Models;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(UserDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ========== REGISTER (GENERIC) ==========
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User already exists with this email" });
            }

            var user = new User
            {
                UserID = Guid.NewGuid().ToString(),
                Email = request.Email,
                PasswordHash = request.Password,
                FullName = request.FullName,
                Role = request.Role,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserID = user.UserID,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        // ========== REGISTER STUDENT ==========
        [HttpPost("RegisterStudent")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> RegisterStudent([FromBody] StudentRegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User already exists with this email" });
            }

            var user = new User
            {
                UserID = Guid.NewGuid().ToString(),
                Email = request.Email,
                PasswordHash = request.Password,
                FullName = request.FullName,
                Role = "Student",
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // FIX: Use a single transaction so User is committed before Student.
            // Without this, EF Core can batch both inserts into one round-trip,
            // causing FK_Students_UserID violation because SQL Server sees the
            // Student INSERT before the User row exists.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();  // User committed first

                var student = new Student
                {
                    StudentID = Guid.NewGuid().ToString(),
                    UserID = user.UserID,
                    EducationLevel = request.EducationLevel,
                    SubjectsOfInterest = request.SubjectsOfInterest,
                    GradeLevel = request.GradeLevel
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();  // Student committed after User exists

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserID = user.UserID,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        // ========== LOGIN ==========
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.PasswordHash != request.Password)
                return Unauthorized(new { message = "Invalid email or password" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is deactivated" });

            // Admin bypasses OTP entirely
            if (user.Role == "Admin")
            {
                var token = GenerateJwtToken(user);
                return Ok(new LoginResponse
                {
                    RequiresOtp = false,
                    Token = token,
                    UserID = user.UserID,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    ProfileImageUrl = user.ProfileImageUrl
                });
            }

            // All other users get OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otpCode;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            user.IsOtpVerified = false;
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                RequiresOtp = true,
                PendingUserId = user.UserID,
                Email = user.Email,
                OtpCode = otpCode   // AccountController will send this via email
            });
        }

        // ========== GET ALL USERS ==========
        [HttpGet("GetAll")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // ========== GET USER BY ID ==========
        [HttpGet("GetById/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> GetById(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        // ========== RESET PASSWORD ==========
        [HttpPut("ResetPassword/{id}")]
        public async Task<ActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.PasswordHash = request.Password;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }

        // ========== DEACTIVATE USER ==========
        [HttpPut("Deactivate/{id}")]
        public async Task<ActionResult> Deactivate(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deactivated successfully" });
        }

        // ========== ACTIVATE USER ==========
        [HttpPut("Activate/{id}")]
        public async Task<ActionResult> Activate(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User activated successfully" });
        }

        // ========== GET STUDENT BY USER ID ==========
        [HttpGet("GetStudent/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult<Student>> GetStudent(string userId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
                return NotFound(new { message = "Student profile not found" });
            return Ok(student);
        }

        // ========== GET STUDENT BY STUDENT ID ==========
        [HttpGet("GetStudentById/{studentId}")]
        [AllowAnonymous]
        public async Task<ActionResult<Student>> GetStudentById(string studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            return Ok(student);
        }

        // ========== GET ALL STUDENTS ==========
        [HttpGet("GetAllStudents")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetAllStudents()
        {
            var students = from u in _context.Users
                           join s in _context.Students on u.UserID equals s.UserID
                           where u.Role == "Student"
                           select new
                           {
                               u.UserID,
                               u.Email,
                               u.FullName,
                               u.PhoneNumber,
                               u.ProfileImageUrl,
                               u.IsActive,
                               u.CreatedAt,
                               s.StudentID,
                               s.EducationLevel,
                               s.SubjectsOfInterest,
                               s.GradeLevel,
                               s.PreferredLanguage
                           };

            return Ok(await students.ToListAsync());
        }

        // ========== GET USER BY EMAIL ==========
        [HttpGet("GetByEmail/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> GetByEmail(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        // ========== UPDATE USER STATUS ==========
        [HttpPatch("UpdateStatus/{id}/{isActive}")]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateStatus(string id, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsActive = isActive;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User status updated to {(isActive ? "Active" : "Inactive")}" });
        }

        // ========== UPDATE USER PROFILE ==========
        [HttpPut("UpdateProfile")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> UpdateProfile([FromBody] User updatedUser)
        {
            var existingUser = await _context.Users.FindAsync(updatedUser.UserID);
            if (existingUser == null)
                return NotFound(new { message = "User not found" });

            existingUser.FullName = updatedUser.FullName;
            existingUser.PhoneNumber = updatedUser.PhoneNumber;
            existingUser.ProfileImageUrl = updatedUser.ProfileImageUrl;

            _context.Entry(existingUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(existingUser);
        }

        // ========== DELETE USER ==========
        [HttpDelete("Delete/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // ========== PUBLIC TEST ENDPOINT ==========
        [HttpGet("Test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new { message = "UserService is running!", timestamp = DateTime.Now });
        }

        // ========== REQUEST MODELS ==========
        public class ResetPasswordRequest
        {
            public string Password { get; set; }
        }

        // PUT: api/User/UpdateTwoFactorSecret/{id}
        [HttpPut("UpdateTwoFactorSecret/{id}")]
        public async Task<ActionResult> UpdateTwoFactorSecret(string id, [FromBody] JsonElement data)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var secretKey = data.GetProperty("secretKey").GetString();
            user.TwoFactorSecret = secretKey;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/User/EnableTwoFactor/{id}
        [HttpPut("EnableTwoFactor/{id}")]
        public async Task<ActionResult> EnableTwoFactor(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsTwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/User/SaveRememberToken/{id}
        [HttpPost("SaveRememberToken/{id}")]
        public async Task<ActionResult> SaveRememberToken(string id, [FromBody] JsonElement data)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var token = data.GetProperty("token").GetString();
            var expiry = data.GetProperty("expiry").GetDateTime();

            user.RememberToken = token;
            user.RememberTokenExpiry = expiry;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/User/VerifyRememberToken/{id}?token=xxx
        [HttpGet("VerifyRememberToken/{id}")]
        public async Task<ActionResult<object>> VerifyRememberToken(string id, string token)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Ok(new { isValid = false });

            if (user.RememberToken != token)
                return Ok(new { isValid = false });

            if (user.RememberTokenExpiry < DateTime.UtcNow)
                return Ok(new { isValid = false, message = "Remember token expired" });

            return Ok(new { isValid = true });
        }

        // PUT: api/User/DisableTwoFactor/{id}
        [HttpPut("DisableTwoFactor/{id}")]
        public async Task<ActionResult> DisableTwoFactor(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/User/GetTwoFactorStatus/{id}
        [HttpGet("GetTwoFactorStatus/{id}")]
        public async Task<ActionResult<object>> GetTwoFactorStatus(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                isEnabled = user.IsTwoFactorEnabled,
                hasSecret = !string.IsNullOrEmpty(user.TwoFactorSecret)
            });
        }

        //

        // PUT: api/User/SaveOtp/{id}
        [HttpPut("SaveOtp/{id}")]
        public async Task<ActionResult> SaveOtp(string id, [FromBody] JsonElement data)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.OtpCode = data.GetProperty("otpCode").GetString();
            user.OtpExpiry = data.GetProperty("otpExpiry").GetDateTime();
            user.IsOtpVerified = false;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/User/VerifyOtp
        // Body: { pendingUserId, otpCode }
        [HttpPost("VerifyOtp")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PendingUserId) || string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequest(new { message = "Missing required fields." });

            var user = await _context.Users.FindAsync(request.PendingUserId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.OtpCode != request.OtpCode.Trim())
                return BadRequest(new { message = "Invalid OTP code. Please try again." });

            if (user.OtpExpiry == null || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "OTP has expired. Please request a new one." });

            // OTP valid
            user.IsOtpVerified = true;
            user.OtpCode = null;
            user.OtpExpiry = null;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                UserID = user.UserID,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        // POST: api/User/ResendOtp/{pendingUserId}
        [HttpPost("ResendOtp/{pendingUserId}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> ResendOtp(string pendingUserId)
        {
            var user = await _context.Users.FindAsync(pendingUserId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var otpCode = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otpCode;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            user.IsOtpVerified = false;
            await _context.SaveChangesAsync();

            return Ok(new { otpCode, email = user.Email });
        }
        // ========== GENERATE JWT TOKEN ==========
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}