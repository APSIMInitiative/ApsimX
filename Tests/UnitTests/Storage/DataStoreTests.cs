using Models.Core;
using Models.Storage;
using NUnit.Framework;
using System.IO;

namespace UnitTests.Storage
{
    [TestFixture]
    public class DataStoreTests
    {
        [Test]
        public void TestFileNameChange()
        {
            Simulations sims = Utilities.GetRunnableSim();
            IDataStore storage = sims.FindInScope<IDataStore>();

            // Write the simulations to disk.
            sims.Write(sims.FileName);

            // Record the database's filename.
            string oldDbFileName = storage.FileName;

            // Write the simulations to disk under a new filename.
            sims.Write(Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"));

            // Record the database's new filename.
            string newDbFileName = storage.FileName;

            // The new file name should not be the same as the old one.
            Assert.That(oldDbFileName, Is.Not.EqualTo(newDbFileName));
        }
    }
}
