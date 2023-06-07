using Models.CLEM.Resources;
using System;
using System.Globalization;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Class for reporting conception status change details
    /// </summary>
    [Serializable]
    public class ConceptionStatusChangedEventArgs : EventArgs
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
        /// Default constructor
        /// </summary>
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
        }

        /// <summary>
        /// Update the conception status of an individual for reporting
        /// </summary>
        /// <param name="status">Status to use</param>
        /// <param name="female">Female this change applies to</param>
        /// <param name="date">Date of change</param>
        /// <param name="offspring">Offspring included</param>
        public void Update(ConceptionStatus status, RuminantFemale female, DateTime date, Ruminant offspring = null)
        {
            Status = status;
            Female = female;
            UpdateConceptionDate(date, offspring?.Age);
        }

        /// <summary>
        /// Performs the update of conception dates based on events such as birth
        /// </summary>
        /// <param name="date"></param>
        /// <param name="offspringAge"></param>
        /// <exception cref="ArgumentException"></exception>
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
