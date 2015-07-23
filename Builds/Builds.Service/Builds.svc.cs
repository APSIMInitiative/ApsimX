
namespace BuildService
{
    using APSIM.Shared.Utilities;
    using Octokit;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Threading.Tasks;


    /// <summary>
    /// Web service that provides access to the ApsimX builds system.
    /// </summary>
    public class Builds : IBuildProvider
    {
        /// <summary>A connection object to the builds DB.</summary>
        private SqlConnection connection = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builds"/> class.
        /// </summary>
        public Builds()
        {
            Open();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:System.ComponentModel.MarshalByValueComponent" />.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>Add a build to the build database.</summary>
        /// <param name="pullRequestNumber">The GitHub pull request number.</param>
        public void AddBuild(int pullRequestNumber)
        {
            CheckDBIsOpen();
            string sql = "INSERT INTO ApsimXBuilds (Date, PullRequestID) " +
                                      "VALUES (@Date, @PullRequestID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd hh:mm tt")));
            command.Parameters.Add(new SqlParameter("@PullRequestID", pullRequestNumber));
            command.ExecuteNonQuery();
        }


        /// <summary>
        /// Gets a list of possible upgrades since the specified pull request.
        /// </summary>
        /// <param name="pullRequestID">The pull request.</param>
        /// <returns>The list of merged pull requests.</returns>
        public List<Upgrade> GetUpgradesSincePullRequest(int pullRequestID)
        {
            string sql = "SELECT * FROM ApsimXBuilds " +
                         "WHERE PullRequestID > " + pullRequestID +
                         " ORDER BY PullRequestID DESC";

            List<Upgrade> upgrades = new List<Upgrade>();

            SqlCommand command = new SqlCommand(sql, connection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int pullID = (int)reader["PullRequestID"];
                DateTime date = (DateTime)reader["Date"];

                Upgrade upgrade = GetUpgrade(pullID);
                if (upgrade != null)
                    upgrades.Add(upgrade);               
            }
            reader.Close();

            return upgrades;
        }

        /// <summary>
        /// Try and get an upgrade object for the specified pull request id. If not 
        /// a valid release then will return null;
        /// </summary>
        /// <remarks>
        /// A valid release is one that is merged and has 'Resolves #xxx" in the body
        /// of the pull request.
        /// </remarks>
        /// <param name="pullID"></param>
        /// <returns></returns>
        private Upgrade GetUpgrade(int pullID)
        {
            GitHubClient github = new GitHubClient(new ProductHeaderValue("ApsimX"));
            github.Credentials = new Credentials("aba686a636c017ccb0b933560d2615e001985c71");
            Task<PullRequest> pullRequestTask = github.PullRequest.Get("APSIMInitiative", "ApsimX", pullID);
            pullRequestTask.Wait();
            PullRequest pullRequest = pullRequestTask.Result;
            if (pullRequest.Merged)
            {
                int issueID = GetIssueID(pullRequest.Body);
                if (issueID != -1)
                {
                    Task<Issue> issueTask = github.Issue.Get("APSIMInitiative", "ApsimX", issueID);
                    issueTask.Wait();
                    Issue issue = issueTask.Result;

                    Upgrade upgrade = new Upgrade();
                    upgrade.ReleaseDate = pullRequest.MergedAt.Value.DateTime;
                    upgrade.pullRequest = pullID;
                    upgrade.IssueTitle = issue.Title;
                    upgrade.IssueURL = @"https://github.com/APSIMInitiative/ApsimX/issues/" + issueID;
                    upgrade.ReleaseURL = @"http://bob.apsim.info/ApsimXFiles/" + pullID + "/APSIMSetup.exe";
                    return upgrade;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a resolved issue id or -1 if not found.
        /// </summary>
        /// <param name="pullRequestBody">The text of the pull request body.</param>
        /// <returns>The issue ID or -1 if not found.</returns>
        private int GetIssueID(string pullRequestBody)
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


        /// <summary>
        /// Open the builds database ready for use.
        /// </summary>
        private void Open()
        {
            if (connection == null)
            {
                string connectionString = File.ReadAllText(@"C:\inetpub\wwwroot\dbConnect.txt") + ";Database=\"APSIM Builds\""; ;
                connection = new SqlConnection(connectionString);
                connection.Open();
            }
        }

        /// <summary>   
        /// Close the SoilsDB connection
        /// </summary>
        private void Close()
        {
            if (connection != null)
            {
                connection.Close();
                connection = null;
            }
        }

        /// <summary>
        /// Check the DB connection is open. Throws if not.
        /// </summary>
        private void CheckDBIsOpen()
        {
            if (connection == null)
                throw new Exception("DB connection isn't open.");
        }

    }
}
