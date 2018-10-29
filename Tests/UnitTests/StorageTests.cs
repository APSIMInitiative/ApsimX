using System;
using NUnit.Framework;
using System.IO;
using Models.Storage;
using Models.Core;

namespace UnitTests
{
    /// <summary>
    /// Unit tests for the storage component.
    /// </summary>
    class StorageTests
    {
        /// <summary>
        /// Tests if we can open a database with foreign characters in the path.
        /// </summary>
        [Test]
        public void ForeignCharacterTest()
        {
            string path = Path.Combine(Path.GetTempPath(), "文档.db");
            DataStore storage = new DataStore();
            storage.FileName = path;
            Assert.DoesNotThrow(() => storage.Open(false));
            try
            {
                storage.Close();
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
