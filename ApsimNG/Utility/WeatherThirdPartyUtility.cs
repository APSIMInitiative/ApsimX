using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APSIMNG.Utility;

/// <summary>
/// Utility class for accessing third party weather data sources.
/// </summary>
public static class WeatherThirdPartyUtility
{
    // ---- required for unit testing ------
    internal static Func<string, CancellationToken, Task<MemoryStream>> ExtractDataFromURL = WebUtilities.ExtractDataFromURL;
    internal static Func<string, CancellationToken, Task<string>> GetStringFromURL = WebUtilities.GetStringFromURL;

    internal static void ResetTestHooks()
    {
        ExtractDataFromURL = WebUtilities.ExtractDataFromURL;
        GetStringFromURL = WebUtilities.GetStringFromURL;
    }

    // ------ ********************* -------

    /// <summary>
    /// URI for accessing the NASA POWER API for global weather data. 
    /// Parameters currently set to return daily max and min temperature, precipitation,
    /// solar radiation, relative humidity and wind speed.
    /// </summary>
    private static string NASAPOWERAPI = "https://power.larc.nasa.gov/api/temporal/daily/point?parameters=T2M_MAX,T2M_MIN,ALLSKY_SFC_SW_DWN,RH2M,WS2M,PRECTOTCORR&community=AG&format=JSON&time-standard=LST&";
 
    /// <summary>
    /// Get the NASA POWER data
    /// </summary>
    /// <param name="lat">Latitude value as a double</param>
    /// <param name="lon">Longitude value as a double</param>
    /// <param name="startDateStr">Date string formatted as YYYY-MM-DD</param>
    /// <param name="endDateStr">Date string formatted as YYYY-MM-DD</param>
    /// <param name="useWorldModellersRain"></param>
    /// <returns>APSIM met file content. This will never return null</returns>
    public static async Task<MetFile> GetNasaPower(double lat, double lon, string startDateStr, string endDateStr, bool useWorldModellersRain)
    {
        string metFileContent = string.Empty;

        // Work out how many days of data.
        DateTime startDate = DateUtilities.GetDate(startDateStr);
        DateTime endDate = DateUtilities.GetDate(endDateStr);
        int numDays = (endDate - startDate).Days + 1;

        string[] constants = [
            "latitude = " + lat.ToString(),
            "longitude = " + lon.ToString(),
        ];

        string[] columns = [
            "year",
            "day",
            "maxt",
            "mint",
            "radn",
            "rh",
            "wind",
            "rain",
        ];

        string[] units = [
            "",
            "",
            "oC",
            "oC",
            "MJ/m^2",
            "%",
            "m/s",
            "mm"
        ];

        try
        {
            // Add Year and Day variables
            Dictionary<DateTime,double> YearDictionary = new();
            Dictionary<DateTime,double> DOYDictionary = new();
            for(int i = 0; i < numDays; i++)
            {
                DateTime date = startDate.AddDays(i);
                YearDictionary[date] = date.Year;
                DOYDictionary[date] = date.DayOfYear;
            }
            List<WeatherVariable> allVariables = [new WeatherVariable("year", YearDictionary)];
            WeatherVariable dayWeatherVariable = new("day", DOYDictionary);
            allVariables.Add(dayWeatherVariable);

            // Get the NASA Power variables.
            allVariables.AddRange(await GetWeatherVariablesFromNASAPower(numDays, lat, lon, startDateStr, endDateStr));

            // Get and add the rain variable from World Modellers API.
            if (useWorldModellersRain == true)
            {
                allVariables.RemoveAll(x => x.Name == "PRECTOTCORR"); // PRECTOTCORR a.k.a rain.
                allVariables.Add(await GetRainVariablesFromWorldModellers(lat, lon, startDate, endDate));
            }

            // Create a MetFile object initially for the NASA Power data.
            // Ensure variables are ordered to match the expected columns so that
            // values are placed into the correct met columns regardless of the
            // order returned by the NASA POWER API.
            List<List<double>> valueLists = new();

            // Helper to map our column name to the variable name returned/created.
            string MapColumnToVariableName(string col)
            {
                return col switch
                {
                    "year" => "year",
                    "day" => "day",
                    "maxt" => "T2M_MAX",
                    "mint" => "T2M_MIN",
                    "radn" => "ALLSKY_SFC_SW_DWN",
                    "rh"   => "RH2M",
                    "wind" => "WS2M",
                    "rain" => useWorldModellersRain ? "rain" : "PRECTOTCORR",
                    _ => col
                };
            }

            foreach (string col in columns)
            {
                string varName = MapColumnToVariableName(col);
                WeatherVariable variable = allVariables.FirstOrDefault(v => string.Equals(v.Name, varName, StringComparison.OrdinalIgnoreCase));
                if (variable == null)
                    throw new Exception($"Expected weather variable '{varName}' not found when constructing met file.");

                List<double> newVariableList = new();
                for (int d = 0; d < numDays; d++)
                {
                    DateTime date = startDate.AddDays(d);
                    if (!variable.Values.TryGetValue(date, out double val))
                        val = double.NaN;
                    newVariableList.Add(val);
                }
                valueLists.Add(newVariableList);
            }

            MetFile nasaMetFile = new MetFile();
            nasaMetFile.Load(constants, columns, units, startDateStr, numDays, valueLists);
            if (nasaMetFile.IsEmpty())
                throw new Exception("Weather cannot be created. No MetFile data was added.");
            return nasaMetFile;
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving NASA POWER data: " + ex.Message, ex);
        }
    }

    private static string CreateAPSIMMetFileString(DateTime startDate, int numDays, List<WeatherVariable> allVariables, List<string> newAPSIMFileLines, bool useWorldModellersRain)
    {
        string metFileContent;
        for (int i = 0; i < numDays; i++)
        {
            DateTime date = startDate.AddDays(i);
            Dictionary<string, string> variableValues = new(){
                {"mint","T2M_MIN"},
                {"maxt","T2M_MAX"},                
                {"radn","ALLSKY_SFC_SW_DWN"},                
                {"rh","RH2M"},                
                {"wind","WS2M"},
                {"rain","rain"},
            };

            // Use the NASA Power variable if 
            if (useWorldModellersRain == false)
                variableValues["rain"] = "PRECTOTCORR";

            // Create a line where the order of variables is as follows:
            // date, mint, maxt, radn, rh, wind, rain
            // The value from Values with the key that matches local 'date' variable will be selected.
            string newLine = string.Format("{0,-12}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}{6,-10}",
                date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                allVariables.FirstOrDefault(w => w.Name == variableValues["mint"]).Values[date],
                allVariables.FirstOrDefault(w => w.Name == variableValues["maxt"]).Values[date],
                allVariables.FirstOrDefault(w => w.Name == variableValues["radn"]).Values[date],
                allVariables.FirstOrDefault(w => w.Name == variableValues["rh"]).Values[date],
                allVariables.FirstOrDefault(w => w.Name == variableValues["wind"]).Values[date],
                allVariables.FirstOrDefault(w => w.Name == variableValues["rain"]).Values[date]);
            newAPSIMFileLines.Add(newLine);

        }
        // Write all the lines with formatting into metFileContent.
        metFileContent = string.Join(Environment.NewLine, newAPSIMFileLines);
        return metFileContent;
    }

    private static async Task<List<WeatherVariable>> GetWeatherVariablesFromNASAPower(int numDays, double lat, double lon, string startDateStr, string endDateStr)
    {
        string url = String.Format("{0}latitude={1}&longitude={2}&start={3:yyyyMMdd}&end={4:yyyyMMdd}",
                        NASAPOWERAPI, lat, lon, startDateStr.Replace("-", ""), endDateStr.Replace("-", ""));
        List<WeatherVariable> allNASAPowerVariables = new();
        MemoryStream stream = await ExtractDataFromURL(url, CancellationToken.None);
        stream.Position = 0;
        using (var reader = new JsonTextReader(new StreamReader(stream)))
        {
            JObject json = JObject.Load(reader);
            JObject parameterBlock = json["properties"]?["parameter"] as JObject;

            int parameterCount = parameterBlock.Children().Count();
            int expectedParameterCount = Enum.GetNames(typeof(NASAPOWERParameters)).Length;
            if (parameterCount < expectedParameterCount)
                throw new Exception($"Unexpected number of parameters returned from NASA POWER API. " +
                $"Expected at least {expectedParameterCount} but got {parameterCount}.");

            // Do a check on the first parameters child count and ensure it matches the number of days we expect. This is a sanity check to ensure we have the expected number of records.
            JToken firstParameter = parameterBlock.Children().First();
            int recordCount = firstParameter.Children().First().Children().Count();
            if (recordCount != numDays)
                throw new Exception($"Unexpected number of records returned from NASA POWER API. " +
                $"Expected {numDays} but got {recordCount}.");

            // Load all the lines for a parameter into their own NasaPowerVariable objects. 
            foreach (JToken parameter in parameterBlock.Children())
            {
                WeatherVariable variable = new WeatherVariable();
                variable.Name = ((JProperty)parameter).Name;
                JObject parameterValues = ((JProperty)parameter).Value as JObject;
                foreach (JToken value in parameterValues.Children())
                {
                    string dateStr = ((JProperty)value).Name;
                    dateStr = dateStr.Insert(6, "-").Insert(4, "-");
                    DateTime date = DateUtilities.GetDate(dateStr);
                    string valueStr = ((JProperty)value).Value.ToString();
                    variable.Values[date] = double.Parse(valueStr);
                }
                allNASAPowerVariables.Add(variable);
            }
        }
        return allNASAPowerVariables;
    }

    private static async Task<WeatherVariable> GetRainVariablesFromWorldModellers(double lat, double lon, DateTime startDate, DateTime endDate)
    {
        // Next we get rain from the worldmodellers api and then combine it with 
        // the NASA POWER data to create the final met file content.
        // Worldmodellers API provides CHIRPS precipitation data which is generally 
        // more accurate than the NASA POWER precipitation data, so we prefer to use that where possible.
        string worldmodellersURL = String.Format("https://worldmodel.csiro.au/gclimate?lat={0}&lon={1}&format=apsim&start={2:yyyyMMdd}&stop={3:yyyyMMdd}",
                        lat, lon, startDate, endDate);
        string apsimDataStr = await GetStringFromURL(worldmodellersURL, CancellationToken.None);
        MetFile metFile = MetFile.Create(apsimDataStr);
        Dictionary<DateTime, double> rainData = new Dictionary<DateTime, double>();
        int rainColumnIndex = Array.IndexOf(metFile.Columns, "rain");
        for (int i = 0; i < metFile.NumberOfDays; i++)
        {
            DateTime date = startDate.AddDays(i);
            double[] record = metFile.GetDay(date);
            rainData[date] = record[rainColumnIndex];
        }
        WeatherVariable rainVariable = new("rain",rainData);
        return rainVariable;
    }

    /// <summary>
    /// Creates a the top an APSIM met file that includes comments, column names and their units.
    /// </summary>
    /// <param name="constants"></param>
    /// <param name="columns"></param>
    /// <param name="units"></param>
    /// <returns></returns>
    private static List<string> CreateAPSIMMetFileTopSection(string[] constants, string[] columns, string[] units)
    {
        List<string> newAPSIMFileLines = new();
        foreach (string constant in constants)
            newAPSIMFileLines.Add(constant);
        newAPSIMFileLines.Add(string.Format("{0,-12}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}{6,-10}",
            columns[0], columns[1], columns[2], columns[3], columns[4], columns[5], columns[6]));
        newAPSIMFileLines.Add(string.Format("{0,-12}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}{6,-10}",
            units[0], units[1], units[2], units[3], units[4], units[5], units[6]));
        return newAPSIMFileLines;
    }
}

/// <summary>
/// Enum for the parameters we are currently retrieving from the NASA POWER API. 
/// </summary>
public enum NASAPOWERParameters
{
    T2M_MAX,
    T2M_MIN,
    ALLSKY_SFC_SW_DWN,
    PRECTOTCORR,
    RH2M,
    WS2M
}

/// <summary>
/// Convenience class for deserialising the NASA POWER API response. 
/// </summary>
public class WeatherVariable
{
    /// <summary>
    /// Constructor
    /// </summary>
    public WeatherVariable(){}

    /// <summary>
    /// Constructor that takes a name.
    /// </summary>
    /// <param name="name"></param>
    public WeatherVariable(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Full Constructor.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    public WeatherVariable(string name, Dictionary<DateTime, double> values)
    {
        Name = name;
        Values = values;
    }

    public string Name { get; set; }
    private Dictionary<DateTime, double> values = new Dictionary<DateTime, double>();

    public Dictionary<DateTime, double> Values
    {
        get { return values; }
        set { values = value; }
    }


}