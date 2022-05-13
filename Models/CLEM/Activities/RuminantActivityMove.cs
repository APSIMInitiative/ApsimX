using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant moving activity</summary>
    /// <summary>This activity moves specified ruminants to a given pasture</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Define the location (pastures) of specified individuals (move) and assign location at the start of the simulation")]
    [Version(1, 0, 2, "Now allows multiple RuminantFilterGroups to identify individuals to be moved")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMove.htm")]
    public class RuminantActivityMove: CLEMRuminantActivityBase, IValidatableObject
    {
        private string pastureName = "";

        /// <summary>
        /// Managed pasture to move to
        /// </summary>
        [Description("Managed pasture to move to")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string ManagedPastureName { get; set; }

        /// <summary>
        /// Determines whether this must be performed to setup herds at the start of the simulation
        /// </summary>
        [Description("Move at start of simulation")]
        [Required]
        public bool PerformAtStartOfSimulation { get; set; }

        /// <summary>
        /// Determines whether sucklings are automatically moved with the mother or separated
        /// </summary>
        [Description("Move sucklings with mother")]
        [Required]
        public bool MoveSucklings { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityMove()
        {
            TransactionCategory = "Livestock.Manage";
        }

        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!FindAllChildren<RuminantGroup>().Any())
            {
                string[] memberNames = new string[] { "Specify individuals" };
                results.Add(new ValidationResult($"No individuals have been specified by [f=RuminantGroup] to be moved in [a={Name}]. Provide at least an empty RuminantGroup to move all individuals.", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // link to graze food store type pasture to move to
            // "Not specified" is general yards.
            pastureName = "";
            if (!ManagedPastureName.StartsWith("Not specified"))
                pastureName = ManagedPastureName.Split('.')[1];

            if (PerformAtStartOfSimulation)
                Move();
        }

        private void Move()
        {
            Status = ActivityStatus.NotNeeded;
            // allow multiple filter groups for moving. 
            var filterGroups = FindAllChildren<RuminantGroup>();
            foreach (RuminantGroup item in filterGroups)
            {
                foreach (Ruminant ind in item.Filter(CurrentHerd(false)))
                {
                    // set new location ID
                    if (ind.Location != pastureName)
                    {
                        this.Status = ActivityStatus.Success;
                        ind.Location = pastureName;

                        // check if sucklings are to be moved with mother
                        if (MoveSucklings)
                        {
                            // if female
                            if (ind is RuminantFemale)
                            {
                                RuminantFemale female = ind as RuminantFemale;
                                // check if mother with sucklings
                                foreach (var suckling in female.SucklingOffspringList)
                                    suckling.Location = pastureName;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            double numberUnits = 0;
            IEnumerable<Ruminant> herd = this.CurrentHerd(false);
            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);
            if (herd.Any())
            {
                switch (requirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        numberUnits = head / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perAE:
                        numberUnits = adultEquivalents / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
                } 
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            // check if labour provided or PartialResources allowed
            if (this.TimingOK)
            {
                if ((this.Status == ActivityStatus.Success || this.Status == ActivityStatus.NotNeeded) || (this.Status == ActivityStatus.Partial && this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable))
                    Move();
            }
            else
                Status = ActivityStatus.Ignored;
        }


        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Move the following groups to ");
                if (ManagedPastureName == null || ManagedPastureName == "")
                    htmlWriter.Write("<span class=\"errorlink\">General yards</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + ManagedPastureName + "</span>");

                if (MoveSucklings)
                    htmlWriter.Write(" moving sucklings with mother");

                htmlWriter.Write("</div>");
                if (PerformAtStartOfSimulation)
                    htmlWriter.Write("\r\n<div class=\"activityentry\">These individuals will be located on the specified pasture at startup</div>");
                if(!FindAllChildren<RuminantGroup>().Where(a => a.Enabled).Any())
                    htmlWriter.Write("\r\n<div class=\"warningbanner\">WARNING: No Rumiant Group has been supplied below. No individuals will be moved!</div>");
                return htmlWriter.ToString(); 
            }
        } 
    }
}
