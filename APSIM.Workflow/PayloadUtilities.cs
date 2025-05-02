using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace APSIM.Workflow;

/// <summary>
/// Utility class for handling payloads in the APSIM workflow.
/// </summary>
public static class PayloadUtilities
{

    /// <summary>
    /// Production token URL
    /// </summary>
    public static string WORKFLO_API_TOKEN_URL = "https://digitalag.csiro.au/workflo/antiforgery/token";
    
    // // Development token URL
    // public static string WORKFLO_API_TOKEN_URL = "http://localhost:8040/antiforgery/token";

    /// <summary>
    /// Production submit azure URL
    /// </summary>
    public static string WORKFLO_API_SUBMIT_AZURE_URL = "https://digitalag.csiro.au/workflo/submit-azure";

    // // Development submit azure URL
    // public static string WORKFLO_API_SUBMIT_AZURE_URL = "http://localhost:8040/submit-azure";

    /// <summary>
    /// Copies the weather files from the specified directories in the apsimx file to the directory.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="apsimFilePaths"></param>
    public static bool CopyWeatherFiles(Options options, List<string> apsimFilePaths)
    {
        try
        {
            foreach (string apsimFilePath in apsimFilePaths)
            {
                string apsimxFileText = InputUtilities.GetApsimXFileTextFromFile(apsimFilePath);
                if (string.IsNullOrWhiteSpace(apsimxFileText))
                    throw new Exception("Error: Failed to get APSIMX file text.");

                if (FileFormat.ReadFromString<Simulations>(apsimxFileText, e => throw e, false).NewModel is not Simulations simulations)
                    throw new Exception("Error: Failed to read simulations from APSIMX file.");

                List<Weather> weatherModels = simulations.FindAllDescendants<Weather>().ToList();

                if (weatherModels.Count != 0)
                {
                    if (options.Verbose)
                        Console.WriteLine("Weather files found");
                }
                else
                {
                    Console.WriteLine("No weather files found");
                    return true;
                }

                foreach (Weather weather in weatherModels)
                {
                    string oldPath = weather.FileName;
                    string source = PathUtilities.GetAbsolutePath(weather.FileName, apsimFilePath).Replace("\\", "/").Replace("APSIM.Workflow/", "");
                    string destination = Path.Combine(options.DirectoryPath, Path.GetFileName(source)).Replace("\\", "/");
                    string containerWorkingDirPath = "/wd";
                    string newPath = Path.Combine(containerWorkingDirPath, Path.GetFileName(source)).Replace("\\", "/");

                    try
                    {
                        CopyWeatherFileIfElsewhere(options, oldPath, source, destination);
                    }
                    catch (FileNotFoundException)
                    {
                        string[] matchingFiles = GetAllFilesMatchingPath(source);
                        string actualFilePath = GetActualFilePath(matchingFiles);
                        CopyWeatherFileIfElsewhere(options, oldPath, actualFilePath, destination);
                        newPath = Path.Combine(containerWorkingDirPath, Path.GetFileName(actualFilePath)).Replace("\\", "/");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to copy weather file from {source} to {destination}. Exception:\n {ex}");
                    }

                    if (options.Verbose)
                        Console.WriteLine($"Copied weather file: " + "'" + source + "'" + " to " + "'" + destination + "'");
                        
                    UpdateWeatherFileNamePathInApsimXFile(apsimxFileText, oldPath, newPath, options, apsimFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            // exitCode = 1;
        }
        return true;
    }

    /// <summary>
    /// Copies the weather file if it is not in the same directory as the current one.
    /// </summary>
    /// <param name="options">command line arguments</param>
    /// <param name="oldPath">The path in the apsimx file</param>
    /// <param name="source">The absolute path of the weather file</param>
    /// <param name="destination">Where the weather file will be copied into</param>
    private static void CopyWeatherFileIfElsewhere(Options options, string oldPath, string source, string destination)
    {
        // only copies if the file is in a directory other than the current one.
        if (InputUtilities.CheckIfSourceIsDestination(source, destination))
        {
            if (options.Verbose)
                Console.WriteLine("Source and destination are the same. No copy required.");
        }
        else if (oldPath.Contains('\\') || oldPath.Contains('/'))
            File.Copy(source, destination, true);
    }

    /// <summary>
    /// Gets the actual file path from the matching files.
    /// /// </summary>
    /// /// <param name="matchingFiles"></param>
    public static string GetActualFilePath(string[] matchingFiles)
    {
        if (matchingFiles.Length != 0)
        {
            if (matchingFiles.Length > 1)
                throw new Exception($"Multiple files found matching the weather file path.");
            return matchingFiles.First();
        }
        return "";
    }

    /// <summary>Gets all files matching the specified path in the container working directory. </summary>
    /// <param name="source"></param>
    public static string[] GetAllFilesMatchingPath(string source)
    {
        string directory = Path.GetDirectoryName(source) ?? throw new ArgumentNullException(nameof(source), "Source path is invalid or null.");
        string pattern = Path.GetFileName(source) ?? throw new ArgumentNullException(nameof(source), "Source path is invalid or null.");
        // Gets all files matching the pattern in the directory case-insensitively.
        EnumerationOptions EnumerationOptions = new()
        {
            IgnoreInaccessible = true,
            MatchType = MatchType.Win32,
            AttributesToSkip = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System,
            MatchCasing = MatchCasing.CaseInsensitive,
        };
        string[] files = Directory.EnumerateFiles(directory, pattern, EnumerationOptions).ToArray();
        return files;
    }

    /// <summary>
    /// Updates the weather file name path in the APSIMX file.
    /// </summary>
    /// <param name="apsimxFileText">The APSIMX file text.</param>
    /// <param name="oldPath">The old path of the weather file.</param>
    /// <param name="newPath">The new path of the weather file.</param>
    /// <param name="options"></param>
    /// <param name="apsimFilePath"></param>
    public static void UpdateWeatherFileNamePathInApsimXFile(string apsimxFileText, string oldPath, string newPath, Options options, string apsimFilePath)
    {
        string newApsimxFileText = apsimxFileText.Replace("\\\\", "\\").Replace(oldPath, newPath);


        if (string.IsNullOrWhiteSpace(options.DirectoryPath))
            throw new Exception("Error: Directory path is null while trying to update weather file path in APSIMX file.");

        string savePath = Path.Combine(options.DirectoryPath, Path.GetFileName(apsimFilePath)).Replace("\\", "/");

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

    /// <summary>
    /// Creates a zip file from the specified directory.
    /// </summary>
    /// <param name="directoryPath">directory where payload files can be found.</param>
    public static bool CreateZipFile(string directoryPath)
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

        ZipFile.CreateFromDirectory(directoryPath, zipFilePath, CompressionLevel.SmallestSize, false);

        RemoveUnusedFilesFromArchive(zipFilePath);

        if (!File.Exists(zipFilePath))
            throw new Exception("Error: Failed to create zip file.");

        string finalZipFilePath = Path.Combine(directoryPath, "payload.zip");
        if(File.Exists(finalZipFilePath))
            File.Delete(finalZipFilePath);

        File.Move(zipFilePath, finalZipFilePath);
        return true;
    }

    /// <summary>
    /// Removes unused files from the payload archive.
    /// </summary>
    /// <param name="zipFilePath"></param>
    /// <exception cref="Exception"></exception>
    public static void RemoveUnusedFilesFromArchive(string zipFilePath)
    {
        string[] FILE_TYPES_TO_KEEP = [".apsimx", ".xlsx", ".met", ".csv", ".yml", ".yaml", ".env"];
        
        if (string.IsNullOrWhiteSpace(zipFilePath))
            throw new Exception("Error: Zip file path is null while trying to remove unused files from payload archive.");
        
        if (!File.Exists(zipFilePath))
            throw new Exception("Error: Zip file does not exist while trying to remove unused files from payload archive.");

        try
        {
            using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update);
            bool entriesDeleted = true;
            do
            {
                entriesDeleted = false;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    bool entryFileTypeIsMatch = false;

                    foreach (string fileType in FILE_TYPES_TO_KEEP)
                    {
                        if (entry.FullName.EndsWith(fileType, StringComparison.OrdinalIgnoreCase))
                        {
                            entryFileTypeIsMatch = true;
                            break;
                        }
                    }

                    if (!entryFileTypeIsMatch)
                    {
                        entry.Delete();
                        entriesDeleted = true;
                        break;
                    }
                }

            } while (entriesDeleted == true);
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occured while removing unused files from payload archive: {Environment.NewLine}" + ex.Message);
        }

    }

    /// <summary>
    /// Submits the WorkFlo job to the Azure API.   
    /// /// </summary>
    /// /// <param name="directoryPath">The directory path where the payload file is located.</param>
    public static async Task SubmitWorkFloJob(string directoryPath)
    {
        // Setup HTTP client with cookie container.
        CookieContainer cookieContainer = new();
        using HttpClientHandler httpClientHandler = new()
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
        };

        // Get Token for subsequent request.
        using HttpClient httpClient = new(httpClientHandler);
        httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        Uri tokenUri = new(WORKFLO_API_TOKEN_URL);
        var response = await httpClient.GetAsync(tokenUri);
        var token = await httpClient.GetStringAsync(tokenUri);
        CookieCollection responseCookies = cookieContainer.GetCookies(tokenUri);
        
        // Submit Azure job.
        Uri azureSubmitJobUri = new(WORKFLO_API_SUBMIT_AZURE_URL);
        var content = new MultipartFormDataContent
        {
            { 
                new StreamContent(File.OpenRead(Path.Combine(directoryPath, "payload.zip")), 8192),
                "file", "payload.zip" 
            }
        };
        content.Headers.Add("X-XSRF-TOKEN", token);
        HttpRequestMessage request = new(HttpMethod.Post, azureSubmitJobUri) { Content = content };
        HttpResponseMessage message = await httpClient.SendAsync(request);

        if (message.IsSuccessStatusCode)
        {
            Console.WriteLine("WorkFlo job submitted successfully.");
        }
        else
        {
            var responseContentJson = await message.Content.ReadAsStringAsync();
            throw new Exception("Error: Failed to submit WorkFlo job. Reason:\n" + responseContentJson);
        }
    }
}