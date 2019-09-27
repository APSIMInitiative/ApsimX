using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UnitTests.ApsimNG
{
    [SetUpFixture]
    [RequiresThread(System.Threading.ApartmentState.STA)]
    public class UITestsMain
    {
        public static MainPresenter MasterPresenter { get; set; }

        private MainView mainForm;

        [OneTimeSetUp]
        public void UITestSetup()
        {
            Gtk.Application.Init();
            Gtk.Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
            IntellisensePresenter.Init();
            mainForm = new MainView();
            MasterPresenter = new MainPresenter();

            MasterPresenter.Attach(mainForm, null);
            mainForm.MainWidget.ShowAll();
        }

        [OneTimeTearDown]
        public void UITestCleanup()
        {
            MasterPresenter.Detach(mainForm);
        }
    }
}
