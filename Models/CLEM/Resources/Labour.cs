using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using Models.CLEM.Activities;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Labour Person models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all labour types (people) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/Labour.htm")]
    public class Labour: ResourceBaseWithTransactions, IValidatableObject
    {
        private List<string> WarningsMultipleEntry = new List<string>();
        private List<string> WarningsNotFound = new List<string>();
        private Relationship adultEquivalentRelationship = null;

        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Labour types currently available.
        /// </summary>
        [XmlIgnore]
        public List<LabourType> Items { get; set; }

        /// <summary>
        /// Allows indiviuals to age each month
        /// </summary>
        [Description("Allow individuals to age")]
        [Required]
        public bool AllowAging { get; set; }

        private LabourAvailabilityList availabilityList;

        /// <summary>
        /// Current pay rate value of individuals
        /// </summary>
        [XmlIgnore]
        public LabourPricing PayList;

        /// <summary>
        /// Determine if a price schedule has been provided for this individual
        /// </summary>
        /// <returns>boolean</returns>
        public bool PricingAvailable { get { return (PayList != null); } }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // locate resources
            availabilityList = Apsim.Children(this, typeof(LabourAvailabilityList)).Cast<LabourAvailabilityList>().FirstOrDefault();

            // locate AE relationship
            adultEquivalentRelationship = Apsim.Children(this, typeof(Relationship)).Where(a => a.Name.ToUpper().Contains("AE")).Cast<Relationship>().FirstOrDefault();

            if (Clock.Today.Day != 1)
            {
                OnStartOfMonth(this, null);
            }
        }

        /// <summary>
        /// A method to calculate the total dietary intake by metric
        /// </summary>
        /// <param name="metric">Metric to use</param>
        /// <param name="includeHiredLabour">Include hired labour in calculations</param>
        /// <param name="reportPerAE">Report result as per Adult Equivalent</param>
        /// <returns>Amount eaten</returns>
        public double GetDietaryValue(string metric, bool includeHiredLabour, bool reportPerAE)
        {
            double value = 0;
            foreach (LabourType ind in Items.Where(a => includeHiredLabour | (a.Hired == false)))
            {
                value += ind.GetDietDetails(metric);
            }
            if(reportPerAE)
            {
                value /= AdultEquivalents(includeHiredLabour);
            }
            return value;
        }

        /// <summary>
        /// A method to calculate the total dietary intake by metric
        /// </summary>
        /// <param name="metric">Metric to use</param>
        /// <param name="includeHiredLabour">Include hired labour in calculations</param>
        /// <param name="reportPerAE">Report result as per Adult Equivalent</param>
        /// <returns>Amount eaten per day</returns>
        public double GetDailyDietaryValue(string metric, bool includeHiredLabour, bool reportPerAE)
        {
            int daysInMonth = DateTime.DaysInMonth(Clock.Today.Year, Clock.Today.Month);
            return GetDietaryValue(metric, includeHiredLabour, reportPerAE) / daysInMonth;
        }

        /// <summary>
        /// Validation of this resource
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Add warning if no individuals defined
            if (Apsim.Children(this, typeof(LabourType)).Count > 0 && Apsim.Children(this, typeof(LabourType)).Cast<LabourType>().Sum(a => a.Individuals) == 0)
            {
                string warningString = "No individuals have been set in any [r=LabourType]\nAdd individuals or consider removing or disabling [r=Labour]";
                if (!WarningsNotFound.Contains(warningString))
                {
                    WarningsNotFound.Add(warningString);
                    Summary.WriteWarning(this, warningString);
                }
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<LabourType>();
            foreach (LabourType labourChildModel in Apsim.Children(this, typeof(LabourType)).Cast<LabourType>().ToList())
            {
                for (int i = 0; i < labourChildModel.Individuals; i++)
                {
                    // get the availability from provided list

                    LabourType labour = new LabourType()
                    {
                        Gender = labourChildModel.Gender,
                        Individuals = 1,
                        Parent = this,
                        InitialAge = labourChildModel.InitialAge,
                        AgeInMonths = labourChildModel.InitialAge * 12,
                        LabourAvailability = labourChildModel.LabourAvailability,
                        Name = labourChildModel.Name + ((labourChildModel.Individuals > 1) ? "_" + (i + 1).ToString() : ""),
                        Hired = labourChildModel.Hired
                    };
                    labour.TransactionOccurred += Resource_TransactionOccurred;
                    Items.Add(labour);
                }
            }
            // clone pricelist so model can modify if needed and not affect initial parameterisation
            if (Apsim.Children(this, typeof(LabourPricing)).Count() > 0)
            {
                PayList = (Apsim.Children(this, typeof(LabourPricing)).FirstOrDefault() as LabourPricing).Clone();
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (LabourType childModel in Apsim.Children(this, typeof(LabourType)))
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
            if (Items != null)
            {
                Items.Clear();
            }
            Items = null;
        }

        /// <summary>An event handler to allow us to check if labour availability is available.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            foreach (LabourType item in Items)
            {
                item.AvailabilityLimiter = 1.0;
                CheckAssignLabourAvailability(item);
                if (item.DietaryComponentList != null)
                {
                    item.DietaryComponentList.Clear();
                }
            }

            // A LabourActivityPayHired may take place after this in CLEMStartOfTimeStep to limit availability
        }

        /// <summary>An event handler to update availability for the timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMUpdateLabourAvailability")]
        private void OnCLEMUpdateLabourAvailability(object sender, EventArgs e)
        {
            int currentmonth = Clock.Today.Month;
            foreach (LabourType item in Items)
            {
                // set available days from availabilityitem
                item.SetAvailableDays(currentmonth);
            }
        }

        private void CheckAssignLabourAvailability(LabourType labour)
        {
            if(availabilityList == null)
            {

            }

            List<LabourType> checkList = new List<LabourType>() { labour };
            if (labour.LabourAvailability != null)
            {
                // check labour availability still ok
                if (checkList.Filter(labour.LabourAvailability).Count == 0)
                {
                    labour.LabourAvailability = null;
                }
            }

            // if not assign new value
            if (labour.LabourAvailability == null)
            {
                foreach (Model availItem in availabilityList.Children.Where(a => typeof(LabourSpecificationItem).IsAssignableFrom(a.GetType())).ToList())
                {
                    if (checkList.Filter(availItem).Count > 0)
                    {
                        labour.LabourAvailability = availItem as LabourSpecificationItem;
                        break;
                    }
                }
                // if still null report error
                if (labour.LabourAvailability == null)
                {
                    throw new ApsimXException(this, string.Format("Unable to find labour availability suitable for labour type [f=Name:{0}] [f=Gender:{1}] [f=Age:{2}]\nAdd additional labour availability item to [r={3}] under [r={4}]", labour.Name, labour.Gender, labour.Age, availabilityList.Name, this.Name));
                }
            }
        }

        /// <summary>Age individuals</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void ONCLEMAgeResources(object sender, EventArgs e)
        {
            if(AllowAging)
            {
                foreach (LabourType item in Items)
                {
                    if (!item.Hired)
                    {
                        item.AgeInMonths++;
                    }

                    //Update labour available if needed.
                    CheckAssignLabourAvailability(item);

                }
            }
        }

        /// <summary>
        /// Calculate the AE of an individual based on provided relationship
        /// </summary>
        /// <returns>value</returns>
        public double? CalculateAE(double ageInMonths)
        {
            if (adultEquivalentRelationship != null)
            {
                return adultEquivalentRelationship.SolveY(ageInMonths);
            }
            else
            {
                // no AE relationship provided.
                return null;
            }
        }

        /// <summary>
        /// Calculate the number of Adult Equivalents on the farm
        /// </summary>
        /// <param name="includeHired">Include hired labour in the calculation</param>
        /// <returns></returns>
        public double AdultEquivalents(bool includeHired)
        {
            double ae = 0;
            foreach (LabourType person in Items)
            {
                if (!person.Hired | (includeHired))
                {
                    ae += CalculateAE(person.AgeInMonths)??1;
                }
            }
            return ae;
        }

        /// <summary>
        /// Get value of a specific individual
        /// </summary>
        /// <returns>value</returns>
        public double PayRate(LabourType ind)
        {
            if (PricingAvailable)
            {
                List<LabourType> labourList = new List<LabourType>() { ind };

                // search through RuminantPriceGroups for first match with desired purchase or sale flag
                foreach (LabourPriceGroup item in Apsim.Children(PayList, typeof(LabourPriceGroup)).Cast<LabourPriceGroup>())
                {
                    if (labourList.Filter(item).Count() == 1)
                    {
                        return item.Value;
                    }
                }
                // no price match found.
                string warningString = $"No [Pay] price entry was found for individual [r={ind.Name}] with details [f=age: {ind.Age}] [f=gender: {ind.Gender.ToString()}]";
                if (!WarningsNotFound.Contains(warningString))
                {
                    WarningsNotFound.Add(warningString);
                    Summary.WriteWarning(this, warningString);
                }
            }
            return 0;
        }

        /// <summary>
        /// Get value of a specific individual with special requirements check (e.g. breeding sire or draught purchase)
        /// </summary>
        /// <returns>value</returns>
        public double PayRate(LabourType ind, LabourFilterParameters property, string value)
        {
            double price = 0;
            if (PricingAvailable)
            {
                string criteria = property.ToString().ToUpper() + ":" + value.ToUpper();
                List<LabourType> labourList = new List<LabourType>() { ind };

                //find first pricing entry matching specific criteria
                LabourPriceGroup matchIndividual = null;
                LabourPriceGroup matchCriteria = null;
                foreach (LabourPriceGroup item in Apsim.Children(PayList, typeof(LabourPriceGroup)).Cast<LabourPriceGroup>())
                {
                    if (labourList.Filter(item).Count() == 1 && matchIndividual == null)
                    {
                        matchIndividual = item;
                    }

                    // check that pricing item meets the specified criteria.
                    if (Apsim.Children(item, typeof(LabourFilter)).Cast<LabourFilter>().Where(a => (a.Parameter.ToString().ToUpper() == property.ToString().ToUpper() && a.Value.ToUpper() == value.ToUpper())).Count() > 0)
                    {
                        if (matchCriteria == null)
                        {
                            matchCriteria = item;
                        }
                        else
                        {
                            // multiple price entries were found. using first. value = xxx.
                            if (!WarningsMultipleEntry.Contains(criteria))
                            {
                                WarningsMultipleEntry.Add(criteria);
                                Summary.WriteWarning(this, "Multiple specific pay rate entries were found where [" + property + "]" + (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".") + "\nOnly the first entry will be used. Pay [" + matchCriteria.Value.ToString("#,##0.##") + "].");
                            }
                        }
                    }
                }

                if (matchCriteria == null)
                {
                    // report specific criteria not found in price list
                    string warningString = "No [Pay] rate entry was found meeting the required criteria [" + property + "]" + (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".");

                    if (matchIndividual != null)
                    {
                        // add using the best pricing available for [][] purchases of xx per head
                        warningString += "\nThe best available pay rate [" + matchIndividual.Value.ToString("#,##0.##") + "] will be used.";
                        price = matchIndividual.Value;
                    }
                    else
                    {
                        Summary.WriteWarning(this, "\nNo alternate pay rate for individuals could be found for the individuals. Add a new [r=LabourPriceGroup] entry in the [r=LabourPricing]");
                    }
                    if (!WarningsNotFound.Contains(criteria))
                    {
                        WarningsNotFound.Add(criteria);
                        Summary.WriteWarning(this, warningString);
                    }
                }
                else
                {
                    price = matchCriteria.Value;
                }
            }
            return price;
        }

        /// <summary>
        /// Return the availability for individual in labour list
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetAvailabilityForEntry(int index)
        {
            if (index < Items.Count)
            {
                return Items[index].AvailableDays;
            }
            else
            {
                return 0;
            }
        }

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if(AllowAging)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Individuals age with time";
                html += "</div>";
            }
            html += "\n<div class=\"holderresourcesub\">";
            html += "\n<div class=\"clearfix resourcebannerlight\">Labour types</div>";
            html += "\n<div class=\"resourcecontentlight\">";
            html += "<table><tr><th>Name</th><th>Gender</th><th>Age (yrs)</th><th>Number</th><th>Hired</th></tr>";
            foreach (LabourType labourType in Apsim.Children(this, typeof(LabourType)).Cast<LabourType>().ToList())
            {
                html += "<tr>";
                html += "<td>" + labourType.Name + "</td>";
                html += "<td><span class=\"setvalue\">" + labourType.Gender.ToString() + "</span></td>";
                html += "<td><span class=\"setvalue\">" + labourType.InitialAge.ToString() + "</span></td>";
                html += "<td><span class=\"setvalue\">" + labourType.Individuals.ToString() + "</span></td>";
                html += "<td" + ((labourType.Hired) ? " class=\"fill\"" : "") + "></td>";
                html += "</tr>";
            }
            html += "</table>";
            html += "</div></div>";
            return html;
        }


    }
}
