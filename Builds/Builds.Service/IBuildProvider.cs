using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace BuildService
{
    /// <summary>
    /// Web service that provides access to the ApsimX builds system.
    /// </summary>
    [ServiceContract]
    public interface IBuildProvider
    {
        /// <summary>Add a build to the build database.</summary>
        /// <param name="pullRequestNumber">The GitHub pull request number.</param>
        /// <param name="issueID">The issue ID.</param>
        /// <param name="issueTitle">The issue title.</param>
        [OperationContract]
        void AddBuild(int pullRequestNumber, int issueID, string issueTitle);

        /// <summary>
        /// Gets a list of possible upgrades since the specified pull request.
        /// </summary>
        /// <param name="pullRequestID">The pull request.</param>
        /// <returns>The list of merged pull requests.</returns>
        [OperationContract]
        List<Upgrade> GetUpgradesSincePullRequest(int pullRequestID);

    }

    /// <summary>
    /// An class encapsulating an upgrade 
    /// </summary>
    public class Upgrade
    {
        public DateTime ReleaseDate { get; set; }
        public int pullRequest { get; set; }
        public string IssueTitle { get; set; }
        public string IssueURL { get; set; }
        public string ReleaseURL { get; set; }
    }

}
