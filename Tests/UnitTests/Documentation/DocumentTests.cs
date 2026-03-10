using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using APSIM.Documentation.Models;
using Models.Core;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using APSIM.Documentation;
using APSIM.Core;
using Models;
using Models.Core.Run;

namespace UnitTests.Documentation
{
    /// <summary>This is a test class for the Autodocumentation system</summary>
    [TestFixture]
    public class DocumentationTests
    {
        /// <summary>
        /// This runs through a set of stored apsimx files and generates documentation for them, both validation and tutorials.
        /// If the documentation structure is changed, or some of the crop models are changed drastically, this will run false.
        /// If the stored tagged need to be recreated (due to an intended change), use update resources in the gui
        /// </summary>
        [Test]
        public void TestDocumentationStructure()
        {
            string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
            foreach (string file in TestUtilities.FILES)
            {
                 string resources = Path.Combine(apsimx, "Tests", "Validation", file) + "/";
                if (file == "Report" || file == "Manager")
                    resources = Path.Combine(apsimx, "Examples", "Tutorials") + "/";

                string json = File.ReadAllText(resources+file+".apsimx");
                Simulations sims = FileFormat.ReadFromString<Simulations>(json).Model as Simulations;

                sims.FileName = "/Tests/Validation/"+file+".apsimx";
                if (file == "Report" || file == "Manager")
                    sims.FileName = "/Examples/Tutorials/"+file+".apsimx";

                List<ITag> actualTags = AutoDocumentation.Document(sims);

                string savedJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".json");
                List<ITag> expectedTags = TestUtilities.GetTags(savedJSON);

                MatchTagStructure(expectedTags, actualTags, file);
            }
        }

        ///<summary>
        /// Recursive function that walks the tree of ITags to see if they match.
        ///</summary>
        public void MatchTagStructure(List<ITag> expectedTags, List<ITag> actualTags, string modelName)
        {
            string errorMessage = $"Documentation structure has been changed for the {modelName} model. If this was expected use the Upgrade Resource Files button to update the test files and commit them.";
            Assert.That(actualTags.Count, Is.EqualTo(expectedTags.Count), message: errorMessage);

            for (int i = 0; i < expectedTags.Count; i++) {
                ITag expected = expectedTags[i];
                ITag actual = actualTags[i];
                Assert.That(expected.GetType() == actual.GetType(), message: errorMessage);
                if (expected.GetType() == typeof(Section))
                {
                    Assert.That((actual as Section).Title, Is.EqualTo((expected as Section).Title), message: errorMessage);
                    MatchTagStructure((expected as Section).Children, (actual as Section).Children, modelName);
                }
                else if (expected.GetType() == typeof(Paragraph))
                {
                    string textE = (expected as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    string textA = (actual as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    Assert.That(textA, Is.EqualTo(textE), message: errorMessage);
                }
            }
        }

        /// <summary>
        /// Test that the citation processor replaces citations with links to references section.
        /// </summary>
        [Test]
        public void TestProcessCitationsReplacesCitationWithLink()
        {
            string text = "This is a citation [#brown_plant_2014] and some more text.";
            string expected = "This is a citation [Brown et al., 2014](#brown_plant_2014) and some more text.";
            WebDocs.ProcessCitations(text, out string actual);
            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>
        /// Test that the inert processor replaces references with content that is referenced
        /// </summary>
        [Test]
        public void TestMemoInserts()
        {
            string input;
            string expected;
            string output;
            string memoName = "My Docs";
            Simulations simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Models.Documentation()
                    {
                        Name = memoName,
                        Text = ""
                    }
                }
            };
            simulations.Node = Node.Create(simulations);

            //Check that it works
            input = "This is a reference to the memo name {[Documentation].Name} and some more text.";
            expected = $"This is a reference to the memo name {memoName} and some more text.";
            (simulations.Children[0] as Models.Documentation).Text = input;
            output = WebDocs.ReplaceInserts(input, simulations);
            Assert.That(output, Is.EqualTo(expected));

            //Check that it doesn't try to do this with a code block which has { } in them
            input = "";
            input += "* Soil evaporation is different between the two models. To get near identical, comment out lines 271-275 in EvaporationModel.cs\n";
            input += "\n";
            input += "```c#\n";
            input += "     // If U has changed (due to summer / winter turn over) and infiltration is zero then reset sumes1 to U to stop\n";
            input += "     // artificially entering stage 1 evap. GitHub Issue #8112\n";
            input += "     if (UYesterday != U)\n";
            input += "     {\n";
            input += "         sumes1 = U;\n";
            input += "         sumes2 = CONA * Math.Pow(t, 0.5);\n";
            input += "     }\n";
            input += " ```\n";
            input += " \n";
            expected = input;
            (simulations.Children[0] as Models.Documentation).Text = input;
            output = WebDocs.ReplaceInserts(input, simulations);
            Assert.That(output, Is.EqualTo(expected));
        }
    }
}