using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace APSIM.Workflow;

/// <summary>
/// APSIM Validation Apparatus (A.V.A) Utility.
/// </summary>
public static class AvaUtilities
{
    /// <summary>Base URL of the A.V.A API. Routes are appended to this.</summary>
    public const string ApiBaseUrl = "https://ava.apsim.info/api/";

    /// <summary>Route, relative to <see cref="ApiBaseUrl"/>, that is notified when a job completes.</summary>
    public const string JobCompleteRoute = "pr/jobcomplete";

    /// <summary>Path prefix of the bind mount inside the docker container.</summary>
    private const string containerMountPrefix = "/wd/";

    /// <summary>
    /// Builds the JSON body expected by the job complete endpoint.
    /// </summary>
    /// <param name="pullRequest">Pull request number.</param>
    /// <param name="commit">Full commit hash.</param>
    /// <param name="path">Path of the APSIM file. Any docker mount prefix is removed.</param>
    /// <param name="time">Elapsed time of the job in seconds.</param>
    /// <param name="logLines">Log lines produced by the job.</param>
    public static string BuildPayload(string pullRequest, string commit, string path, string time, IEnumerable<string> logLines)
    {
        string relativePath = "";
        if (path is not null)
            relativePath = path;
        if (relativePath.StartsWith(containerMountPrefix, StringComparison.Ordinal))
            relativePath = relativePath[containerMountPrefix.Length..];

        Dictionary<string, object> payload = new()
        {
            ["pullRequest"] = pullRequest ?? "",
            ["commit"] = commit ?? "",
            ["path"] = relativePath,
            ["time"] = time ?? "",
            ["data"] = logLines ?? []
        };
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Reads the log file, if it exists, as a list of lines.
    /// </summary>
    /// <param name="logFile">Path to the log file.</param>
    public static string[] ReadLogLines(string logFile)
    {
        if (string.IsNullOrEmpty(logFile) || !File.Exists(logFile))
            return [];
        return File.ReadAllLines(logFile);
    }

    /// <summary>
    /// Posts the payload to a route of the A.V.A API.
    /// </summary>
    /// <param name="client">HTTP client used to send the request.</param>
    /// <param name="payload">JSON body.</param>
    /// <param name="route">Route relative to <see cref="ApiBaseUrl"/>, e.g. "pr/jobcomplete".</param>
    public static async Task PostAsync(HttpClient client, string payload, string route)
    {
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(ApiBaseUrl + route.TrimStart('/'), content);
        response.EnsureSuccessStatusCode();
    }
}
