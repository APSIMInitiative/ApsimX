namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Importer;
    using Models;
    using Models.Core;
    using Models.PMF;
    using Models.Report;
    using Models.Soils;
    using Models.Storage;
    using Models.Surface;
    using NUnit.Framework;
    using System;
    using System.IO;
    using UserInterface.Presenters;
    using UserInterface.Views;

    /// <summary>This is a test class for the .apsim file importer.</summary>
    [TestFixture]
    public class ImporterTests
    {
        /// <summary>Ensure CLOCK imports OK</summary>
        [Test]
        public void ImporterTests_ClockImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <clock>" +
            "      <start_date type=\"date\" description=\"Enter the start date of the simulation\">01/01/1940</start_date>" +
            "      <end_date type=\"date\" description=\"Enter the end date of the simulation\">31/12/1950</end_date> " +
            "    </clock>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            Assert.IsTrue(sims.Children[0] is Simulation);
            Assert.IsTrue(sims.Children[1] is DataStore);

            Clock c = sims.Children[0].Children[0] as Clock;
            Assert.AreEqual(c.StartDate, new DateTime(1940, 1, 1));
            Assert.AreEqual(c.EndDate, new DateTime(1950, 12, 31));
        }

        /// <summary>Ensure shortcuts import OK</summary>
        [Test]
        public void ImporterTests_ShortcutsWork()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <folder name=\"shared\">" +
            "    <clock>" +
            "      <start_date type=\"date\" description=\"Enter the start date of the simulation\">01/01/1940</start_date>" +
            "      <end_date type=\"date\" description=\"Enter the end date of the simulation\">31/12/1950</end_date> " +
            "    </clock>" +
            "  </folder>" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <clock shortcut=\"/simulations/shared/Clock\">" +
            "    </clock>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            Clock c = sims.Children[1].Children[0] as Clock;
            Assert.AreEqual(c.StartDate, new DateTime(1940, 1, 1));
            Assert.AreEqual(c.EndDate, new DateTime(1950, 12, 31));
        }

        /// <summary>Ensure METFILE imports OK</summary>
        [Test]
        public void ImporterTests_MetFileImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <metfile name=\"met\">" +
            "      <filename name=\"filename\" input=\"yes\">%apsim%/Examples/MetFiles/Goond.met</filename>" +
            "    </metfile>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var w = sims.Children[0].Children[0] as Models.Weather;
            Assert.AreEqual(w.FileName, @"\Examples\MetFiles\Goond.met");
        }

        /// <summary>Ensure AREA imports OK</summary>
        [Test]
        public void ImporterTests_AreaImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <area name=\"paddock\">" +
            "      <paddock_area>100</paddock_area>" +
            "    </area>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            Zone z = sims.Children[0].Children[0] as Zone;
            Assert.AreEqual(z.Area, 100);
        }

        /// <summary>Ensure SOIL imports OK</summary>
        [Test]
        public void ImporterTests_SoilImports()
        {
            string oldXml = ReflectionUtilities.GetResourceAsString("UnitTests.ImporterTestsSoilImports.xml");

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            Soil s = sims.Children[0].Children[0] as Soil;
            Assert.AreEqual(s.Name, "Soil");

            InitialWater initWater = s.Children[0] as InitialWater;
            Assert.AreEqual(initWater.FractionFull, 0.5);
            Assert.AreEqual(initWater.PercentMethod, InitialWater.PercentMethodEnum.FilledFromTop);

            Physical w = s.Children[1] as Physical;
            Assert.AreEqual(w.Thickness, new double[] { 150, 150, 300, 300 });
            Assert.AreEqual(w.BD, new double[] { 1.02, 1.03, 1.02, 1.02 });
            Assert.AreEqual(w.LL15, new double[] { 0.29, 0.29, 0.29, 0.29 });

            SoilWater sw = s.Children[2] as SoilWater;
            Assert.AreEqual(sw.Thickness, new double[] { 150, 150, 300, 300 });
            Assert.AreEqual(sw.SWCON, new double[] { 0.3, 0.3, 0.3, 0.3 });
            Assert.AreEqual(sw.SummerCona, 3.5);
            Assert.AreEqual(sw.SummerU, 6);
            Assert.AreEqual(sw.WinterCona, 2);
            Assert.AreEqual(sw.WinterU, 2);

            Assert.IsTrue(s.Children[3] is SoilNitrogen);

            Organic som = s.Children[4] as Organic;
            Assert.AreEqual(som.Thickness, new double[] { 150, 150, 300, 300 });
            Assert.AreEqual(som.Carbon, new double[] { 1.04, 0.89, 0.89, 0.89 });
            Assert.AreEqual(som.FBiom, new double[] { 0.025, 0.02, 0.015, 0.01});

            Chemical a = s.Children[5] as Chemical;
            Assert.AreEqual(a.Thickness, new double[] { 150, 150, 300, 300 });
            Assert.AreEqual(a.NO3N, new double[] { 6.5, 2.1, 2.1, 1.0 });
            Assert.AreEqual(a.NH4N, new double[] { 0.5, 0.1, 0.1, 0.2 });
            Assert.AreEqual(a.EC, new double[] { 0.2, 0.25, 0.31, 0.40 });
            Assert.AreEqual(a.PH, new double[] { 8.4, 8.8, 9.0, 9.2 });

            Sample sam = s.Children[6] as Sample;
            Assert.AreEqual(sam.Thickness, new double[] { 150, 150, 300 });

            SoilCrop crop = s.Children[1].Children[0] as SoilCrop;
            Assert.AreEqual(crop.LL, new double[] { 0.29, 0.29, 0.32, 0.38 });
            Assert.AreEqual(crop.KL, new double[] { 0.1, 0.1, 0.08, 0.06 });
            Assert.AreEqual(crop.XF, new double[] { 1, 1, 1, 1 });
        }

        /// <summary>Ensure WHEAT imports OK</summary>
        [Test]
        public void ImporterTests_WheatImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <wheat />" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var f = sims.Children[0].Children[0] as Plant;
            Assert.IsNotNull(f);
        }

        /// <summary>Ensure MANAGER imports OK</summary>
        [Test]
        public void ImporterTests_ManagerImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <manager name=\"Sowing fertiliser\">" +
            "      <ui>" +
            "        <category description=\"When should fertiliser be applied\" type=\"category\" />" +
            "        <modulename type=\"modulename\" description=\"On which module should the event come from : \">wheat</modulename>" +
            "        <eventname type=\"text\" description=\"On which event should fertiliser be applied : \">sowing</eventname>" +
            "        <category description=\"Fertiliser application details\" type=\"category\" />" +
            "        <fertmodule type=\"modulename\" description=\"Module used to apply the fertiliser : \">fertiliser</fertmodule>" +
            "        <fert_amount_sow type=\"text\" description=\"Amount of starter fertiliser at sowing (kg/ha) : \">150</fert_amount_sow>" +
            "        <fert_type_sow type=\"list\" listvalues=\"NO3_N, NH4_N, NH4NO3, urea_N, urea_no3, urea, nh4so4_n, rock_p, banded_p, broadcast_p\" description=\"Sowing fertiliser type : \">urea_N</fert_type_sow>" +
            "      </ui>" +
            "      <script name=\"[modulename].[eventname]\">" +
            "        <text>" +
            "    [fertmodule] apply amount = [fert_amount_sow] (kg/ha), depth = 50 (mm), type = [fert_type_sow]" +
            "        </text>" +
            "        <event>[modulename].[eventname]</event>" +
            "      </script>" +
            "    </manager>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var m = sims.Children[0].Children[0] as Manager;
            Assert.IsNotNull(m);
            Assert.IsTrue(m.Code != string.Empty);
            Assert.IsNotNull(m.Code);
            Assert.AreEqual(m.Children.Count, 1);
        }

        /// <summary>Ensure MANAGER2 imports OK</summary>
        [Test]
        public void ImporterTests_Manager2Imports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <manager2 name=\"Economics\">" +
            "      <ui>" +
            "        <category type=\"category\" description=\"Dummy module\" />" +
            "        <labourCost type=\"text\" description=\"Cost of labour($/ hr)\">0.0</labourCost>" +
            "      </ui>" +
            "      <text>" +
            "    using System;" +
            "    using ModelFramework;" +
            "    public class Script" +
            "    {" +
            "       [EventHandler] public void OnBuyCows() {Console.WriteLine(\"Buying a cow\");}" +
            "       [EventHandler] public void OnSellCows() {Console.WriteLine(\"Selling a cow\");}" +
            "       [EventHandler] public void OnBuyShoats() {Console.WriteLine(\"Buying a goat\");}" +
            "       [EventHandler] public void OnSellShoats() {Console.WriteLine(\"Selling a goat\");}" +
            "    }   " +
            "      </text>" +
            "    </manager2>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var m = sims.Children[0].Children[0] as Manager;
            Assert.IsNotNull(m);
            Assert.IsTrue(m.Code != string.Empty);
            Assert.IsNotNull(m.Code);
            Assert.AreEqual(m.Children.Count, 1);
        }

        /// <summary>Ensure OUTPUTFILE imports OK</summary>
        [Test]
        public void ImporterTests_OutputFileImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <outputfile>" +
            "      <filename output=\"yes\">Continuous Wheat.out</filename>" +
            "      <title>Continuous Wheat</title>" +
            "      <variables name=\"Variables\">" +
            "        <variable array=\" ?\" description=\"Date (dd/mm/yyyy)\">dd/mm/yyyy as Date</variable>" +
            "        <variable array=\" ?\" description=\"Biomass\">biomass</variable>" +
            "        <variable array=\" ?\" description=\"Yield\">yield</variable>" +
            "        <variable array=\" ?\" description=\"grain protein content\">grain_protein</variable>" +
            "        <variable array=\" ?\" description=\"Size of each grain\">grain_size</variable>" +
            "        <variable array=\" ?\" description=\"Extractable Soil Water (mm)\">esw</variable>" +
            "      </variables>" +
            "      <events name=\"Reporting Frequency\">" +
            "        <event description=\"\">harvesting</event>" +
            "      </events>" +
            "    </outputfile>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var r = sims.Children[0].Children[0] as Models.Report.Report;
            Assert.IsNotNull(r);
            Assert.AreEqual(r.VariableNames[0], "[Clock].Today");
            Assert.AreEqual(r.VariableNames[1], "biomass");
            Assert.AreEqual(r.VariableNames[2], "yield");
            Assert.AreEqual(r.VariableNames[3], "grain_protein");
            Assert.AreEqual(r.VariableNames[4], "grain_size");
            Assert.AreEqual(r.VariableNames[5], "esw");

            Assert.AreEqual(r.EventNames[0], "[Clock].DoReport");
        }

        /// <summary>Ensure SURFACEORGANICMATTER imports OK</summary>
        [Test]
        public void ImporterTests_SurfaceOMImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <surfaceom name=\"SurfaceOrganicMatter\">" +
            "      <PoolName type=\"text\" description=\"Organic Matter pool name\">wheat</PoolName >" +
            "      <type type=\"list\" listvalues=\"TropicalPasture,barley,base_type,broccoli,camaldulensis,canola,centro,chickpea,chikenmanure_base,cm,cmA,cmB,constants,cotton,cowpea,danthonia,fababean,fieldpea,fym,gbean,globulus,goatmanure,grandis,grass,horsegram,inert,lablab,lentil,lucerne,lupin,maize,manB,manure,medic,millet,mucuna,nativepasture,navybean,oats,orobanche,peanut,pigeonpea,potato,rice,sorghum,soybean,stylo,sugar,sunflower,sweetcorn,sweetsorghum,tillage,tithonia,vetch,weed,wheat\" description=\"Organic Matter type\">wheat</type>" +
            "      <mass type=\"text\" description=\"Initial surface residue (kg/ha)\">1000</mass>" +
            "      <cnr type=\"text\" description=\"C:N ratio of initial residue\">80</cnr>" +
            "      <standing_fraction type=\"text\" description=\"Fraction of residue standing\">0</standing_fraction>" +
            "    </surfaceom>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var som = sims.Children[0].Children[0] as SurfaceOrganicMatter;
            Assert.IsNotNull(som);
            Assert.AreEqual(som.InitialResidueMass, 1000);
            Assert.AreEqual(som.InitialCNR, 80);
            Assert.AreEqual(som.InitialResidueName, "wheat");
        }
        
        /// <summary>Ensure MICROMET imports OK</summary>
        [Test]
        public void ImporterTests_MicroMetImports()
        {
            string oldXml =
            "<folder version=\"36\" creator=\"Apsim 7.5-r3183\" name=\"simulations\">" +
            "  <simulation name=\"Continuous Wheat\">" +
            "    <micromet name=\"MicroMet\">" +
            "      <soilalbedo name=\"soilalbedo\">0.23</soilalbedo>" +
            "      <a_interception name=\"a_interception\">0.1</a_interception>" +
            "      <b_interception name=\"b_interception\">0.2</b_interception>" +
            "      <c_interception name=\"c_interception\">0.3</c_interception>" +
            "      <d_interception name=\"d_interception\">0.4</d_interception>" +
            "    </micromet>" +
            "  </simulation>" +
            "</folder>";

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            var m = sims.Children[0].Children[0] as MicroClimate;
            Assert.IsNotNull(m);
            Assert.AreEqual(m.soil_albedo, 0.23);
            Assert.AreEqual(m.a_interception, 0.1);
            Assert.AreEqual(m.b_interception, 0.2);
            Assert.AreEqual(m.c_interception, 0.3);
            Assert.AreEqual(m.d_interception, 0.4);
        }

        /// <summary>
        /// This test ensures that failures in the importer do not cause the UI
        /// to crash.
        /// </summary>
        [Test]
        public void EnsureNoCrash()
        {
            // First, write the faulty .apsimx file to a temp file on disk.
            string defective = ReflectionUtilities.GetResourceAsString("UnitTests.defective.apsim");
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "-defective.apsim");
            File.WriteAllText(fileName, defective);
            MainPresenter presenter = new MainPresenter();
            Assert.DoesNotThrow(() => presenter.Import(fileName));
        }

        /// <summary>Ensure entire old APSIM file loads OK.</summary>
        [Test]
        public void EnsureOldAPSIMFileLoads()
        {
            string oldXml = ReflectionUtilities.GetResourceAsString("UnitTests.ImporterTestsOldAPSIM.xml");

            APSIMImporter importer = new APSIMImporter();
            Simulations sims = importer.CreateSimulationsFromXml(oldXml);

            Assert.IsNotNull(sims);
        }
    }
}
