using APSIM.Numerics;
using DocumentFormat.OpenXml.Bibliography;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Models.CLEM.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [ValidParent(ParentType = typeof(SpecifyRuminant))]
    [Description("Cohort component for specifying an individual during simulation or initalising the herd at the start")]
    [Version(1, 0, 3, "Includes set previous conception specification")]
    [Version(1, 0, 2, "Includes attribute specification")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohort.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantTypeCohort : CLEMModel, IValidatableObject
    {
        private SetPreviousConception setPreviousConception = null;
        [Link]
        private RuminantHerd ruminantHerd = null;
        [Link]
        private ResourcesHolder resources = null;

        /// <summary>
        /// Associated Ruminant Herd
        /// </summary>
        public RuminantHerd AssociatedHerd { get { return ruminantHerd; } }

        /// <summary>
        /// Sex
        /// </summary>
        [Description("Sex")]
        [Required]
        public Sex Sex { get; set; }

        /// <summary>
        /// Provides the age in a user friendly format of "years (optional), months (optional), days"
        /// </summary>
        [Description("Age")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        public AgeSpecifier AgeDetails { get; set; } = new int[] { 0, 12, 0 };

        /// <summary>
        /// Standard deviation of starting age. Use 0 to use age specified only
        /// </summary>
        [Description("Standard deviation of age (0 Age only)")]
        [Required, GreaterThanEqualValue(0)]
        public double AgeSD { get; set; }

        /// <summary>
        /// Age in days
        /// </summary>
        [JsonIgnore]
        public int Age 
        {
            get
            {
                if ((AgeDetails?.Parts??null) is not null)
                    return AgeDetails.InDays;
                return 0;
            }
        }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number of individuals")]
        [Required, GreaterThanEqualValue(0)]
        [Core.Display(VisibleCallback = "DisplayNumber")]
        public double Number { get; set; } = 1;

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Live weight (kg)")]
        [Units("kg")]
        [Required, GreaterThanEqualValue(0)]
        public double Weight { get; set; }

        /// <summary>
        /// Standard deviation of starting weight. Use 0 to use starting weight only
        /// </summary>
        [Description("Standard deviation of weight (0 weight only)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeightSD { get; set; }

        /// <summary>
        /// Style of calculating the intial fat and protein mass of the indivdiual
        /// </summary>
        [Description("Style of assigning initial fat and protein")]
        [Required, GreaterThanEqualValue(0)]
        public InitialiseFatProteinAssignmentStyle InitialFatProteinStyle { get; set; } = InitialiseFatProteinAssignmentStyle.NotProvided;

        /// <summary>
        /// Values to use in initialising initial fat and protein mass (fat, muscle protein, visceral protein (optional))
        /// </summary>
        [Description("Values for assigning initial fat and protein")]
        public double[] InitialFatProteinValues { get; set; }

        /// <summary>
        /// Is suckling?
        /// </summary>
        [Description("Still suckling")]
        [Required]
        public bool Suckling { get; set; } = false;

        /// <summary>
        /// Breeding sire?
        /// </summary>
        [Description("Breeding sire?")]
        [Required]
        public bool Sire { get; set; } = false;

        /// <summary>
        /// Display number of individuals
        /// </summary>
        public bool DisplayNumber { get { return Parent is RuminantInitialCohorts; } }

        /// <summary>
        /// Define the proportion of fleece to include at creation
        /// </summary>
        [Description("Proportion of size adjusted standard fleece weight present")]
        [Required, Proportion]
        public double ProportionFleecePresent { get; set; }

        /// <summary>
        /// Managed pasture name to move to
        /// </summary>
        [Description("Pasture to place on")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified", typeof(GrazeFoodStore) } })]
        public string ManagedPastureName { get; set; } = "Not specified";

        /// <summary>
        /// Managed pasture to move to
        /// </summary>
        public GrazeFoodStoreType ManagedPasture { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            setPreviousConception = Structure.FindChild<SetPreviousConception>();

            if (ManagedPastureName is not null && ManagedPastureName != "" && ManagedPastureName.StartsWith("Not specified") == false)
            {
                ManagedPasture = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ManagedPastureName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="initialAttributes">The initial attributes found from parent</param>
        /// <param name="date">The date these individuals are created</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(List<ISetAttribute> initialAttributes, DateTime date, RuminantType ruminantType = null)
        {
            List<ISetAttribute> localAttributes = new ();
            // add any whole herd attributes
            if (initialAttributes != null)
                localAttributes.AddRange(initialAttributes);
            // Add any attributes defined at the cohort level
            localAttributes.AddRange(Structure.FindChildren<ISetAttribute>().ToList());

            return CreateIndividuals(Convert.ToInt32(this.Number, CultureInfo.InvariantCulture), localAttributes, date, ruminantType);
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="number">The number of individuals to create</param>
        /// <param name="initialAttributes">The initial attributes found from parent and this cohort</param>
        /// <param name="date">The date these individuals are created</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <param name="getUniqueID">Switch to determine if unique id is assigned. Not needed when added to purchase list</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(int number, List<ISetAttribute> initialAttributes, DateTime date, RuminantType ruminantType = null, bool getUniqueID = true)
        {
            if (number <= 0) 
                return new();

            List<Ruminant> individuals = new();
            initialAttributes ??= new();
            setPreviousConception = Structure.FindChild<SetPreviousConception>();

            RuminantType parent = ruminantType;
            parent ??= Structure.FindParent<RuminantType>(recurse: true);

            // get Ruminant Herd resource for unique ids
            RuminantHerd ruminantHerd = parent.Parent as RuminantHerd; 

            for (int i = 1; i <= number; i++)
            {
                double weight = GetWeightFromNormalDistribution(Weight, WeightSD);
                int age = Convert.ToInt32(Math.Round(GetWeightFromNormalDistribution(Age, AgeSD)));

                int? id = (getUniqueID)? ruminantHerd.NextUniqueID : null;
                Ruminant newIndividual = Ruminant.Create(Sex, date, parent.Parameters, age, weight, id, this, initialAttributes, setPreviousConception);
                // set location if specified by a managed pasture 
                if (ManagedPasture is not null)
                {
                    newIndividual.Location = ManagedPasture.Name;
                }
                else
                {
                    if (this.Parent is RuminantInitialCohorts initCohorts && initCohorts.ManagedPasture is not null)
                    {
                        newIndividual.Location = initCohorts.ManagedPasture.Name;
                    }
                }
                individuals.Add(newIndividual);
            }

            // add any mandatory attributes to the list on the ruminant type
            foreach (var mattrib in initialAttributes.Where(a => a.Mandatory))
                parent.AddMandatoryAttribute(mattrib.AttributeName);

            return individuals;
        }

        private double GetWeightFromNormalDistribution(double mean, double sd)
        {
            if (sd == 0)
                return mean;    
            // if weight is 0 then the normalised weight will be applied in Ruminant constructor.
            double u1 = RandomNumberGenerator.Generator.NextDouble();
            double u2 = RandomNumberGenerator.Generator.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                            Math.Sin(2.0 * Math.PI * u2);
            double result = mean + sd * randStdNormal;
            if (MathUtilities.IsNegative(result))
            {
                string warn = $"A negative initial weight was calculated for [r={NameWithParent}] given mean [{mean}] and sd [{sd}]. Mean weight was used.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                return mean;

            }
            return result;
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Age == 0)
                yield return new ValidationResult($"New born individuals [Age = 0] are not permitted in initial herd for [r={NameWithParent}]", new string[] { "AgeDetails" });

            string[] valueLabels = new string[] { "Fat", "Muscle protein", "Visceral protein" };
            if (ruminantHerd.RuminantGrowActivity.IncludeFatAndProtein == false)
                yield break; 

            if(InitialFatProteinStyle == InitialiseFatProteinAssignmentStyle.NotProvided)
            {
                yield return new ValidationResult($"Initial fat and protein values are required in all [r=RuminantTypeCohort] for the specified ruminant growth model.{Environment.NewLine}Set the Style of assigning initial fat and protein in all [r=Cohorts] of [r=InitialCohortList] and [SpecifyRuminant] components.", new string[] { "InitialFatProteinValues" });
            }

            if (InitialFatProteinValues is null)
            {
                if (InitialFatProteinStyle == InitialiseFatProteinAssignmentStyle.EstimateFromRelativeCondition)
                    yield break;
                yield return new ValidationResult("Initial fat and protein values are required in all [r=RuminantTypeCohort] for the specified ruminant growth model", new string[] { "InitialFatProteinValues" });
                yield break;
            }

            int entries;
            if (ruminantHerd.RuminantGrowActivity.IncludeVisceralProteinMass)
            {
                entries = 3;
            }
            else
            {
                entries = 2;
                if ((InitialFatProteinValues?.Length??0) == 2)
                {
                    valueLabels[1] = "Protein";
                }
            }

            if (InitialFatProteinValues is not null && InitialFatProteinValues.Length < entries)
            {
                yield return new ValidationResult($"Insufficient values provided for initial fat and protein mass. {entries} values are required for specified ruminant growth model", new string[] { "InitialFatProteinValues" });
            }

            // if proportion check all values are 0-1
            if (InitialFatProteinStyle == InitialiseFatProteinAssignmentStyle.ProportionOfEmptyBodyMass)
            {
                for(int i = 0; i < InitialFatProteinValues.Length; i++)
                {
                    if (InitialFatProteinValues[i] < 0 | InitialFatProteinValues[i] > 1)
                    {
                        yield return new ValidationResult($"Value for initial [{valueLabels[i]}] proportion of empty body weight must be between 0 and 1", new string[] { "InitialFatProteinValues" });
                    }
                }
            }

            //// check paddock exists if used.
            //if (ManagedPastureName is not null && ManagedPastureName != "" && ManagedPastureName.StartsWith("Not specified") == false)
            //{
            //    GrazeFoodStoreType grazeFoodStore = FindInScope<GrazeFoodStoreType>(ManagedPastureName);
            //    if (grazeFoodStore == null)
            //        yield return new ValidationResult($"Could not find the GrazeFoodStore (pasture) in which to place new individuals from {this.NameWithParent}", new string[] { "ManagedPastureName" });
            //}

            // ToDo check that fleece prop hasn't been set when no wool growth included.
        }

        #endregion

    }
}



