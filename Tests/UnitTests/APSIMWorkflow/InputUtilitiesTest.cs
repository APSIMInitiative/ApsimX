using System;
using System.IO;
using NUnit.Framework;
using APSIM.Workflow;

namespace UnitTests.APSIMWorkflowTests;

[TestFixture]
public class InputUtilitiesTest
{
    [Test]
    public void GetApsimXFileTextFromFile_ValidFilePath_ReturnsFileContent()
    {
        // Arrange
        string testFilePath = "test.apsimx";
        string expectedContent = "Test APSIMX file content";
        File.WriteAllText(testFilePath, expectedContent);

        try
        {
            // Act
            string result = InputUtilities.GetApsimXFileTextFromFile(testFilePath);

            // Assert
            Assert.That(expectedContent, Is.EqualTo(result));
        }
        finally
        {
            // Cleanup
            File.Delete(testFilePath);
        }
    }

    [Test]
    public void GetApsimXFileTextFromFile_NullOrEmptyFilePath_ThrowsException()
    {
        // Arrange
        string invalidFilePath = null;

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => InputUtilities.GetApsimXFileTextFromFile(invalidFilePath));
        Assert.That(exception.Message, Does.Contain("APSIMX file not found"));
    }

    [Test]
    public void GetApsimXFileTextFromFile_FileDoesNotExist_ThrowsException()
    {
        // Arrange
        string nonExistentFilePath = "nonexistent.apsimx";

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => InputUtilities.GetApsimXFileTextFromFile(nonExistentFilePath));
        Assert.That(exception.Message, Does.Contain("Unable to read the APSIMX file"));
    }

    [Test]
    public void GetApsimXFileTextFromFile_EmptyFileContent_ThrowsException()
    {
        // Arrange
        string testFilePath = "empty.apsimx";
        File.WriteAllText(testFilePath, string.Empty);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<Exception>(() => InputUtilities.GetApsimXFileTextFromFile(testFilePath));
            Assert.That(exception.Message, Does.Contain("file text, it was found to be null"));
        }
        finally
        {
            // Cleanup
            File.Delete(testFilePath);
        }
    }
}
