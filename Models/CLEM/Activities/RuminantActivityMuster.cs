using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant muster activity</summary>
    /// <summary>This activity moves specified ruminants to a given pasture</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs mustering based upon the current herd filtering. It is also used to assign individuals to pastures (paddocks) at the start of the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMustering.htm")]
    public class RuminantActivityMuster: CLEMRuminantActivityBase
    {
        /// <summary>
        /// Managed pasture to muster to
        /// </summary>
        [Description("Managed pasture to muster to")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string ManagedPastureName { get; set; }

        private string pastureName = "";

        /// <summary>
        /// Determines whether this must be performed to setup herds at the start of the simulation
        /// </summary>
        [Description("Perform muster at start of simulation")]
        [Required]
        public bool PerformAtStartOfSimulation { get; set; }

        /// <summary>
        /// Determines whether sucklings are automatically mustered with the mother or separated
        /// </summary>
        [Description("Move sucklings with mother")]
        [Required]
        public bool MoveSucklings { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // link to graze food store type pasture to muster to
            // "Not specified" is general yards.
            pastureName = "";
            if (!ManagedPastureName.StartsWith("Not specified"))
            {
                pastureName = ManagedPastureName.Split('.')[1];
            }

            if (PerformAtStartOfSimulation)
            {
                Muster();
            }
        }

        private void Muster()
        {
            Status = ActivityStatus.NotNeeded;
            foreach (Ruminant ind in this.CurrentHerd(false))
            {
                // set new location ID
                if(ind.Location != pastureName)
                {
                    this.Status = ActivityStatus.Success;
                }

                ind.Location = pastureName;

                // check if sucklings are to be moved with mother
                if (MoveSucklings)
                {
                    // if female
                    if (ind.GetType() == typeof(RuminantFemale))
                    {
                        RuminantFemale female = ind as RuminantFemale;
                        // check if mother with sucklings
                        if (female.SucklingOffspringList.Count > 0)
                        {
                            foreach (var suckling in female.SucklingOffspringList)
                            {
                                suckling.Location = pastureName;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            double numberUnits = 0;
            List<Ruminant> herd = this.CurrentHerd(false);
            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // check if labour provided or PartialResources allowed
            if (this.TimingOK)
            {
                if ((this.Status == ActivityStatus.Success || this.Status == ActivityStatus.NotNeeded) || (this.Status == ActivityStatus.Partial && this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable))
                {
                    Muster();
                }
            }
            else
            {
                Status = ActivityStatus.Ignored;
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Muster to ";
            if (ManagedPastureName == null || ManagedPastureName == "")
            {
                html += "<span class=\"errorlink\">General yards</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ManagedPastureName + "</span>";
            }
            if(MoveSucklings)
            {
                html += " moving sucklings with mother";
            }
            html += "</div>";
            if(PerformAtStartOfSimulation)
            {
                html += "\n<div class=\"activityentry\">This muster is performed at startup to ensure animals are on pasture</div>";
            }
            return html;
        }


    }
}
