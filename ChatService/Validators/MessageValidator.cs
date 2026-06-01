using System.Collections.Concurrent;

namespace ChatService.Validators
{
    /// <summary>
    /// Validates chat messages and enforces per-user rate limiting.
    /// </summary>
    public static class MessageValidator
    {
        private const int MaxMessageLength = 5000;
        private const int MaxMessagesPerMinute = 10;

        // Thread-safe rate-limit store: userId → (window start, count)
        private static readonly ConcurrentDictionary<string, (DateTime WindowStart, int Count)> _rateLimits = new();

        public static List<string> Validate(string? content, string userId)
        {
            var errors = new List<string>();

            // Empty check
            if (string.IsNullOrWhiteSpace(content))
            {
                errors.Add("Message cannot be empty.");
                return errors;
            }

            // Length check
            if (content.Length > MaxMessageLength)
                errors.Add($"Message cannot exceed {MaxMessageLength} characters.");

            // Rate limit check
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var now = DateTime.UtcNow;
                var entry = _rateLimits.GetOrAdd(userId, _ => (now, 0));

                if ((now - entry.WindowStart).TotalSeconds > 60)
                {
                    // Reset window
                    _rateLimits[userId] = (now, 1);
                }
                else
                {
                    entry = (entry.WindowStart, entry.Count + 1);
                    _rateLimits[userId] = entry;

                    if (entry.Count > MaxMessagesPerMinute)
                        errors.Add($"Rate limit exceeded. Maximum {MaxMessagesPerMinute} messages per minute.");
                }
            }

            return errors;
        }
    }
}
