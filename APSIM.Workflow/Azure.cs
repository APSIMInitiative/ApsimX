using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System.Reflection;
using System.Globalization;
using Models.Core;


namespace APSIM.Workflow;

/// <summary>
/// Handles sending jobs to Azure Cloud Service during APSIM Validation runs.
/// </summary>
public class Azure
{
    private static readonly string AZURE_ACCOUNT_URL = "https://apsimbuildsysbatch.australiaeast.batch.azure.com";
    private static readonly string AZURE_ACCOUNT_NAME = "apsimbuildsysbatch";
    private static readonly string AZURE_STORAGE_ACCOUNT_NAME = "apsimbuildsysstorage";

    private static readonly string autoScaleScript =
        "TimeIntervalMinute = 5;\n" +
        "MaxNumberNodes = 60;\n" +
        "NumberCPUPerNode = 2;\n" +
        "NumberTasks = max($PendingTasks.GetSample(TimeIntervalMinute * 2));\n" +
        "NumberNodes = NumberTasks == 0 ? 0 : (NumberTasks + 1) / NumberCPUPerNode;\n" +
        "$TargetDedicatedNodes = min(MaxNumberNodes, NumberNodes);\n" +
        "$NodeDeallocationOption = taskcompletion;\n";

    private static string GenerateRandomPassword(int length = 24)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*()-_=+[]{}";
        byte[] data = RandomNumberGenerator.GetBytes(length);
        char[] password = new char[length];

        for (int i = 0; i < length; i++)
            password[i] = chars[data[i] % chars.Length];

        return new string(password);
    }

    /// <summary>
    /// Create an return reference to pool.
    /// </summary>
    /// <param name="primaryAccessKey">An Azure primary access key</param>
    /// <param name="poolName">The name of a pool, typical takes the form: PR_NUMBER-COMMIT_SHA e.g. 11569-11a24b </param>
    /// <param name="nodeNumber">The number of nodes to allocate in the pool. If not provided, it will be calculated based on the number of tasks and CPUs per node.</param>
    /// <param name="isAutoscaling">Should the pool be autoscaled?</param>
    /// <param name="vmsize">The name of a Azure VM e.g. Standard_D4d_v5. More information can be found here: https://learn.microsoft.com/en-us/azure/virtual-machines/sizes/overview?tabs=breakdownseries%2Cgeneralsizelist%2Ccomputesizelist%2Cmemorysizelist%2Cstoragesizelist%2Cgpusizelist%2Cfpgasizelist%2Chpcsizelist </param>
    public static void CreatePool(string primaryAccessKey, string poolName, string nodeNumber = "60", bool isAutoscaling = true, string vmsize = "Standard_D4d_v5")
    {
        try
        {

            BatchSharedKeyCredentials batchCredentials = new(AZURE_ACCOUNT_URL, AZURE_ACCOUNT_NAME, primaryAccessKey);
            using BatchClient batchClient = BatchClient.Open(batchCredentials);
            CloudPool pool = batchClient.PoolOperations.ListPools().FirstOrDefault(p => p.Id == poolName);
            if (pool == null)
            {
                var imageReference = new ImageReference(
                    publisher: "Canonical",
                    offer: "ubuntu-24_04-lts",
                    sku: "server-gen1",
                    version: "latest");

                var vmConfiguration = new VirtualMachineConfiguration(
                    imageReference: imageReference,
                    nodeAgentSkuId: "batch.node.ubuntu 24.04");

                pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolName,
                    virtualMachineSize: vmsize,
                    virtualMachineConfiguration: vmConfiguration);
                pool.TaskSlotsPerNode = 2;
                string poolPassword = GenerateRandomPassword();
                pool.UserAccounts = new List<UserAccount>
                {
                    new("admin", poolPassword, ElevationLevel.Admin),
                };

                if (isAutoscaling == false)
                {
                    pool.TargetDedicatedComputeNodes = int.Parse(nodeNumber);
                    pool.Commit();
                }
                else
                {
                    pool.Commit();
                    batchClient.PoolOperations.EnableAutoScale(poolName, autoScaleScript, TimeSpan.FromMinutes(5));
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating pool {poolName}: {ex}");
        }
    }

    /// <summary>
    /// Creates a list of all the jobs to send to the APSIM Acceptance Tests System.
    /// </summary>
    /// <param name="primaryAccessKey">An Azure primary access key</param>
    /// <param name="paths">A list of the paths to include in the job</param>
    /// <param name="envVars">A dictionary representation of the environment variables.</param>
    /// <param name="uniqueJobName">A unique job name</param>
    /// <param name="key">A key one or key two value from an Azure batch account</param>
    /// <param name="prNumber">The pull request number for this acceptance test run.</param>
    /// <param name="poolName">The Azure batch pool name</param>
    public static void CreateJobs(string primaryAccessKey, string[] paths, Dictionary<string,string> envVars, string uniqueJobName, string key, string prNumber, string poolName)
    {
        List<CloudTask> cloudTasks = [];
        string scriptName = "workflow.sh";
        string commandLine = "bash workflow.sh run_00001";
        CloudJob azureJob = null;
        string isoDate = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        string storageName = $"{uniqueJobName}-storage".ToLowerInvariant(); // used for naming the blob storage container for this run.
        BatchSharedKeyCredentials batchCredentials = new(AZURE_ACCOUNT_URL, AZURE_ACCOUNT_NAME, primaryAccessKey);

        using BatchClient batchClient = BatchClient.Open(batchCredentials);
        CloudPool pool = batchClient.PoolOperations.ListPools().FirstOrDefault(p => p.Id == poolName);
        // check if we have a pool already created. 
        if (pool == null)
            throw new ArgumentException($"A pool with the ID {poolName} could not be found");

        // Copy files to Azure storage so the tasks can access them.
        string keyOne = key;
        string storageConnectionString = 
            $"DefaultEndpointsProtocol=https;AccountName={AZURE_STORAGE_ACCOUNT_NAME};AccountKey={keyOne};EndpointSuffix=core.windows.net";

        // TODO: this should just send the workflow.sh file up rather than all the apsim validation files.
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyDirectory))
            throw new InvalidOperationException("Could not determine the workflow assembly directory.");
        string scriptPath = Path.Combine(assemblyDirectory, scriptName);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("The workflow.sh script file could not be found and as such was not uploaded to the storage container.");
        CopyFilesToAzure(scriptPath, storageConnectionString, storageName);

        //TODO: replace this with a real value.
        envVars.Add("PR_NUMBER", prNumber);
        envVars.Add("OUTPUT_FILES", "local.stdout.txt");
        envVars.Add("AZURE_STORAGE_CONTAINER", storageName);
        envVars.Add("AZURE_STORAGE_CONNECTION_STRING", storageConnectionString);

        // Create a cloud task for each aspimx file path.
        int pathIndex = 0;
        foreach (string apsimFilePath in paths)
        {
            List<ResourceFile> resourceFiles = [ResourceFile.FromAutoStorageContainer(storageName, blobPrefix: scriptName)];
            string cloudTaskName = $"{pathIndex}-{Path.GetFileNameWithoutExtension(apsimFilePath).Replace(" ", "_")}"; // spaces are not allowed.
            CloudTask cloudTask = new(cloudTaskName, commandLine)
            {
                UserIdentity = new UserIdentity("admin"),
                ResourceFiles = resourceFiles,
                EnvironmentSettings = envVars.Select(e => new EnvironmentSetting(e.Key, e.Value))
                    .Append(new EnvironmentSetting("Path", apsimFilePath[1..])) // Path has to be added here so it's unique for each task.
                    .ToList(),
                ExitConditions = new ExitConditions
                {
                    Default = new ExitOptions
                    {
                        DependencyAction = DependencyAction.Satisfy
                    }
                },
            };
            cloudTasks.Add(cloudTask);
            pathIndex++;
        }

        if (cloudTasks.Count != 0)
        {
            if (azureJob == null)
            {
                // Create an Azure job.
                azureJob = batchClient.JobOperations.CreateJob();
                azureJob.Id = uniqueJobName;
                azureJob.PoolInformation = new PoolInformation { PoolId = poolName };
                azureJob.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
                azureJob.UsesTaskDependencies = true;
                azureJob.JobPreparationTask = new JobPreparationTask("bash workflow.sh initialise")
                {
                    ResourceFiles = [ResourceFile.FromAutoStorageContainer(storageName, blobPrefix: scriptName)],
                    UserIdentity = new UserIdentity("admin")
                };
                azureJob.Commit();
            }
            batchClient.JobOperations.AddTaskAsync(azureJob.Id, cloudTasks).Wait();
            cloudTasks.Clear();
        }    
    }


    /// <summary>
    /// Creates an Azure container using the given login information and name
    /// Will throw if the container already exists.
    /// Then copies over the list of input files that are given, using the 
    /// input directory as the relative path to find them.
    /// </summary>
    /// <param name="scriptPath">Path to the script which needs to be copied to the azure storage container.</param>
    /// <param name="storageConnectionString">An Azure storage connection string</param>
    /// <param name="storageName">The name of the Azure storage container for this run</param>
    /// <exception cref="Exception"></exception>
    public static void CopyFilesToAzure(string scriptPath, string storageConnectionString, string storageName)
    {
        //Connect to Azure storage
        BlobContainerClient containerClient = new(storageConnectionString, storageName);

        // throw if the container already exists.
        // Note: in debug situations it's normal for this line to through 3 times before moving on.
        bool containerExists = containerClient.Exists();
        if (containerExists)
            throw new Exception($"Cannot create output container on Azure as it already exists.");

        //Create the container
        containerClient.Create();

        // Copy or append files to storage.
        if (!File.Exists(scriptPath))
            throw new Exception($"File {scriptPath} does not exist, but is listed as an input file");

        using FileStream data = File.OpenRead(scriptPath);
        BlobClient blobClient = containerClient.GetBlobClient(Path.GetFileName(scriptPath));
        blobClient.Upload(data, overwrite: true);
    }

    /// <summary>Resizes a pool by locating a pool with a specific name and applying an autoscal script.</summary>
    /// <param name="primaryAccessKey">An Azure batch account primary access key.</param>
    /// <param name="poolName">The name of the Azure batch pool for this run.</param>
    public static void EnablePoolAutoReszing(string primaryAccessKey, string poolName)
    {
        BatchSharedKeyCredentials batchCredentials = new(AZURE_ACCOUNT_URL, AZURE_ACCOUNT_NAME, primaryAccessKey);
        using BatchClient batchClient = BatchClient.Open(batchCredentials);
        CloudPool pool = batchClient.PoolOperations.ListPools().FirstOrDefault(p => p.Id == poolName);
        batchClient.PoolOperations.EnableAutoScale(poolName, autoScaleScript, TimeSpan.FromMinutes(5));
    }
}
