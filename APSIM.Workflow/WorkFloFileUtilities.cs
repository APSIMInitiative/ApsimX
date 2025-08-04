
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace APSIM.Workflow;

/// <summary>
/// Utility class for creating and managing WorkFlo files.
/// A workflow file is a YAML file that defines a series of tasks to be executed in a specific order.
/// </summary>
public static class WorkFloFileUtilities
{
    /// <summary>
    /// Creates a validation workflow file in the specified directory.
    /// </summary>
    /// <param name="options">Command line argument values</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(Options options)
    {
        try
        {
            // string currentBuildNumber = Task.Run(GetCurrentBuildNumberAsync).Result; // TODO: Uncomment currentBuildNumber once development is complete
            string currentBuildNumber = "10018"; // Placeholder for development, replace with actual call to GetCurrentBuildNumberAsync
            string timeFormat = "yyyy.M.d-HH:mm";
            TimeZoneInfo brisbaneTZ = TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time");
            DateTime brisbaneDatetimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, brisbaneTZ);
            string workFloFileName = "workflow.yml";
            // Include the workflow yml file for debugging purposes
            string[] inputFiles = [
                ".env",
                workFloFileName,
                "grid.csv"
            ];
            string workFloFileContents = $"""
            name: workflo_apsim_validation
            inputfiles:
            - .env
            - workflow.yml
            - grid.csv
            tasks:
            - name: 1
              inputfiles:
              - .env
              - workflow.yml
              grid: grid.csv
              steps:
                - uses: ric394/apsimx:{options.DockerImageTag}
                  args: --recursive "$Path*.apsimx"

                - uses: apsiminitiative/postats2-collector:latest
                  args: upload {currentBuildNumber} {options.CommitSHA} {options.GitHubAuthorID} {brisbaneDatetimeNow.ToString(timeFormat)} "$Path"
              finally:
                - uses: apsiminitiative/postats2-collector:latest
                  args: upload {currentBuildNumber} {options.CommitSHA} {options.GitHubAuthorID} {brisbaneDatetimeNow.ToString(timeFormat)} "$Path"
            """;
            File.WriteAllText(Path.Combine(options.DirectoryPath, workFloFileName), workFloFileContents);
            Console.WriteLine($"Workflow.yml contents:\n{workFloFileContents}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating validation workflow file: {ex.Message}\n{ex.StackTrace}");
        }

    }


    /// <summary>
    /// Gets the directory name from the specified directory path.
    /// </summary>
    /// <param name="directoryPathString"></param>
    public static string GetDirectoryName(string directoryPathString)
    {
        if (string.IsNullOrEmpty(directoryPathString))
        {
            throw new ArgumentNullException("directoryPathString");
        }
        string dirName = Path.GetFileName(Path.GetDirectoryName(directoryPathString))!;
        return dirName;
    }


    /// <summary>
    /// Gets the current build number.
    /// </summary>
    /// <returns></returns>
    public static async Task<string> GetCurrentBuildNumberAsync()
    {
        using HttpClient client = new()
        {
            BaseAddress = new Uri("https://builds.apsim.info/api/"),
        };
        using HttpResponseMessage response = await client.GetAsync("nextgen/nextversion/");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return jsonResponse;
    }

}
