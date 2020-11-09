using NUnit.Framework;
using System;
using APSIM.Shared.Utilities;
using System.IO;
using System.Reflection;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class PathUtilitiesTests
    {
        [Test]
        public void TestGetRelativePath()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string binDir = Path.GetDirectoryName(assembly);
            string rootDir = Directory.GetParent(binDir).FullName;
            string subDir = Path.Combine(binDir, "a");

            // string rel = "Bin/a";
            string rel = Path.Combine(Path.GetFileName(Path.GetDirectoryName(assembly)), "a");

            Assert.AreEqual("a", PathUtilities.GetRelativePath(subDir, assembly));
            // Passing in a directory name as relative path will give a different
            // result to passing the name of a file inside that directory.
            Assert.AreEqual(rel, PathUtilities.GetRelativePath(subDir, binDir));

            Assert.AreEqual(subDir, PathUtilities.GetRelativePath(subDir, null));
            Assert.AreEqual(subDir, PathUtilities.GetRelativePath(subDir, ""));
            Assert.AreEqual(null, PathUtilities.GetRelativePath(null, binDir));
            Assert.AreEqual(null, PathUtilities.GetRelativePath(null, null));
            Assert.AreEqual(null, PathUtilities.GetRelativePath(null, ""));
            Assert.AreEqual("", PathUtilities.GetRelativePath("", binDir));
            Assert.AreEqual("", PathUtilities.GetRelativePath("", null));
            Assert.AreEqual("", PathUtilities.GetRelativePath("", ""));
        }

        [Test]
        public void TestGetAbsolutePath()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string bin = Path.GetDirectoryName(assembly);
            string subDir = Path.Combine(bin, "a");
            string apsimxDir = Directory.GetParent(bin).FullName;
            // string rel = "Bin/a";
            string parent = Path.Combine(Directory.GetParent(bin).FullName, "a");

            Assert.AreEqual(subDir, PathUtilities.GetAbsolutePath("a", assembly));
            Assert.AreEqual(subDir, PathUtilities.GetAbsolutePath("a", bin));

            Assert.AreEqual("a", PathUtilities.GetAbsolutePath("a", null));
            Assert.AreEqual("a", PathUtilities.GetAbsolutePath("a", ""));
            Assert.AreEqual(null, PathUtilities.GetAbsolutePath(null, subDir));
            Assert.AreEqual(null, PathUtilities.GetAbsolutePath(null, null));
            Assert.AreEqual(null, PathUtilities.GetAbsolutePath(null, ""));
            Assert.AreEqual("", PathUtilities.GetAbsolutePath("", subDir));
            Assert.AreEqual("", PathUtilities.GetAbsolutePath("", null));
            Assert.AreEqual("", PathUtilities.GetAbsolutePath("", ""));
            Assert.AreEqual(apsimxDir, PathUtilities.GetAbsolutePath("%root%", bin));
            Assert.AreEqual(apsimxDir, PathUtilities.GetAbsolutePath("%root%", null));
            Assert.AreEqual(apsimxDir, PathUtilities.GetAbsolutePath("%root%", ""));
            Assert.AreEqual(bin, PathUtilities.GetAbsolutePath("%root%\\Bin", ""));
        }
    }
}
