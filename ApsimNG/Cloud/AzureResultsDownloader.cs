using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using System.IO;
using System.ComponentModel;
using UserInterface.Presenters;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SQLite;


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
        /// Mutual exclusion semaphore for updating the progress of a job.
        /// </summary>
        private object progressMutex;

        public AzureResultsDownloader(Guid id, string jobName, string path, AzureJobDisplayPresenter explorer, bool export, bool includeDebugFiles, bool keepOutputFiles)
        {
            numBlobsComplete = 0;
            jobId = id;
            exportToCsv = export;
            saveDebugFiles = includeDebugFiles;
            saveRawOutputFiles = keepOutputFiles;
            outputPath = path;
            rawResultsPath = outputPath + "\\" + jobName.ToString();
            tempPath = Path.GetTempPath() + "\\" + jobId;
            progressMutex = new object();
            presenter = explorer;
            try
            {
                // if we need to save files, create a directory under the output directory
                if ((saveDebugFiles|| saveRawOutputFiles || exportToCsv) && !Directory.Exists(rawResultsPath)) Directory.CreateDirectory(rawResultsPath);
            }
            catch (Exception ex)
            {
                presenter.ShowError(ex.ToString());
                return;
            }
            
            name = jobName;
            StorageCredentials storageCredentials = StorageCredentials.FromConfiguration();
            BatchCredentials batchCredentials = BatchCredentials.FromConfiguration();
            storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
            var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
            batchClient = BatchClient.Open(sharedCredentials);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(3), 10);        
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
            if (async) downloader.RunWorkerAsync();
            else Downloader_DoWork(null, null);
        }

        private void Downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            CancellationToken ct;

            var outputHashLock = new object();
            HashSet<string> downloadedOutputs = GetDownloadedOutputFiles();
            int success = 0;
            while (true)
            {
                try
                {
                    if (downloader.CancellationPending || ct.IsCancellationRequested) return;
                    // TODO : evaluate efficiency of this method
                    bool complete = IsJobComplete();
                    var outputs = ListJobOutputsFromStorage();
                    
                    try
                    {
                        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                        Directory.CreateDirectory(tempPath);
                    } catch (Exception ex)
                    {
                        presenter.ShowError(ex.ToString());
                        success = 3;
                    }
                    var zips = outputs.Where(blob => Path.GetExtension(blob.Name.ToLower()) == ".zip");

                    Parallel.ForEach(outputs,
                                     new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 },
                                     blob => 
                                     {
                                         bool skip = false;
                                         string extension = Path.GetExtension(blob.Name.ToLower());
                                         string downloadPath = (extension == ".stdout" || extension == ".sum" || saveRawOutputFiles) ? Path.Combine(rawResultsPath, blob.Name) : Path.Combine(tempPath, blob.Name);
                                         
                                         // if we don't want to download debugging files and this is a debugging file, skip it
                                         if (!saveDebugFiles && (extension == ".stdout" || extension == ".sum")) skip = true;

                                         if (!skip && !downloadedOutputs.Contains(blob.Name))
                                         {
                                             //blob.DownloadToFile(Path.Combine(rawResultsPath, blob.Name), FileMode.Create);
                                             blob.DownloadToFile(downloadPath, FileMode.Create);
                                             lock (outputHashLock)
                                             {
                                                 downloadedOutputs.Add(blob.Name);
                                                 downloader.ReportProgress(0, blob.Name);
                                             }
                                         }                                         
                                     });
                    var test2 = outputs.First(x => Path.GetExtension(x.Name.ToLower()) == ".zip");
                    if (complete) break;
                }
                catch (AggregateException ae)
                {
                    presenter.ShowError(ae.InnerException.ToString());
                }
            }
            // todo : remember to set success appropriately after moving the results into the data store
            if (exportToCsv)
            {                
                success = SummariseResults(true);
            }

            while (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
            
            
            presenter.DownloadComplete(jobId);
            presenter.DisplayFinishedDownloadStatus(name, success, outputPath, DateTime.Now);
        }

        /// <summary>
        /// Event handler for the background worker's progress changed event. Passes the download progress and job name on to the presenter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string jobName = e.UserState.ToString();
            lock (progressMutex)
            {
                numBlobsComplete++;
                double progress = 1.0 * numBlobsComplete / numBlobs;
                presenter.UpdateDownloadProgress(progress);
            }
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
                string resultPath = saveRawOutputFiles ? rawResultsPath : tempPath;
                foreach (string f in Directory.GetFiles(resultPath))
                {
                    if (Path.GetExtension(f) == ".out")
                    {
                        isClassic = true;
                        fileSpec = ".out";
                        break;
                    }
                }

                string[] resultFiles = Directory.GetFiles(resultPath, "*" + fileSpec);

                // if there are no results (possibly a bad simulation?), no need to create an empty csv file.
                if (resultFiles.Count() == 0) return 1;

                int count = 0;
                int lastComplete = -1;
                bool printHeader = true;
                bool csv = false;
                string delim = ", ";
                string sep = "";

                StringBuilder output = new StringBuilder();
                string csvPath = outputPath + "\\" + name + "\\" + name + ".csv";
                Regex rx_name = detectCommonChars(resultFiles);
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
                                    if (includeFileNames) output.Append(AddOutline("File " + sr.ReadLine(), "", delim));
                                    else output.AppendLine(sr.ReadLine());
                                } else
                                {
                                    sr.ReadLine();
                                }
                                sr.ReadLine();
                                if (csv) sep = ",";
                                if (includeFileNames)
                                {
                                    output.Append(AddOutline(sr.ReadToEnd(), rx_name.Replace(Path.GetFileName(currentFile), sep), delim));
                                } else
                                {
                                    output.Append(sr.ReadToEnd());
                                }
                                sr.Close();
                            }
                            file.Write(output.ToString());
                            output.Clear();
                        } else // results are from apsim X
                        {
                            count++;
                            var x = ReadSqliteDB(currentFile, printHeader, delim);
                            file.Write(x);
                        }
                        printHeader = false;
                    }
                    file.Close();
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
            StringBuilder output = new StringBuilder();
            SQLiteConnection m_dbConnection;
            Dictionary<string, string> simNames = new Dictionary<string, string>();

            DataTable table = new DataTable();
            m_dbConnection = new SQLiteConnection("Data Source=" + path + ";Version=3;");
            m_dbConnection.Open();

            // Enumerate the simulation names
            string sql = "SELECT * FROM simulations";
            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //Console.WriteLine("SimName: " + reader["Name"] + ", ID: " + reader["ID"]);
                    simNames.Add(reader["ID"].ToString(), reader["Name"].ToString());
                }
            } catch (Exception e)
            {
                Console.WriteLine("Error enumerating simulation names: " + e.ToString());
            }

            sql = "SELECT * FROM Report";
            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                table.Load(reader); // faster to load the result into a DataTable for some reason
                printHeader = true;
                foreach (DataRow row in table.Rows)
                {
                    if (printHeader)
                    {
                        output.Append("FileName" + delim + "SimName" + delim);
                        foreach (DataColumn column in table.Columns)
                        {
                            output.Append(column.ColumnName + delim);
                        }
                        output.Append("\n");
                        printHeader = false;
                    }

                    output.Append(Path.GetFileNameWithoutExtension(path) + delim + simNames[row.ItemArray[0].ToString()] + delim);
                    output.Append(String.Join(delim, row.ItemArray));
                    output.Append("\n");
                }
                return output.ToString();
            } catch (Exception e)
            {
                Console.WriteLine("Error getting report: " + e.ToString());
            }
            return "";   
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
            line = rx_date.Replace(
                line,
                new MatchEvaluator(
                    delegate (Match m)
                    {
                        return string.Format("{0:0}", (DateTime.Parse(m.Value) - new DateTime(1900, 1, 1)).TotalDays + 2);
                    })
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
        private Regex detectCommonChars(string[] args)
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
    }
}
