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

            Assert.That(PathUtilities.GetRelativePath(subDir, assembly), Is.EqualTo("a"));
            // Passing in a directory name as relative path will give a different
            // result to passing the name of a file inside that directory.
            Assert.That(PathUtilities.GetRelativePath(subDir, binDir), Is.EqualTo(rel));

            Assert.That(PathUtilities.GetRelativePath(subDir, null), Is.EqualTo(subDir));
            Assert.That(PathUtilities.GetRelativePath(subDir, ""), Is.EqualTo(subDir));
            Assert.That(PathUtilities.GetRelativePath(null, binDir), Is.Null);
            Assert.That(PathUtilities.GetRelativePath(null, null), Is.Null);
            Assert.That(PathUtilities.GetRelativePath(null, ""), Is.Null);
            Assert.That(PathUtilities.GetRelativePath("", binDir), Is.EqualTo(""));
            Assert.That(PathUtilities.GetRelativePath("", null), Is.EqualTo(""));
            Assert.That(PathUtilities.GetRelativePath("", ""), Is.EqualTo(""));
        }

        [Test]
        public void TestGetAbsolutePath()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string bin = Path.GetDirectoryName(assembly);
            string subDir = Path.Combine(bin, "a");
            string apsimxDir = new DirectoryInfo(Path.Combine(bin, "..", "..", "..")).FullName;
            // string rel = "Bin/a";

            Assert.That(PathUtilities.GetAbsolutePath("a", assembly), Is.EqualTo(subDir));
            Assert.That(PathUtilities.GetAbsolutePath("a", bin), Is.EqualTo(subDir));

            Assert.That(PathUtilities.GetAbsolutePath("a", null), Is.EqualTo("a"));
            Assert.That(PathUtilities.GetAbsolutePath("a", ""), Is.EqualTo("a"));
            Assert.That(PathUtilities.GetAbsolutePath(null, subDir), Is.Null);
            Assert.That(PathUtilities.GetAbsolutePath(null, null), Is.Null);
            Assert.That(PathUtilities.GetAbsolutePath(null, ""), Is.Null);
            Assert.That(PathUtilities.GetAbsolutePath("", subDir), Is.EqualTo(""));
            Assert.That(PathUtilities.GetAbsolutePath("", null), Is.EqualTo(""));
            Assert.That(PathUtilities.GetAbsolutePath("", ""), Is.EqualTo(""));
            Assert.That(PathUtilities.GetAbsolutePath("%root%", bin), Is.EqualTo(apsimxDir));
            Assert.That(PathUtilities.GetAbsolutePath("%root%", null), Is.EqualTo(apsimxDir));
            Assert.That(PathUtilities.GetAbsolutePath("%root%", ""), Is.EqualTo(apsimxDir));
            Assert.That(PathUtilities.GetAbsolutePath("%root%\\bin", ""), Is.EqualTo(Path.Combine(apsimxDir, "bin")));
        }
    }
}
