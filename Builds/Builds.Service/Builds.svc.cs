
namespace BuildService
{
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
        /// <param name="issueID">The issue ID.</param>
        /// <param name="issueTitle">The issue title.</param>
        public void AddBuild(int pullRequestNumber, int issueID, string issueTitle)
        {
            CheckDBIsOpen();
            string sql = "INSERT INTO ApsimXBuilds (Date, PullRequestID, IssueNumber, IssueTitle) " +
                                      "VALUES (@Date, @PullRequestID, @IssueNumber, @IssueTitle)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd hh:mm tt")));
            command.Parameters.Add(new SqlParameter("@PullRequestID", pullRequestNumber));
            command.Parameters.Add(new SqlParameter("@IssueNumber", issueID));
            command.Parameters.Add(new SqlParameter("@IssueTitle", issueTitle));
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

                if (IsMerged(pullID))
                {
                    Upgrade upgrade = new Upgrade();
                    upgrade.ReleaseDate = (DateTime)reader["Date"];
                    upgrade.pullRequest = pullID;
                    upgrade.IssueTitle = (string)reader["IssueTitle"];
                    upgrade.IssueURL = @"https://github.com/APSIMInitiative/ApsimX/issues/" + (int)reader["IssueNumber"];
                    upgrade.ReleaseURL = @"http://bob.apsim.info/ApsimXFiles/" + pullID + "/APSIMSetup.exe";

                    upgrades.Add(upgrade);
                }
            }
            reader.Close();

            return upgrades;
        }

        /// <summary>
        /// Return true if pull request has been merged.
        /// </summary>
        /// <param name="pullID">The pull request ID.</param>
        /// <returns>true if pull request was merged.</returns>
        private bool IsMerged(int pullID)
        {
            GitHubClient github = new GitHubClient(new ProductHeaderValue("ApsimX"));
            string token = File.ReadAllText(@"C:\inetpub\wwwroot\GitHubToken.txt");
            github.Credentials = new Credentials(token);
            Task<PullRequest> pullRequestTask = github.PullRequest.Get("APSIMInitiative", "ApsimX", pullID);
            pullRequestTask.Wait();
            PullRequest pullRequest = pullRequestTask.Result;
            return (pullRequest != null && pullRequest.Merged);
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
