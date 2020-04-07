using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// CLEM Zone to control simulation
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This represents all CLEM farm resources and activities")]
    [HelpUri(@"Content/Features/CLEMComponent.htm")]
    [Version(1, 0, 4, "Random numbers and iteration property moved form this component to a stand-alone component\nChanges will be required to your setup")]
    [Version(1, 0, 3, "Updated filtering logic to improve performance")]
    [Version(1, 0, 2, "New ResourceUnitConverter functionality added that changes some reporting.\nThis change will cause errors for all previous custom resource ledger reports created using the APSIM Report component.\nTo fix errors add \".Name\" to all LastTransaction.ResourceType and LastTransaction.Activity entries in custom ledgers (i.e. LastTransaction.ResourceType.Name as Resource). The CLEM ReportResourceLedger component has been updated to automatically handle the changes")]
    [Version(1,0,1,"")]
    [ScopedModel]
    public class ZoneCLEM: Zone, IValidatableObject, ICLEMUI
    {
        [Link]
        ISummary Summary = null;
        [Link]
        Clock Clock = null;
        [Link]
        Simulation Simulation = null;
        [Link]
        IDataStore DataStore = null;

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        [XmlIgnore]
        public string SelectedTab { get; set; }

        /// <summary>
        /// Multiplier from single farm to regional number of farms for market transactions
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, GreaterThanValue(0)]
        [Description("Farm multiplier to supply and receive from market")]
        public double FarmMultiplier { get; set; }

        /// <summary>
        /// Index of the simulation Climate Region
        /// </summary>
        [Description("Climate region index")]
        public int ClimateRegion { get; set; }

        /// <summary>
        /// Ecological indicators calculation interval (in months, 1 monthly, 12 annual)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(12)]
        [Description("Ecological indicators calculation interval (in months, 1 monthly, 12 annual)")]
        [XmlIgnore]
        public int EcologicalIndicatorsCalculationInterval { get; set; }

        /// <summary>
        /// End of month to calculate ecological indicators
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(7)]
        [Description("End of month to calculate ecological indicators")]
        [Required, Month]
        public int EcologicalIndicatorsCalculationMonth { get; set; }

        /// <summary>
        /// Month this overhead is next due.
        /// </summary>
        [XmlIgnore]
        public DateTime EcologicalIndicatorsNextDueDate { get; set; }

        // ignore zone base class properties

        /// <summary>Area of the zone.</summary>
        /// <value>The area.</value>
        [XmlIgnore]
        public new double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        /// <value>The slope.</value>
        [XmlIgnore]
        public new double Slope { get; set; }

        /// <summary>
        /// not used in CLEM
        /// </summary>
        [XmlIgnore]
        public new double AspectAngle { get; set; }

        /// <summary>Local altitude (meters above sea level).</summary>
        [XmlIgnore]
        public new double Altitude { get; set; } = 50;

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Clock.StartDate.ToShortDateString() == "1/01/0001") 
            {
                string[] memberNames = new string[] { "Clock.StartDate" };
                results.Add(new ValidationResult(String.Format("Invalid start date {0}", Clock.StartDate.ToShortDateString()), memberNames));
            }
            if (Clock.EndDate.ToShortDateString() == "1/01/0001")
            {
                string[] memberNames = new string[] { "Clock.EndDate" };
                results.Add(new ValidationResult(String.Format("Invalid end date {0}", Clock.EndDate.ToShortDateString()), memberNames));
            }
            if (Clock.StartDate.Day != 1)
            {
                string[] memberNames = new string[] { "Clock.StartDate" };
                results.Add(new ValidationResult(String.Format("CLEM must commence on the first day of a month. Invalid start date {0}", Clock.StartDate.ToShortDateString()), memberNames));
            }
            // check that one resources and on activities are present.
            int holderCount = this.Children.Where(a => a.GetType() == typeof(ResourcesHolder)).Count();
            if (holderCount == 0)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("CLEM must contain a Resources Holder to manage resources", memberNames));
            }
            if (holderCount > 1)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("CLEM must contain only one (1) Resources Holder to manage resources", memberNames));
            }
            holderCount = this.Children.Where(a => a.GetType() == typeof(ActivitiesHolder)).Count();
            if (holderCount == 0)
            {
                string[] memberNames = new string[] { "CLEM.Activities" };
                results.Add(new ValidationResult("CLEM must contain an Activities Holder to manage activities", memberNames));
            }
            if (holderCount > 1)
            {
                string[] memberNames = new string[] { "CLEM.Activities" };
                results.Add(new ValidationResult("CLEM must contain only one (1) Activities Holder to manage activities", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMValidate")]
        private void OnCLEMValidate(object sender, EventArgs e)
        {
            // validation is performed here
            // this event fires after Activity and Resource validation so that resources are available to check in the validation.
            // commencing is too early as Summary has not been created for reporting.
            // some values assigned in commencing will not be checked before processing, but will be caught here
            if (!Validate(Simulation, ""))
            {
                string error = "@i:Invalid parameters in model";

                // find IStorageReader of simulation
                IModel parentSimulation = Apsim.Parent(this, typeof(Simulation));
                IStorageReader ds = DataStore.Reader;
                if (ds.GetData(simulationName: parentSimulation.Name, tableName: "_Messages") != null)
                {
                    DataRow[] dataRows = ds.GetData(simulationName: parentSimulation.Name, tableName: "_Messages").Select().OrderBy(a => a[7].ToString()).ToArray();
                    // all all current errors and validation problems to error string.
                    foreach (DataRow dr in dataRows)
                    {
                        error += "\n" + dr[6].ToString();
                    }
                }
                throw new ApsimXException(this, error);
            }

            if (Clock.StartDate.Year > 1) // avoid checking if clock not set.
            {
                if (EcologicalIndicatorsCalculationMonth >= Clock.StartDate.Month)
                {
                    // go back from start month in intervals until
                    DateTime trackDate = new DateTime(Clock.StartDate.Year, EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
                    while (trackDate.AddMonths(-EcologicalIndicatorsCalculationInterval) >= Clock.Today)
                    {
                        trackDate = trackDate.AddMonths(-EcologicalIndicatorsCalculationInterval);
                    }
                    EcologicalIndicatorsNextDueDate = trackDate;
                }
                else
                {
                    EcologicalIndicatorsNextDueDate = new DateTime(Clock.StartDate.Year, EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
                    while (Clock.StartDate > EcologicalIndicatorsNextDueDate)
                    {
                        EcologicalIndicatorsNextDueDate = EcologicalIndicatorsNextDueDate.AddMonths(EcologicalIndicatorsCalculationInterval);
                    }
                }
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            EcologicalIndicatorsCalculationInterval = 12;
        }

        /// <summary>
        /// Internal method to iterate through all children in CLEM and report any parameter setting errors
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelPath">Pass blank string. Used for tracking model path</param>
        /// <returns>Boolean indicating whether validation was successful</returns>
        private bool Validate(Model model, string modelPath)
        {
            string starter = "[";
            if(typeof(IResourceType).IsAssignableFrom(model.GetType()))
            {
                starter = "[r=";
            }
            if(model.GetType() == typeof(ResourcesHolder))
            {
                starter = "[r=";
            }
            if (model.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
            {
                starter = "[r=";
            }
            if (model.GetType() == typeof(ActivitiesHolder))
            {
                starter = "[a=";
            }
            if (model.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
            {
                starter = "[a=";
            }
            if (model.GetType().Name.Contains("Group"))
            {
                starter = "[f=";
            }
            if (model.GetType().Name.Contains("Timer"))
            {
                starter = "[f=";
            }
            if (model.GetType().Name.Contains("Filter"))
            {
                starter = "[f=";
            }

            modelPath += starter+model.Name+"]";
            modelPath = modelPath.Replace("][", "]&shy;[");
            bool valid = true;
            var validationContext = new ValidationContext(model, null, null);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            if (validationResults.Count > 0)
            {
                valid = false;
                // report all errors
                foreach (var validateError in validationResults)
                {
                    // get description
                    string text = "";
                    var property = model.GetType().GetProperty(validateError.MemberNames.FirstOrDefault());
                    if (property != null)
                    {
                        text = "";
                        if (property.GetCustomAttributes(typeof(DescriptionAttribute), true).Count() > 0)
                        {
                            var attribute = property.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                            var description = (DescriptionAttribute)attribute;
                            text = description.ToString();
                        }
                    }
                    string error = String.Format("@validation:Invalid parameter value in " + modelPath + "" + Environment.NewLine + "PARAMETER: " + validateError.MemberNames.FirstOrDefault());
                    if (text != "")
                    {
                        error += String.Format(Environment.NewLine + "DESCRIPTION: " + text );
                    }
                    error += String.Format(Environment.NewLine + "PROBLEM: " + validateError.ErrorMessage + Environment.NewLine);
                    Summary.WriteWarning(this, error);
                }
            }
            foreach (var child in model.Children)
            {
                bool result = Validate(child, modelPath);
                if (valid && !result)
                {
                    valid = false;
                }
            }
            return valid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="useFullDescription">Use full verbose description</param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        public string GetFullSummary(object model, bool useFullDescription, string htmlString)
        {
            string html = "";
            html += "\n<div class=\"holdermain\" style=\"opacity: " + ((!this.Enabled) ? "0.4" : "1") + "\">";

            // get clock
            IModel parentSim = Apsim.Parent(this, typeof(Simulation));

            // find random number generator
            RandomNumberGenerator rnd = Apsim.Children(parentSim, typeof(RandomNumberGenerator)).FirstOrDefault() as RandomNumberGenerator;
            if(rnd != null)
            {
                html += "\n<div class=\"clearfix defaultbanner\">";
                html += "<div class=\"namediv\">" + rnd.Name + "</div>";
                html += "<div class=\"typediv\">RandomNumberGenerator</div>";
                html += "</div>";
                html += "\n<div class=\"defaultcontent\">";
                html += "\n<div class=\"activityentry\">Random numbers are provided for this simultion.<br />";
                if (rnd.Seed == 0)
                {
                    html += "Every run of this simulation will be different.";
                }
                else
                {
                    html += "Each run of this simulation will be identical using the seed <span class=\"setvalue\">" + rnd.Seed.ToString() + "</span>";
                }
                html += "\n</div>";
                html += "\n</div>";
            }

            Clock clk = Apsim.Children(parentSim, typeof(Clock)).FirstOrDefault() as Clock;
            if (clk != null)
            {
                html += "\n<div class=\"clearfix defaultbanner\">";
                html += "<div class=\"namediv\">" + clk.Name + "</div>";
                html += "<div class=\"typediv\">Clock</div>";
                html += "</div>";
                html += "\n<div class=\"defaultcontent\">";
                html += "\n<div class=\"activityentry\">This simulation runs from ";
                if (clk.StartDate == null)
                {
                    html += "<span class=\"errorlink\">[START DATE NOT SET]</span>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + clk.StartDate.ToShortDateString() + "</span>";
                }
                html += " to ";
                if (clk.EndDate == null)
                {
                    html += "<span class=\"errorlink\">[END DATE NOT SET]</span>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + clk.EndDate.ToShortDateString() + "</span>";
                }
                html += "\n</div>";
                html += "\n</div>";
                html += "\n</div>";
            }

            foreach (CLEMModel cm in Apsim.Children(this, typeof(CLEMModel)).Cast<CLEMModel>())
            {
                html += cm.GetFullSummary(cm, true, "");
            }
            return html;
        }

        /// <summary>
        /// Method to determine if this is the month to calculate ecological indicators
        /// </summary>
        /// <returns></returns>
        public bool IsEcologicalIndicatorsCalculationMonth()
        {
            return this.EcologicalIndicatorsNextDueDate.Year == Clock.Today.Year && this.EcologicalIndicatorsNextDueDate.Month == Clock.Today.Month;
        }

        /// <summary>Data stores to clear at start of month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            if(IsEcologicalIndicatorsCalculationMonth())
            {
                this.EcologicalIndicatorsNextDueDate = this.EcologicalIndicatorsNextDueDate.AddMonths(this.EcologicalIndicatorsCalculationInterval);
            }
        }

    }
}
