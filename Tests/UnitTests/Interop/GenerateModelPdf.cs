using System;
using NUnit.Framework;
using APSIM.Interop.Markdown;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using System.Collections.Generic;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using FormattedText = MigraDocCore.DocumentObjectModel.FormattedText;
using Text = MigraDocCore.DocumentObjectModel.Text;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using Models.Core;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Models.PMF;
using APSIM.Shared.Documentation;

namespace UnitTests.Interop.Documentation
{
    /// <summary>
    /// Tests for generating a model pdf.
    /// </summary>
    [TestFixture]
    public class GeneratetModelPdf
    {
        /// <summary>Initialise the PDF buidler and its document.</summary>
        [Test]
        public void SetUp()
        {
            string imagePath = Path.GetTempPath();
            var modelsAssembly = Assembly.GetAssembly(typeof(Model));
            var json = JObject.Parse(ReflectionUtilities.GetResourceAsString(modelsAssembly, "Models.Resources.Wheat.json"));
            var children = json["Children"] as JArray;
            var model = FileFormat.ReadFromString<Plant>(children[0].ToString(), (ex) => throw ex, false).NewModel;

            var links = new Links();
            links.Resolve(model, allLinks: true, recurse: true, throwOnFail: false);
            var tags = new Section($"The APSIM {model.Name} model", model.Document());

            var writer = new PdfWriter(new PdfOptions(imagePath, null));
            writer.Write("test.pdf", new ITag[] { tags });
        }
    }
}
