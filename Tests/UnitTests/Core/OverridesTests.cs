using Models;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Models.Core.Overrides;

namespace UnitTests.Core
{
    /// <summary>
    /// A test set for the edit file feature which
    /// allows for automated editing of .apsimx
    /// files from the command line by using the
    /// /Edit switch on Models.exe.
    /// </summary>
    [TestFixture]
    public class OverridesTests
    {
        private class ListClass<T> : Model
        {
            public ListClass(string name, int n)
            {
                Name = name;
                Data = new List<T>(n);
            }
            public List<T> Data { get; set; }
        }

        /// <summary>Basic simulation.</summary>
        private Simulations sims1;

        /// <summary>
        /// Path to a second .apsimx file which has a few weather
        /// nodes in it, which will be imported into the first
        /// .apsimx file via the /Edit feature.
        /// </summary>
        private string extFile;

        [SetUp]
        public void Initialise()
        {
            sims1 = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2017, 1, 1),
                                EndDate = new DateTime(2018, 1, 1)
                            },
                            new Zone()
                            {
                                Name = "Zone1",
                                Area = 1,
                                Children = new List<IModel>()
                                {
                                    new Models.Report()
                                    {
                                        Name = "Report1",
                                        VariableNames = new string[]
                                        {
                                            "AA"
                                        }
                                    },
                                    new Models.Report()
                                    {
                                        Name = "Report2",
                                        VariableNames = new string[]
                                        {
                                            "BB"
                                        }
                                    }
                                }
                            },
                            new Zone()
                            {
                                Name = "Zone2",
                                Area = 1,
                                Children = new List<IModel>()
                                {
                                    new Models.Report()
                                    {
                                        Name = "Report1",
                                        VariableNames = new string[]
                                        {
                                            "CC"
                                        }
                                    },
                                    new Models.Report()
                                    {
                                        Name = "Report3",
                                        VariableNames = new string[]
                                        {
                                            "DD"
                                        }
                                    },
                                    new ListClass<string>("StringList", 5)
                                }
                            }
                        }
                    }
                }
            };
            sims1.ParentAllDescendants();

            // Create a new .apsimx file containing two clock nodes.
            Simulations sims2 = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                Name = "Clock1",
                                StartDate = new DateTime(2020, 1, 1),
                                EndDate = new DateTime(2020, 1, 31)
                            },
                            new Clock()
                            {
                                Name = "Clock2",
                                StartDate = new DateTime(2021, 1, 1),
                                EndDate = new DateTime(2021, 1, 31)
                            }
                        }
                    }
                }
            };
            sims2.ParentAllDescendants();
            extFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".apsimx");
            sims2.Write(extFile);
        }

        /// <summary>Set an array property in multiple models (match on type), supplying a csv string.</summary>
        [Test]
        public void SetPropertyInTypeMatchedModels()
        {
            var undos = Overrides.Apply(sims1, "[Report].VariableNames", "x,y,z", Override.MatchTypeEnum.NameAndType);

            foreach (var report in sims1.FindAllInScope<Models.Report>())
                Assert.AreEqual(new[] { "x", "y", "z" }, report.VariableNames);

            // Now undo the overrides.
            Overrides.Apply(sims1, undos);
            var reports = sims1.FindAllInScope<Models.Report>().ToArray();
            Assert.AreEqual(new[] { "AA" }, reports[0].VariableNames);
            Assert.AreEqual(new[] { "BB" }, reports[1].VariableNames);
            Assert.AreEqual(new[] { "CC" }, reports[2].VariableNames);
            Assert.AreEqual(new[] { "DD" }, reports[3].VariableNames);
        }

        /// <summary>Set an array property in specific models (match on name), supplying a csv string.</summary>
        [Test]
        public void SetPropertyInNameMatchedModels()
        {
            var undos = Overrides.Apply(sims1, "[Report1].VariableNames", "x,y,z", Override.MatchTypeEnum.NameAndType);

            // It should have changed all Report1 models.
            foreach (var report1 in sims1.FindAllInScope<Models.Report>("Report1"))
                Assert.AreEqual(new[] { "x", "y", "z" }, report1.VariableNames);

            // It should not have changed Report2 and Report3
            var reports = sims1.FindAllInScope<Models.Report>().ToArray();

            Assert.AreEqual(new[] { "x", "y", "z" }, reports[0].VariableNames);
            Assert.AreEqual(new[] { "BB" }, reports[1].VariableNames);
            Assert.AreEqual(new[] { "x", "y", "z" }, reports[2].VariableNames);
            Assert.AreEqual(new[] { "DD" }, reports[3].VariableNames);

            // Now undo the overrides.
            Overrides.Apply(sims1, undos);
            Assert.AreEqual(new[] { "AA" }, reports[0].VariableNames);
            Assert.AreEqual(new[] { "BB" }, reports[1].VariableNames);
            Assert.AreEqual(new[] { "CC" }, reports[2].VariableNames);
            Assert.AreEqual(new[] { "DD" }, reports[3].VariableNames);
        }

        /// <summary>Set a date property, supplying dates in different formats.</summary>
        [Test]
        public void SetDateProperty()
        {
            var undos = Overrides.Apply(sims1, "[Clock].StartDate", new DateTime(2000, 01, 01), Override.MatchTypeEnum.NameAndType);

            var clock = sims1.FindInScope<Clock>();
            Assert.AreEqual(new DateTime(2000, 01, 01), clock.StartDate);

            // Now undo the overrides.
            Overrides.Apply(sims1, undos);
            Assert.AreEqual(new DateTime(2017, 1, 1), clock.StartDate);
        }

        /// <summary>Set a model from and external file (finds the first matching model).</summary>
        [Test]
        public void SetModelFromExternalFileFirstMatchingModel()
        {
            Overrides.Apply(sims1, "[Clock]", extFile, Override.MatchTypeEnum.NameAndType);

            var clock = sims1.FindInScope<Clock>();
            Assert.AreEqual(new DateTime(2020, 01, 01), clock.StartDate);
        }

        /// <summary>Set a model from and external file (finds a specific model).</summary>
        [Test]
        public void SetModelFromExternalFileSpecificModel()
        {
            Overrides.Apply(sims1, "[Clock]", $"{extFile};[Clock2]", Override.MatchTypeEnum.NameAndType);

            var clock = sims1.FindInScope<Clock>();
            Assert.AreEqual(new DateTime(2021, 01, 01), clock.StartDate);
        }

        /// <summary>Replace a model using name (not type matching)</summary>
        [Test]
        public void ReplaceModelUsingNameMatch()
        {
            var newVariableNames = new string[] { "New" };
            var undos = Overrides.Apply(sims1, "Report1", new Models.Report() { Name = "Report4", VariableNames = newVariableNames }, Override.MatchTypeEnum.Name);

            // It should have changed all Report1 models to Report4
            var reports = sims1.FindAllInScope<Models.Report>().ToArray();

            // The names should still be the same.
            Assert.AreEqual(4, reports.Length);
            Assert.AreEqual("Report1", reports[0].Name);
            Assert.AreEqual("Report2", reports[1].Name);
            Assert.AreEqual("Report1", reports[2].Name);
            Assert.AreEqual("Report3", reports[3].Name);

            // The variablenames property should be different.
            Assert.AreEqual(newVariableNames, reports[0].VariableNames);
            Assert.AreEqual(new string[] { "BB" }, reports[1].VariableNames);
            Assert.AreEqual(newVariableNames, reports[2].VariableNames);
            Assert.AreEqual(new string[] { "DD" }, reports[3].VariableNames);


            // Now undo the overrides.
            Overrides.Apply(sims1, undos);
            reports = sims1.FindAllInScope<Models.Report>().ToArray();
            Assert.AreEqual(new string[] { "AA" }, reports[0].VariableNames);
            Assert.AreEqual(new string[] { "BB" }, reports[1].VariableNames);
            Assert.AreEqual(new string[] { "CC" }, reports[2].VariableNames);
            Assert.AreEqual(new string[] { "DD" }, reports[3].VariableNames);
        }

        [Test]
        public void TestEditingArrayElements()
        {
            var overrides = new Override[]
            {
                // Set an entire (string) list.
                new Override("[StringList].Data", "1, x, y, true, 0.5", Override.MatchTypeEnum.NameAndType),
                
                // Modify a single element of a (string) list.
                new Override("[StringList].Data[1]", 6, Override.MatchTypeEnum.NameAndType),

                // Modify multiple elements of a (string) list.
                new Override("[StringList].Data[3:4]", "xyz", Override.MatchTypeEnum.NameAndType),
            };

            var undos = Overrides.Apply(sims1, overrides);

            var stringList = (ListClass<string>)sims1.FindInScope<ListClass<string>>();

            Assert.AreEqual(new List<string>(new[]
            {
                "6",
                "x",
                "xyz",
                "xyz",
                "0.5"
            }), stringList.Data);

            // Now undo the overrides.
            Overrides.Apply(sims1, undos);

            Assert.AreEqual(0, stringList.Data.Count);
        }
    }
}
