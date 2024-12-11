using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using APSIM.Documentation.Models;
using Models.Core;
using Models.Core.ApsimFile;
using System.Collections.Generic;
using NUnit.Framework;

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
            foreach (string file in APSIM.Documentation.TestUtilities.FILES)
            {
                //read in our base test that we'll use for this
                string json = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".apsimx");
                Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

                if (file == "Report" || file == "Manager" || file == "Morris")
                    sims.FileName = "/Examples/Tutorials/"+file+".apsimx";
                else
                    sims.FileName = "/Tests/Validation/"+file+".apsimx";

                List<ITag> actualTags = AutoDocumentation.Document(sims);

                string savedJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".json");
                List<ITag> expectedTags = APSIM.Documentation.TestUtilities.GetTags(savedJSON);

                MatchTagStructure(expectedTags, actualTags);
            }
        }

        ///<summary>
        /// Recursive function that walks the tree of ITags to see if they match.
        ///</summary>
        public void MatchTagStructure(List<ITag> expectedTags, List<ITag> actualTags)
        {
            string errorMessage = "Documentation structure has been changed for a model. If this was expected use the Upgrade Resource Files button to update the test files and commit them.";
            Assert.That(actualTags.Count, Is.EqualTo(expectedTags.Count), message: errorMessage);

            for (int i = 0; i < expectedTags.Count; i++) {
                ITag expected = expectedTags[i];
                ITag actual = actualTags[i];
                Assert.That(expected.GetType() == actual.GetType(), message: errorMessage);
                if (expected.GetType() == typeof(Section))
                {
                    Assert.That((actual as Section).Title, Is.EqualTo((expected as Section).Title), message: errorMessage);
                    MatchTagStructure((expected as Section).Children, (actual as Section).Children);
                }
                else if (expected.GetType() == typeof(Paragraph))
                {
                    string textE = (expected as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    string textA = (actual as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    Assert.That(textA, Is.EqualTo(textE), message: errorMessage);
                }
            }
        }
    }
}