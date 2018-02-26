using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation
    /// Will convert one resource into another (e.g. $ => labour) 
    /// These re defined under each ResourceType in the Resources section of the UI tree
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IResourceType))]
    [Description("This Transmutation will convert any other resource into the current resource where there is a shortfall. This is placed under any resource type where you need to provide a transmutation. For example to convert Finance Type (money) into a Animal Food Store Type (Lucerne) or effectively purchase fodder when low.")]
    public class Transmutation: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Amount of this resource per unit purchased
        /// </summary>
        [Description("Amount of this resource per unit purchased")]
        [Required, GreaterThanEqualValue(1)]
        public double AmountPerUnitPurchase { get; set; }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Apsim.Children(this, typeof(TransmutationCost)).Count() == 0)
            {
                string[] memberNames = new string[] { "TransmutationCosts" };
                results.Add(new ValidationResult("No costs provided under this transmutation", memberNames));
            }
            return results;
        }
    }

    ///<summary>
    /// Resource transmutation cost item
    /// Determines the amount of resource required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    public class TransmutationCost : CLEMModel, IValidatableObject
    {
        [XmlIgnore]
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Name of resource to use
        /// </summary>
        [Description("Name of Resource to use")]
        [Required]
        public string ResourceName { get; set; }

        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Cost per unit")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            object result = Resources.GetResourceByName(ResourceName);
            if (result == null)
            {
                string[] memberNames = new string[] { "ResourceTypeName" };
                results.Add(new ValidationResult("Could not find resource " + this.ResourceName + " in transmutation cost", memberNames));
            }
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // determine resource type from name
            object result = Resources.GetResourceByName(ResourceName);
            //if(result==null)
            //{
            //    throw new Exception("Could not find resource " + this.ResourceName + " in transmutation " + this.Name);
            //}
            ResourceType = result.GetType();
        }
    }

}
