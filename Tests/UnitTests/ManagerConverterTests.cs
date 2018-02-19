

namespace UnitTests
{
    using Models.Core.ApsimFile;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Test the writer's load/save .apsimx capability 
    /// </summary>
    [TestFixture]
    public class ManagerConverterTests
    {
        /// <summary>Ensure we can get using statements from manager</summary>
        [Test]
        public void ManagerConverter_GetUsingStatements()
        {
            string script =
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System" + Environment.NewLine +
                Environment.NewLine +
                "using Models.Soils;" + Environment.NewLine +
                "using APSIM.Shared.Utilities;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);
            Assert.AreEqual(converter.GetUsingStatements(), 
                            new string[] { "System", "Models.Soils", "APSIM.Shared.Utilities" });
        }

        /// <summary>Ensure we can set using statements</summary>
        [Test]
        public void ManagerConverter_SetUsingStatements()
        {
            string script =
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System" + Environment.NewLine +
                Environment.NewLine +
                "using Models.Soils;" + Environment.NewLine +
                "using APSIM.Shared.Utilities;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);
            converter.SetUsingStatements(new string[] { "System" });
            Assert.AreEqual(converter.ToString(),
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine);

        }

        /// <summary>Ensure we can find declarations</summary>
        [Test]
        public void ManagerConverter_FindDeclaration()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager   mySolutes1;" + Environment.NewLine +
                "        [Link] " + Environment.NewLine +
                "        [Units(0-1)] " + Environment.NewLine +
                "        Fertiliser  fert;" + Environment.NewLine +
                "        [ChildLinkByName] " + Environment.NewLine +
                "        Soil mySoil;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);
            Declaration declaration1 = converter.FindDeclaration("mySolutes1");
            Assert.AreEqual(declaration1.LineIndex, 5);
            Assert.AreEqual(declaration1.InstanceName, "mySolutes1");
            Assert.AreEqual(declaration1.TypeName, "SoluteManager");
            Assert.AreEqual(declaration1.Attributes[0], "[Link]");

            Declaration declaration2 = converter.FindDeclaration("fert");
            Assert.AreEqual(declaration2.LineIndex, 8);
            Assert.AreEqual(declaration2.InstanceName, "fert");
            Assert.AreEqual(declaration2.TypeName, "Fertiliser");
            Assert.IsTrue(declaration2.Attributes.Contains("[Link]"));
            Assert.IsTrue(declaration2.Attributes.Contains("[Units(0-1)]"));

            Declaration declaration3 = converter.FindDeclaration("mySoil");
            Assert.AreEqual(declaration3.LineIndex, 10);
            Assert.AreEqual(declaration3.InstanceName, "mySoil");
            Assert.AreEqual(declaration3.TypeName, "Soil");
            Assert.AreEqual(declaration3.Attributes[0], "[ChildLinkByName]");
        }

        /// <summary>Ensure we can find method calls</summary>
        [Test]
        public void ManagerConverter_FindMethodCalls()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager mySolutes1;" + Environment.NewLine +
                "        [Link] " +
                "        SoluteManager mySolutes2;" + Environment.NewLine +
                "        Fertiliser fert;" + Environment.NewLine +
                "        private void OnSimulationCommencing(object sender, EventArgs e)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            mySolutes1.Add(arg1, arg2);" + Environment.NewLine +
                "            mySolutes2.Add (arg3,arg4);" + Environment.NewLine +
                "            fake.Add (arg3,arg4);" + Environment.NewLine +
                "            fert.Add (arg3,arg4);" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);
            List<MethodCall> methods = converter.FindMethodCalls("SoluteManager", "Add");
            Assert.AreEqual(methods.Count, 2);
            Assert.AreEqual(methods[0].InstanceName, "mySolutes1");
            Assert.AreEqual(methods[0].MethodName, "Add");
            Assert.AreEqual(methods[0].Arguments, new string[] { "arg1", "arg2" });
            Assert.AreEqual(methods[1].InstanceName, "mySolutes2");
            Assert.AreEqual(methods[1].MethodName, "Add");
            Assert.AreEqual(methods[1].Arguments, new string[] { "arg3", "arg4" });
        }

        /// <summary>Ensure we can set method call</summary>
        [Test]
        public void ManagerConverter_SetMethodCall()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager mySolutes1;" + Environment.NewLine +
                "        private void OnSimulationCommencing(object sender, EventArgs e)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            mySolutes1.Add(arg1, arg2);" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);

            MethodCall method = new MethodCall();
            method.LineIndex = 8;
            method.InstanceName = "mySolutes1";
            method.MethodName = "Add2";
            method.Arguments = new List<string>();
            method.Arguments.Add("10");

            converter.SetMethodCall(method);

            // Make sure we can't find old one.
            Assert.AreEqual(converter.FindMethodCalls("SoluteManager", "Add").Count, 0);

            // Make sure we find new one.
            var foundMethod = converter.FindMethodCalls("SoluteManager", "Add2")[0];
            Assert.AreEqual(foundMethod.LineIndex, 8);
            Assert.AreEqual(foundMethod.InstanceName, "mySolutes1");
            Assert.AreEqual(foundMethod.MethodName, "Add2");
            Assert.AreEqual(foundMethod.Arguments, new string[] { "10" });
        }
    }
}
