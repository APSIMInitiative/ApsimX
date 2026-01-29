using DocumentFormat.OpenXml.Drawing;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant graze activity</summary>
    /// <summary>This activity determines how a ruminant group will graze</summary>
    /// <summary>It is designed to request food via a food store arbitrator</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform grazing of all herds and pastures (paddocks)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    public class RuminantActivityGrazeAll : CLEMRuminantActivityBase, IValidatableObject
    {
        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; } = 8;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            bool buildTransactionFromTree = Structure.FindParent<ZoneCLEM>(recurse: true).BuildTransactionCategoryFromTree;
            string transCat = "";
            if (!buildTransactionFromTree)
            {
                transCat = TransactionCategory;
            }

            GrazeFoodStore grazeFoodStore = Resources.FindResourceGroup<GrazeFoodStore>();
            if (grazeFoodStore is null)
            {
                Summary.WriteMessage(this, $"No GrazeFoodStore is available for the ruminant grazing activity [a={Name}]!", MessageType.Warning);
                return;
            }

            InitialiseHerd(true, true);
            
            // create activity for each pasture type (not common land) and breed at startup
            Guid nextUID = ActivitiesHolder.AddToGuID(this.UniqueID, 1);
            foreach (GrazeFoodStoreType pastureType in grazeFoodStore.Children.Where(a => a.GetType() == typeof(GrazeFoodStoreType) || a.GetType() == typeof(CommonLandFoodStoreType)))
            {
                var newGrazePasture = new RuminantActivityGrazePasture(this, pastureType, transCat, nextUID);
                Core.ApsimFile.Structure.Add(newGrazePasture, this);
                var events = new Events(newGrazePasture);
                // Publish Commencing event
                events.PublishToModelAndChildren("CLEMInitialiseActivity", new object[] { Parent, new EventArgs() });

                nextUID = ActivitiesHolder.AddToGuID(nextUID, 2);
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (Status != ActivityStatus.Partial && Status != ActivityStatus.Critical)
            {
                Status = ActivityStatus.NoTask;
            }

            return;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // single grazeall
            if(Structure.FindChildren<RuminantActivityGrazeAll>(relativeTo: ActivitiesHolder, recurse: true).Count() > 1)
            {
                yield return new ValidationResult($"Only one [a=RuminantActivityGrazeAll] is permitted per [CLEM] component{Environment.NewLine}The GrazeAll activity will manage all possible grazing on the farm", new string[] { "Ruminant graze all activity" });
            }
        }
        #endregion
    }
}
