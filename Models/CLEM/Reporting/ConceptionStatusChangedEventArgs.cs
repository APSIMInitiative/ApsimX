using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Class for reporting conception status change details
    /// </summary>
    [Serializable]
    public class ConceptionStatusChangedEventArgs: EventArgs
    {
        /// <summary>
        /// Current status to report
        /// </summary>
        public ConceptionStatus Status { get; set; }

        /// <summary>
        /// Female being reported
        /// </summary>
        public RuminantFemale Female { get; set; }

        /// <summary>
        /// Date of conception
        /// </summary>
        public DateTime ConceptionDate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="status">Change status</param>
        /// <param name="female">Individual being reported</param>
        /// <param name="dateTime">Current clock</param>
        public ConceptionStatusChangedEventArgs(ConceptionStatus status, RuminantFemale female, DateTime dateTime)
        {
            Status = status;
            Female = female;

            // Calculate conception date
            switch (Status)
            {
                case ConceptionStatus.Conceived:
                case ConceptionStatus.Failed:
                case ConceptionStatus.Birth:
                case ConceptionStatus.Weaned:
                    ConceptionDate = dateTime.AddMonths(-1 * Convert.ToInt32(female.Age - female.AgeAtLastConception, CultureInfo.InvariantCulture));
                    ConceptionDate = new DateTime(ConceptionDate.Year, ConceptionDate.Month, DateTime.DaysInMonth(ConceptionDate.Year, ConceptionDate.Month));
                    break;
                case ConceptionStatus.Unsuccessful:
                case ConceptionStatus.NotMated:
                case ConceptionStatus.NotReady:
                    ConceptionDate = dateTime;
                    break;
                default:
                    break;
            }
        }
    }
}
