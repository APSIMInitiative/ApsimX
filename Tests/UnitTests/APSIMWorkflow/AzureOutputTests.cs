using System;
using System.Diagnostics;
using System.IO;
using APSIM.Shared.Utilities;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using NUnit.Framework;
using WorkflowAzure = APSIM.Workflow.Azure;

namespace UnitTests.APSIMWorkflowTests;

[TestFixture]
public class AzureOutputTests
{
    [Test]
    public void DatabaseOutputUsesTaskSpecificPathAndUploadsOnCompletion()
    {
        Uri containerUri = new("https://storage.invalid/run-storage?sv=test");

        OutputFile output = WorkflowAzure.CreateDatabaseOutputFile(containerUri, "12-Example", "/Tests/Validation/A folder/Example.apsimx");
        OutputFile otherOutput = WorkflowAzure.CreateDatabaseOutputFile(containerUri, "13-Example", "/Examples/Example.apsimx");
        OutputFile retryOutput = WorkflowAzure.CreateDatabaseOutputFile(containerUri, "14-Example", "/Tests/Validation/A folder/Example.apsimx");

        Assert.That(output.FilePattern, Is.EqualTo("Tests/Validation/A folder/Example.db"));
        Assert.That(output.UploadOptions.UploadCondition, Is.EqualTo(OutputFileUploadCondition.TaskCompletion));
        Assert.That(output.Destination.Container.ContainerUrl, Is.EqualTo(containerUri.ToString()));
        Assert.That(output.Destination.Container.Path, Is.EqualTo("databases/12-Example/Tests/Validation/A folder/Example.db"));
        Assert.That(otherOutput.Destination.Container.Path, Is.EqualTo("databases/13-Example/Examples/Example.db"));
        Assert.That(otherOutput.Destination.Container.Path, Is.Not.EqualTo(output.Destination.Container.Path));
        Assert.That(retryOutput.Destination.Container.Path, Is.Not.EqualTo(output.Destination.Container.Path));
    }

    [TestCase("/Tests/A[1].apsimx")]
    [TestCase("/Tests/A?.apsimx")]
    [TestCase("/Tests/$name.apsimx")]
    public void DatabaseOutputRejectsBatchPatternCharacters(string path)
    {
        Uri containerUri = new("https://storage.invalid/run-storage?sv=test");
        Assert.Throws<ArgumentException>(() => WorkflowAzure.CreateDatabaseOutputFile(containerUri, "task", path));
    }

    [Test]
    public void UploadUriIsContainerScopedAndExpiresAfterSevenDays()
    {
        string key = Convert.ToBase64String(new byte[32]);
        BlobContainerClient container = new(new Uri("https://storage.invalid/job-storage"), new StorageSharedKeyCredential("testaccount", key));
        DateTimeOffset submission = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Uri uri = WorkflowAzure.CreateDatabaseUploadUri(container, submission);
        string query = Uri.UnescapeDataString(uri.Query);

        Assert.That(uri.AbsolutePath, Is.EqualTo("/job-storage"));
        Assert.That(uri.Host, Is.EqualTo("storage.invalid"));
        Assert.That(query, Does.Contain("sr=c"));
        Assert.That(query, Does.Contain("sp=cw"));
        Assert.That(query, Does.Contain("se=2026-01-09T03:04:05Z"));
    }

    [Test]
    public void WorkflowReportsMissingDatabases()
    {
        if (OperatingSystem.IsWindows())
            Assert.Ignore("The workflow runs on Linux; run workflow-tests.sh with Bash to test it on Windows.");

        string script = Path.Combine(PathUtilities.GetApsimXDirectory(), "Tests", "UnitTests", "APSIMWorkflow", "workflow-tests.sh");
        ProcessStartInfo startInfo = new("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(script);
        using Process process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0), $"stdout: {output}\nstderr: {error}");
    }
}
