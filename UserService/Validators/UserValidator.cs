using System.Text.RegularExpressions;
using UserService.Models;

namespace UserService.Validators
{
    /// <summary>
    /// Server-side validators for all UserService request DTOs.
    /// No external packages required — uses plain C# regex and logic.
    /// </summary>
    public static class UserValidator
    {
        private static readonly Regex _emailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _phoneRegex =
            new(@"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.Compiled);

        // ── Registration ──────────────────────────────────────────────
        public static List<string> ValidateRegistration(RegisterRequest req)
        {
            var errors = new List<string>();

            // Email
            if (string.IsNullOrWhiteSpace(req.Email))
                errors.Add("Email is required.");
            else if (!_emailRegex.IsMatch(req.Email))
                errors.Add("Email format is invalid.");

            // Password
            if (string.IsNullOrWhiteSpace(req.Password))
                errors.Add("Password is required.");
            else if (req.Password.Length < 6)
                errors.Add("Password must be at least 6 characters.");
            else if (req.Password.Length > 50)
                errors.Add("Password cannot exceed 50 characters.");
            else if (!req.Password.Any(char.IsLetter) || !req.Password.Any(char.IsDigit))
                errors.Add("Password must contain at least one letter and one number.");

            // FullName
            if (string.IsNullOrWhiteSpace(req.FullName))
                errors.Add("Full name is required.");
            else if (req.FullName.Length > 100)
                errors.Add("Full name cannot exceed 100 characters.");

            // Role
            var validRoles = new[] { "Student", "Tutor", "Admin" };
            if (string.IsNullOrWhiteSpace(req.Role) || !validRoles.Contains(req.Role))
                errors.Add("Role must be Student, Tutor, or Admin.");

            // PhoneNumber (optional)
            if (!string.IsNullOrWhiteSpace(req.PhoneNumber) && !_phoneRegex.IsMatch(req.PhoneNumber))
                errors.Add("Phone number format is invalid.");

            return errors;
        }

        // ── Student-specific registration ─────────────────────────────
        public static List<string> ValidateStudentRegistration(StudentRegisterRequest req)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(req.Email))
                errors.Add("Email is required.");
            else if (!_emailRegex.IsMatch(req.Email))
                errors.Add("Email format is invalid.");

            if (string.IsNullOrWhiteSpace(req.Password))
                errors.Add("Password is required.");
            else if (req.Password.Length < 6)
                errors.Add("Password must be at least 6 characters.");
            else if (req.Password.Length > 50)
                errors.Add("Password cannot exceed 50 characters.");
            else if (!req.Password.Any(char.IsLetter) || !req.Password.Any(char.IsDigit))
                errors.Add("Password must contain at least one letter and one number.");

            if (string.IsNullOrWhiteSpace(req.FullName))
                errors.Add("Full name is required.");
            else if (req.FullName.Length > 100)
                errors.Add("Full name cannot exceed 100 characters.");

            if (!string.IsNullOrWhiteSpace(req.PhoneNumber) && !_phoneRegex.IsMatch(req.PhoneNumber))
                errors.Add("Phone number format is invalid.");

            return errors;
        }

        // ── Email format helper (used to check uniqueness after format) ─
        public static bool IsValidEmail(string email)
            => !string.IsNullOrWhiteSpace(email) && _emailRegex.IsMatch(email);

        // ── Phone format helper ────────────────────────────────────────
        public static bool IsValidPhone(string phone)
            => string.IsNullOrWhiteSpace(phone) || _phoneRegex.IsMatch(phone);
    }
}
