using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using Models.CLEM.Extras;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// The <c>CropActivityManageCrop</c> component manages a crop or rotation of crops in a specified area of the farm
    /// </summary>
    /// <remarks>
    /// <param>This activity requests and sets aside land for the crop(s)</param> 
    /// <param>This activity manages any rotation of crops in the field</param>
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages a crop(s) by assigning land to be used for child activities.")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Rotational cropping implemented")]
    [HelpUri(@"Content/Features/Activities/Crop/ManageCrop.htm")]
    [ModelAssociations(associatedModels: [typeof(CropActivityManageProduct)],
        associationStyles: [ModelAssociationStyle.Child])]
    public class CropActivityManageCrop: CLEMActivityBase, IValidatableObject, IPastureManager
    {
        private int currentCropIndex = 0;
        private int numberOfCrops = 0;

        /// <summary>
        /// The <c>LandItemNameToUse</c> property specifies the land type where the crop is to be grown.
        /// </summary>
        /// <remarks>
        /// This will be a drop down list of all land types available in the simulation. This is a required field and
        /// must be specified.
        /// </remarks>
        [Description("Land type where crop is to be grown")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(Land) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land resource type required")]
        public string LandItemNameToUse { get; set; }

        /// <summary>
        /// The area of land requested
        /// </summary>
        [Description("Area of crop")]
        [Required, GreaterThanEqualValue(0)]
        public double AreaRequested { get; set; }

        /// <summary>
        /// This flag specifies that unallocated available land should be used for this field
        /// </summary>
        [Description("Use unallocated land")]
        public bool UseAreaAvailable { get; set; }

        /// <summary>
        /// The <c>Area</c> property provides the area of land actually received and available for the crop. This may be
        /// less than the area requested if there is insufficient land available.
        /// </summary>
        [JsonIgnore]
        public double Area { get; set; }

        /// <summary>
        /// The <c>LinkedLandItem</c> property provides a reference to the land item associated with the crop.
        /// </summary>
        [JsonIgnore]
        public LandType LinkedLandItem { get; set; }

        /// <summary>An event handler allowing for any activity initialisation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the <c>CLEMEvents.CLEMInitialiseActivity</c> event
        /// </remarks>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (LandItemNameToUse != null && LandItemNameToUse != "")
            {
                // locate Land Type resource for this forage.
                LinkedLandItem = Resources.FindResourceType<Land, LandType>(this, LandItemNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

                if (UseAreaAvailable)
                {
                    LinkedLandItem.TransactionOccurred += LinkedLandItem_TransactionOccurred;
                }
            }
        }

        /// <summary>
        /// An event handler to perform any final initialisation of the activity
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This method is called during the FinalInitialise event to ensure all resources and the initial
        /// conditions of this activity are set up correctly.
        /// </remarks>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialiseForCrop(object sender, EventArgs e)
        {
            // set and enable first crop in the list for rotational cropping.
            int i = 0;
            foreach (CropActivityManageProduct item in Children.OfType<CropActivityManageProduct>())
            {
                numberOfCrops = i+1;
                item.CurrentlyManaged = (i == currentCropIndex);
                if (item.CurrentlyManaged && LinkedLandItem != null)
                {
                    // get land for this crop (first crop in list)
                    // this may include a multiplier to modify the crop area planted and needed
                    AdjustLand(item);
                }
                i++;
            }

            if (Area == 0 && UseAreaAvailable)
            {
                Summary.WriteMessage(this, $"No area of [r={LinkedLandItem.NameWithParent}] has been assigned for [a={NameWithParent}] at the start of the simulation.\r\nThis is because you have selected to use unallocated land and all land is used by other activities.", MessageType.Warning);
            }
        }

        /// <summary>
        /// The <c>RotateCrop</c> method rotates to the next crop in the list
        /// </summary>
        public void RotateCrop()
        {
            if (numberOfCrops > 1)
            {
                currentCropIndex++;
                if (currentCropIndex >= numberOfCrops)
                {
                    currentCropIndex = 0;
                }

                int i = 0;
                foreach (CropActivityManageProduct item in Structure.FindChildren<CropActivityManageProduct>())
                {
                    item.CurrentlyManaged = (i == currentCropIndex);
                    if (item.CurrentlyManaged)
                    {
                        AdjustLand(item);
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// The <c>AdjustLand</c> method adjusts the area planted if the crop has an area planted multiplier
        /// </summary>
        /// <param name="cropProduct">The crop product details to define final land area</param>
        private void AdjustLand(CropActivityManageProduct cropProduct)
        {
            // is this using available land and not yet assigned, or not using available land
            if (Area == 0 || !UseAreaAvailable)
            {
                // is the requested land different to land currently provided
                double areaNeeded = UseAreaAvailable ? LinkedLandItem.AmountAvailable : (AreaRequested * cropProduct.PlantedMultiplier) - Area;
                if (areaNeeded != 0)
                {
                    if (areaNeeded > 0)
                    {
                        ResourceRequestList = [
                            new () {
                                Resource = LinkedLandItem,
                                AllowTransmutation = false,
                                Required = areaNeeded,
                                ResourceType = typeof(Land),
                                ResourceTypeName = LandItemNameToUse,
                                ActivityModel = this,
                                Category = TransactionCategory,
                                FilterDetails = null,
                                RelatesToResource = cropProduct.LinkedResourceItem.Name
                            }
                        ];

                        if (!UseAreaAvailable & LinkedLandItem != null)
                        {
                            CheckResources(ResourceRequestList, Guid.NewGuid());
                            TakeResources(ResourceRequestList);
                            //Now the Land has been allocated we have an Area
                            //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                            Area += ResourceRequestList.FirstOrDefault().Provided;
                        }
                        else
                        {
                            Area += areaNeeded;
                        }
                        ResourceRequestList = [];
                    }
                    else
                    {
                        // excess land for planting can be returned to land resource
                        // careful that this doesn't get taken by a use all available elsewhere if you want it back again.
                        if (LinkedLandItem != null)
                        {
                            LinkedLandItem.AddToResource(-areaNeeded, this, cropProduct.LinkedResourceItem.Name, TransactionCategory);
                            Area += areaNeeded;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// An event handler to override the base class method to allow for clean up of event
        /// handlers
        /// </summary>
        /// <remarks>
        /// This method is called by Clock.Completed event
        /// </remarks>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (LinkedLandItem != null && UseAreaAvailable)
                LinkedLandItem.TransactionOccurred -= LinkedLandItem_TransactionOccurred;
        }

        /// <summary>
        /// The <c>LinkedLandItem_TransactionOccurred</c> method listens for land use transactions and adjusts the area
        /// of land available for this crop when <c>UseAreaAvailable</c> is set to true. 
        /// </summary>
        private void LinkedLandItem_TransactionOccurred(object sender, EventArgs e)
        {
            Area = LinkedLandItem.AmountAvailable;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NoTask;
            return;
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var cropProductChildren = Children.OfType<CropActivityManageProduct>();
            if (cropProductChildren.GroupBy(a => a.CropName).Select(a => a.Count()).Max() > 1)
            {
                yield return new ValidationResult($"More than one [a=CropActivityManageProduct] with the same [CropName] of were provided. Use rotation croppping, \"HarvestTag\" and different crop names to manage the same crop", ["Multiple crop product activities"]);
            }
        }
        #endregion
    }
}
