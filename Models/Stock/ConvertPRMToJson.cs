namespace Models.GrazPlan
{
    using Models.Core;
    using Models.Core.ApsimFile;

    /// <summary>
    /// Convert a stock .prm file to JSON.
    /// </summary>
    public class ConvertPRMToJson
    {
        /// <summary>Convert a GrazPlan PRM file to JSON.</summary>
        public static string Go()
        {
            var paramSet = StockList.MakeParamSet("");
            var simulations = new Simulations();
            simulations.Children.Add(paramSet);
            simulations.Version = Converter.LatestVersion;
            return FileFormat.WriteToString(simulations);
        }
    }
}
