using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals grow activity</summary>
    /// <summary>This activity grows other animals and includes aging</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs the growth and aging of a specified type of other animal")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityGrow.htm")]
    public class OtherAnimalsActivityGrow : CLEMActivityBase
    {
        /// <summary>
        /// Name of Other Animal Type
        /// </summary>
        [Description("Name of Other Animal Type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Other Animal Type to use required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(OtherAnimals) } })]
        public string OtherAnimalType { get; set; }

        private OtherAnimalsType animalType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate OtherAnimalsType resource
            animalType = Resources.FindResourceType<OtherAnimals, OtherAnimalsType>(this, OtherAnimalType, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>
        /// Method to age other animals
        /// This needs to be undertaken prior to herd management
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            // grow all individuals
            foreach (OtherAnimalsTypeCohort cohort in animalType.Cohorts.OfType<OtherAnimalsTypeCohort>())
                cohort.Age++;

            // death from old age
            while(animalType.Cohorts.Where(a => a.Age > animalType.MaxAge).Count() > 0)
                animalType.Remove(animalType.Cohorts.Where(a => a.Age > animalType.MaxAge).FirstOrDefault(), this, "Died");

        }

    }
}
