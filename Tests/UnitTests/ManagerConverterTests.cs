

namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        public void ManagerConverter_Declarations()
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
                "        [Link(Type = LinkType.Descendant, ByName = true)] " + Environment.NewLine +
                "        Soil mySoil;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            ManagerConverter converter = new ManagerConverter();
            converter.Read(script);

            var declarations = converter.GetDeclarations();

            Assert.AreEqual(declarations[0].LineIndex, 5);
            Assert.AreEqual(declarations[0].InstanceName, "mySolutes1");
            Assert.AreEqual(declarations[0].TypeName, "SoluteManager");
            Assert.AreEqual(declarations[0].Attributes[0], "[Link]");

            Assert.AreEqual(declarations[1].LineIndex, 8);
            Assert.AreEqual(declarations[1].InstanceName, "fert");
            Assert.AreEqual(declarations[1].TypeName, "Fertiliser");
            Assert.IsTrue(declarations[1].Attributes.Contains("[Link]"));
            Assert.IsTrue(declarations[1].Attributes.Contains("[Units(0-1)]"));

            Assert.AreEqual(declarations[2].LineIndex, 10);
            Assert.AreEqual(declarations[2].InstanceName, "mySoil");
            Assert.AreEqual(declarations[2].TypeName, "Soil");
            Assert.AreEqual(declarations[2].Attributes[0], "[Link(Type = LinkType.Descendant, ByName = true)]");
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

        /// <summary>
        /// Ensures the SearchReplaceManagerText method works correctly.
        /// </summary>
        [Test]
        public void ReplaceManagerTextTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.ReplaceManagerText.json");
            JObject rootNode = JObject.Parse(json);

            var manager = new ManagerConverter(rootNode);

            string newText = "new text";
            manager.Replace("original text", newText);

            // Ensure the code was modified correctly.
            Assert.AreEqual(newText + Environment.NewLine, manager.ToString());

            // Ensure that passing in a null search string causes no changes.
            manager.Replace(null, "test");
            Assert.AreEqual(newText + Environment.NewLine, manager.ToString());

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var childWithNoCode = new ManagerConverter(JsonUtilities.Children(rootNode).First());
            childWithNoCode.Replace("test1", "test2");
            Assert.Null(childWithNoCode.ToString());
        }

        /// <summary>
        /// Ensures the ReplaceManagerCodeUsingRegex method works correctly.
        /// </summary>
        [Test]
        public void ReplaceManagerCodeRegexTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.ReplaceManagerTextRegex.json");
            JObject rootNode = JObject.Parse(json);

            var manager = new ManagerConverter(rootNode);

            // The manager's code is "original text".
            // This regular expression will effectively remove the first space.
            // There are simpler ways to achieve this but this method tests
            // backreferencing.
            string newText = "originaltext\r\n";
            manager.ReplaceRegex(@"([^\s]*)\s", @"$1");
            Assert.AreEqual(manager.ToString(), newText);

            // Ensure that passing in a null search string causes no changes.
            manager.ReplaceRegex(null, "test");
            Assert.AreEqual(manager.ToString(), newText);

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var childWithNoCode = new ManagerConverter(JsonUtilities.Children(rootNode).First());
            childWithNoCode.ReplaceRegex("test1", "test2");
            Assert.Null(childWithNoCode.ToString());
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has an empty script.
        /// </summary>
        [Test]
        public void AddManagerDeclarationToEmptyScript()
        {
            JObject rootNode = new JObject();
            rootNode["Code"] = "using System;";
            var manager = new ManagerConverter(rootNode);

            manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(manager.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has no declarations.
        /// </summary>
        [Test]
        public void AddManagerDeclarationToEmptyDeclarationSection()
        {
            JObject rootNode = new JObject();
            rootNode["Code"] =
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            var manager = new ManagerConverter(rootNode);

            manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(manager.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has no declarations.
        /// </summary>
        [Test]
        public void AddManagerDeclarationToExistingDeclarationSection()
        {
            JObject rootNode = new JObject();
            rootNode["Code"] =
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        A B;" + Environment.NewLine + 
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            var manager = new ManagerConverter(rootNode);

            manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(manager.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private A B;" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has no declarations.
        /// </summary>
        [Test]
        public void AddManagerDeclarationHandleProperties()
        {
            JObject rootNode = new JObject();
            rootNode["Code"] =
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] private A B = null;" + Environment.NewLine +
                "        [Link] " + Environment.NewLine +
                "        public C D;" + Environment.NewLine +
                "        [Link] E F;" + Environment.NewLine +
                "        [Description(\"Turn ferliser applications on? \")]" + Environment.NewLine +
                "        public yesnoType AllowFertiliser { get; set; }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            var manager = new ManagerConverter(rootNode);

            manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(manager.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] private A B;" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        public C D;" + Environment.NewLine +
                "        [Link] private E F;" + Environment.NewLine + 
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "        [Description(\"Turn ferliser applications on? \")]" + Environment.NewLine +
                "        public yesnoType AllowFertiliser { get; set; }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }
    }
}
