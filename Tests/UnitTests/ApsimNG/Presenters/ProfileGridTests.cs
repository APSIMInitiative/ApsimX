using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using NUnit.Framework;
using UnitTests.ApsimNG.Utilities;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UnitTests.ApsimNG.Presenters
{
    [TestFixture]
    public class ProfileGridTests
    {
        /// <summary>
        /// This test ensures that the user can change the value of
        /// property (NO3N) which is set to null. This test reproduces
        /// bug #4364:
        /// 
        /// https://github.com/APSIMInitiative/ApsimX/issues/4364
        /// 
        /// </summary>
        [Test]
        public void TestEditingNullProperty()
        {
            ExplorerPresenter explorerPresenter = UITestUtilities.OpenResourceFileInTab(Assembly.GetExecutingAssembly(),
                                                    "UnitTests.ApsimNG.Resources.SampleFiles.NullSample.apsimx");
            GtkUtilities.WaitForGtkEvents();

            Simulations sims = explorerPresenter.ApsimXFile;
            Soil soil = Apsim.Find(sims, typeof(Soil)) as Soil;
            Sample sample = Apsim.Child(soil, typeof(Sample)) as Sample;

            explorerPresenter.SelectNode(sample);
            GtkUtilities.WaitForGtkEvents();

            Assert.IsNull(sample.NO3N);

            ProfileView view = explorerPresenter.CurrentRightHandView as ProfileView;
            GridView grid = view.ProfileGrid as GridView;

            // Click on the first cell in the second column (the NO3N column) and type 1.1 and then hit enter.
            GtkUtilities.ClickOnGridCell(grid, 0, 1, Gdk.EventType.ButtonPress, Gdk.ModifierType.None, GtkUtilities.ButtonPressType.LeftClick);
            GtkUtilities.SendKeyPress(grid.Grid, '1');
            GtkUtilities.SendKeyPress(grid.Grid, '.');
            GtkUtilities.SendKeyPress(grid.Grid, '1');
            GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Return, Gdk.ModifierType.None);

            // The sample's NO3N property should now be an array containing 1.1.
            Assert.NotNull(sample.NO3N);
            Assert.AreEqual(new double[1] { 1.1 }, sample.NO3N);
        }
    }
}
