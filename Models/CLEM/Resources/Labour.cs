using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Labour Person models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all labour types (people) in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/Labour.htm")]
    public class Labour : ResourceBaseWithTransactions, IValidatableObject, IHandlesActivityCompanionModels
    {
        [Link]
        private IClock clock = null;

        private List<string> warningsMultipleEntry = new List<string>();
        private List<string> warningsNotFound = new List<string>();
        private Relationship adultEquivalentRelationship = null;
        private LabourAvailabilityList availabilityList;

        /// <summary>
        /// Labour types currently available.
        /// </summary>
        [JsonIgnore]
        public List<LabourType> Items { get; set; }

        /// <summary>
        /// Use cohorts for all analysis or use individuals
        /// </summary>
        [Description("Maintain cohorts")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        [Required]
        public bool UseCohorts { get; set; }

        /// <summary>
        /// Allows indiviuals to age each month
        /// </summary>
        [Description("Allow individuals to age")]
        [Required]
        public bool AllowAging { get; set; }

        /// <summary>
        /// Current pay rate value of individuals
        /// </summary>
        [JsonIgnore]
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
            availabilityList = Structure.FindChildren<LabourAvailabilityList>().FirstOrDefault();

            if (clock.Today.Day != 1)
                OnStartOfMonth(this, null);
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
            foreach (LabourType ind in Items.Where(a => includeHiredLabour | (a.IsHired == false)))
                value += ind.GetDietDetails(metric); // / (reportPerAE?ind.TotalAdultEquivalents:1);
            return value / (reportPerAE ? AdultEquivalents(includeHiredLabour) : 1);
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
            int daysInMonth = DateTime.DaysInMonth(clock.Today.Year, clock.Today.Month);
            return GetDietaryValue(metric, includeHiredLabour, reportPerAE) / daysInMonth;
        }

        /// <inheritdoc/>
        public LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "Relationship":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() { "Adult equivalent" },
                        measures: new List<string>()
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        #region validation

        /// <summary>
        /// Validation of this resource
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Add warning if no individuals defined
            if (Structure.FindChildren<LabourType>().Count() > 0 && Structure.FindChildren<LabourType>().Cast<LabourType>().Sum(a => a.Individuals) == 0)
            {
                string warningString = "No individuals have been set in any [r=LabourType]\r\nAdd individuals or consider removing or disabling [r=Labour]";
                if (!warningsNotFound.Contains(warningString))
                {
                    warningsNotFound.Add(warningString);
                    Summary.WriteMessage(this, warningString, MessageType.Warning);
                }
            }
            return results;
        }

        #endregion

        /// <summary>An event handler to allow us to create labour list when simualtion commences</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private new void OnSimulationCommencing(object sender, EventArgs e)
        {
            // locate AE relationship
            adultEquivalentRelationship = Structure.FindChildren<Relationship>().FirstOrDefault(a => a.Identifier == "Adult equivalent");

            Items = new List<LabourType>();
            foreach (LabourType labourChildModel in Structure.FindChildren<LabourType>())
            {
                IndividualAttribute att = new IndividualAttribute() { StoredValue = labourChildModel.Name };
                if (UseCohorts)
                {
                    LabourType labour = new LabourType()
                    {
                        Sex = labourChildModel.Sex,
                        Individuals = labourChildModel.Individuals,
                        Parent = this,
                        InitialAge = labourChildModel.InitialAge,
                        AgeInMonths = labourChildModel.InitialAge * 12,
                        LabourAvailability = labourChildModel.LabourAvailability,
                        Name = labourChildModel.Name,
                        IsHired = labourChildModel.IsHired
                    };
                    labour.SetParentResourceBaseWithTransactions(this);
                    labour.Attributes.Add("Group", att);
                    labour.TransactionOccurred += Resource_TransactionOccurred;
                    Items.Add(labour);
                }
                else
                {
                    for (int i = 0; i < labourChildModel.Individuals; i++)
                    {
                        // get the availability from provided list
                        LabourType labour = new LabourType()
                        {
                            Sex = labourChildModel.Sex,
                            Individuals = 1,
                            Parent = this,
                            InitialAge = labourChildModel.InitialAge,
                            AgeInMonths = labourChildModel.InitialAge * 12,
                            LabourAvailability = labourChildModel.LabourAvailability,
                            Name = labourChildModel.Name + ((labourChildModel.Individuals > 1) ? "_" + (i + 1).ToString() : ""),
                            IsHired = labourChildModel.IsHired
                        };
                        labour.SetParentResourceBaseWithTransactions(this);
                        labour.Attributes.Add("Group", att);
                        labour.TransactionOccurred += Resource_TransactionOccurred;
                        Items.Add(labour);
                    }
                }
            }
            PayList = Structure.FindChild<LabourPricing>();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private new void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (LabourType childModel in Structure.FindChildren<LabourType>())
                childModel.TransactionOccurred -= Resource_TransactionOccurred;

            if (Items != null)
                Items.Clear();

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
                    item.DietaryComponentList.Clear();

                // reset check and take labour trackers for last activity to avoid between month carryover
                for (int i = 0; i < 2; i++)
                {
                    item.LastActivityLabour[i] = 0;
                    item.LastActivityRequestID[i] = new Guid();
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
            int currentmonth = clock.Today.Month;
            foreach (LabourType item in Items)
                // set available days from availabilityitem
                item.SetAvailableDays(currentmonth);
        }

        private void CheckAssignLabourAvailability(LabourType labour)
        {
            List<LabourType> checkList = new List<LabourType>() { labour };
            if (labour.LabourAvailability != null)
            {
                // check labour availability still ok
                if (!(labour.LabourAvailability as IFilterGroup).Filter(checkList).Any())
                    labour.LabourAvailability = null;
            }

            // if not assign new value
            if (labour.LabourAvailability == null)
            {
                foreach (var availItem in Structure.FindChildren<ILabourSpecificationItem>(relativeTo: availabilityList))
                {
                    if (availItem is IFilterGroup group && group.Filter(checkList).Any())
                    {
                        labour.LabourAvailability = availItem;
                        break;
                    }
                }
                // if still null report error
                if (labour.LabourAvailability == null)
                {
                    string msg = $"Unable to find labour availability suitable for labour type" +
                        $" [f=Name:{labour.Name}] [f=Gender:{labour.Sex}] [f=Age:{labour.Age}]" +
                        $"\r\nAdd additional labour availability item to " +
                        $"[r={availabilityList.Name}] under [r={Name}]";

                    throw new ApsimXException(this, msg);
                }
            }
        }

        /// <summary>Age individuals</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void ONCLEMAgeResources(object sender, EventArgs e)
        {
            if (AllowAging)
            {
                foreach (LabourType item in Items)
                {
                    if (!item.IsHired)
                        item.AgeInMonths++;

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
                return adultEquivalentRelationship.SolveY(ageInMonths);
            else
                // no AE relationship provided.
                return null;
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
                if (!person.IsHired | (includeHired))
                    ae += (CalculateAE(person.AgeInMonths) ?? 1) * Convert.ToDouble(person.Individuals, System.Globalization.CultureInfo.InvariantCulture);
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
                // search through RuminantPriceGroups for first match with desired purchase or sale flag
                foreach (LabourPriceGroup item in Structure.FindChildren<LabourPriceGroup>(relativeTo: PayList))
                    if (item.Filter(ind))
                        return item.Value;

                // no price match found.
                string warningString = $"No [Pay] price entry was found for individual [r={ind.Name}] with details [f=age: {ind.Age}] [f=sex: {ind.Sex}]";
                if (!warningsNotFound.Contains(warningString))
                {
                    warningsNotFound.Add(warningString);
                    Summary.WriteMessage(this, warningString, MessageType.Warning);
                }
            }
            return 0;
        }

        /// <summary>
        /// Get value of a specific individual with special requirements check (e.g. breeding sire or draught purchase)
        /// </summary>
        /// <returns>value</returns>
        public double PayRate(LabourType ind, PropertyInfo property, string value)
        {
            double price = 0;
            if (PricingAvailable)
            {
                string criteria = property.Name.ToUpper() + ":" + value.ToUpper();

                //find first pricing entry matching specific criteria
                LabourPriceGroup matchIndividual = null;
                LabourPriceGroup matchCriteria = null;
                foreach (LabourPriceGroup priceGroup in Structure.FindChildren<LabourPriceGroup>(relativeTo: PayList))
                {
                    if (priceGroup.Filter(ind) && matchIndividual == null)
                        matchIndividual = priceGroup;

                    // check that pricing item meets the specified criteria.
                    var items = Structure.FindChildren<FilterByProperty>(relativeTo: priceGroup)
                        .Where(f => priceGroup.GetProperty(f.PropertyOfIndividual) == property)
                        .Where(f => f.Value.ToString().ToUpper() == value.ToUpper());

                    var suitableFilters = Structure.FindChildren<FilterByProperty>(relativeTo: priceGroup)
                        .Where(a => (priceGroup.GetProperty(a.PropertyOfIndividual) == property) &
                        (
                            (a.Operator == System.Linq.Expressions.ExpressionType.Equal && a.Value.ToString().ToUpper() == value.ToUpper()) |
                            (a.Operator == System.Linq.Expressions.ExpressionType.NotEqual && a.Value.ToString().ToUpper() != value.ToUpper()) |
                            (a.Operator == System.Linq.Expressions.ExpressionType.IsTrue && value.ToUpper() == "TRUE") |
                            (a.Operator == System.Linq.Expressions.ExpressionType.IsFalse && value.ToUpper() == "FALSE")
                        )
                        ).Any();

                    if (suitableFilters)
                    {
                        if (matchCriteria == null)
                            matchCriteria = priceGroup;
                        else
                        {
                            // multiple price entries were found. using first. value = xxx.
                            if (!warningsMultipleEntry.Contains(criteria))
                            {
                                warningsMultipleEntry.Add(criteria);
                                Summary.WriteMessage(this, $"Multiple specific pay rate entries were found where [{property}]{(value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".")}\r\nOnly the first entry will be used. Pay [{matchCriteria.Value.ToString("#,##0.##")}].", MessageType.Warning);
                            }
                        }
                    }
                }

                if (matchCriteria == null)
                {
                    // report specific criteria not found in price list
                    string warningString = $"No [Pay] rate entry was found meeting the required criteria [{property.Name}]{(value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".")}";

                    if (matchIndividual != null)
                    {
                        // add using the best pricing available for [][] purchases of xx per head
                        warningString += $"\r\nThe best available pay rate [{matchIndividual.Value:#,##0.##}] will be used.";
                        price = matchIndividual.Value;
                    }
                    else
                        Summary.WriteMessage(this, "\r\nNo alternate pay rate for individuals could be found for the individuals. Add a new [r=LabourPriceGroup] entry in the [r=LabourPricing]", MessageType.Warning);

                    if (!warningsNotFound.Contains(criteria))
                    {
                        warningsNotFound.Add(criteria);
                        Summary.WriteMessage(this, warningString, MessageType.Warning);
                    }
                }
                else
                    price = matchCriteria.Value;
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
                return Items[index].AvailableDays;
            else
                return 0;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (AllowAging)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Individuals age with time");
                    htmlWriter.Write("</div>");
                }
                htmlWriter.Write("\r\n<div class=\"holderresourcesub\">");
                htmlWriter.Write("\r\n<div class=\"clearfix resourcebannerlight\">Labour types</div>");
                htmlWriter.Write("\r\n<div class=\"resourcecontentlight\">");
                htmlWriter.Write("<table><tr><th>Name</th><th>Gender</th><th>Age (yrs)</th><th>Number</th><th>Hired</th></tr>");
                foreach (LabourType labourType in Structure.FindChildren<LabourType>(relativeTo: this))
                {
                    htmlWriter.Write("<tr>");
                    htmlWriter.Write($"<td>{labourType.Name}</td>");
                    htmlWriter.Write($"<td><span class=\"setvalue\">{labourType.Sex}</span></td>");
                    htmlWriter.Write($"<td><span class=\"setvalue\">{labourType.InitialAge}</span></td>");
                    htmlWriter.Write($"<td><span class=\"setvalue\">{labourType.Individuals}</span></td>");
                    htmlWriter.Write("<td" + ((labourType.IsHired) ? " class=\"fill\"" : "") + "></td>");
                    htmlWriter.Write("</tr>");
                }
                htmlWriter.Write("</table>");
                htmlWriter.Write("</div></div>");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
