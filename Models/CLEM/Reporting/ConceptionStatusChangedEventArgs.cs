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
        /// <param name="events"></param>
        public ConceptionStatusChangedEventArgs(ConceptionStatus status, RuminantFemale female, DateTime dateTime, Ruminant offspring = null, CLEMEvents events = null)
        {
            Update(status, female, dateTime, offspring, events);
        }

        /// <summary>
        /// Update the conception status of an individual for reporting
        /// </summary>
        /// <param name="status">Status to use</param>
        /// <param name="female">Female this change applies to</param>
        /// <param name="date">Date of change</param>
        /// <param name="offspring">Offspring included</param>
        /// <param name="events"></param>
        public void Update(ConceptionStatus status, RuminantFemale female, DateTime date, Ruminant offspring = null, CLEMEvents events = null)
        {
            Status = status;
            Female = female;
            Female.LastConceptionStatus = status;
            // a null female representing the mother is created if a weaned individual has no mother for reporting purposes only.
            if (Female.Parameters is not null)
            {
                switch (Status)
                {
                    case ConceptionStatus.Conceived:
                    case ConceptionStatus.Failed:
                    case ConceptionStatus.Birth:
                        ConceptionDate = female.DateOfLastConception;
                        break;
                    case ConceptionStatus.Weaned:
                        if (offspring is null)
                            throw new ArgumentException("Code logic error: An offspring must be supplied in ConceptionStatusChangedEventArgs when status is Weaned");
                        DateTime conceived = offspring.DateOfBirth.AddDays(-1 * Convert.ToInt32(offspring.Parameters.General.GestationLength.InDays, CultureInfo.InvariantCulture)+1);
                        ConceptionDate = events.GetTimeStepRangeContainingDate(conceived).start;
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
}
