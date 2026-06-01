using BookingService.Models;

namespace BookingService.Validators
{
    /// <summary>
    /// Validates BookingRequest objects before they are persisted.
    /// Returns a list of field-level errors in the standard MentoraX format.
    /// </summary>
    public static class BookingValidator
    {
        public static List<ValidationError> Validate(BookingRequest request)
        {
            var errors = new List<ValidationError>();

            if (request == null)
            {
                errors.Add(new ValidationError("Request", "Request body cannot be empty."));
                return errors;
            }

            // DurationHours — stored as int in the model, but we treat 0 as invalid.
            // Min 1 hour (the model uses int; decimal 0.5 support can be added by changing the model type).
            if (request.DurationHours <= 0)
                errors.Add(new ValidationError("DurationHours", "Duration must be at least 1 hour."));

            if (request.DurationHours > 4)
                errors.Add(new ValidationError("DurationHours", "Duration cannot exceed 4 hours."));

            // BookingDate must not be in the past
            if (request.BookingDate != default && request.BookingDate < DateTime.UtcNow.Date)
                errors.Add(new ValidationError("BookingDate", "Booking date cannot be in the past."));

            // BookingDate must be within 30 days from today
            if (request.BookingDate != default && request.BookingDate > DateTime.UtcNow.Date.AddDays(30))
                errors.Add(new ValidationError("BookingDate", "Booking date must be within the next 30 days."));

            // StudentID required
            if (string.IsNullOrWhiteSpace(request.StudentID))
                errors.Add(new ValidationError("StudentID", "StudentID is required."));

            // TutorID required
            if (string.IsNullOrWhiteSpace(request.TutorID))
                errors.Add(new ValidationError("TutorID", "TutorID is required."));

            // Topic max 500 chars
            if (!string.IsNullOrEmpty(request.Topic) && request.Topic.Length > 500)
                errors.Add(new ValidationError("Topic", "Topic cannot exceed 500 characters."));

            // Notes max 1000 chars
            if (!string.IsNullOrEmpty(request.Notes) && request.Notes.Length > 1000)
                errors.Add(new ValidationError("Notes", "Notes cannot exceed 1000 characters."));

            return errors;
        }
    }

    /// <summary>
    /// Validates ReviewRequest objects.
    /// </summary>
    public static class ReviewValidator
    {
        public static List<ValidationError> Validate(ReviewRequest request)
        {
            var errors = new List<ValidationError>();

            if (request == null)
            {
                errors.Add(new ValidationError("Request", "Request body cannot be empty."));
                return errors;
            }

            if (request.Rating < 1 || request.Rating > 5)
                errors.Add(new ValidationError("Rating", "Rating must be between 1 and 5."));

            if (string.IsNullOrWhiteSpace(request.BookingID))
                errors.Add(new ValidationError("BookingID", "BookingID is required."));

            if (!string.IsNullOrEmpty(request.Comment) && request.Comment.Length > 1000)
                errors.Add(new ValidationError("Comment", "Comment cannot exceed 1000 characters."));

            return errors;
        }
    }

    /// <summary>
    /// Standardised validation error payload (matches Part 8 of the spec).
    /// </summary>
    public class ValidationError
    {
        public string Field   { get; set; }
        public string Message { get; set; }

        public ValidationError(string field, string message)
        {
            Field   = field;
            Message = message;
        }
    }
}
