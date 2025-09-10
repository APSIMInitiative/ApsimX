using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Models.Core;
using NUnit.Framework;
using UserInterface.Presenters;

namespace UnitTests
{
    /// <summary>
    /// Selenium-compatible HTML generation tests for MainView UI components.
    /// This test creates HTML content that could be tested with Selenium and validates its structure.
    /// Since this is a GTK desktop application, we generate a test page that represents the menu structure.
    /// </summary>
    [TestFixture]
    public class MainViewSeleniumCompatibilityTests
    {
        /// <summary>
        /// Test that generates HTML content representing MainView menu options
        /// and validates it contains the correct number of menu items.
        /// This demonstrates how the menu structure would appear to Selenium.
        /// </summary>
        [Test]
        public void TestMainViewMenuOptionsHtmlGeneration()
        {
            // Generate HTML content using reflection on MainMenu class
            var htmlContent = GenerateMainMenuHtml();
            
            // Validate that the HTML contains the expected number of menu items
            var menuItemOccurrences = CountOccurrences(htmlContent, "class=\"menu-item\"");
            
            Assert.That(menuItemOccurrences, Is.EqualTo(8), 
                $"Expected 8 menu items in generated HTML, but found {menuItemOccurrences}");
            
            // Validate that all expected menu items are present in the HTML
            var expectedMenuItems = new[]
            {
                "Save",
                "Save As", 
                "Undo",
                "Redo",
                "Split Screen",
                "Clear Status",
                "Help",
                "Run"
            };
            
            foreach (var expectedItem in expectedMenuItems)
            {
                Assert.That(htmlContent, Does.Contain(expectedItem), 
                    $"Expected menu item '{expectedItem}' not found in generated HTML");
            }
        }

        /// <summary>
        /// Test that demonstrates how Selenium would interact with the generated HTML.
        /// This creates a temporary HTML file and validates its structure.
        /// </summary>
        [Test]
        public void TestSeleniumReadableHtmlStructure()
        {
            var tempPath = Path.GetTempFileName() + ".html";
            
            try
            {
                // Generate the HTML file that Selenium would read
                var htmlContent = GenerateMainMenuHtml();
                File.WriteAllText(tempPath, htmlContent);
                
                // Validate the file was created and contains expected content
                Assert.That(File.Exists(tempPath), Is.True, "HTML file should be created");
                
                var fileContent = File.ReadAllText(tempPath);
                
                // Validate HTML structure that Selenium would find
                Assert.That(fileContent, Does.Contain("<!DOCTYPE html>"), "Should be valid HTML document");
                Assert.That(fileContent, Does.Contain("<title>APSIM-X Main Menu Items</title>"), "Should have descriptive title");
                Assert.That(fileContent, Does.Contain("class=\"menu-item\""), "Should contain menu-item CSS class for Selenium selection");
                Assert.That(fileContent, Does.Contain("Total Menu Items: 8"), "Should display correct count");
                
                // Count menu items using the same approach Selenium would use
                var menuItemCount = CountOccurrences(fileContent, "class=\"menu-item\"");
                Assert.That(menuItemCount, Is.EqualTo(8), 
                    $"Selenium would find {menuItemCount} menu items, expected 8");
            }
            finally
            {
                // Clean up
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        /// <summary>
        /// Test that validates the CSS structure for Selenium element selection.
        /// </summary>
        [Test]
        public void TestSeleniumCssSelectors()
        {
            var htmlContent = GenerateMainMenuHtml();
            
            // Verify that CSS classes needed for Selenium are present
            Assert.That(htmlContent, Does.Contain("class=\"menu-item\""), 
                "Should contain menu-item class for Selenium By.ClassName selection");
            Assert.That(htmlContent, Does.Contain("class=\"menu-count\""), 
                "Should contain menu-count class for validation");
            Assert.That(htmlContent, Does.Contain("id=\"menu-container\""), 
                "Should contain menu-container ID for Selenium By.Id selection");
        }

        /// <summary>
        /// Generates HTML content that represents the MainMenu structure
        /// in a format that Selenium can test.
        /// </summary>
        /// <returns>Complete HTML document as string</returns>
        private static string GenerateMainMenuHtml()
        {
            var menuItems = GetMainMenuItems();
            
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <title>APSIM-X Main Menu Items</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("        .menu-item { display: block; padding: 5px; margin: 2px; background-color: #f0f0f0; }");
            html.AppendLine("        .menu-count { font-weight: bold; color: #333; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <h1>APSIM-X Main Menu Items</h1>");
            html.AppendLine($"    <p class=\"menu-count\">Total Menu Items: {menuItems.Count}</p>");
            html.AppendLine("    <div id=\"menu-container\">");
            
            foreach (var menuItem in menuItems)
            {
                var hotkey = !string.IsNullOrEmpty(menuItem.Hotkey) ? $" ({menuItem.Hotkey})" : "";
                html.AppendLine($"        <div class=\"menu-item\">{menuItem.MenuName}{hotkey}</div>");
            }
            
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        /// <summary>
        /// Uses reflection to find all methods in MainMenu class that have the [MainMenu] attribute.
        /// </summary>
        /// <returns>List of MainMenuAttribute instances</returns>
        private static System.Collections.Generic.List<MainMenuAttribute> GetMainMenuItems()
        {
            var mainMenuType = typeof(MainMenu);
            var methods = mainMenuType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            var menuItems = new System.Collections.Generic.List<MainMenuAttribute>();
            
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<MainMenuAttribute>();
                if (attribute != null)
                {
                    menuItems.Add(attribute);
                }
            }
            
            return menuItems;
        }

        /// <summary>
        /// Helper method to count occurrences of a substring in a string.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="substring">Substring to count</param>
        /// <returns>Number of occurrences</returns>
        private static int CountOccurrences(string text, string substring)
        {
            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(substring, index)) != -1)
            {
                count++;
                index += substring.Length;
            }
            
            return count;
        }
    }
}