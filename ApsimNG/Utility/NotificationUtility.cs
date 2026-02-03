

using System.Net.Http;

namespace APSIMNG.Utility
{

    /// <summary>
    /// Utility class for handling notifications in the main view.
    /// </summary>
    public class NotificationUtility
    {
        /// <summary>
        /// Get the notification markdown text to be displayed.
        /// </summary>
        /// <returns>Markdown text.</returns>
        public string GetNotificationMarkdownText()
        {
            // Fetch the markdown text from GitHub repository.
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri("https://raw.githubusercontent.com/");
            string url = "APSIMInitiative/APSIM.Notifications/refs/heads/main/notifications.md";
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string markdownText = response.Content.ReadAsStringAsync().Result;
                markdownText += "Example Picture\n![Row Alley](RowAlley.png)";
                return markdownText;
            }
            else
            {
                // If fetching fails, return a default message.
                return "# Notifications Unavailable\n\nUnable to fetch notifications at this time.";
            }
        }

    }
}