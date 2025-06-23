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

        /// <summary>DO NOT use in pre-sim step, FindByPath uses links that break serialization</summary>
        public static IVariable NameMatchesAPSIMModel(Simulations sims, string name)
        {
            if (!InputUtilities.NameIsAPSIMFormat(name))
                return null;

            string cleanedName = name;

            //strip ( ) out of columns that refer to arrays
            if (name.Contains('(') || name.Contains(')'))
            {
                int openPos = cleanedName.IndexOf('(');
                cleanedName = cleanedName.Substring(0, openPos);
            }

            string[] nameParts = cleanedName.Split('.');
            List<IModel> models = sims.FindAllDescendants(nameParts[0]).ToList();

            foreach(IModel model in models)
            {
                string fullPath = model.FullPath;
                for (int i = 1; i < nameParts.Length; i++)
                    fullPath += "." + nameParts[i];

                IVariable testModel = sims.FindByPath(fullPath);
                if (testModel != null)
                    return testModel;
            }

            return null;
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
