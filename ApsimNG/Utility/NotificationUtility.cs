using System;
using System.Net.Http;

namespace APSIMNG.Utility
{

    /// <summary> Utility class for handling notifications in the main view.</summary>
    public class NotificationUtility
    {
        /// <summary> Get the notification markdown text to be displayed.</summary>
        /// <returns>Markdown text.</returns>
        public static string GetNotificationMarkdownText()
        {
            try
            {
                // Fetch the markdown text from GitHub repository.
                using HttpClient client = new HttpClient();
                client.BaseAddress = new System.Uri("https://raw.githubusercontent.com/");
                string url = "APSIMInitiative/APSIM.Notifications/refs/heads/main/notifications.md";
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    string markdownText = response.Content.ReadAsStringAsync().Result;
                    return markdownText;
                }
                return "# Notifications Unavailable\n\nUnable to fetch notifications at this time.";
            }
            catch (Exception)
            {
                // If fetching fails, return a default message.
                return "# Notifications Unavailable\n\nUnable to fetch notifications at this time.";
            }

        }

        /// <summary> Get the AI banner path to be displayed.</summary>
        /// <returns>Path to the AI banner image.</returns>
        public static string GetAIBannerPath()
        {
            return "AIBanner-trimmed.png";
        }
    }
}