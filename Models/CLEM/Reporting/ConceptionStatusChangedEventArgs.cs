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

        public ConceptionStatusChangedEventArgs()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="status">Change status</param>
        /// <param name="female">Individual being reported</param>
        /// <param name="dateTime">Current clock</param>
        /// <param name="offspring">The offspring related to</param>
        public ConceptionStatusChangedEventArgs(ConceptionStatus status, RuminantFemale female, DateTime dateTime, Ruminant offspring = null)
        {
            Update(status, female, dateTime, offspring);
            //Status = status;
            //Female = female;

            //// Calculate conception date
            //UpdateConceptionDate(dateTime, offspring?.Age ?? null);
            //switch (Status)
            //{
            //    case ConceptionStatus.Conceived:
            //    case ConceptionStatus.Failed:
            //    case ConceptionStatus.Birth:
            //        ConceptionDate = dateTime.AddMonths(-1 * Convert.ToInt32(female.Age - female.AgeAtLastConception, CultureInfo.InvariantCulture));
            //        ConceptionDate = new DateTime(ConceptionDate.Year, ConceptionDate.Month, DateTime.DaysInMonth(ConceptionDate.Year, ConceptionDate.Month));
            //        break;
            //    case ConceptionStatus.Weaned:
            //        if (offspring is null)
            //            throw new ArgumentException("Code logice error: An offspring must be supplied in ConceptionStatusChangedEventArgs when status is Weaned");
            //        ConceptionDate = dateTime.AddMonths(-1 * Convert.ToInt32(offspring.Age + female.BreedParams.GestationLength, CultureInfo.InvariantCulture));
            //        ConceptionDate = new DateTime(ConceptionDate.Year, ConceptionDate.Month, DateTime.DaysInMonth(ConceptionDate.Year, ConceptionDate.Month));
            //        break;
            //    case ConceptionStatus.Unsuccessful:
            //    case ConceptionStatus.NotMated:
            //    case ConceptionStatus.NotReady:
            //        ConceptionDate = dateTime;
            //        break;
            //    default:
            //        break;
            //}
        }

        public void Update(ConceptionStatus status, RuminantFemale female, DateTime date, Ruminant offspring = null)
        {
            Status = status;
            Female = female;
            UpdateConceptionDate(date, offspring?.Age);
        }

        public void UpdateConceptionDate(DateTime date, double? offspringAge = null)
        {
            switch (Status)
            {
                case ConceptionStatus.Conceived:
                case ConceptionStatus.Failed:
                case ConceptionStatus.Birth:
                    ConceptionDate = date.AddMonths(-1 * Convert.ToInt32(Female.Age - Female.AgeAtLastConception, CultureInfo.InvariantCulture));
                    ConceptionDate = new DateTime(ConceptionDate.Year, ConceptionDate.Month, DateTime.DaysInMonth(ConceptionDate.Year, ConceptionDate.Month));
                    break;
                case ConceptionStatus.Weaned:
                    if (offspringAge is null)
                        throw new ArgumentException("Code logice error: An offspring must be supplied in ConceptionStatusChangedEventArgs when status is Weaned");
                    ConceptionDate = date.AddMonths(-1 * Convert.ToInt32(offspringAge + Female.BreedParams.GestationLength, CultureInfo.InvariantCulture));
                    ConceptionDate = new DateTime(ConceptionDate.Year, ConceptionDate.Month, DateTime.DaysInMonth(ConceptionDate.Year, ConceptionDate.Month));
                    break;
                case ConceptionStatus.Unsuccessful:
                case ConceptionStatus.NotMated:
                case ConceptionStatus.NotReady:
                    ConceptionDate = date;
                    break;
                default:
                    break;
            }
        }
    }
}
