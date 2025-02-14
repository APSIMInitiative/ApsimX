using CommandLine;
using System.Diagnostics;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Climate;
using APSIM.Shared.Utilities;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;

namespace APSIM.Workflow;

/// <summary>
/// Main program class for the APSIM.Workflow application.
/// </summary>
public class Program
{
    private static int exitCode = 0;
    public static string apsimFileName = string.Empty;

    /// <summary>
    /// Production token URL
    /// </summary>
    public static string WORKFLO_API_TOKEN_URL = "https://digitalag.csiro.au/workflo/antiforgery/token";
    
    // // Development token URL
    // public static WORKFLO_API_TOKEN_URL = "http://localhost:8040/antiforgery/token";

    /// <summary>
    /// Production submit azure URL
    /// </summary>
    public static string WORKFLO_API_SUBMIT_AZURE_URL = "https://digitalag.csiro.au/workflo/submit-azure";
    // // Development submit azure URL
    // public static WORKFLO_API_SUBMIT_AZURE_URL = "http://localhost:8040/submit-azure";



    public static int Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions).WithNotParsed(HandleParseError);
        return exitCode;
    }

    /// <summary>
    /// Runs the application with the specified options.
    /// </summary>
    /// <param name="options"></param>
    private static void RunOptions(Options options)
    {
        try
        {
            if (options.DirectoryPath != null)
            {
                Console.WriteLine("Processing file: " + options.DirectoryPath);
                CopyWeatherFiles(options);

                WorkFloFileUtilities.CreateValidationWorkFloFile(options.DirectoryPath, apsimFileName);                
                if (!File.Exists(Path.Combine(options.DirectoryPath, "workflow.yml")))
                    throw new Exception("Error: Failed to create validation workflow file.");

                if(options.Verbose)
                    Console.WriteLine("Validation workflow file created.");

                CreateZipFile(options.DirectoryPath);

                if(options.Verbose)
                    Console.WriteLine("Zip file created.");
                    
                SubmitWorkFloJob(options.DirectoryPath).Wait();

                if (options.Verbose)
                    Console.WriteLine("Finished with exit code " + exitCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            exitCode = 1;
        }
    }

    private static async Task SubmitWorkFloJob(string directoryPath)
    {

        // Setup HTTP client with cookie container.
        CookieContainer cookieContainer = new();
        HttpClientHandler httpClientHandler = new()
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
        };

        // Get Token for subsequent request.
        using HttpClient httpClient = new(httpClientHandler);
        Uri tokenUri = new(Program.WORKFLO_API_TOKEN_URL);
        var response = await httpClient.GetAsync(tokenUri);
        var token = await httpClient.GetStringAsync(tokenUri);
        CookieCollection responseCookies = cookieContainer.GetCookies(tokenUri);
        
        // Submit Azure job.
        HttpResponseMessage submitAzureRequest = await SendSubmitAzureJobRequest(directoryPath, httpClient, token);
        if (submitAzureRequest.IsSuccessStatusCode)
        {
            Console.WriteLine("WorkFlo job submitted successfully.");
        }
        else
        {
            var responseContentJson = await submitAzureRequest.Content.ReadFromJsonAsync<string>();
            Console.WriteLine("Error: Failed to submit WorkFlo job. Reason:\n" + responseContentJson);
            exitCode = 1;
        }
    }

    private static async Task<HttpResponseMessage> SendSubmitAzureJobRequest(string directoryPath, HttpClient httpClient, string token)
    {
        Uri azureSubmitJobUri = new(Program.WORKFLO_API_SUBMIT_AZURE_URL);
        var content = new MultipartFormDataContent
        {
            { new StreamContent(File.OpenRead(Path.Combine(directoryPath, "payload.zip")), 8192), "file", "payload.zip" }
        };
        content.Headers.Add("X-XSRF-TOKEN", token);
        HttpRequestMessage request = new(HttpMethod.Post, azureSubmitJobUri) { Content = content };
        HttpResponseMessage message = await httpClient.SendAsync(request);
        return message;
    }

    /// <summary>
    /// Creates a zip file from the specified directory.
    /// </summary>
    /// <param name="directoryPath">directory where payload files can be found.</param>
    private static void CreateZipFile(string directoryPath)
    {
        if (directoryPath == null)
            throw new Exception("Error: Directory path is null while trying to create zip file.");

        var parentDirectory = Directory.GetParent(directoryPath);
        if (parentDirectory == null || parentDirectory.Parent == null)
            throw new Exception("Error: Directory path is invalid while trying to create zip file.");
        string directoryParentPath = parentDirectory.Parent.FullName;
        string zipFilePath = Path.Combine(directoryParentPath, "payload.zip");

        if (File.Exists(zipFilePath))
            File.Delete(zipFilePath);

        RemoveDatabaseFiles(directoryPath);

        ZipFile.CreateFromDirectory(directoryPath, zipFilePath, CompressionLevel.SmallestSize, false);

        if (!File.Exists(zipFilePath))
            throw new Exception("Error: Failed to create zip file.");

        string finalZipFilePath = Path.Combine(directoryPath, "payload.zip");
        if(File.Exists(finalZipFilePath))
            File.Delete(finalZipFilePath);

        File.Move(zipFilePath, finalZipFilePath);
    }

    /// <summary>
    /// Removes the database files from Directory to reduce request entity size for WorkFlo API.
    /// </summary>
    /// <remarks>The db files are not needed as they will be regenerated on Azure.</remarks>
    /// <param name="directoryPath"></param>
    private static void RemoveDatabaseFiles(string directoryPath)
    {
        foreach(string dbFilePath in Directory.GetFiles(directoryPath,"*.db"))
        {
            File.Delete(dbFilePath);
        }
    }

    /// <summary>
    /// Handles parser errors to ensure that a non-zero exit code
    /// is returned when parse errors are encountered.
    /// </summary>
    /// <param name="errors">Parse errors.</param>
    private static void HandleParseError(IEnumerable<Error> errors)
    {
        // To help with Jenkins only errors.
        foreach (var error in errors)
        {
            //We need to exclude these as the nuget package has a bug that causes them to appear even if there is no error.
            if (error as VersionRequestedError == null && error as HelpRequestedError == null && error as MissingRequiredOptionError == null)
            {
                Console.WriteLine("Console error output: " + error.ToString());
                Trace.WriteLine("Trace error output: " + error.ToString());
            }
        }

        if (!(errors.IsHelp() || errors.IsVersion() || errors.Any(e => e is MissingRequiredOptionError)))
            exitCode = 1;
    }

    /// <summary>
    /// Copies the weather files from the specified directories in the apsimx file to the directory.
    /// </summary>
    /// <param name="zipFile"></param>
    private static void CopyWeatherFiles(Options options)
    {
        try
        {            
            string[] apsimxFileInfoArray = GetApsimXFileTextFromDirectory(options.DirectoryPath);
            if (apsimxFileInfoArray.Length != 2)
            {
                throw new Exception("Error: Failed to get APSIMX file text and name from directory.");
            }

            apsimFileName = apsimxFileInfoArray[0];
            string apsimxFileText = apsimxFileInfoArray[1];

            if (apsimxFileText == null)
            {
                throw new Exception("Error: APSIMX file not found.");
            }

            var simulations = FileFormat.ReadFromString<Simulations>(apsimxFileText, e => throw e, false).NewModel as Simulations;
            
            if (simulations == null)
            {
                throw new Exception("Error: Failed to read simulations from APSIMX file.");
            }
            List<Weather> weatherModels = simulations.FindAllDescendants<Weather>().ToList();

            if (weatherModels.Count != 0)
            {
                if (options.Verbose)
                {
                    Console.WriteLine("Weather files found");
                }
            }
            else
            {
                Console.WriteLine("No weather files found");
                return;
            } 

            foreach (Weather weather in weatherModels)
            {
                string oldPath = weather.FileName;
                string source = PathUtilities.GetAbsolutePath(weather.FileName,"").Replace("\\", "/").Replace("APSIM.Workflow/","");
                string destination = options.DirectoryPath + Path.GetFileName(source).Replace("\\", "/");
                string containerWorkingDirPath = "/wd";
                string newPath = Path.Combine(containerWorkingDirPath, Path.GetFileName(source)).Replace("\\", "/");

                try
                {
                    // only copies if the file is in a directory other than the current one.
                    if (oldPath.Contains("\\") || oldPath.Contains("/"))
                        File.Copy(source, destination, true);
                }
                catch(Exception ex)
                {
                    throw new Exception($"Unable to copy weather file from {source} to {destination}. Exception:\n {ex}");
                }

                if (options.Verbose)
                {
                    Console.WriteLine($"Copied weather file: " + "'" + source + "'" + " to " + "'" + destination + "'");
                }
                UpdateWeatherFileNamePathInApsimXFile(apsimxFileText, oldPath, newPath, options);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            exitCode = 1;
        }

    }

    /// <summary>
    /// Gets the path of the apsimx file from the directory.
    /// </summary>
    /// <param name="zipFile"></param>
    /// <returns>An array with two strings: the apsim file name and the text from an apsimx file.</returns>
    /// <exception cref="Exception"></exception>
    public static string[] GetApsimXFileTextFromDirectory(string directoryPathString)
    {
        string apsimFileName = string.Empty;
        string apsimxFileText = string.Empty;
        try
        {
            if (directoryPathString.Last() != '/' && directoryPathString.Last() != '\\')
            {
                directoryPathString += "/"; // Ensure the directory path ends with a forward slash.
            }

            var directoryPath = Path.GetDirectoryName(directoryPathString);

            if (directoryPath == null)
            {
                throw new Exception("Error: Directory path is invalid.");
            }

            string[] files = Directory.GetFiles(directoryPath, "*.apsimx", SearchOption.TopDirectoryOnly);

            if (files.Length > 1)
            {
                throw new Exception("Expected to find a single .apsimx file in the directory. More than one was found.");
            }

            apsimFileName = files[0];
            
            if (string.IsNullOrWhiteSpace(apsimFileName))
            {
                throw new Exception("Error: APSIMX file not found while searching the directory.");
            }

            apsimxFileText = File.ReadAllText(apsimFileName);

            if (string.IsNullOrWhiteSpace(apsimxFileText))
            {
                throw new Exception("Error: While getting apsimx file text, it was found to be null.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            exitCode = 1;
        }

        return new string[] { apsimFileName, apsimxFileText };
    }

        /// <summary>
        /// Updates the weather file name path in the APSIMX file.
        /// </summary>
        /// <param name="apsimxFileText">The APSIMX file text.</param>
        /// <param name="oldPath">The old path of the weather file.</param>
        /// <param name="newPath">The new path of the weather file.</param>
        /// <param name="directoryPath">The directory path.</param>
        public static void UpdateWeatherFileNamePathInApsimXFile(string apsimxFileText, string oldPath, string newPath, Options options)
        {
            string newApsimxFileText = apsimxFileText.Replace("\\\\", "\\").Replace(oldPath, newPath);

            if (string.IsNullOrWhiteSpace(options.DirectoryPath))
                throw new Exception("Error: Directory path is null while trying to update weather file path in APSIMX file.");

            string savePath = Path.Combine(options.DirectoryPath, Path.GetFileName(apsimFileName)).Replace("\\", "/");

            try
            {
                File.WriteAllText(savePath, newApsimxFileText);
            }
            catch (Exception)
            {
                throw new Exception($"Unable to save new weather file path to weather file at :{savePath}");
            }

            if(options.Verbose)
            {
                Console.WriteLine("Successfully updated weather file path in " + savePath);
            }
        }

}



