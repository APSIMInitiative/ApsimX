using NUnit.Framework;
using System;
using System.IO;
using APSIM.Workflow;
using System.IO.Compression;
using System.Linq;

namespace UnitTests.APSIMWorkflowTests;

[TestFixture]
public class PayloadUtilitiesTest
{
    [Test]
    public void GetAllFilesMatchingPath_ValidSource_ReturnsMatchingFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string testFile1 = Path.Combine(testDirectory, "test1.txt");
        string testFile2 = Path.Combine(testDirectory, "test2.txt");
        File.WriteAllText(testFile1, "Test file 1 content");
        File.WriteAllText(testFile2, "Test file 2 content");

        string source = Path.Combine(testDirectory, "*.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result, Does.Contain(testFile1));
            Assert.That(result, Does.Contain(testFile2));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetAllFilesMatchingPath_InvalidSource_ThrowsArgumentNullException()
    {
        // Arrange
        string source = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PayloadUtilities.GetAllFilesMatchingPath(source));
    }

    [Test]
    public void GetAllFilesMatchingPath_NoMatchingFiles_ReturnsEmptyArray()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string source = Path.Combine(testDirectory, "*.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetAllFilesMatchingPath_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "*.txt");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => PayloadUtilities.GetAllFilesMatchingPath(source));
    }

    [Test]
    public void GetAllFilesMatchingPath_CaseInsensitiveMatching_ReturnsMatchingFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string testFile = Path.Combine(testDirectory, "TESTFILE.TXT");
        
        File.WriteAllText(testFile, "Test file content");

        string testFileLower = Path.Combine(testDirectory, "testfile.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(testFileLower);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(1, Is.EqualTo(result.Length));
            Assert.That(result, Contains.Item(testFile));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetActualFilePath_SingleMatchingFile_ReturnsFilePath()
    {
        // Arrange
        string[] matchingFiles = { "file1.txt" };

        // Act
        string result = PayloadUtilities.GetActualFilePath(matchingFiles);

        // Assert
        Assert.That(result, Is.EqualTo("file1.txt"));
    }

    [Test]
    public void GetActualFilePath_MultipleMatchingFiles_ThrowsException()
    {
        // Arrange
        string[] matchingFiles = { "file1.txt", "file2.txt" };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.GetActualFilePath(matchingFiles));
        Assert.That(ex.Message, Is.EqualTo("Multiple files found matching the weather file path."));
    }

    [Test]
    public void GetActualFilePath_NoMatchingFiles_ReturnsEmptyString()
    {
        // Arrange
        string[] matchingFiles = Array.Empty<string>();

        // Act
        string result = PayloadUtilities.GetActualFilePath(matchingFiles);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }


    [Test]
    public void RemoveUnusedFilesFromArchive_ValidZipFile_RemovesUnusedFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        string validFile = Path.Combine(testDirectory, "valid.apsimx");
        string invalidFile = Path.Combine(testDirectory, "invalid.txt");

        File.WriteAllText(validFile, "Valid file content");
        File.WriteAllText(invalidFile, "Invalid file content");
        
        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);
        File.Delete(validFile);
        File.Delete(invalidFile);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            Assert.That(archive.Entries.Count, Is.EqualTo(1));
            Assert.That(archive.Entries[0].FullName, Is.EqualTo("valid.apsimx"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
            File.Delete(zipFilePath);
        }
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_NullZipFilePath_ThrowsException()
    {
        // Arrange
        string zipFilePath = null;

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath));
        Assert.That(ex.Message, Is.EqualTo("Error: Zip file path is null while trying to remove unused files from payload archive."));
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_NonExistentZipFile_ThrowsException()
    {
        // Arrange
        string zipFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.zip");

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath));
        Assert.That(ex.Message, Is.EqualTo("Error: Zip file does not exist while trying to remove unused files from payload archive."));
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_EmptyZipFile_NoChanges()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            Assert.That(archive.Entries.Count, Is.EqualTo(0));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
            File.Delete(zipFilePath);
        }
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_ArchiveWithOnlyValidFiles_NoChanges()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        string validFile1 = Path.Combine(testDirectory, "file1.apsimx");
        string validFile2 = Path.Combine(testDirectory, "file2.csv");

        File.WriteAllText(validFile1, "Valid file 1 content");
        File.WriteAllText(validFile2, "Valid file 2 content");

        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);
        File.Delete(validFile1);
        File.Delete(validFile2);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);           
            Assert.That(archive.Entries.Count, Is.EqualTo(2));
            string[] expectedFiles = { "file1.apsimx", "file2.csv" };
            Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EquivalentTo(expectedFiles));
        }
        finally
        {
            // Cleanup
            File.Delete(zipFilePath);
            Directory.Delete(testDirectory, true);
        }
    }
}