using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UserInterface.Presenters;
using UserInterface.Views;
using Models;
using GLib;
using Gtk;

namespace UnitTests
{
    [TestFixture]
    public class NGManagerTests
    {
        /// <summary>
        /// This reproduces a bug where disabling summary output would
        /// cause a simulation to fail.
        /// </summary>
        [Test]
        public void Test()
        {
            Gtk.Application.Init();

            Gtk.Settings.Default.SetProperty("gtk-overlay-scrolling", new GLib.Value(0));

            IntellisensePresenter.Init();
            MainView mainForm = new MainView();
            MainPresenter mainPresenter = new MainPresenter();
            mainPresenter.Attach(mainForm, new string[] { "C:/git/ApsimX/Examples/Wheat.apsimx" });
            mainForm.MainWidget.ShowAll();

            /*
            ManagerView view = new ManagerView(mainPresenter.GetCurrentExplorerPresenter().CurrentRightHandView);
            ManagerPresenter presenter = new ManagerPresenter();
            Manager manager = new Manager();
            presenter.Attach(manager, view, mainPresenter.GetCurrentExplorerPresenter());
            */


            //expandedRows.ForEach(row => treeview1.ExpandRow(new TreePath(row), false));

            //mainPresenter.GetCurrentExplorerPresenter().Tree.ExpandNodes();



        }
    }
}
