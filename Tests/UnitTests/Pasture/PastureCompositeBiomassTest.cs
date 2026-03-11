using APSIM.Core;
using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models;
using Models.Core;
using Models.GrazPlan;
using Models.GrazPlan.Organs;
using Models.Soils;
using Models.Storage;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using static Models.GrazPlan.GrazType;
using Models.PMF.Struct;
using SQLitePCL;
using APSIM.Numerics;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml;
using System.Data;
using JetBrains.Annotations;
using DocumentFormat.OpenXml.Presentation;
using Models.GrazPlan.Biomass;

namespace UnitTests
{
    /// <summary>
    /// Unit test for generic organs
    /// </summary>
    [TestFixture]
    public class PastureCompositeBiomassesTest
    {
        public double[] LeafWt { get; private set; }
        public double[] StemWt { get; private set; }
        public double[] RootWt { get; private set; }
        public double[] AbovegroundWtgms { get; private set; }
        public double[] ShootDMgms { get; private set; }
        public double[] GreenDMgms { get; private set; }
        public double[] DeadDMgms { get; private set; }
        public double[] AboveGroundDeadgrms { get; private set; }
        public double[] AbovegroundLivegrms { get; private set; }
        public double[] TotalWt { get; private set; }
        public double[] TotalBiomass { get; private set; }

        [Test]
        public void TestPastureCompositeBiomasses()
        {
            
            string path = Path.Combine("%root%","Prototypes","Ryegrass", "Pasture.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;


            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            
            

            var report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = new[]
            {
                "[Clock].Today",
                "[Pasture].Leaf.Wt",
                "[Pasture].ShootDM/10",
                "[Pasture].Stem.Wt",
                "[Pasture].Root.Wt",
                "[Pasture].AboveGround.Wt",
                "[Pasture].GreenDM/10",
                "[Pasture].AboveGroundLive.Wt",
                "[Pasture].DeadDM/10",
                "[Pasture].AboveGroundDead.Wt",
                "[Pasture].Total.Wt",
                "[Pasture].Leaf.Wt+[Pasture].Stem.Wt+[Pasture].Root.Wt"
            };
            report.EventNames = new[]
            {
                "[Clock].EndOfDay"
            };

            //Run simulation

            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            //Get data from the datastore and retrieve data from specific columns
           
            var dataTable = storage.Reader.GetData("DailyReport");
            foreach(DataColumn col in dataTable.Columns)
            {
                  
                    if (col.ColumnName == "Pasture.Leaf.Wt")
                    {
                        LeafWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Leaf.Wt", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.Stem.Wt")
                    {
                        StemWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Stem.Wt", CultureInfo.InvariantCulture);
                    }

                     if (col.ColumnName == "Pasture.Root.Wt")
                    {
                        RootWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Root.Wt", CultureInfo.InvariantCulture);
                    }

                    if (col.ColumnName == "Pasture.AboveGround.Wt")
                    {
                        AbovegroundWtgms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.AboveGround.Wt", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.ShootDM/10")
                    {
                        ShootDMgms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.ShootDM/10", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.GreenDM/10")
                    {
                        GreenDMgms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.GreenDM/10", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.DeadDM/10")
                    {
                        DeadDMgms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.DeadDM/10", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.AboveGroundLive.Wt")
                    {
                        AbovegroundLivegrms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.AboveGroundLive.Wt", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.AboveGroundDead.Wt")
                    {
                        AboveGroundDeadgrms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.AboveGroundDead.Wt", CultureInfo.InvariantCulture);
                    }

                    if (col.ColumnName == "Pasture.Total.Wt")
                    {
                        TotalWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Total.Wt", CultureInfo.InvariantCulture);

                    }
                     if (col.ColumnName == "Pasture.Leaf.Wt+Pasture.Stem.Wt+Pasture.Root.Wt" )
                    {
                        TotalBiomass = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Leaf.Wt+Pasture.Stem.Wt+Pasture.Root.Wt" , CultureInfo.InvariantCulture);
                    }
                    
            }
            
            
           //Assert biomasses calculated in AusFarm  and refactored Pasture model return same values.
           
           Assert.That(LeafWt.Add(StemWt), Is.EqualTo(AbovegroundWtgms));
           Assert.That(ShootDMgms, Is.EqualTo(AbovegroundWtgms).Within(1e-10));
           Assert.That(AboveGroundDeadgrms,Is.EqualTo(DeadDMgms));
           Assert.That(AbovegroundLivegrms, Is.EqualTo(GreenDMgms));
           Assert.That(TotalWt, Is.EqualTo(TotalBiomass));
          


        }
 

    }
}
        