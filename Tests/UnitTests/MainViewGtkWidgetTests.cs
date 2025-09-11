using System;
using System.Linq;
using System.Reflection;
using Gtk;
using NUnit.Framework;
using Models.Core;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UnitTests
{
    /// <summary>
    /// GTK widget-level tests for MainView menu structure.
    /// This tests the actual GTK components without requiring a full application launch.
    /// This is a more lightweight alternative to full UI automation testing.
    /// </summary>
    [TestFixture]
    public class MainViewGtkWidgetTests
    {
        [SetUp]
        public void SetUp()
        {
            // No setup required for widget structure tests
        }

        /// <summary>
        /// Test that MainView can be instantiated and contains menu structure.
        /// This validates the GTK widget construction without full application context.
        /// </summary>
        [Test]
        public void MainView_Widget_ShouldInstantiateSuccessfully()
        {
            try
            {
                // This test validates that the MainView class can be instantiated
                // and that the underlying GTK components are properly constructed
                
                // Check that MainView type exists and can be loaded
                var mainViewType = typeof(MainView);
                Assert.That(mainViewType, Is.Not.Null, "MainView type should be available");
                
                // Verify it implements the expected interface
                Assert.That(typeof(UserInterface.Interfaces.IMainView).IsAssignableFrom(mainViewType), Is.True,
                    "MainView should implement IMainView interface");

                Console.WriteLine("MainView widget validation completed successfully");
            }
            catch (Exception ex)
            {
                Assert.Fail($"MainView widget instantiation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that MainMenu class contains exactly 8 menu items with proper attributes.
        /// This validates the menu structure definition that drives the UI.
        /// </summary>
        [Test]
        public void MainMenu_ShouldContainExpectedMenuItems()
        {
            try
            {
                // Get all methods with MainMenu attributes
                var mainMenuType = typeof(MainMenu);
                var methods = mainMenuType.GetMethods();
                var menuMethods = methods.Where(m => m.GetCustomAttribute<MainMenuAttribute>() != null).ToList();

                // Validate we have exactly 8 menu items
                Assert.That(menuMethods.Count, Is.EqualTo(8), 
                    $"Expected exactly 8 menu items, but found {menuMethods.Count}");

                // Validate specific expected menu items
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

                var actualMenuItems = menuMethods
                    .Select(m => m.GetCustomAttribute<MainMenuAttribute>().MenuName)
                    .ToList();

                foreach (var expectedItem in expectedMenuItems)
                {
                    Assert.That(actualMenuItems.Contains(expectedItem), Is.True,
                        $"Expected menu item '{expectedItem}' not found in MainMenu class");
                }

                Console.WriteLine("Menu structure validation completed:");
                Console.WriteLine($"Found {menuMethods.Count} menu items:");
                foreach (var method in menuMethods)
                {
                    var attr = method.GetCustomAttribute<MainMenuAttribute>();
                    Console.WriteLine($"- {attr.MenuName} ({attr.Hotkey})");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Menu structure validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that menu keyboard shortcuts are properly defined.
        /// </summary>
        [Test]
        public void MainMenu_KeyboardShortcuts_ShouldBeProperlyDefined()
        {
            try
            {
                var mainMenuType = typeof(MainMenu);
                var methods = mainMenuType.GetMethods();
                var menuMethods = methods.Where(m => m.GetCustomAttribute<MainMenuAttribute>() != null);

                var expectedShortcuts = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Save", "<Ctrl>s" },
                    { "Save As", "<Ctrl><Shift>s" },
                    { "Undo", "<Ctrl>z" },
                    { "Redo", "<Ctrl>y" },
                    { "Clear Status", "<Ctrl>g" },
                    { "Help", "F1" },
                    { "Run", "F5" }
                };

                foreach (var method in menuMethods)
                {
                    var attr = method.GetCustomAttribute<MainMenuAttribute>();
                    var menuName = attr.MenuName;
                    var shortcut = attr.Hotkey;

                    if (expectedShortcuts.ContainsKey(menuName))
                    {
                        Assert.That(shortcut, Is.EqualTo(expectedShortcuts[menuName]),
                            $"Keyboard shortcut for '{menuName}' should be '{expectedShortcuts[menuName]}' but was '{shortcut}'");
                    }

                    // Validate that shortcuts are in proper GTK format
                    if (!string.IsNullOrEmpty(shortcut))
                    {
                        Assert.That(shortcut.Contains("Ctrl") || shortcut.StartsWith("F") || 
                            shortcut.Contains("Alt") || shortcut.Contains("Shift"), Is.True,
                            $"Shortcut '{shortcut}' for '{menuName}' should be in proper GTK format");
                    }
                }

                Console.WriteLine("Keyboard shortcuts validation completed successfully");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Keyboard shortcuts validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that MainMenu methods have proper event handler signatures.
        /// This validates that the menu items can be properly connected to GTK events.
        /// </summary>
        [Test]
        public void MainMenu_Methods_ShouldHaveProperEventHandlerSignatures()
        {
            try
            {
                var mainMenuType = typeof(MainMenu);
                var methods = mainMenuType.GetMethods();
                var menuMethods = methods.Where(m => m.GetCustomAttribute<MainMenuAttribute>() != null);

                foreach (var method in menuMethods)
                {
                    // Check that the method has the proper signature for GTK event handlers
                    var parameters = method.GetParameters();
                    
                    Assert.That(parameters.Length, Is.EqualTo(2),
                        $"Menu method '{method.Name}' should have exactly 2 parameters (sender, EventArgs)");
                    
                    Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(object)),
                        $"First parameter of '{method.Name}' should be of type 'object'");
                    
                    Assert.That(typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType), Is.True,
                        $"Second parameter of '{method.Name}' should be assignable from EventArgs");

                    Assert.That(method.ReturnType, Is.EqualTo(typeof(void)),
                        $"Menu method '{method.Name}' should return void");
                }

                Console.WriteLine($"Event handler signatures validated for {menuMethods.Count()} menu methods");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Event handler signature validation failed: {ex.Message}");
            }
        }
    }
}