namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Report;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestFixture]
    public class TestDataStore
    {
        private string fileName;

        [SetUp]
        public void Initialise()
        {
            fileName = Path.Combine(Path.GetTempPath(), "TestDataStore.db");
            File.Delete(fileName);
            
            string sqliteSourceFileName = FindSqlite3DLL();

            string sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            if (!File.Exists(sqliteFileName))
            {
                File.Copy(sqliteSourceFileName, sqliteFileName);
            } 
        }

        [TearDown]
        public void Cleanup()
        {
            File.Delete(fileName);
        }


        /// <summary>
        /// Find an return the database file name.
        /// </summary>
        /// <returns>The filename</returns>
        public static string FindSqlite3DLL()
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (directory != null)
            {
                string[] directories = Directory.GetDirectories(directory, "Bin");
                if (directories.Length == 1)
                {
                    string[] files = Directory.GetFiles(directories[0], "sqlite3.dll");
                    if (files.Length == 1)
                    {
                        return files[0];
                    }
                }

                directory = Path.GetDirectoryName(directory); // parent directory
            }

            throw new Exception("Cannot find apsimx bin directory");
        }

        [Test]
        public void EnsureDatastoreWritesToDB()
        {
            using (DataStore2 storage = new DataStore2(fileName))
            {
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim2" },
                                     simulationNamesBeingRun: new string[] { "Sim2" });

                string[] columnNames1 = new string[] { "Col1", "Col2" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 1.0, 11.0 });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 2.0, 12.0 });
                storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 3.0, 13.0 });
                storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 4.0, 14.0 });

                string[] columnNames2 = new string[] { "Col3", "Col4" };
                storage.WriteRow("Sim1", "Report2", columnNames2, new string[] { null, "(g/m2)" }, new object[] { 21.0, 31.0 });
                storage.WriteRow("Sim1", "Report2", columnNames2, new string[] { null, "(g/m2)" }, new object[] { 22.0, 32.0 });
                storage.WriteRow("Sim2", "Report2", columnNames2, new string[] { null, "(g/m2)" }, new object[] { 23.0, 33.0 });
                storage.WriteRow("Sim2", "Report2", columnNames2, new string[] { null, "(g/m2)" }, new object[] { 24.0, 34.0 });
                storage.EndWriting();
            }
        }
    }
}