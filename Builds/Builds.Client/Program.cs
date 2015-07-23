
namespace Builds.Client
{
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
                if (args.Length != 1)
                    throw new Exception("Usage: Builds.Client.exe PullRequestID");

                int pullRequestID = Convert.ToInt32(args[0]);

                using (BuildService.BuildProviderClient buildService = new BuildService.BuildProviderClient())
                {
                    buildService.AddBuild(pullRequestID);
                }

                return 0;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return 1;
            }
        }
    }
}
