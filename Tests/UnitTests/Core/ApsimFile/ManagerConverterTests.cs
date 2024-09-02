

namespace UnitTests.Core.ApsimFile
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
            Assert.That(converter.GetUsingStatements(), Is.EqualTo( 
                            new string[] { "System", "Models.Soils", "APSIM.Shared.Utilities" }));
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
            Assert.That(converter.ToString(), Is.EqualTo(
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine));

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

            Assert.That(declarations[0].LineIndex, Is.EqualTo(5));
            Assert.That(declarations[0].InstanceName, Is.EqualTo("mySolutes1"));
            Assert.That(declarations[0].TypeName, Is.EqualTo("SoluteManager"));
            Assert.That(declarations[0].Attributes[0], Is.EqualTo("[Link]"));

            Assert.That(declarations[1].LineIndex, Is.EqualTo(8));
            Assert.That(declarations[1].InstanceName, Is.EqualTo("fert"));
            Assert.That(declarations[1].TypeName, Is.EqualTo("Fertiliser"));
            Assert.That(declarations[1].Attributes.Contains("[Link]"), Is.True);
            Assert.That(declarations[1].Attributes.Contains("[Units(0-1)]"), Is.True);

            Assert.That(declarations[2].LineIndex, Is.EqualTo(10));
            Assert.That(declarations[2].InstanceName, Is.EqualTo("mySoil"));
            Assert.That(declarations[2].TypeName, Is.EqualTo("Soil"));
            Assert.That(declarations[2].Attributes[0], Is.EqualTo("[Link(Type = LinkType.Descendant, ByName = true)]"));
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
            Assert.That(methods.Count, Is.EqualTo(2));
            Assert.That(methods[0].InstanceName, Is.EqualTo("mySolutes1"));
            Assert.That(methods[0].MethodName, Is.EqualTo("Add"));
            Assert.That(methods[0].Arguments, Is.EqualTo(new string[] { "arg1", "arg2" }));
            Assert.That(methods[1].InstanceName, Is.EqualTo("mySolutes2"));
            Assert.That(methods[1].MethodName, Is.EqualTo("Add"));
            Assert.That(methods[1].Arguments, Is.EqualTo(new string[] { "arg3", "arg4" }));
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
            Assert.That(converter.FindMethodCalls("SoluteManager", "Add").Count, Is.EqualTo(0));

            // Make sure we find new one.
            var foundMethod = converter.FindMethodCalls("SoluteManager", "Add2")[0];
            Assert.That(foundMethod.LineIndex, Is.EqualTo(8));
            Assert.That(foundMethod.InstanceName, Is.EqualTo("mySolutes1"));
            Assert.That(foundMethod.MethodName, Is.EqualTo("Add2"));
            Assert.That(foundMethod.Arguments, Is.EqualTo(new string[] { "10" }));
        }

        /// <summary>
        /// Ensures the SearchReplaceManagerText method works correctly.
        /// </summary>
        [Test]
        public void ReplaceManagerTextTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ManagerConverterTestsReplaceManagerText.json");
            JObject rootNode = JObject.Parse(json);

            var manager = new ManagerConverter(rootNode);

            string newText = "new text";
            manager.Replace("original text", newText);

            // Ensure the code was modified correctly.
            Assert.That(manager.ToString(), Is.EqualTo(newText + Environment.NewLine));

            // Ensure that passing in a null search string causes no changes.
            manager.Replace(null, "test");
            Assert.That(manager.ToString(), Is.EqualTo(newText + Environment.NewLine));

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var childWithNoCode = new ManagerConverter(JsonUtilities.Children(rootNode).First());
            childWithNoCode.Replace("test1", "test2");
            Assert.That(childWithNoCode.ToString(), Is.Null);
        }

        /// <summary>
        /// Ensures the ReplaceManagerCodeUsingRegex method works correctly.
        /// </summary>
        [Test]
        public void ReplaceManagerCodeRegexTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ManagerConverterTestsReplaceManagerTextRegex.json");
            JObject rootNode = JObject.Parse(json);

            var manager = new ManagerConverter(rootNode);

            // The manager's code is "original text".
            // This regular expression will effectively remove the first space.
            // There are simpler ways to achieve this but this method tests
            // backreferencing.
            string newText = "originaltext" + Environment.NewLine;
            manager.ReplaceRegex(@"([^\s]*)\s", @"$1");
            Assert.That(manager.ToString(), Is.EqualTo(newText));

            // Ensure that passing in a null search string causes no changes.
            manager.ReplaceRegex(null, "test");
            Assert.That(manager.ToString(), Is.EqualTo(newText));

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var childWithNoCode = new ManagerConverter(JsonUtilities.Children(rootNode).First());
            childWithNoCode.ReplaceRegex("test1", "test2");
            Assert.That(childWithNoCode.ToString(), Is.Null);
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
            Assert.That(manager.ToString(), Is.EqualTo(
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine));
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
            Assert.That(manager.ToString(), Is.EqualTo(
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine));
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
            Assert.That(manager.ToString(), Is.EqualTo(
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
                "}" + Environment.NewLine));
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
            Assert.That(manager.ToString(), Is.EqualTo(
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
                "}" + Environment.NewLine));
        }
    }
}
