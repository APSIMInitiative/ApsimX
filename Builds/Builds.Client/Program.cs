
namespace Builds.Client
{
    using Octokit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple command line program that is used by Jenkins build to store
    /// the pull request id for successful runs. This is used later by
    /// the APSIMX during upgrade requests.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2)
                {
                    int pullRequestID = Convert.ToInt32(args[0]);
                    int issueID;
                    string issueTitle;
                    using (BuildService.BuildProviderClient buildService = new BuildService.BuildProviderClient())
                    {
                        GetIssueDetails(pullRequestID, out issueID, out issueTitle);

                        if (args[1] == "/GetIssueID")
                        {
                            Console.Write(issueID);
                        }
                        else if (args[1] == "/AddBuild")
                        {
                            if (issueID > 0)
                                buildService.AddBuild(pullRequestID, issueID, issueTitle);
                        }
                        else
                            throw new Exception("Invalid switch. Should be /GetIssueID or /AddBuild");
                    }
                }
                else
                    throw new Exception("Usage: Builds.Client.exe PullRequestID [/GetIssueID | /AddBuild]");

                return 0;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return 1;
            }
        }

        /// <summary>
        /// Try and get a issue number and title for the specified pull request id. If not 
        /// a valid release then will return issueID = 0;
        /// </summary>
        /// <remarks>
        /// A valid release is one that is merged and has 'Resolves #xxx" in the body
        /// of the pull request.
        /// </remarks>
        /// <param name="pullID"></param>
        /// <param name="issueID">The issue ID found.</param>
        /// <param name="issueTitle">The issue title found.</param>
        private static void GetIssueDetails(int pullID, out int issueID, out string issueTitle)
        {
            issueID = 0;
            issueTitle = null;

            GitHubClient github = new GitHubClient(new ProductHeaderValue("ApsimX"));
            github.Credentials = new Credentials("aba686a636c017ccb0b933560d2615e001985c71");
            Task<PullRequest> pullRequestTask = github.PullRequest.Get("APSIMInitiative", "ApsimX", pullID);
            pullRequestTask.Wait();
            PullRequest pullRequest = pullRequestTask.Result;
            if (pullRequest.Merged)
            {
                issueID = GetIssueID(pullRequest.Body);
                if (issueID != -1)
                {
                    Task<Issue> issueTask = github.Issue.Get("APSIMInitiative", "ApsimX", issueID);
                    issueTask.Wait();
                    issueTitle = issueTask.Result.Title;
                }
            }
        }

        /// <summary>
        /// Returns a resolved issue id or -1 if not found.
        /// </summary>
        /// <param name="pullRequestBody">The text of the pull request body.</param>
        /// <returns>The issue ID or -1 if not found.</returns>
        private static int GetIssueID(string pullRequestBody)
        {
            int posResolves = pullRequestBody.IndexOf("Resolves", StringComparison.InvariantCultureIgnoreCase);
            if (posResolves != -1)
            {
                int posHash = pullRequestBody.IndexOf("#", posResolves);
                if (posHash != -1)
                {
                    int issueID = 0;

                    int posSpace = pullRequestBody.IndexOfAny(new char[] { ' ', '\r', '\n',
                                                                           '\t', '.', ';',
                                                                           ':', '+', '&' }, posHash);
                    if (posSpace != -1)
                        if (Int32.TryParse(pullRequestBody.Substring(posHash + 1, posSpace - posHash - 1), out issueID))
                            return issueID;
                }
            }
            return -1;
        }


    }
}
