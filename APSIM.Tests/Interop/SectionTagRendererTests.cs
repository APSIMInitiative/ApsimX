using System;
using NUnit.Framework;
using APSIM.Server.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;
using System.Collections.Generic;

namespace APSIM.Tests.Interop.Documentation
{
    /// <summary>
    /// Tests for the <see cref="PdfBuilder"/> class.
    /// </summary>
    [TestFixture]
    public class SectionTagRendererTests
    {
        private PdfBuilder pdfBuilder;

        [SetUp]
        internal void SetUp()
        {
            pdfBuilder = new PdfBuilder(new MigraDocCore.DocumentObjectModel.Document(), PdfOptions.Default);
        }

        [Test]
        private void TestHeadings()
        {
            
        }
    }
}
