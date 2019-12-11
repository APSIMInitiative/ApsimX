using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;
using UserInterface.Presenters;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace ApsimNG.Cloud
{
    class AzureResultsDownloader
    {        
        /// <summary>
        /// User-specified download directory. 
        /// This is where any .csv files should be saved to.
        /// </summary>
        private string outputPath;

        /// <summary>
        /// Directory to contain the result (.db) and debugging (.out | .stdout) files.
        /// Located at outputPath\jobName.        
        /// </summary>
        private string rawResultsPath;

        /// <summary>
        /// Temporary directory to hold the result files if the user does not wish to keep them.
        /// This directory and its contents are deleted once the download is complete.
        /// </summary>
        private string tempPath;

        /// <summary>
        /// Job ID.
        /// </summary>
        private Guid jobId;

        /// <summary>
        /// Job name/description.
        /// </summary>
        private string name;

        /// <summary>
        /// Whether or not results should be combined and exported to a .csv file.
        /// </summary>
        private bool exportToCsv;

        /// <summary>
        /// Whether or not the debugging files should also be downloaded.
        /// </summary>
        private bool saveDebugFiles;

        /// <summary>
        /// Whether or not the raw output files should be saved.
        /// </summary>
        private bool saveRawOutputFiles;

        /// <summary>
        /// Whether or not the result files should be extracted from the archive.
        /// </summary>
        private bool unzipResults;

        /// <summary>
        /// Whether or not the result files should be downloaded.
        /// </summary>
        private bool downloadResults;

        /// <summary>
        /// Azure cloud storage account.
        /// </summary>
        private CloudStorageAccount storageAccount;

        /// <summary>
        /// Client for the user's batch account.
        /// </summary>
        private BatchClient batchClient;

        /// <summary>
        /// Client for user's cloud storage.
        /// </summary>
        private CloudBlobClient blobClient;

        /// <summary>
        /// Presenter trying to download the jobs.
        /// </summary>
        private AzureJobDisplayPresenter presenter;

        /// <summary>
        /// Worker thread to asynchronously download the job.
        /// </summary>
        private BackgroundWorker downloader;

        /// <summary>
        /// How many blobs have been downloaded.
        /// </summary>
        private int numBlobsComplete;

        /// <summary>
        /// Total number of blobs to download.
        /// </summary>
        private int numBlobs;

        /// <summary>
        /// Array of all valid result file formats.
        /// </summary>
        private readonly string[] resultFileFormats = { ".db", ".out" };

        /// <summary>
        /// Array of all valid debug file formats.
        /// </summary>
        private readonly string[] debugFileFormats = { ".stdout", ".sum" };

        /// <summary>
        /// Array of all valid zip file formats.
        /// </summary>
        private readonly string[] zipFileFormats = { ".zip" };

        /// <summary>
        /// Mutual exclusion sempahore for reading the .db files.
        /// </summary>
        private object databaseMutex;

        /// <summary>
        /// Constructor. Requires Azure credentials to already be set in ApsimNG.Properties.Settings. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="jobName"></param>
        /// <param name="path"></param>
        /// <param name="explorer"></param>
        /// <param name="export"></param>
        /// <param name="includeDebugFiles"></param>
        /// <param name="keepOutputFiles"></param>
        public AzureResultsDownloader(Guid id, string jobName, string path, AzureJobDisplayPresenter explorer, bool getResults, bool export, bool includeDebugFiles, bool keepOutputFiles, bool unzipResultFiles)
        {
            numBlobsComplete = 0;
            jobId = id;
            downloadResults = getResults;
            exportToCsv = export;
            saveDebugFiles = includeDebugFiles;
            saveRawOutputFiles = keepOutputFiles;
            outputPath = path;
            rawResultsPath = outputPath + "\\" + jobName.ToString() + "_Results";
            tempPath = Path.GetTempPath() + "\\" + jobId;
            databaseMutex = new object();
            presenter = explorer;
            unzipResults = unzipResultFiles;
            try
            {
                // if we need to save files, create a directory under the output directory
                if ((saveDebugFiles|| saveRawOutputFiles || exportToCsv) && !Directory.Exists(rawResultsPath)) Directory.CreateDirectory(rawResultsPath);
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
                return;
            }
            
            name = jobName;
            StorageCredentials storageCredentials = StorageCredentials.FromConfiguration();
            BatchCredentials batchCredentials = BatchCredentials.FromConfiguration();
            storageAccount = new CloudStorageAccount(new Microsoft.Azure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
            var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
            batchClient = BatchClient.Open(sharedCredentials);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new Microsoft.Azure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(3), 10);        
        }

        /// <summary>
        /// Downloads the results of a job.
        /// </summary>
        /// <param name="async">If true, results will be downloaded in a separate thread.</param>
        public void DownloadResults(bool async)
        {            
            numBlobs = CountBlobs();

            downloader = new BackgroundWorker();
            downloader.WorkerReportsProgress = true;
            downloader.DoWork += Downloader_DoWork;
            downloader.ProgressChanged += DownloadProgressChanged;

            // if the job is not complete, the worker will spinlock until the worker must run asynchronously - it spinlocks until the job is complete
            if (async || !IsJobComplete())
            {                
                downloader.RunWorkerAsync();
            }
            else
            {                
                Downloader_DoWork(null, null);
            }
            
        }

        private void Downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            CancellationToken ct;

            var outputHashLock = new object();
            HashSet<string> downloadedOutputs = GetDownloadedOutputFiles();
            int success = 0;
            while (!IsJobComplete())
            {
                Thread.Sleep(10000);
            }

            try
            {
                if (downloader.CancellationPending || ct.IsCancellationRequested) return;
                
                // delete temp directory if it already exists and create a new one in its place
                try
                {
                    if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                    Directory.CreateDirectory(tempPath);
                }
                catch (Exception err)
                {
                    presenter.ShowError(err);
                    success = 3;
                }

                // might be worth including a 'download all' flag (for testing purposes?), where everything in outputs gets downloaded
                List<CloudBlockBlob> outputs = ListJobOutputsFromStorage().ToList();
                if (outputs == null || outputs.Count < 1)
                {
                    presenter.ShowErrorMessage("No files in output container.");
                    return;
                }

                if (saveDebugFiles)
                {
                    List<CloudBlockBlob> debugBlobs = outputs.Where(blob => debugFileFormats.Contains(Path.GetExtension(blob.Name.ToLower()))).ToList();
                    Download(debugBlobs, rawResultsPath, ref ct);
                }

                // Only download the results if the user wants a CSV or the result files themselves
                if (downloadResults || saveRawOutputFiles || exportToCsv)
                {                    
                    List<CloudBlockBlob> zipBlobs = outputs.Where(blob => zipFileFormats.Contains(Path.GetExtension(blob.Name.ToLower()))).ToList();
                    if (zipBlobs != null && zipBlobs.Count > 0) // if the result file are nicely zipped up for us
                    {
                        List<string> localZipFiles = Download(zipBlobs, rawResultsPath, ref ct);
                        if (!unzipResults)
                        {
                            // if user doesn't want to extract the results, we're done
                            presenter.DownloadComplete(jobId);
                            presenter.DisplayFinishedDownloadStatus(name, 0, outputPath);
                            return;
                        }
                        foreach (string archive in localZipFiles)
                        {                            
                            ExtractZipArchive(archive, rawResultsPath);                            
                            try
                            {
                                File.Delete(archive);
                            }
                            catch
                            {
                                
                            }                            
                        }
                    }
                    else
                    {
                        // Results are not zipped up (probably because the job was run on the old Azure job manager), so download each individual result file
                        List<CloudBlockBlob> resultBlobs = outputs.Where(blob => resultFileFormats.Contains(Path.GetExtension(blob.Name.ToLower()))).ToList();
                        Download(resultBlobs, rawResultsPath, ref ct);                        
                    }

                    if (exportToCsv)
                    {
                        success = SummariseResults(true);
                        // Delete the output files if the user doesn't want to keep them
                        if (!saveRawOutputFiles)
                        {
                            // this will delete all .db and .out files in the output directory
                            foreach (string resultFile in Directory.EnumerateFiles(rawResultsPath).Where(file => resultFileFormats.Contains(Path.GetExtension(file))))
                            {
                                try
                                {
                                    File.Delete(resultFile);
                                }
                                catch (Exception err)
                                {
                                    presenter.ShowError(new Exception("Unable to delete " + resultFile + ": ", err));
                                }
                            }
                        }
                    }
                }
            }
            catch (AggregateException err)
            {
                presenter.ShowError(err);
            }
            
            
            presenter.DownloadComplete(jobId);
            presenter.DisplayFinishedDownloadStatus(name, success, outputPath);
        }

        /// <summary>
        /// Downloads each given blob from Azure and returns a list of file names.
        /// </summary>
        /// <param name="blobs">List of Azure blobs to download.</param>
        /// <param name="downloadPath">Path to download the blobs to.</param>
        private List<string> Download(List<CloudBlockBlob> blobs, string downloadPath, ref CancellationToken ct)
        {
            object outputHashLock = new object();
            List<string> zipFiles = new List<string>();
            Parallel.ForEach(blobs,
                             new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 },
                             blob =>
                             {
                                 string filename = Path.Combine(downloadPath, blob.Name);
                                 try
                                 {
                                     blob.DownloadToFile(filename, FileMode.Create);
                                 }
                                 catch
                                 {

                                 }
                                 
                                 lock (outputHashLock)
                                 {
                                     downloader.ReportProgress(0, blob.Name);
                                     zipFiles.Add(filename);
                                 }
                             });
            return zipFiles;
        }

        /// <summary>
        /// Event handler for the background worker's progress changed event. Passes the download progress and job name on to the presenter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // job name is currently unused, but maybe in the future the view could display the name of the currently downloading file 
            // as well as the progress
            string jobName = e.UserState.ToString();
            numBlobsComplete++;
            double progress = 1.0 * numBlobsComplete / numBlobs;
            presenter.UpdateDownloadProgress(progress);
        }

        /// <summary>
        /// Summarises a directory of results into a single file.
        /// </summary>        
        /// <param name="includeFileNames">Whether or not to include the file names as a column</param>
        /// <returns>An error code. -1 means unknown error, 0 means success, 1 means invalid simulation (no results).</returns>
        public int SummariseResults(bool includeFileNames)
        {
            try
            {
                bool isClassic = false;
                string fileSpec = ".db";                
                foreach (string f in Directory.GetFiles(rawResultsPath))
                {
                    if (Path.GetExtension(f) == ".out")
                    {
                        isClassic = true;
                        fileSpec = ".out";
                        break;
                    }
                }

                string[] resultFiles = Directory.GetFiles(rawResultsPath, "*" + fileSpec);

                // if there are no results (possibly a bad simulation?), no need to create an empty csv file.
                if (resultFiles.Count() == 0) return 1;

                int count = 0;
                int lastComplete = -1;
                bool printHeader = true;
                bool csv = false;
                string delim = ", ";
                string sep = "";

                StringBuilder output = new StringBuilder();
                string csvPath = rawResultsPath + "\\" + name + ".csv";
                Regex rx_name = DetectCommonChars(resultFiles);
                using (StreamWriter file = new StreamWriter(csvPath, false))
                {
                    foreach (string currentFile in resultFiles)
                    {
                        int complete = (count * 100 / resultFiles.Count() * 100) / 100;
                        if (complete != lastComplete)
                        {
                            lastComplete = complete;
                        }

                        if (isClassic)
                        {
                            using (StreamReader sr = new StreamReader(currentFile))
                            {
                                // read a couple of lines from the top
                                for (int x = 0; x < 2; x++)
                                {
                                    string s = sr.ReadLine();
                                    if (s.ToLower() == "format = csv")
                                    {
                                        sr.ReadLine();
                                        csv = true;
                                    }
                                }

                                if (count++ == 0)
                                {
                                    if (includeFileNames)
                                        output.Append(AddOutline("File " + sr.ReadLine(), "", delim));
                                    else
                                        output.AppendLine(sr.ReadLine());
                                }
                                else
                                {
                                    sr.ReadLine();
                                }
                                sr.ReadLine();
                                if (csv) sep = ",";
                                if (includeFileNames)
                                {
                                    output.Append(AddOutline(sr.ReadToEnd(), rx_name.Replace(Path.GetFileName(currentFile), sep), delim));
                                }
                                else
                                {
                                    output.Append(sr.ReadToEnd());
                                }
                                sr.Close();
                            }
                            file.Write(output.ToString());
                            output.Clear();
                        }
                        else // results are from apsim X
                        {
                            count++;
                            var x = ReadSqliteDB(currentFile, printHeader, delim);
                            if (x == "") return 4;
                            file.Write(x);
                        }
                        printHeader = false;
                    }
                    file.Close();
                }
                return 0;
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
                return 2;
            }
        }

        /// <summary>
        /// Reads an apsimx .db result file and returns the results as a string.
        /// </summary>
        /// <param name="path">Directory containing the results</param>
        /// <param name="printHeader">Whether or not to include the file names as a column</param>
        /// <param name="delim">Field delimiter</param>
        /// <returns></returns>
        private string ReadSqliteDB(string path, bool printHeader, string delim)
        {
            lock(databaseMutex)
            {
                SQLiteConnection dbConnection;
                Dictionary<string, string> simNames = new Dictionary<string, string>();

                dbConnection = new SQLiteConnection("Data Source=" + path + ";Version=3;", true);                

                try
                {
                    dbConnection.Open();                    
                }
                catch (Exception err)
                {
                    presenter.ShowError(new Exception("Failed to open db at " + path + ": ", err));
                    // No point continuing if unable to open the database
                    return "";
                }
                

                // Enumerate the simulation names
                string sql = "SELECT * FROM _Simulations";
                try
                {
                    SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        simNames.Add(reader["ID"].ToString(), reader["Name"].ToString());
                    }
                    command.Dispose();
                    reader.Close();
                }
                catch (Exception err)
                {
                    presenter.ShowError(new Exception("Error enumerating simulation names: ", err));
                }

                // Enumerate the table names
                sql = "SELECT name FROM sqlite_master WHERE type='table'";
                List<string> tables = new List<string>();
                try
                {
                    SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read()) tables.Add(reader[0].ToString());
                    command.Dispose();
                    reader.Close();
                }
                catch (Exception err)
                {
                    presenter.ShowError(new Exception("Error reading table names: ", err));
                }

                // Read data from each 'report' table (any table whose name doesn't start with an underscore)
                // Hopefully the user doesn't rename their report to start with an underscore.
                List<string> reportTables = tables.Where(name => name[0] != '_').ToList();                                
                DataTable master = new DataTable(); // master data table, containing data merged from all report tables

                try
                {
                    foreach (string tableName in reportTables)
                    {
                        DataTable reportTable = new DataTable();
                        sql = "SELECT * FROM " + tableName;
                        SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                        SQLiteDataReader reader = command.ExecuteReader();
                        reportTable.Load(reader);
                        reportTable.Merge(master);
                        master = reportTable;
                        command.Dispose();
                        reader.Close();
                    }
                }
                catch (Exception err)
                {
                    presenter.ShowError(new Exception("Error reading or merging table: ", err));
                }
                dbConnection.Close();
                // Generate the CSV file data                
                // enumerate delimited column names
                string csvData = "File Name" + delim + "Sim Name" + delim + master.Columns.Cast<DataColumn>().Select(x => x.ColumnName).Aggregate((a, b) => a + delim + b) + "\n";

                // for each row, append the contents of that row to the data table, delimited by the given character
                master.Rows.Cast<DataRow>().ToList().ForEach(row => csvData += Path.GetFileNameWithoutExtension(path) + delim + simNames[row.ItemArray[0].ToString()] + delim + row.ItemArray.ToList().Aggregate((a, b) => a + delim + b) + "\n");

                return csvData;
            }            
        }
        
        /// <summary>
        /// Reads apsim classic result files and returns the combined results as a formatted csv file
        /// As of 08/01/2018 this functionality is untested and is just a direct port from MARS.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="prefixCol"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        private string AddOutline(string line, string prefixCol, string delim)
        {
            line = line.TrimEnd();
            Regex rx_date = new Regex(@"\d{2}/\d{2}/\d{4}");            
            // change date to total days since 1900 for Excel
            line = rx_date.Replace
            (
                line,
                new MatchEvaluator
                (
                    delegate (Match m)
                    {
                        return string.Format("{0:0}", (DateTime.Parse(m.Value) - new DateTime(1900, 1, 1)).TotalDays + 2, CultureInfo.InvariantCulture);
                    }
                )
            );

            // swap out delimiters
            line = new Regex("[ ]+").Replace(line, new MatchEvaluator(delegate (Match m) { return delim; }));

            // if the line is actually multiple lines then prepend the prefixcol to each line inside it
            line = line.Replace(Environment.NewLine, Environment.NewLine + prefixCol);

            return prefixCol + line + Environment.NewLine;
        }

        /// <summary>
        /// Find the set of characters at the end of the filenames that are all common (ie ' Results.out')
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Regex DetectCommonChars(string[] args)
        {
            if (args.Count() < 2) return new Regex(".out");

            // grab the first filename and reverse it
            string[] common = args[0].Split(new char[] { ' ', '.' });
            Array.Reverse(common);

            int count_unique = int.MaxValue;

            // for each subsequent filename look backwards to find the first character that doesn't match up with our common character array
            // here we are looking for the least possible number of common characters across all files
            foreach (string arg in args)
            {
                string[] split = arg.Split(new char[] { ' ', '.' });
                Array.Reverse(split);

                for (int i = 0; i < split.Length; i++)
                {
                    if (common[i] != split[i])
                    {
                        count_unique = Math.Min(count_unique, i);
                        break;
                    }
                }
            }

            string strResult = "";

            for (int i = 0; i < count_unique; i++)
            {
                strResult = "[ .]" + common[i] + strResult;
            }

            return new Regex(strResult);

        }
        /// <summary>
        /// Cancels a download in progress.
        /// </summary>
        /// <param name="block">Whether or not the function should block until the download thread exits.</param>
        private void CancelDownload(bool block = false)
        {
            downloader.CancelAsync();
            while (block && downloader.IsBusy);
        }
        
        /// <summary>
        /// Counts the number of blobs in a cloud storage container.
        /// </summary>
        /// <param name="jobId">Id of the container.</param>
        /// <param name="includeDebugFiles">Whether or not to count debug file blobs.</param>
        /// <returns></returns>
        private int CountBlobs()
        {
            try
            {
                CloudBlobContainer container = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
                container.FetchAttributes();
                int count = 0;
                var blobs = container.ListBlobs();

                foreach (var blob in blobs)
                {
                    string extension = Path.GetExtension(blob.Uri.LocalPath.ToLower());
                    if (saveDebugFiles || !(extension == ".stdout" || extension == ".sum")) count++;
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lists all files in the output directory.
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GetDownloadedOutputFiles()
        {
            return new HashSet<string>(Directory.EnumerateFiles(rawResultsPath).Select(f => Path.GetFileName(f)));
        }

        private IEnumerable<CloudBlockBlob> ListJobOutputsFromStorage()
        {
            var containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
            if (!containerRef.Exists())
            {
                return Enumerable.Empty<CloudBlockBlob>();
            }
            return containerRef.ListBlobs().Select(b => ((CloudBlockBlob)b));
        }

        /// <summary>
        /// Tests if a job has been completed.
        /// </summary>
        /// <returns>True if the job was completed or disabled, otherwise false.</returns>
        private bool IsJobComplete()
        {
            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id" };
            // This is how it was done in MARS. Not sure that both of these are necessary.
            CloudJob tmpJob = batchClient.JobOperations.ListJobs(detailLevel).FirstOrDefault(j => string.Equals(jobId.ToString(), j.Id));
            CloudJob job = tmpJob == null ? tmpJob : batchClient.JobOperations.GetJob(jobId.ToString());
            return job == null || job.State == JobState.Completed || job.State == JobState.Disabled;

            // a simpler solution would be -
            //return batchClient.JobOperations.GetJob(jobId.ToString()).Id;
        }

        private void ReportFinished(bool successful)
        {

        }

        /// <summary>
        /// By default, ZipFile.ExtractToDirectory will throw an exception if one of its files already exist in the output directory.
        /// This function acts as a replacement - it extracts a zip file to a directory, but silently overwrites any conflicting files.
        /// </summary>
        /// <param name="archive">Zip archive to be extracted.</param>
        /// <param name="path">Directory to extract the files to.</param>
        private void ExtractZipArchive(string archivePath, string path)
        {            
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    try
                    {
                        string filePath = Path.Combine(path, file.FullName);
                        string dir = Path.GetDirectoryName(filePath);

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        if (file.Name != "")
                            file.ExtractToFile(filePath, true);

                        file.ExtractToFile(filePath, true);
                    }
                    catch (Exception err)
                    {
                        presenter.ShowError(err);
                    }
                }
            }
        }
    }
}
