using TutorService.Models;

namespace TutorService.Validators
{
    /// <summary>
    /// Server-side validators for TutorService request objects.
    /// </summary>
    public static class TutorValidator
    {
        // ── Tutor Profile ─────────────────────────────────────────────
        public static List<string> ValidateTutorProfile(TutorProfileRequest req)
        {
            var errors = new List<string>();

            if (req == null)
            {
                errors.Add("Request body cannot be empty.");
                return errors;
            }

            if (req.HourlyRate < 0)
                errors.Add("Hourly rate cannot be negative.");
            if (req.HourlyRate > 500)
                errors.Add("Hourly rate cannot exceed $500.");

            if (req.ExperienceYears < 0)
                errors.Add("Experience years cannot be negative.");
            if (req.ExperienceYears > 50)
                errors.Add("Experience years cannot exceed 50.");

            if (string.IsNullOrWhiteSpace(req.Specialization))
                errors.Add("Specialization is required.");

            if (string.IsNullOrWhiteSpace(req.Qualification))
                errors.Add("Qualification is required.");

            if (!string.IsNullOrEmpty(req.Bio) && req.Bio.Length > 2000)
                errors.Add("Bio cannot exceed 2000 characters.");

            // Validate availability slots
            if (req.Availability != null)
            {
                foreach (var avail in req.Availability)
                {
                    if (avail.DayOfWeek < 0 || avail.DayOfWeek > 6)
                        errors.Add($"DayOfWeek must be between 0 (Sunday) and 6 (Saturday). Got: {avail.DayOfWeek}.");

                    if (TimeSpan.TryParse(avail.StartTime, out var start) &&
                        TimeSpan.TryParse(avail.EndTime, out var end))
                    {
                        if (start >= end)
                            errors.Add($"Start time must be before end time for day {avail.DayOfWeek}.");
                    }
                }
            }

            return errors;
        }

        // ── Availability Request ──────────────────────────────────────
        public static List<string> ValidateAvailability(TutorAvailabilityRequest req)
        {
            var errors = new List<string>();

            if (req.DayOfWeek < 0 || req.DayOfWeek > 6)
                errors.Add("DayOfWeek must be between 0 (Sunday) and 6 (Saturday).");

            if (!TimeSpan.TryParse(req.StartTime, out var start))
                errors.Add("StartTime is not a valid time format (HH:mm).");

            if (!TimeSpan.TryParse(req.EndTime, out var end))
                errors.Add("EndTime is not a valid time format (HH:mm).");

            if (errors.Count == 0 && start >= end)
                errors.Add("StartTime must be before EndTime.");

            return errors;
        }
    }
}
