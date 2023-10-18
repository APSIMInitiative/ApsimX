using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// A  class to allow the user to easily define the age of individuals
    /// </summary>
    [Serializable]
    public class AgeSpecifier
    {
        private const decimal daysPerYear = 365.25M;
        private const decimal daysPerMonth = 30.4M;

        int age = -1;
        int[] parts = null;

        /// <summary>
        /// The number of days per year
        /// </summary>
        [JsonIgnore]
        public static decimal DaysPerYear { get { return daysPerYear; } }

        /// <summary>
        /// The number of days per month
        /// </summary>
        [JsonIgnore]
        public static decimal DaysPerMonth { get { return daysPerMonth; } }

        /// <summary>
        /// Age in days
        /// </summary>
        public int InDays
        {
            get
            {
                if (age < 0)
                {
                    if (Parts is null || Parts.Length == 0)
                    {
                        return 0;
                    }
                    age = CalculateAgeInDays(Parts);
                }
                return age;
            }
        }

        /// <summary>
        /// An array containing each part of the specified Age representing years, months, days
        /// </summary>
        [Description("Age")]
        [Units("years, months, days")]
        [Required, ArrayItemCount(1, 3)]
        public int[] Parts 
        {
            get
            { 
                return parts;
            }
            set
            {
                parts = value;
                if (Parts is not null && Parts.Length > 0)
                    age = CalculateAgeInDays(Parts);
            }
        }

        /// <summary>
        /// Converts the AgeSpecifier to a strings 
        /// </summary>
        /// <returns>Comma separated years, months, days values</returns>
        public override string ToString() => string.Join(", ", Parts);

        /// <summary>
        /// Converts the AgeSpecifier to a strings 
        /// </summary>
        /// <returns>Comma separated years, months, days values</returns>
        public string ToDescriptionString()
        {
            StringWriter sw = new StringWriter();
            int index = 0;
            foreach (var component in Parts)
            {
                sw.Write(index switch
                {
                    0 => (component>0 || Parts.Length == 1)?$"{component} days":"",
                    1 => (component > 0) ? $"{component} months, " : "",
                    2 => (component > 0) ? $"{component} years, " : "",
                    _ => $"Invalid age component [{index}], "
                });
                index++;
            }
            return sw.ToString().TrimEnd(new char[] { ',', ' '});
        }

        /// <summary>
        /// A method to calculate age in days from user provided age in years,months,days array
        /// </summary>
        /// <param name="ageComponents">The array of 1 to 3 items representing age</param>
        /// <returns>Age in days</returns>
        /// <exception cref="InvalidOperationException">The array passed has too many items</exception>
        public static int CalculateAgeInDays(IEnumerable<int> ageComponents)
        {
            int age = 0;
            int index = 0;
            foreach (var component in ageComponents)
            {
                age += index switch
                {
                    0 => component,
                    1 => Convert.ToInt32(DaysPerMonth * component),
                    2 => Convert.ToInt32(DaysPerYear * component),
                    _ => throw new InvalidOperationException("Invalid array length provided to CalculateAgeInDays. Expecting 1 to 3 items in array representing years (optional), months (optional), and days."),
                };
                index++;
            }
            return age;
        }
    }

}