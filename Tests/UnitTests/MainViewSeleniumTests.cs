using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Models.Core;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using UserInterface.Presenters;

namespace UnitTests
{
    /// <summary>
    /// Selenium tests for MainView UI components.
    /// Since APSIM-X is a GTK desktop application, this test creates a temporary HTML page
    /// that reflects the menu structure and tests it with Selenium.
    /// </summary>
    [TestFixture]
    public class MainViewSeleniumTests
    {
        private IWebDriver driver;
        private string testHtmlPath;

        [SetUp]
        public void SetUp()
        {
            // Create a temporary HTML file with menu items from MainMenu class
            testHtmlPath = CreateTestHtmlPage();
            
            // Only attempt to set up Selenium if Chrome is available
            try
            {
                // Set up Chrome driver with headless option for CI environments
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--remote-debugging-port=9222");
                
                driver = new ChromeDriver(options);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            }
            catch (Exception ex)
            {
                // If Chrome driver fails to initialize, mark tests as inconclusive
                Assert.Inconclusive($"Chrome driver not available for Selenium tests: {ex.Message}. " +
                    "This is expected in environments without Chrome/ChromeDriver. " +
                    "Use MainViewSeleniumCompatibilityTests for validating menu structure without browser dependency.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            
            // Clean up the temporary HTML file
            if (File.Exists(testHtmlPath))
            {
                File.Delete(testHtmlPath);
            }
        }

        /// <summary>
        /// Test that the correct number of menu options appears in the MainView.
        /// This test uses reflection to find all methods in MainMenu class with [MainMenu] attributes
        /// and verifies that Selenium can count them correctly.
        /// </summary>
        [Test]
        public void TestMainViewMenuOptionsCount()
        {
            // Navigate to the test HTML page
            driver.Navigate().GoToUrl($"file://{testHtmlPath}");
            
            // Find all menu items using Selenium
            var menuItems = driver.FindElements(By.ClassName("menu-item"));
            
            // Assert that we have the correct number of menu items
            Assert.That(menuItems.Count, Is.EqualTo(8), 
                $"Expected 8 main menu items, but found {menuItems.Count}. " +
                $"Menu items found: {string.Join(", ", menuItems.Select(item => item.Text))}");
        }

        /// <summary>
        /// Test that all expected menu items are present with their correct names.
        /// </summary>
        [Test]
        public void TestMainViewMenuItemNames()
        {
            // Navigate to the test HTML page
            driver.Navigate().GoToUrl($"file://{testHtmlPath}");
            
            // Find all menu items
            var menuItems = driver.FindElements(By.ClassName("menu-item"));
            var menuTexts = menuItems.Select(item => item.Text).ToList();
            
            // Expected menu items based on MainMenu.cs analysis
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
            
            // Check that all expected menu items are present
            foreach (var expectedItem in expectedMenuItems)
            {
                Assert.That(menuTexts, Does.Contain(expectedItem), 
                    $"Expected menu item '{expectedItem}' not found. Available items: {string.Join(", ", menuTexts)}");
            }
        }

        /// <summary>
        /// Creates a temporary HTML page that displays the main menu items
        /// found via reflection in the MainMenu class.
        /// </summary>
        /// <returns>Path to the created HTML file</returns>
        private string CreateTestHtmlPage()
        {
            var tempPath = Path.GetTempFileName() + ".html";
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
                html.AppendLine($"        <div class=\"menu-item\">{menuItem.MenuName}</div>");
            }
            
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            File.WriteAllText(tempPath, html.ToString());
            return tempPath;
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
    }
}