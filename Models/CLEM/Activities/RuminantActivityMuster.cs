using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

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
    public class RuminantActivityMuster: CLEMRuminantActivityBase
    {
        /// <summary>
        /// Name of managed pasture to muster to
        /// </summary>
        [Description("Name of managed pasture to muster to")]
        [Required]
        public string ManagedPastureName { get; set; }

        /// <summary>
        /// Determines whether this must be performed to setup herds at the start of the simulation
        /// </summary>
        [Description("Perform muster at start of simulation")]
        [Required]
        public bool PerformAtStartOfSimulation { get; set; }

        /// <summary>
        /// Determines whether sucklings are automatically mustered with the mother or seperated
        /// </summary>
        [Description("Move sucklings with mother")]
        [Required]
        public bool MoveSucklings { get; set; }

        private GrazeFoodStoreType Pasture { get; set; }
        private List<LabourFilterGroupSpecified> labour { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // link to graze food store type pasture to muster to
            // blank is general yards.
            if (ManagedPastureName != "")
            {
                Pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStore), ManagedPastureName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();

            if (PerformAtStartOfSimulation)
            {
                Muster();
            }
        }

        private void Muster()
        {
            foreach (Ruminant ind in this.CurrentHerd(false))
            {
                // set new location ID
                ind.Location = Pasture.Name;

                this.SetStatusSuccess();

                // check if sucklings are to be moved with mother
                if (MoveSucklings)
                {
                    // if female
                    if (ind.GetType() == typeof(RuminantFemale))
                    {
                        RuminantFemale female = ind as RuminantFemale;
                        // check if mother with sucklings
                        if (female.SucklingOffspring.Count > 0)
                        {
                            foreach (var suckling in female.SucklingOffspring)
                            {
                                suckling.Location = Pasture.Name;
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
            ResourceRequestList = null;
            if (this.TimingOK)
            {
                List<Ruminant> herd = this.CurrentHerd(false);
                int head = herd.Count();
                double AE = herd.Sum(a => a.AdultEquivalent);

                if (head == 0) return null;

                // for each labour item specified
                foreach (var item in labour)
                {
                    double daysNeeded = 0;
                    switch (item.UnitType)
                    {
                        case LabourUnitType.Fixed:
                            daysNeeded = item.LabourPerUnit;
                            break;
                        case LabourUnitType.perHead:
                            daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
                            break;
                        case LabourUnitType.perAE:
                            daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
                            break;
                        default:
                            throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                    }
                    if (daysNeeded > 0)
                    {
                        if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                        ResourceRequestList.Add(new ResourceRequest()
                        {
                            AllowTransmutation = false,
                            Required = daysNeeded,
                            ResourceType = typeof(Labour),
                            ResourceTypeName = "",
                            ActivityModel = this,
                            FilterDetails = new List<object>() { item }
                        }
                        );
                    }
                }
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // check if labour provided or PartialResources allowed

            if (this.TimingOK)
            {
                if ((this.Status == ActivityStatus.Success | this.Status == ActivityStatus.NotNeeded) || (this.Status == ActivityStatus.Partial && this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable))
                {
                    // move individuals
                    Muster();
                }
                //TriggerOnActivityPerformed();
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

    }
}
