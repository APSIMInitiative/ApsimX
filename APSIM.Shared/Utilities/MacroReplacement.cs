using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace APSIM.Shared.Utilities;

/// <summary>
/// This class performs string replacements. It looks for macros like $weather-file-name
/// and replacements them with a value.
/// </summary>
/// <remarks>
/// [Soil]=SoilLibrary.apsimx;[$soil-name]
/// [Weather].FileName=$weather-file-name
/// [SimulationExp].Name=$sim-name
/// </remarks>
public class Macro
{
    /// <summary>
    /// Perform replacements on a string.
    /// </summary>
    /// <param name="st">The string</param>
    /// <param name="values">Replacement values.</param>
    /// <returns>A string will all macros removed.</returns>
    public static string Replace(string st, Dictionary<string, string> values)
    {
        foreach (var item in values)
            st = st.Replace($"${item.Key}", item.Value);
        return st;
    }
}