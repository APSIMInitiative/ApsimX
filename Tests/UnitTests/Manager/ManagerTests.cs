using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using NUnit.Framework;
using System;
using APSIM.Shared.Documentation;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnitTests.Storage;
using Shared.Utilities;
using DocumentFormat.OpenXml.Spreadsheet;

namespace UnitTests.ManagerTests
{

    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
        /// <summary>
        /// This test reproduces a bug in which a simulation could run without
        /// error despite a manager script containing a syntax error.
        /// </summary>
        [Test]
        public void TestManagerWithError()
        {
            var simulations = new Simulations()
            { 
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Name = "Sim",
                        FileName = Path.GetTempFileName(),
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2019, 1, 1),
                                EndDate = new DateTime(2019, 1, 2)
                            },
                            new MockSummary(),
                            new Manager()
                            {
                                Code = "asdf"
                            }
                        }
                    }
                }
            };

            var runner = new Runner(simulations);
            Assert.IsNotNull(runner.Run());
        }

        /// <summary>
        /// This test ensures that scripts aren't recompiled after events have
        /// been hooked up. Such behaviour would cause scripts to not receive
        /// any events, and the old/discarded scripts would receive events.
        /// </summary>
        [Test]
        public void TestScriptNotRebuilt()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.bork.apsimx");
            IModel file = FileFormat.ReadFromString<IModel>(json, e => throw e, false).NewModel as IModel;
            Simulation sim = file.FindInScope<Simulation>();
            Assert.DoesNotThrow(() => sim.Run());
        }

        /// <summary>
        /// Ensures that Manager Scripts are allowed to override the
        /// OnCreated() method.
        /// </summary>
        /// <remarks>
        /// OnCreatedError.apsimx contains a manager script which overrides
        /// the OnCreated() method and throws an exception from this method.
        /// 
        /// This test ensures that an exception is thrown and that it is the
        /// correct exception.
        /// 
        /// The manager in this file is disabled, but its OnCreated() method
        /// should still be called.
        /// </remarks>
        [Test]
        public void ManagerScriptOnCreated()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.OnCreatedError.apsimx");
            List<Exception> errors = new List<Exception>();
            FileFormat.ReadFromString<IModel>(json, e => errors.Add(e), false);

            Assert.NotNull(errors);
            Assert.AreEqual(1, errors.Count, "Encountered the wrong number of errors when opening OnCreatedError.apsimx.");
            Assert.That(errors[0].ToString().Contains("Error thrown from manager script's OnCreated()"), "Encountered an error while opening OnCreatedError.apsimx, but it appears to be the wrong error: {0}.", errors[0].ToString());
        }

        /// <summary>
        /// Reproduces issue #5202. This appears to be due to a bug where manager script parameters are not being 
        /// correctly overwritten by factors of an experiment (more precisely, they are overwritten, and then the 
        /// overwritten values are themselves being overwritten by the original values).
        /// </summary>
        [Test]
        public void TestManagerOverrides()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Manager.ManagerOverrides.apsimx");
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            foreach (Runner.RunTypeEnum runType in Enum.GetValues(typeof(Runner.RunTypeEnum)))
            {
                Runner runner = new Runner(sims);
                List<Exception> errors = runner.Run();
                if (errors != null && errors.Count > 0)
                    throw errors[0];
            }
        }

        /// <summary>
        /// This test ensures one manager model can call another.
        /// </summary>
        [Test]
        public void TestOneManagerCallingAnother()
        {
            var simulations = new Simulations()
            { 
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 1)},
                            new MockSummary(),
                            new MockStorage(),
                            new Manager()
                            {
                                Name = "Manager1",
                                Code = "using Models.Core;" + Environment.NewLine +
                                       "using System;" + Environment.NewLine +
                                       "namespace Models" + Environment.NewLine +
                                       "{" + Environment.NewLine +
                                       "    [Serializable]" + Environment.NewLine +
                                       "    public class Script1 : Model" + Environment.NewLine +
                                       "    {" + Environment.NewLine +
                                       "        public int A = 1;" + Environment.NewLine +
                                       "    }" + Environment.NewLine +
                                       "}"
                            },
                            new Manager()
                            {
                                Name = "Manager2",
                                Code = "using Models.Core;" + Environment.NewLine +
                                       "using System;" + Environment.NewLine +
                                       "namespace Models" + Environment.NewLine +
                                       "{" + Environment.NewLine +
                                       "    [Serializable]" + Environment.NewLine +
                                       "    public class Script2 : Model" + Environment.NewLine +
                                       "    {" + Environment.NewLine +
                                       "        [Link] Script1 otherScript;" + Environment.NewLine +
                                       "        public int B { get { return otherScript.A + 1; } }" + Environment.NewLine +
                                       "    }" + Environment.NewLine +
                                       "}"
                            },
                            new Models.Report()
                            {
                                VariableNames = new string[] { "[Script2].B" },
                                EventNames = new string[] { "[Clock].EndOfDay" }
                            }
                        }
                    }
                }
            };
            //Apsim.InitialiseModel(simulations);

            var storage = simulations.Children[0].Children[2] as MockStorage;

            var runner = new Runner(simulations);
            runner.Run();

            double[] actual = storage.Get<double>("[Script2].B");
            double[] expected = new double[] { 2 };
            Assert.AreNotEqual(expected, actual);
        }

        /// <summary>
        /// This test makes runs one or more tests on each function in the model class.
        /// It tracks if there are any methods or properties that don't have a test in this test.
        /// </summary>
        [Test]
        public void ManagerMethodTests()
        {
            List<string> methodsTested = new List<string>();
            List<string> propertiesTested = new List<string>();

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

            List<MethodInfo> methods = ReflectionUtilities.GetAllMethodsWithoutProperties(typeof(Manager));
            List<PropertyInfo> properties = ReflectionUtilities.GetAllProperties(typeof(Manager), flags, false);

            string basicCode = "";
            basicCode += "using System.Linq;\n";
            basicCode += "using System;\n";
            basicCode += "using Models.Core;\n";
            basicCode += "namespace Models {\n";
            basicCode += "\t[Serializable]\n";
            basicCode += "\tpublic class Script : Model {\n";
            basicCode += "\t\t[Description(\"AProperty\")]\n";
            basicCode += "\t\tpublic string AProperty { get; set; } = \"Hello World\";\n";
            basicCode += "\t\t\tpublic void Document() { return; }\n";
            basicCode += "\t}\n";
            basicCode += "}\n";

            string basicCodeBroken = basicCode.Replace("{", "");

            Simulations sims = new Simulations();
            ScriptCompiler compiler = new ScriptCompiler();
            Manager testManager = new Manager();

            // ---------------
            // Private Methods
            // ----------------

            //---------------------------------------------
            //These two functions work together
            methodsTested.Add("SetCompiler");
            methodsTested.Add("Compiler");
            typeof(Manager).InvokeMember("SetCompiler", flags, null, testManager, new object[] { compiler });
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("Compiler", flags, null, testManager, null));

            //---------------------------------------------
            methodsTested.Add("TryGetCompiler");
            //Should be false if running without a compiler
            testManager = new Manager();
            Assert.False((bool)typeof(Manager).InvokeMember("TryGetCompiler", flags, null, testManager, null));
            //should be found in sims
            testManager.Parent = sims;
            Assert.True((bool)typeof(Manager).InvokeMember("TryGetCompiler", flags, null, testManager, null));
            //check if works assigning directly.
            testManager = new Manager();
            typeof(Manager).InvokeMember("SetCompiler", flags, null, testManager, new object[] { compiler });
            Assert.True((bool)typeof(Manager).InvokeMember("TryGetCompiler", flags, null, testManager, null));

            //---------------------------------------------
            methodsTested.Add("OnStartOfSimulation");
            //should not throw, but not do anything
            testManager = new Manager();
            testManager.Parent = sims;
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("OnStartOfSimulation", flags, null, testManager, new object[] { new object(), new EventArgs() }));
            Assert.IsNull(testManager.Parameters);

            //should work
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("OnStartOfSimulation", flags, null, testManager, new object[] { new object(), new EventArgs() }));
            Assert.AreEqual(1, testManager.Parameters.Count);

            //Should fail, as broken code was last to compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            try { testManager.Code = basicCodeBroken; } catch { }
            try
            {
                typeof(Manager).InvokeMember("OnStartOfSimulation", flags, null, testManager, new object[] { new object(), new EventArgs() });
                Assert.Fail();
            }
            catch { }

            //---------------------------------------------
            methodsTested.Add("SetParametersInScriptModel");
            //Should not throw, but not make parameters
            testManager = new Manager();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", flags, null, testManager, new object[] { }));
            Assert.IsNull(testManager.Parameters);

            //Should make parameters
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            testManager.GetParametersFromScriptModel();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", flags, null, testManager, new object[] { }));
            Assert.AreEqual(1, testManager.Parameters.Count);

            //Should not make parameters
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Enabled = false;
            testManager.Code = basicCode;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", flags, null, testManager, new object[] { }));
            Assert.IsNull(testManager.Parameters);

            //---------------------------------------------
            methodsTested.Add("GetParametersFromScriptModel");
            //Should not throw, but not make parameters
            testManager = new Manager();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("GetParametersFromScriptModel", flags, null, testManager, new object[] { }));
            Assert.IsNull(testManager.Parameters);

            //Should make parameters
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("GetParametersFromScriptModel", flags, null, testManager, new object[] { }));
            Assert.AreEqual(1, testManager.Parameters.Count);

            // ---------------
            // Public Methods
            // ----------------

            //---------------------------------------------
            methodsTested.Add("OnCreated");
            //shouldn't throw, but shouldn't load any script
            testManager = new Manager();
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.IsNull(testManager.Parameters);

            //shouldn't throw, but shouldn't load any script
            testManager = new Manager();
            testManager.Parent = sims;
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.AreEqual(0, testManager.Parameters.Count);

            //should compile the script
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.AreEqual(1, testManager.Parameters.Count);

            //---------------------------------------------
            methodsTested.Add("RebuildScriptModel");
            //should not throw, but should not compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.IsNull(testManager.Parameters);

            //should compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.AreEqual(1, testManager.Parameters.Count);

            //should not compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.Enabled = false;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.IsNull(testManager.Parameters);

            //should not compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.AreEqual(0, testManager.Parameters.Count);

            //should not compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = "";
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.IsNull(testManager.Parameters);

            //should throw
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCodeBroken;
            try { testManager.OnCreated(); } catch { }
            Assert.Throws<Exception>(() => testManager.RebuildScriptModel());
            Assert.IsNull(testManager.Parameters);

            //---------------------------------------------
            methodsTested.Add("Document");
            //should work
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            List<ITag> tags = new List<ITag>();
            foreach (ITag tag in testManager.Document())
                tags.Add(tag);
            Assert.AreEqual(1, tags.Count);

            //should not work
            testManager = new Manager();
            testManager.Code = basicCode;
            testManager.OnCreated();
            tags = new List<ITag>();
            foreach (ITag tag in testManager.Document())
                tags.Add(tag);
            Assert.AreEqual(0, tags.Count);

            // ---------------
            // Properties
            // ----------------

            //---------------------------------------------
            propertiesTested.Add("CodeArray");
            testManager = new Manager();
            testManager.Code = basicCode;
            string[] array = testManager.CodeArray;
            testManager.CodeArray = array;
            string[] array2 = testManager.CodeArray;
            Assert.AreEqual(array, array2);

            //---------------------------------------------
            propertiesTested.Add("Code");
            //empty
            testManager = new Manager();
            testManager.Code = "";
            Assert.AreEqual("", testManager.Code);
            //one space
            testManager = new Manager();
            testManager.Code = " ";
            Assert.AreEqual(" ", testManager.Code);
            //two lines
            testManager = new Manager();
            testManager.Code = " \n ";
            Assert.AreEqual(" \n ", testManager.Code);
            //null
            testManager = new Manager();
            try
            {
                testManager.Code = null;
                Assert.Fail();
            } catch { }
            //code in and out
            testManager = new Manager();
            testManager.Code = basicCode;
            string code = testManager.Code;
            Assert.AreEqual(basicCode, code);
            //should remove \r characters
            code = code.Replace("\n", "\r\n");
            testManager.Code = basicCode;
            Assert.AreNotEqual(code, testManager.Code);
            //should compile
            testManager = new Manager();
            testManager.Parent = sims;
            testManager.Code = basicCode;
            testManager.OnCreated();
            testManager.GetParametersFromScriptModel();
            Assert.AreEqual(1, testManager.Parameters.Count);

            //---------------------------------------------
            propertiesTested.Add("Parameters");
            List<KeyValuePair<string, string>> paras = new List<KeyValuePair<string, string>>();
            paras.Add(new KeyValuePair<string, string>("AProperty", "Hello World"));
            
            testManager = new Manager();
            testManager.Parameters = paras;
            Assert.AreEqual(paras, testManager.Parameters);

            testManager = new Manager();
            testManager.Parameters = null;
            Assert.IsNull(testManager.Parameters);

            //---------------------------------------------
            propertiesTested.Add("Cursor");
            testManager = new Manager();
            ManagerCursorLocation loc = new ManagerCursorLocation();
            loc.TabIndex = 1;
            testManager.Cursor = loc;
            Assert.AreEqual(loc, testManager.Cursor);

            testManager = new Manager();
            testManager.Cursor = null;
            Assert.IsNull(testManager.Cursor);

            //---------------------------------------------
            propertiesTested.Add("SuccessfullyCompiledLast");
            //Private property, cannot be tested.

            foreach (MethodInfo method in methods)
                if (methodsTested.Contains(method.Name) == false)
                    Assert.Fail($"{method.Name} is not tested by an individual unit test.");

            foreach (PropertyInfo prop in properties)
                if (propertiesTested.Contains(prop.Name) == false)
                    Assert.Fail($"{prop.Name} is not tested by an individual unit test.");
        }
    }
}
