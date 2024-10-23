using System;
using NUnit.Framework;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Extensions;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;

namespace UnitTests.Interop.PdfRendering
{
    /// <summary>
    /// Tests for <see cref="SectionExtensions" /> class.
    /// </summary>
    [TestFixture]
    public class SectionExtensionsTests
    {
        /// <summary>
        /// The document used for running tests. Each test can assume
        /// that the document is initialised with one empty section.
        /// </summary>
        private Document document;

        [SetUp]
        public void SetUp()
        {
            document = new Document();
            document.AddSection();
        }

        /// <summary>
        /// Ensure that calls to GetLastTable() fail with an appropriate
        /// exception if we pass in a null section.
        /// </summary>
        [Test]
        public void TestGetLastTableNullSection()
        {
            Assert.Throws<ArgumentNullException>(() => SectionExtensions.GetLastTable(null));
        }

        /// <summary>
        /// Ensure GetLastTable() works correctly on a section with a given
        /// number of tables.
        /// </summary>
        /// <param name="numTables">Number of tables to add to the section.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TestGetLastTable(int numTables)
        {
            Section section = document.LastSection;
            List<Table> tables = new List<Table>();
            for (int i = 0; i < numTables; i++)
                tables.Add(section.AddTable());
            if (numTables <= 0)
                Assert.Throws<InvalidOperationException>(() => section.GetLastTable());
            else
                Assert.That(section.GetLastTable(), Is.EqualTo(tables[numTables - 1]));
        }
    }
}
