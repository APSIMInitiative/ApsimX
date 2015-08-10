
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
        /// Gets a list of possible upgrades since the specified issue number.
        /// </summary>
        /// <param name="issueNumber">The issue number.</param>
        /// <returns>The list of possible upgrades.</returns>
        public List<Upgrade> GetUpgradesSinceIssue(int issueNumber)
        {
            List<Upgrade> upgrades = new List<Upgrade>();

            DateTime issueResolvedDate = GetIssueResolvedDate(issueNumber);

            string sql = "SELECT * FROM ApsimXBuilds " +
                             //"WHERE Date >= Convert(datetime, '" + string.Format("{0:yyyy-MM-dd}", issueResolvedDate) + "')" +
                             "WHERE Date >= " + string.Format("'{0:yyyy-MM-ddThh:mm:ss tt}'", issueResolvedDate) +
                             " ORDER BY Date DESC";

            SqlCommand command = new SqlCommand(sql, connection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int pullID = (int)reader["PullRequestID"];
                DateTime date = (DateTime)reader["Date"];

                int buildIssueNumber = (int)reader["IssueNumber"];

                string version = ((DateTime)reader["Date"]).ToString("yyyy.MM.dd") + "." + buildIssueNumber;

                Upgrade upgrade = new Upgrade();
                upgrade.ReleaseDate = (DateTime)reader["Date"];
                upgrade.issueNumber = buildIssueNumber;
                upgrade.IssueTitle = (string)reader["IssueTitle"];
                upgrade.IssueURL = @"https://github.com/APSIMInitiative/ApsimX/issues/" + buildIssueNumber;
                upgrade.ReleaseURL = @"http://bob.apsim.info/ApsimXFiles/" + buildIssueNumber + "/Apsim" + version + " Setup.exe";

                upgrades.Add(upgrade);
            }
            reader.Close();

            return upgrades;
        }

        /// <summary>
        /// Return the date the specified issue was resolved.
        /// </summary>
        /// <param name="issueNumber">The issue number</param>
        /// <returns>The date.</returns>
        private DateTime GetIssueResolvedDate(int issueNumber)
        {
            DateTime resolvedDate = new DateTime(2015, 1, 1);

            string sql = "SELECT * FROM ApsimXBuilds " +
                         "WHERE IssueNumber = " + issueNumber;
            SqlCommand command = new SqlCommand(sql, connection);
            SqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
                resolvedDate = (DateTime)reader["Date"];
            reader.Close();

            return resolvedDate;
        }

        /// <summary>
        /// Add a upgrade registration into the database.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="organisation"></param>
        /// <param name="address1"></param>
        /// <param name="address2"></param>
        /// <param name="city"></param>
        /// <param name="state"></param>
        /// <param name="postcode"></param>
        /// <param name="country"></param>
        /// <param name="email"></param>
        /// <param name="product"></param>
        public void RegisterUpgrade(string firstName, string lastName, string organisation, string address1, string address2,
                    string city, string state, string postcode, string country, string email, string product)
        {
            string connectionString = System.IO.File.ReadAllText(@"C:\inetpub\wwwroot\dbConnect.txt") + ";Database=ProductRegistrations";
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string SQL = "INSERT INTO Registrations (Date, FirstName, LastName, Organisation, Address1, Address2, City, State, Postcode, Country, Email, Product) " +
                        "VALUES (@Date, @FirstName, @LastName, @Organisation, @Address1, @Address2, @City, @State, @Postcode, @Country, @Email, @Product)";

            SqlCommand command = new SqlCommand(SQL, connection);
            command.Parameters.Add(new SqlParameter("@Date", DateTime.Now));
            command.Parameters.Add(new SqlParameter("@FirstName", firstName));
            command.Parameters.Add(new SqlParameter("@LastName", lastName));
            command.Parameters.Add(new SqlParameter("@Organisation", organisation));
            command.Parameters.Add(new SqlParameter("@Address1", address1));
            command.Parameters.Add(new SqlParameter("@Address2", address2));
            command.Parameters.Add(new SqlParameter("@City", city));
            command.Parameters.Add(new SqlParameter("@State", state));
            command.Parameters.Add(new SqlParameter("@Postcode", postcode));
            command.Parameters.Add(new SqlParameter("@Country", country));
            command.Parameters.Add(new SqlParameter("@Email", email));
            command.Parameters.Add(new SqlParameter("@Product", product));
            command.ExecuteNonQuery();

            connection.Close();
        }

        /// <summary>
        /// Return true if issue has been closed.
        /// </summary>
        /// <param name="issueID">The issue ID.</param>
        /// <returns>true if issue is closed.</returns>
        private bool IsClosed(int issueID)
        {
            GitHubClient github = new GitHubClient(new ProductHeaderValue("ApsimX"));
            string token = File.ReadAllText(@"C:\inetpub\wwwroot\GitHubToken.txt");
            github.Credentials = new Credentials(token);
            Task<Issue> issueTask = github.Issue.Get("APSIMInitiative", "ApsimX", issueID);
            issueTask.Wait();
            Issue issue = issueTask.Result;
            return (issue != null && issue.State == ItemState.Closed);
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
