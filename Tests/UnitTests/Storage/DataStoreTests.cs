using Models.Core;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Storage
{
    [TestFixture]
    public class DataStoreTests
    {
        [Test]
        public void TestFileNameChange()
        {
            Simulations sims = Utilities.GetRunnableSim();
            IDataStore storage = Apsim.Find(sims, typeof(IDataStore)) as IDataStore;

            // Write the simulations to disk.
            storage.Close();
            sims.Write(sims.FileName);
            storage.Open();

            // Record the .db file's filename.
            string oldDbFileName = storage.FileName;

            // Write the simulations to disk under a new filename.
            storage.Close();
            sims.Write(Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"));
            storage.Open();

            // Record the .db file's new filename.
            string newDbFileName = storage.FileName;

            // The new .db file name should not be the same as the old one.
            Assert.AreNotEqual(oldDbFileName, newDbFileName);
        }
    }
}
