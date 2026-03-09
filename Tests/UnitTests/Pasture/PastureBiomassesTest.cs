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

namespace UnitTests
{
    /// <summary>
    /// Unit test for generic organs
    /// </summary>
    [TestFixture]
    public class PastureBiomassesTest
    {
        

        // /// <summary>Structure instance supplied by APSIM.core.</summary>
        // [field: NonSerialized]
        // public IStructure Structure { private get; set; }
        public double[] LeafDMgms { get; private set; }
        public double[] LeafWt { get; private set; }
        public double[] StemWt { get; private set; }
        public double[] StemWtGms { get; private set; }
        public double[] RootWtGms { get; private set; }
        public double[] RootWt { get; private set; }

        [Test]
        public void TestPastureBiomasses()
        {

            string path = Path.Combine("%root%","Prototypes","Ryegrass", "Pasture.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;


            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            
            //Add Organs models to Pasture Model 
            GenericOrgan Leaf = new GenericOrgan
            {
                Name = "Leaf",
                IsAboveGround = true
            };
            sims.Node.FindChild<Pasture>(recurse:true).AddChild(Leaf);
            

            GenericOrgan Stem = new GenericOrgan
            {
                Name = "Stem",
                IsAboveGround = true
            };
            sims.Node.FindChild<Pasture>(recurse:true).AddChild(Stem);

            GenericOrgan Root = new GenericOrgan
            {
                Name = "Root",
                IsAboveGround = false
            };
            sims.Node.FindChild<Pasture>(recurse:true).AddChild(Root);

            var report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = new[]
            {
                "[Clock].Today",
                "[Pasture].LeafDM/10",
                "[Pasture].Leaf.Wt",
                "[Pasture].StemDM/10",
                "[Pasture].Stem.Wt",
                "[Pasture].Root.Wt",
                "[Pasture].RootDM/10"
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
                   if (col.ColumnName == "Pasture.LeafDM/10")
                    {
                        LeafDMgms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.LeafDM/10", CultureInfo.InvariantCulture);
                        
                    }
                    if (col.ColumnName == "Pasture.Leaf.Wt")
                    {
                        LeafWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Leaf.Wt", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.Stem.Wt")
                    {
                        StemWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Stem.Wt", CultureInfo.InvariantCulture);
                    }
                    if (col.ColumnName == "Pasture.StemDM/10")
                    {
                        StemWtGms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.StemDM/10", CultureInfo.InvariantCulture);
                    }
                     if (col.ColumnName == "Pasture.RootDM/10")
                    {
                        RootWtGms = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.RootDM/10", CultureInfo.InvariantCulture);
                    }
                     if (col.ColumnName == "Pasture.Root.Wt")
                    {
                        RootWt = DataTableUtilities.GetColumnAsDoubles(dataTable, "Pasture.Root.Wt", CultureInfo.InvariantCulture);
                    }
            }
            

           //Assert biomasses calculated in AusFarm  and refactored Pasture model return same values.
   
            Assert.That (LeafDMgms, Is.EqualTo(LeafWt));
            Assert.That(StemWtGms, Is.EqualTo(StemWt));
            Assert.That(RootWtGms, Is.EqualTo(RootWt));

              
        }

        
    }
    

}

