using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// Convert an integer to a written word e.g. 21 becomes "Twenty One".
    /// </summary>
    public class Integer
    {
        static string[] ones = new string[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        static string[] teens = new string[] { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        static string[] tens = new string[] { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        static string[] thousandsGroups = { "", " Thousand", " Million", " Billion" };

        private static string FriendlyInteger(int n, string leftDigits, int thousands)
        {
            if (n == 0)
                return leftDigits;

            string friendlyInt = leftDigits;
            if (friendlyInt.Length > 0)
                friendlyInt += " ";

            if (n < 10)
                friendlyInt += ones[n];
            else if (n < 20)
                friendlyInt += teens[n - 10];
            else if (n < 100)
                friendlyInt += FriendlyInteger(n % 10, tens[n / 10 - 2], 0);
            else if (n < 1000)
                friendlyInt += FriendlyInteger(n % 100, (ones[n / 100] + " Hundred"), 0);
            else
                friendlyInt += FriendlyInteger(n % 1000, FriendlyInteger(n / 1000, "", thousands + 1), 0);

            return friendlyInt + thousandsGroups[thousands];
        }

        /// <summary>
        /// Perform conversion of int to string.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string ToWritten(int n)
        {
            if (n == 0)
                return "Zero";
            else if (n < 0)
                return "Negative " + ToWritten(-n);

            return FriendlyInteger(n, "", 0);
        }
    }
}
