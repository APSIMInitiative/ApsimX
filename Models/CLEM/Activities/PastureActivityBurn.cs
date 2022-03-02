using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to perform controlled burning of native pastures</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Apply controlled burning to a specified graze food store (i.e. native pasture paddock)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Pasture/BurnPasture.htm")]
    public class PastureActivityBurn: CLEMActivityBase
    {
        private GrazeFoodStoreType pasture;
        private GreenhouseGasesType methaneStore;
        private GreenhouseGasesType n2oStore;

        /// <summary>
        /// Minimum proportion green for fire to carry
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Description("Minimum proportion green for fire to carry")]
        [Required(AllowEmptyStrings = false), Proportion]
        public double MinimumProportionGreen { get; set; }

        /// <summary>
        /// Name of graze food store/paddock to burn
        /// </summary>
        [Description("Name of graze food store/paddock to burn")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        [Required(AllowEmptyStrings = false)]
        public string PaddockName { get; set; }

        /// <summary>
        /// Methane store for emissions
        /// </summary>
        [Description("Greenhouse gas store for methane emissions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named Methane if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store named Methane if present")]
        public string MethaneStoreName { get; set; }

        /// <summary>
        /// Nitrous oxide store for emissions
        /// </summary>
        [Description("Greenhouse gas store for nitrous oxide emissions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named N2O if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store named N2O if present")]
        public string NitrousOxideStoreName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PastureActivityBurn()
        {
            this.SetDefaults();
            TransactionCategory = "Pasture.Burn";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get pasture
            pasture = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, PaddockName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                methaneStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            else
                methaneStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, MethaneStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if (NitrousOxideStoreName is null || NitrousOxideStoreName == "Use store named N2O if present")
                n2oStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "N2O", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            else
                n2oStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, NitrousOxideStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();
            return resourcesNeeded;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            // labour is consumed and shortfall has no impact at present
            // could lead to other paddocks burning in future.

            if(Status != ActivityStatus.Partial)
                Status = ActivityStatus.NotNeeded;

            // proportion green
            double green = pasture.Pools.Where(a => a.Age < 2).Sum(a => a.Amount);
            double total = pasture.Amount;
            if (total>0)
            {
                if(green / total <= MinimumProportionGreen)
                {
                    // TODO add weather to calculate fire intensity
                    // TODO calculate patchiness from intensity
                    // TODO influence trees and weeds

                    // burn
                    // remove biomass
                    pasture.Remove(new ResourceRequest()
                    {
                        ActivityModel = this,
                        Required = total,
                        AllowTransmutation = false,
                        Category = TransactionCategory,
                        ResourceTypeName = PaddockName,
                        Resource = pasture
                    }
                    );

                    // add emissions
                    double burnkg = total * 0.76 * 0.46; // burnkg * burning efficiency * carbon content
                    if (methaneStore != null)
                        //TODO change emissions for green material
                        methaneStore.Add(burnkg * 1.333 * 0.0035, this, PaddockName, TransactionCategory); // * 21; // methane emissions from fire (CO2 eq)

                    if (n2oStore != null)
                        n2oStore.Add(burnkg * 1.571 * 0.0076 * 0.12, this, PaddockName, TransactionCategory); // * 21; // N20 emissions from fire (CO2 eq)

                    // TODO: add fertilisation to pasture for given period.

                    Status = ActivityStatus.Success;
                }
            }
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded;
            double numberUnits;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHa:
                    numberUnits = (pasture.Manager.Area * Resources.FindResource<Land>().UnitsOfAreaToHaConversion) / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, pasture.NameWithParent);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Burn ");

                if (PaddockName == null || PaddockName == "")
                    htmlWriter.Write("<span class=\"errorlink\">[Pasture NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + PaddockName + "</span>");

                htmlWriter.Write("if less than <span class=\"setvalue\">" + (MinimumProportionGreen).ToString("0.#%") + "</span> green.</div>");
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">Methane emissions will be placed in ");
                if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                    htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].Methane</span> if present");
                else
                    htmlWriter.Write($"<span class=\"resourcelink\">{MethaneStoreName}</span>");

                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">Nitrous oxide emissions will be placed in ");
                if (NitrousOxideStoreName is null || NitrousOxideStoreName == "Use store named N2O if present")
                    htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].N2O</span> if present");
                else
                    htmlWriter.Write($"<span class=\"resourcelink\">{NitrousOxideStoreName}</span>");

                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
