using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Manger for all resources available to the model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [Description("This holds all resource groups used in the CLEM simulation")]
    public class ResourcesHolder: CLEMModel, IValidatableObject
    {

        // Scoping rules of Linking in Apsim means that you can only link to 
        // Models beneath or above or siblings of the ones above.
        // Can not link to children of siblings that are above.
        // Because we have chosen to put Resources and Activities as siblings.
        // Activities are not going to be able to directly link to Resources.
        // They will only be able to link to the very top "Resources".
        // So Activities will have to link to that very top Resources
        // Then you have to go down from there.
         
        // Also we have to use a list. Can't use [Soil].SoilWater method because  
        // you don't have to have every single Resource Group added every single
        // simluation. You only add the Resource Groups that you are going to use
        // in this simlulation and you do this by dragging and dropping them in
        // as child nodes. So first thing you need to do when the simulation starts
        // is figure out which ones have been dragged into this specific simulation.
        // Hence we need to use this list approach.

        /// <summary>
        /// List of the all the Resource Groups.
        /// </summary>
        [XmlIgnore]
        private List<IModel> ResourceTypeList;

        [Link]
        ISummary Summary = null;

        private IModel GetByName(string Name)
        {
            return ResourceTypeList.Find(x => x.Name == Name);
        }

        private IModel GetByType(Type type)
        {
            return ResourceTypeList.Find(x => x.GetType() == type);
        }

        /// <summary>
        /// Get resource by name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public object GetResourceByName(string Name)
        {
            return ResourceTypeList.Find(x => x.Name == Name);
        }

        /// <summary>
        /// Retrieve a ResourceType from a ResourceGroup based on a request item including filter and sort options
        /// </summary>
        /// <param name="Request">A resource request item</param>
        /// <param name="MissingResourceAction">Action to take if requested resource group not found</param>
        /// <param name="MissingResourceTypeAction">Action to take if requested resource type not found</param>
        /// <returns>A reference to the item of type Model</returns>
        public Model GetResourceItem(ResourceRequest Request, OnMissingResourceActionTypes MissingResourceAction, OnMissingResourceActionTypes MissingResourceTypeAction)
        {
            if (Request.FilterDetails != null)
            {
                if (Request.ResourceType == null)
                {
                    string errorMsg = String.Format("Resource type must be supplied in resource request from {0}", Request.ActivityModel.Name);
                    Summary.WriteWarning(Request.ActivityModel, String.Format("Resource type must be supplied in resource request from {0}",Request.ActivityModel.Name));
                    throw new Exception(errorMsg);
                }

                IModel resourceGroup = this.GetByType(Request.ResourceType);
                if(resourceGroup== null)
                {
                    string errorMsg = String.Format("Unable to locate resources of type ({0}) for ({1})", Request.ResourceType, Request.ActivityModel.Name);
                    switch (MissingResourceAction)
                    {
                        case OnMissingResourceActionTypes.ReportErrorAndStop:
                            throw new Exception(errorMsg);
                        case OnMissingResourceActionTypes.ReportWarning:
                            Summary.WriteWarning(Request.ActivityModel, errorMsg);
                            break;
                        default:
                            break;
                    }
                    return null;
                }

                // get list of children matching the conditions in filter
                // and return the lowest item that has enough time available
                object resourceGroupObject = resourceGroup as object;
                switch (resourceGroupObject.GetType().ToString())
                {
                    case "Models.CLEM.Resources.Labour":
                        // get matching labour types
                        // use activity uid to ensure unique for this request
                        List<LabourType> items = (resourceGroup as Labour).Items;
                        items = items.Filter(Request.FilterDetails.FirstOrDefault() as Model);
                        items = items.Where(a => a.LastActivityRequestID != Request.ActivityID).ToList();
                        if (items.Where(a => a.Amount >= Request.Required).Count()>0)
                        {
                            // get labour least available but with the amount needed
                            return items.Where(a => a.Amount >= Request.Required).OrderByDescending(a => a.Amount).FirstOrDefault();
                        }
                        else
                        {
                            // get labour with most available but with less than the amount needed
                            return items.OrderByDescending(a => a.Amount).FirstOrDefault();
                        }
                    default:
                        string errorMsg = "Resource cannot be filtered. Filtering not implemented for " + resourceGroupObject.GetType().ToString() + " from activity (" + Request.ActivityModel.Name + ")";
                        Summary.WriteWarning(Request.ActivityModel, errorMsg);
                        throw new Exception(errorMsg);
                }
            }
            else
            {
                return GetResourceItem(Request.ActivityModel, Request.ResourceType, Request.ResourceTypeName, MissingResourceAction, MissingResourceTypeAction);
            }
        }

        /// <summary>
        /// Retrieve a ResourceType from a ResourceGroup with specified names
        /// </summary>
        /// <param name="RequestingModel">name of model requesting resource</param>
        /// <param name="ResourceType">Type of the resource group</param>
        /// <param name="ResourceTypeName">Name of the resource item</param>
        /// <param name="MissingResourceAction">Action to take if requested resource group not found</param>
        /// <param name="MissingResourceTypeAction">Action to take if requested resource type not found</param>
        /// <returns>A reference to the item of type object</returns>
        public Model GetResourceItem(Model RequestingModel, Type ResourceType, string ResourceTypeName, OnMissingResourceActionTypes MissingResourceAction, OnMissingResourceActionTypes MissingResourceTypeAction)
        {
            // locate specified resource
            Model resourceGroup = Apsim.Children(this, ResourceType).FirstOrDefault() as Model;
            if (resourceGroup != null)
            {
                Model resource = resourceGroup.Children.Where(a => a.Name == ResourceTypeName).FirstOrDefault();
                if (resource == null)
                {
                    string errorMsg = String.Format("Unable to locate resources type ({0}) in resources ({1}) for ({2})", ResourceTypeName, ResourceType.ToString(), RequestingModel.Name);
                    switch (MissingResourceTypeAction)
                    {
                        case OnMissingResourceActionTypes.ReportErrorAndStop:
                            throw new Exception(errorMsg);
                        case OnMissingResourceActionTypes.ReportWarning:
                            Summary.WriteWarning(RequestingModel, errorMsg);
                            break;
                        default:
                            break;
                    }
                    return null;
                }
                return resource;
            }
            else
            {
                string errorMsg = String.Format("Unable to locate resources of type ({0}) for ({1})", ResourceType.ToString(), RequestingModel.Name);
                switch (MissingResourceAction)
                {
                    case OnMissingResourceActionTypes.ReportErrorAndStop:
                        throw new Exception(errorMsg);
                    case OnMissingResourceActionTypes.ReportWarning:
                        Summary.WriteWarning(RequestingModel, errorMsg);
                        break;
                    default:
                        break;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the Resource Group for Products
        /// </summary>
        /// <returns></returns>
        public ProductStore Products()
        {
            return GetByType(typeof(ProductStore)) as ProductStore;
        }

        /// <summary>
        /// Get the Resource Group for Animal Feed
        /// </summary>
        /// <returns></returns>
        public AnimalFoodStore AnimalFoodStore()
        {
            return GetByType(typeof(AnimalFoodStore)) as AnimalFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for OtherAnimals
        /// </summary>
        /// <returns></returns>
        public OtherAnimals OtherAnimalsStore()
        {
            return GetByType(typeof(OtherAnimals)) as OtherAnimals;
        }

        /// <summary>
        /// Get the Resource Group for FoodStore
        /// </summary>
        /// <returns></returns>
        public HumanFoodStore HumanFoodStore()
        {
            return GetByType(typeof(HumanFoodStore)) as HumanFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for GreenhouseGases
        /// </summary>
        /// <returns></returns>
        public GreenhouseGases GreenhouseGases()
        {
            return GetByType(typeof(GreenhouseGases)) as GreenhouseGases;
        }

        /// <summary>
        /// Get the Resource Group for Labour Family
        /// </summary>
        /// <returns></returns>
        public Labour LabourFamily()
        {
            return GetByType(typeof(Labour)) as Labour;
        }

        /// <summary>
        /// Get the Resource Group for Land
        /// </summary>
        /// <returns></returns>
        public Land Land()
        {
            return GetByType(typeof(Land)) as Land;
        }

        /// <summary>
        /// Get the Resource Group for the GrazeFoodStore
        /// </summary>
        /// <returns></returns>
        public GrazeFoodStore GrazeFoodStore()
        {
            return GetByType(typeof(GrazeFoodStore)) as GrazeFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for Ruminant Herd
        /// </summary>
        /// <returns></returns>
        public RuminantHerd RuminantHerd()
        {
            return GetByType(typeof(RuminantHerd)) as RuminantHerd;
        }

        /// <summary>
        /// Get the Resource Group for Finances
        /// </summary>
        /// <returns></returns>
        public Finance FinanceResource()
        {
            return GetByType(typeof(Finance)) as Finance;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResourceTypeList = Apsim.Children(this, typeof(IModel));
        }

        /// <summary>
        /// Performs the transmutation of resources into a required resource
        /// </summary>
        public void TransmutateShortfall(List<ResourceRequest> requests, bool QueryOnly)
        {
            List<ResourceRequest> shortfallRequests = requests.Where(a => a.Required > a.Available).ToList();

            // Search through all limited resources and determine if transmutation available
            foreach (ResourceRequest request in shortfallRequests)
            {
                // Check if transmutation would be successful 
                if (request.AllowTransmutation & (QueryOnly || request.TransmutationPossible))
                {
                    // get resource type
                    IModel model = this.GetResourceItem(request.ActivityModel, request.ResourceType, request.ResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as IModel;
                    if (model != null)
                    {
                        // check if transmutations provided
                        foreach (Transmutation trans in Apsim.Children(model, typeof(Transmutation)))
                        {
                            // check if resources available for activity and transmutation
                            double unitsNeeded = Math.Ceiling((request.Required - request.Available) / trans.AmountPerUnitPurchase);
                            foreach (TransmutationCost transcost in Apsim.Children(trans, typeof(TransmutationCost)))
                            {
                                double transmutationCost = unitsNeeded * transcost.CostPerUnit;

                                // get transcost resource
                                IResourceType transResource = null;
                                if (transcost.ResourceName == "Labour")
                                {
                                    // get by labour group filter under the transmutation cost
                                    ResourceRequest labourRequest = new ResourceRequest();
                                    labourRequest.ActivityModel = request.ActivityModel;
                                    labourRequest.ResourceType = typeof(Labour);
                                    labourRequest.FilterDetails = Apsim.Children(transcost, typeof(LabourFilterGroup)).ToList<object>();
                                    transResource = this.GetResourceItem(labourRequest, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as IResourceType;

                                    // TODO: put group name in the transcost resource type name
                                    // this still needs to be checked
                                    transcost.ResourceTypeName = (transResource as LabourType).Name;
                                }
                                else
                                {
                                    transResource = this.GetResourceItem(request.ActivityModel, transcost.ResourceType, transcost.ResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as IResourceType;
                                }

                                if (!QueryOnly)
                                {
                                    //remove cost
                                    // create new request for this transmutation cost
                                    ResourceRequest transRequest = new ResourceRequest();
                                    transRequest.Reason = trans.Name + " " + trans.Parent.Name;
                                    transRequest.Required = transmutationCost;
                                    transRequest.ResourceType = transcost.ResourceType;
                                    transRequest.ActivityModel = request.ActivityModel;

                                    // used to pass request, but this is not the transmutation cost
                                    transResource.Remove(transRequest);
                                }
                                else
                                {
                                    double activityCost = requests.Where(a => a.ResourceType == transcost.ResourceType & a.ResourceTypeName == transcost.ResourceTypeName).Sum(a => a.Required);
                                    if (transmutationCost + activityCost <= transResource.Amount)
                                    {
                                        request.TransmutationPossible = true;
                                        break;
                                    }
                                }
                            }
                            if(!QueryOnly)
                            {
                                // Add resource
                                (model as IResourceType).Add(unitsNeeded * trans.AmountPerUnitPurchase, trans.Name, "");
                            }
                        }
                    }

                }

            }
        }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            var t = this.Children.GroupBy(a => a.GetType());

            // check that only one instance of each resource group is present
            foreach (var item in this.Children.GroupBy(a => a.GetType()).Where(b => b.Count() > 1))
            {
                string[] memberNames = new string[] { item.Key.FullName };
                results.Add(new ValidationResult(String.Format("Only one (1) instance of any resource group is allowed in the Resources Holder. Multiple Resource Groups [{0}] found!", item.Key.FullName), memberNames));
            }
            return results;
        }
    }
}
