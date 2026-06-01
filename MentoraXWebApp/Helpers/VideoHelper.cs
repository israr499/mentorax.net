namespace MentoraXWebApp.Helpers
{
    public static class VideoHelper
    {
        public static string GetEmbedUrl(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl))
                return "";

            // YouTube
            if (videoUrl.Contains("youtube.com/watch?v="))
            {
                var videoId = videoUrl.Split("v=")[1].Split('&')[0];
                return $"https://www.youtube.com/embed/{videoId}";
            }

            // YouTube Short URL (youtu.be)
            if (videoUrl.Contains("youtu.be/"))
            {
                var videoId = videoUrl.Split("youtu.be/")[1].Split('?')[0];
                return $"https://www.youtube.com/embed/{videoId}";
            }

            // Vimeo
            if (videoUrl.Contains("vimeo.com"))
            {
                var videoId = videoUrl.Split("vimeo.com/")[1].Split('?')[0];
                return $"https://player.vimeo.com/video/{videoId}";
            }

            // If already an embed URL
            if (videoUrl.Contains("embed"))
                return videoUrl;

            return videoUrl;
        }
    }
}