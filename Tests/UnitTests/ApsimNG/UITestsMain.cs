using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UserInterface.Presenters;
using UserInterface.Views;
using Utility;

namespace UnitTests.ApsimNG
{
    /// <summary>
    /// Init/cleanup methods for UI tests.
    /// </summary>
    [SetUpFixture]
    [RequiresThread(System.Threading.ApartmentState.STA)]
    public class UITestsMain
    {
        /// <summary>
        /// We turn off dark mode when tests are run, to ensure
        /// a consistent test environment. When the tests finish,
        /// we want to reset the dark theme option to its original
        /// value. Hence this variable to track the original value.
        /// </summary>
        private static bool darkTheme;

        /// <summary>
        /// Reference to the main view.
        /// </summary>
        private static MainView mainForm;

        /// <summary>
        /// Reference to the main presenter which can be used for UI tests.
        /// </summary>
        public static MainPresenter MasterPresenter { get; set; }

        /// <summary>
        /// Init method which starts the GUI. Run before any UI tests.
        /// </summary>
        [OneTimeSetUp]
        public static void UITestSetup()
        {
            darkTheme = Configuration.Settings.DarkTheme;
            Configuration.Settings.DarkTheme = false;

            Gtk.Application.Init();
            Gtk.Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
            IntellisensePresenter.Init();
            mainForm = new MainView();
            MasterPresenter = new MainPresenter();

            MasterPresenter.Attach(mainForm, null);
            mainForm.MainWidget.ShowAll();
        }

        /// <summary>
        /// Performs a one-time cleanup process after all UI tests have finished running.
        /// </summary>
        [OneTimeTearDown]
        public static void UITestCleanup()
        {
            MasterPresenter.Detach(mainForm);
            Configuration.Settings.DarkTheme = darkTheme;
        }
    }
}
