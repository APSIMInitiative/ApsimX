using DocumentFormat.OpenXml.Drawing.Charts;
using Models.Core;
using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// A  class to allow the user to easily define the age of individuals in a convienient y,m,d format
    /// </summary>
    [Serializable]
    public class AgeSpecifier: ICloneable
    {
        private const decimal daysPerYear = 365.25M;
        private const decimal daysPerMonth = 30.4M;

        int age = -1;
        int[] parts = new int[] { 0 };

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
        /// Default constructor
        /// </summary>
        public AgeSpecifier()
        {
            
        }

        /// <summary>
        /// Convert from string "y,m,d"
        /// </summary>
        /// <param name="age">Age string representing integers y,m,d</param>
        public static implicit operator AgeSpecifier(String age)
        {
            var parts = Array.ConvertAll(age.Split(','), int.Parse);
            AgeSpecifier temp = new() { Parts = parts };
            return temp;
        }

        /// <summary>
        /// Convert from decimal (months)
        /// </summary>
        /// <param name="age">Decimal age in months</param>
        public static implicit operator AgeSpecifier(decimal age)
        {
            AgeSpecifier temp = new(age);
            return temp;
        }

        /// <summary>
        /// Convert from integer array {y,m,d}
        /// </summary>
        /// <param name="age">An integer array of y,m,d</param>
        public static implicit operator AgeSpecifier(int[] age)
        {
            AgeSpecifier temp = new() { Parts = age};
            return temp;
        }

        /// <summary>
        /// Create instance based on age
        /// </summary>
        /// <param name="months">Age in months</param>
        public AgeSpecifier(decimal months)
        {
            if(months > 12)
            {
                decimal years = Math.Floor(months / 12);
                decimal remainingMonths = months - (years * 12);
                decimal partmonths = months - decimal.Floor(months);
                Parts = new int[] { Convert.ToInt32(years), Convert.ToInt32(remainingMonths), Convert.ToInt32(partmonths*30.4M) };
            }
            else
            {
                decimal partmonths = months - decimal.Floor(months);
                Parts = new int[] { 0, Convert.ToInt32(months), Convert.ToInt32(partmonths * 30.4M) };
            }
            //Parts = new int[] { Convert.ToInt32(months), Convert.ToInt32(30.4m * (months - Convert.ToDecimal(Math.Floor(months)))) };
        }

        /// <summary>
        /// Set by array of integers (y,m,d)
        /// </summary>
        /// <param name="inVal"></param>
        public void SetAgeSpecifier(int[] inVal)
        {
            Parts = inVal;
        }

        /// <summary>
        /// Age in days
        /// </summary>
        [JsonIgnore]
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
        /// An array specifying age in years, months, days
        /// </summary>
        [Description("Age")]
        [Units("y,m,d")]
        [Tooltip("At least days are required with months and years optional (e.g. 5,3 is 5 months and 3 days).")]
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
                if (Parts is not null && Parts.Any())
                    age = CalculateAgeInDays(Parts);
            }
        }

        /// <summary>
        /// Converts the AgeSpecifier to a string  
        /// </summary>
        /// <returns>Comma separated string of years, months, days values</returns>
        public override string ToString() => string.Join(", ", Parts);

        /// <summary>
        /// Converts the AgeSpecifier to a full description string
        /// </summary>
        /// <returns>Comma separated string of number of years, months, days values</returns>
        public string ToDescriptionString()
        {
            StringWriter sw = new();
            int index = 1;
            foreach (var component in Parts)
            {
                if (component > 0 | Parts.Length == 1)
                {
                    if (index > 1 && Parts.Length - index == 0)
                        sw.Write("and ");
                    sw.Write(component.ToString());
                    sw.Write((Parts.Length - index) switch
                    {
                        0 => $" day{((component==1)?"":"s")}",
                        1 => $" month{((component == 1) ? "" : "s")}, ",
                        2 => $" year{((component == 1) ? "" : "s")}, ",
                        _ => $"Invalid age component [{index}], "
                    });
                }
                index++;
            }
            return sw.ToString().TrimEnd(new char[] { ',', ' '});
        }

        /// <summary>
        /// A method to calculate age in days from user provided age in years,months,days array
        /// </summary>
        /// <param name="ageComponents">The array of 1 to 3 items representing age (y,m,d)</param>
        /// <returns>Age in days</returns>
        /// <exception cref="InvalidOperationException">The array passed has too many items</exception>
        public int CalculateAgeInDays(IEnumerable<int> ageComponents)
        {
            int age = 0;
            int index = 1;
            foreach (var component in ageComponents)
            {
                age += ((Parts.Length - index) switch
                {
                    0 => component,
                    1 => Convert.ToInt32(DaysPerMonth * component),
                    2 => Convert.ToInt32(DaysPerYear * component),
                    _ => throw new InvalidOperationException("Invalid array length provided to CalculateAgeInDays. Expecting 1 to 3 items in array representing years (optional), months (optional), and days."),
                });
                index++;
            }
            return age;
        }

        /// <summary>
        /// Create clone of this AgeSpecifier
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            AgeSpecifier cloned = new();
            cloned = Parts;
            return cloned;
        }
    }

}