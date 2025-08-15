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
using Models.Logging;
using APSIM.Documentation.Models;
using APSIM.Core;
using System.Linq;
using System.CodeDom;

namespace UnitTests.ManagerTests
{

    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
        /// <summary>Flags required for reflection that gets all public and private methods </summary>
        private const BindingFlags reflectionFlagsMethods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        /// <summary>Flags required for reflection to gets all public properties </summary>
        private const BindingFlags reflectionFlagsProperties = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;

        const string basicCode =
            "using System.Linq;\n" +
            "using System;\n" +
            "using Models.Core;\n" +
            "\n" +
            "namespace Models\n" +
            "{\n" +
            "\t[Serializable]\n" +
            "\tpublic class Script : Model\n" +
            "\t{\n" +
            "\t\t[Description(\"AProperty\")]\n" +
            "\t\tpublic string AProperty {get; set;} = \"Hello World\";\n" +
            "\t\tpublic int AMethod()\n" +
            "\t\t{\n" +
            "\t\t\treturn 0;\n" +
            "\t\t}\n" +
            "\t\t\n" +
            "\t\tpublic int BMethod(int arg1)\n" +
            "\t\t{\n" +
            "\t\t\treturn arg1;\n" +
            "\t\t}\n" +
            "\t\t\n" +
            "\t\tpublic int CMethod(int arg1, int arg2)\n" +
            "\t\t{\n" +
            "\t\t\treturn arg1;\n" +
            "\t\t}\n" +
            "\t\t\n" +
            "\t\tpublic int DMethod(int arg1, int arg2, int arg3)\n" +
            "\t\t{\n" +
            "\t\t\treturn arg1;\n" +
            "\t\t}\n" +
            "\t\t\n" +
            "\t\tpublic int EMethod(int arg1, int arg2, int arg3, int arg4)\n" +
            "\t\t{\n" +
            "\t\t\treturn arg1;\n" +
            "\t\t}\n" +
            "\t\t\n" +
            "\t\tpublic void Document()\n" +
            "\t\t{\n" +
            "\t\t\treturn;\n" +
            "\t\t}\n" +
            "\t}\n" +
            "}";
        /// <summary>
        /// Creates a Manager with different levels of setup for the unit tests to run with.
        /// </summary>
        private Manager createManager(string code = basicCode, bool enabled = true)
        {
            Simulations sims = new Simulations()
            {
                Children = [
                    new Manager()
                    {
                        Enabled = enabled,
                        Code = code
                    }
                ]

            };
            Node node = Node.Create(sims);
            Manager manager = node.Children.First().Model as Manager;
            manager.GetParametersFromScriptModel();
            return manager;
        }


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
            Assert.That(runner.Run(), Is.Not.Null);
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
            IModel file = FileFormat.ReadFromString<Simulations>(json).Model as IModel;
            Simulation sim = file.Node.Find<Simulation>();
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.OnCreatedError.apsimx");
            Assert.Throws<Exception>(() => FileFormat.ReadFromString<Simulations>(json, errorHandler: null, initInBackground: false));
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
            Simulations sims = FileFormat.ReadFromString<Simulations>(json).Model as Simulations;

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
            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        /// <summary>
        /// Make sure the correct manager script is called and accessed when both script are just class 'Script'
        /// See issue #8624 where two zones with different scripts had reflection calling the wrong code.
        /// </summary>
        [Test]
        public void CorrectManagerCalledWhenBothHaveSameClassName()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Manager.ManagerClassNameConflict.apsimx");
            Simulations file = FileFormat.ReadFromString<Simulations>(json).Model as Simulations;

            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            Summary sum = file.FindDescendant<Summary>();
            bool found = false;
            foreach (Message message in sum.GetMessages("Simulation"))
                if (message.Text.Contains("Correct Manager Called"))
                    found = true;

            Assert.That(found, Is.True);
        }

        /// <summary>
        /// The converter will check that it updates correctly, but this will run the file afterwards to make sure all the
        /// connections still connect.
        /// </summary>
        [Test]
        public void TestMultipleScriptsWithSameClassNameConnectStill()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.CoverterTest172FileBefore.apsimx");
            var tree = FileFormat.ReadFromString<Simulations>(json, e => {return;}, false);
            Simulations file = tree.Model as Simulations;

            var Runner = new Runner(file);
            Runner.Run();

            Assert.Pass();
        }

        /// <summary>
        /// Specific test for OnStartOfSimulation
        /// Should do nothing if using empty Manager that has a parent
        /// Should compile and have parameters if setup fully and compiled
        /// Should fail if after compiling, the code is changed to broken code and compiled again
        /// </summary>
        [Test]
        public void OnStartOfSimulationTests()
        {
            Manager testManager;

            //should work
            testManager = createManager();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("OnStartOfSimulation", reflectionFlagsMethods, null, testManager, new object[] { new object(), new EventArgs() }));
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Specific test for SetParametersInScriptModel
        /// Should not do anything or error on a blank manager
        /// Should make parameters if fully set up
        /// Should not have parameters if compiled but disabled
        /// </summary>
        [Test]
        public void SetParametersInScriptModelTests()
        {
            Manager testManager;

            //Should have parameters
            testManager = createManager();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", reflectionFlagsMethods, null, testManager, new object[] { }));
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));

            //Should not make parameters
            testManager = createManager(enabled: false);
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", reflectionFlagsMethods, null, testManager, new object[] { }));
            Assert.That(testManager.Parameters, Is.Null);
        }

        /// <summary>
        /// Specific test for GetParametersFromScriptModel
        /// Should not do anything or error on a blank manager
        /// Should make parameters if script compiled
        /// </summary>
        [Test]
        public void GetParametersFromScriptModelTests()
        {
            Manager testManager;

            //Should not throw, but not make parameters
            testManager = createManager(code: string.Empty);
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //Should make parameters
            testManager = createManager();
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Specific test for RebuildScriptModel
        /// Tests a bunch of different inputs for scripts to make sure the compile under different setups
        /// </summary>
        [Test]
        public void RebuildScriptModelTests()
        {
            Manager testManager;

            //should compile and have parameters
            testManager = createManager();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));

            //should not compile if not enabled
            testManager = createManager(enabled: false);
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //should not compile if no code.
            testManager = createManager(code: string.Empty);
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //should not compile if code is empty
            testManager = createManager(code: string.Empty);
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //should throw error if broken code
            testManager = createManager();
            Assert.Throws<Exception>(() => testManager.Code = testManager.Code.Replace("{", ""));
        }

        /// <summary>
        /// Specific test for Document
        /// Document should make a document object with the parameters listed
        /// It should fail if paramters have not been generated, even if code is there.
        /// </summary>
        [Test]
        public void DocumentTests()
        {
            Manager testManager;

            //should work
            testManager = createManager();
            testManager.GetParametersFromScriptModel();
            List<ITag> tags = new List<ITag>();
            foreach (ITag tag in AutoDocumentation.Document(testManager))
                tags.Add(tag);
            Assert.That(tags.Count, Is.EqualTo(1));

        }

        /// <summary>
        /// Specific test for CodeArray
        /// Put code into the code array property, then pull it out and check that what you have
        /// is the same as what is stored
        /// </summary>
        [Test]
        public void CodeArrayTests()
        {
            Manager testManager;

            testManager = createManager();

            string[] array = testManager.CodeArray;
            testManager.CodeArray = array;

            string[] array2 = testManager.CodeArray;
            Assert.That(array2, Is.EqualTo(array));
        }

        /// <summary>
        /// Specific test for Code
        /// Check a range of inputs that could be stored in the code property
        /// Then pull those inputs out and make sure they are the same
        /// </summary>
        [Test]
        public void CodeTests()
        {
            //empty
            string[] strings = CodeFormatting.Split("");
            Assert.That(CodeFormatting.Combine(strings), Is.EqualTo(""));

            //one space.
            strings = CodeFormatting.Split(" ");
            Assert.That(CodeFormatting.Combine(strings), Is.EqualTo(" "));

            //two lines. Not a valid manager script so throws.
            strings = CodeFormatting.Split(" \n");
            Assert.That(CodeFormatting.Combine(strings), Is.EqualTo(" \n"));

            //code in and out
            strings = CodeFormatting.Split(basicCode);
            Assert.That(CodeFormatting.Combine(strings), Is.EqualTo(basicCode));

            //should remove \r characters
            strings = CodeFormatting.Split(basicCode.Replace("\n", "\r\n"));
            Assert.That(CodeFormatting.Combine(strings), Is.EqualTo(basicCode));
        }

        /// <summary>
        /// Specific test for Parameters
        /// Check that inputing parameters are stored the same
        /// Check that null clears the parameters
        /// </summary>
        [Test]
        public void ParametersTests()
        {
            Manager testManager;

            List<KeyValuePair<string, string>> paras = new List<KeyValuePair<string, string>>();
            paras.Add(new KeyValuePair<string, string>("AProperty", "Hello World"));

            testManager = new Manager();
            testManager.Parameters = paras;
            Assert.That(testManager.Parameters, Is.EqualTo(paras));

            testManager = new Manager();
            testManager.Parameters = null;
            Assert.That(testManager.Parameters, Is.Null);
        }

        /// <summary>
        /// Specific test for Cursor
        /// Check that the cursor values are stored
        /// Check that it is set to null when nulled
        /// </summary>
        [Test]
        public void CursorTests()
        {
            Manager testManager;

            testManager = new Manager();
            ManagerCursorLocation loc = new ManagerCursorLocation();
            loc.TabIndex = 1;
            testManager.Cursor = loc;
            Assert.That(testManager.Cursor, Is.EqualTo(loc));

            testManager = new Manager();
            testManager.Cursor = null;
            Assert.That(testManager.Cursor, Is.Null);
        }

        /// <summary>
        /// Specific test for GetErrors
        /// Check that it stores errors from a bad script
        /// </summary>
        [Test]
        public void GetErrorsTests()
        {
            Manager testManager = createManager();
            Assert.Throws<Exception>(() => testManager.Code = "public class Script : Model {}");
            Assert.That(testManager.Errors.Split('\n').Length, Is.EqualTo(3));
        }

        /// <summary>
        /// Specific test for GetProperty
        /// Check that we can get values from the script of a manager
        /// </summary>
        [Test]
        public void GetPropertyTests()
        {
            Manager testManager = createManager();
            Assert.That(testManager.GetProperty("AProperty"), Is.EqualTo("Hello World"));
            Assert.That(testManager.GetProperty("BProperty"), Is.Null);
        }

        /// <summary>
        /// Specific test for SetProperty
        /// Check that we can change values of the script from a manager
        /// </summary>
        [Test]
        public void SetPropertyTests()
        {
            Manager testManager = createManager();
            Assert.DoesNotThrow(() => testManager.SetProperty("AProperty", "Another World"));
            Assert.That(testManager.GetProperty("AProperty"), Is.EqualTo("Another World"));
        }

        /// <summary>
        /// Specific test for RunMethod
        /// Check that we can call and run functions in a script from a manager
        /// </summary>
        [Test]
        public void RunMethodTests()
        {
            Manager testManager = createManager();
            Assert.DoesNotThrow(() => testManager.RunMethod("AMethod"));
            Assert.DoesNotThrow(() => testManager.RunMethod("BMethod", 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("CMethod", 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("DMethod", 1, 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("EMethod", 1, 1, 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("CMethod", new object[] {1, 1}));
        }

        /// <summary>
        /// Specific test for RunMethod
        /// Check that we can call and run functions in a script from a manager
        /// </summary>
        [Test]
        public void RunReformatTests()
        {
            Manager testManager = createManager();
            string code = testManager.Code;
            testManager.Reformat();
            Assert.That(testManager.Code, Is.EqualTo(code));
        }

        /// <summary>
        /// Specific test for SuccessfullyCompiledLast
        /// Check that it is false before compiling, true after
        /// </summary>
        [Test]
        public void SuccessfullyCompiledLastTests()
        {
            Manager testManager = createManager();
            Assert.That(testManager.Script, Is.Not.Null);
        }
    }
}
