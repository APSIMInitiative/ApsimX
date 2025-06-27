using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models
{
    /// <summary></summary>
    public static class InputUtilities
    {
        /// <summary>DO NOT use in pre-sim step, FindByPath uses links that break serialization</summary>
        public static Type GetTypeOfCell(object value)
        {
            if (value == null)
                return null;

            string valueAsString = value.ToString();
            if (valueAsString.Length == 0)
                return null;

            valueAsString.Trim();

            if (DateUtilities.ValidateStringHasYear(valueAsString)) //try parsing to date
            {
                string dateString = DateUtilities.ValidateDateString(valueAsString);
                if (dateString != null)
                {
                    DateTime date = DateUtilities.GetDate(valueAsString);
                    if (DateUtilities.CompareDates("1900/01/01", date) >= 0)
                        return typeof(DateTime);
                }
            }

            //try parsing to double
            bool d = double.TryParse(valueAsString, out double num);
            if (d == true)
            {
                double wholeNum = num - Math.Floor(num);
                if (wholeNum == 0) //try parsing to int
                    return typeof(int);
                else
                    return typeof(double);
            }

            bool b = bool.TryParse(valueAsString, out bool boolean);
            if (b == true)
                return typeof(bool);

            return typeof(string);
        }

        /// <summary></summary>
        public static bool NameIsAPSIMFormat(string columnName)
        {
            if (columnName.Contains('.'))
                return true;
            else
                return false;
        }
    }
}
