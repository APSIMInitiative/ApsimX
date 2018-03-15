using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Description("This manages all CLEM resources and activities in the simulation.")]
    [HelpUri("http://www.csiro.au")]
    public class ZoneCLEM: Zone, IValidatableObject
    {
        [Link]
        ISummary Summary = null;
        [Link]
        Clock Clock = null;
        [Link]
        Simulation Simulation = null;

        /// <summary>
        /// Seed for random number generator (0 uses clock)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, GreaterThanEqualValue(0) ]
        [Description("Random number generator seed (0 to use clock)")]
        public int RandomSeed { get; set; }

        private static Random randomGenerator;

        /// <summary>
        /// Access the CLEM random number generator
        /// </summary>
        [XmlIgnore]
        [Description("Random number generator for simulation")]
        public static Random RandomGenerator { get { return randomGenerator; } }

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
        /// Constructor
        /// </summary>
        public ZoneCLEM()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Clock.StartDate.Day != 1)
            {
                string[] memberNames = new string[] { "Clock.StartDate" };
                results.Add(new ValidationResult(String.Format("CLEM must commence on the first day of a month. Invalid start date {0}", Clock.StartDate.ToShortDateString(), memberNames)));
            }
            // check that one resources and on activities are present.
            int HolderCnt = this.Children.Where(a => a.GetType() == typeof(ResourcesHolder)).Count();
            if (HolderCnt == 0)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("CLEM must contain a Resources Holder to manage resources", memberNames));
            }
            if (HolderCnt > 1)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("CLEM must contain only one (1) Resources Holder to manage resources", memberNames));
            }
            HolderCnt = this.Children.Where(a => a.GetType() == typeof(ActivitiesHolder)).Count();
            if (HolderCnt == 0)
            {
                string[] memberNames = new string[] { "CLEM.Activities" };
                results.Add(new ValidationResult("CLEM must contain an Activities Holder to manage activities", memberNames));
            }
            if (HolderCnt > 1)
            {
                string[] memberNames = new string[] { "CLEM.Activities" };
                results.Add(new ValidationResult("CLEM must contain only one (1) Activities Holder to manage activities", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // validation is performed here
            // commencing is too early as Summary has not been created for reporting.
            // some values assigned in commencing will not be checked before processing, but will be caught here
            if (!Validate(Simulation, ""))
            {
                string error = "Invalid parameters in model (see summary for details)";
                throw new ApsimXException(this, error);
            }

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (RandomSeed==0)
            {
                randomGenerator = new Random();
            }
            else
            {
                randomGenerator = new Random(RandomSeed);
            }
            EcologicalIndicatorsCalculationInterval = 12;
        }

        /// <summary>
        /// Internal method to iterate through all children in CLEM and report any parameter setting errors
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ModelPath">Pass blank string. Used for tracking model path</param>
        /// <returns>Boolean indicating whether validation was successful</returns>
        private bool Validate(Model model, string ModelPath)
        {
            ModelPath += "["+model.Name+"]";
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
                    string error = String.Format("Invalid parameter value in model object " + ModelPath + Environment.NewLine + "PARAMETER: " + validateError.MemberNames.FirstOrDefault() + Environment.NewLine + "PROBLEM: " + validateError.ErrorMessage + Environment.NewLine);
                    Summary.WriteWarning(this, error);
                }
            }
            foreach (var child in model.Children)
            {
                bool result = Validate(child, ModelPath);
                if (valid & !result)
                {
                    valid = false;
                }
            }
            return valid;
        }

        /// <summary>
        /// Method to set defaults from   
        /// </summary>
        public void SetDefaults()
        {
            foreach (var property in GetType().GetProperties())
            {
                foreach (Attribute attr in property.GetCustomAttributes(true))
                {
                    if (attr is System.ComponentModel.DefaultValueAttribute)
                    {
                        System.ComponentModel.DefaultValueAttribute dv = (System.ComponentModel.DefaultValueAttribute)attr;
                        try
                        {
                            if (property.PropertyType.IsArray)
                            {
                                property.SetValue(this, dv.Value, null);
                            }
                            else
                            {
                                property.SetValue(this, dv.Value, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            Summary.WriteWarning(this, ex.Message);
                            //eat it... Or maybe Debug.Writeline(ex);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Method to determine if this is the month to calculate ecological indicators
        /// </summary>
        /// <returns></returns>
        public bool IsEcologicalIndicatorsCalculationMonth()
        {
            return this.EcologicalIndicatorsNextDueDate.Year == Clock.Today.Year & this.EcologicalIndicatorsNextDueDate.Month == Clock.Today.Month;
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
