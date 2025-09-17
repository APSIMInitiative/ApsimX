using NUnit.Framework;
using System;
using APSIM.Shared.Utilities;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

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

        [Test]
        public void GetAllApsimXFilePaths_ValidDirectory_ReturnsCorrectPaths()
        {
            // Arrange
            string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDirectory);

            string file1 = Path.Combine(testDirectory, "file1.apsimx");
            string file2 = Path.Combine(testDirectory, "file2.apsimx");
            string subDirectory = Path.Combine(testDirectory, "SubDir");
            Directory.CreateDirectory(subDirectory);
            string file3 = Path.Combine(subDirectory, "file3.apsimx");

            File.WriteAllText(file1, "Test content");
            File.WriteAllText(file2, "Test content");
            File.WriteAllText(file3, "Test content");

            try
            {
                // Act
                List<string> result = PathUtilities.GetAllApsimXFilePaths(testDirectory);

                // Assert
                Assert.That(result.Count, Is.EqualTo(3));
                Assert.That(result, Does.Contain(Path.GetFullPath(file1)));
                Assert.That(result, Does.Contain(Path.GetFullPath(file2)));
                Assert.That(result, Does.Contain(Path.GetFullPath(file3)));
            }
            finally
            {
                // Cleanup
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void GetAllApsimXFilePaths_NullDirectory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PathUtilities.GetAllApsimXFilePaths(null));
        }

        [Test]
        public void GetAllApsimXFilePaths_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            string nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => PathUtilities.GetAllApsimXFilePaths(nonExistentDirectory));
        }

        [Test]
        public void GetAllApsimXFilePaths_EmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDirectory);

            try
            {
                // Act
                List<string> result = PathUtilities.GetAllApsimXFilePaths(testDirectory);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(0));
            }
            finally
            {
                // Cleanup
                Directory.Delete(testDirectory, true);
            }
        }
    }
}
