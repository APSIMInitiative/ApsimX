using System.Linq;
using System.Reflection;
using Models.Core;
using NUnit.Framework;
using UserInterface.Presenters;

namespace UnitTests
{
    /// <summary>
    /// Simple reflection tests to verify MainMenu structure before using Selenium.
    /// This validates the core functionality without requiring web browser dependencies.
    /// </summary>
    [TestFixture]
    public class MainMenuReflectionTests
    {
        /// <summary>
        /// Test that the MainMenu class has exactly 8 methods with [MainMenu] attributes.
        /// This is the core test that the Selenium test will also verify.
        /// </summary>
        [Test]
        public void TestMainMenuMethodCount()
        {
            var mainMenuType = typeof(MainMenu);
            var methods = mainMenuType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            var menuMethods = methods.Where(method => method.GetCustomAttribute<MainMenuAttribute>() != null).ToList();
            
            Assert.That(menuMethods.Count, Is.EqualTo(8), 
                $"Expected 8 main menu methods, but found {menuMethods.Count}. " +
                $"Methods found: {string.Join(", ", menuMethods.Select(m => m.Name))}");
        }

        /// <summary>
        /// Test that all expected menu item names are present.
        /// </summary>
        [Test]
        public void TestMainMenuItemNames()
        {
            var mainMenuType = typeof(MainMenu);
            var methods = mainMenuType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            var menuAttributes = methods
                .Select(method => method.GetCustomAttribute<MainMenuAttribute>())
                .Where(attr => attr != null)
                .ToList();
            
            var menuNames = menuAttributes.Select(attr => attr.MenuName).ToList();
            
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
                Assert.That(menuNames, Does.Contain(expectedItem), 
                    $"Expected menu item '{expectedItem}' not found. Available items: {string.Join(", ", menuNames)}");
            }
            
            // Also verify the count
            Assert.That(menuNames.Count, Is.EqualTo(expectedMenuItems.Length), 
                $"Expected {expectedMenuItems.Length} menu items, but found {menuNames.Count}");
        }

        /// <summary>
        /// Test that menu items have expected keyboard shortcuts where applicable.
        /// </summary>
        [Test]
        public void TestMainMenuHotkeys()
        {
            var mainMenuType = typeof(MainMenu);
            var methods = mainMenuType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            var menuAttributes = methods
                .Select(method => method.GetCustomAttribute<MainMenuAttribute>())
                .Where(attr => attr != null)
                .ToList();
            
            // Check for specific hotkeys we expect
            var saveMenu = menuAttributes.FirstOrDefault(attr => attr.MenuName == "Save");
            Assert.That(saveMenu, Is.Not.Null, "Save menu should exist");
            Assert.That(saveMenu.Hotkey, Is.EqualTo("<Ctrl>s"), "Save menu should have Ctrl+S hotkey");
            
            var helpMenu = menuAttributes.FirstOrDefault(attr => attr.MenuName == "Help");
            Assert.That(helpMenu, Is.Not.Null, "Help menu should exist");
            Assert.That(helpMenu.Hotkey, Is.EqualTo("F1"), "Help menu should have F1 hotkey");
            
            var runMenu = menuAttributes.FirstOrDefault(attr => attr.MenuName == "Run");
            Assert.That(runMenu, Is.Not.Null, "Run menu should exist");
            Assert.That(runMenu.Hotkey, Is.EqualTo("F5"), "Run menu should have F5 hotkey");
        }
    }
}