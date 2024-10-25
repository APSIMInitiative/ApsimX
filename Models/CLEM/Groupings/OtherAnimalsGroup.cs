using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual other animals
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Selects specific individuals from the other animals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/OtherAnimalsGroup.htm")]
    [ValidParent(ParentType = typeof(OtherAnimalsActivitySell))]
    [ValidParent(ParentType = typeof(OtherAnimalsActivityCost))]
    public class OtherAnimalsGroup : FilterGroup<OtherAnimalsTypeCohort>
    {
        /// <summary>
        /// A protected link to the CLEM resource holder
        /// </summary>
        [Link(ByName = true)]
        protected ResourcesHolder Resources = null;

        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Name of other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Other Animal Type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(OtherAnimals) } })]
        public string AnimalTypeName { get; set; }

        /// <summary>
        /// The Other animal type this group points to
        /// </summary>
        public OtherAnimalsType SelectedOtherAnimalsType = null;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            SelectedOtherAnimalsType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, AnimalTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as OtherAnimalsType;
        }

    }
}
