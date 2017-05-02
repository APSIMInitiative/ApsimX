using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Groupings;

namespace Models.WholeFarm.Resources
{

    ///<summary>
    /// Manger for all resources available to the model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(WholeFarm))]
    public class ResourcesHolder: WFModel
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
		/// Retrieve a ResourceType from a ResourceGroup based on a request item including filter and sort options
		/// </summary>
		/// <param name="Request">A resource request item</param>
		/// <param name="ResourceAvailable">Determines whether the resource was found successfully. An empty return is therefore a real lack of resource such as no labour found</param>
		/// <returns>A reference to the item of type Model</returns>
		public Model GetResourceItem(ResourceRequest Request, out bool ResourceAvailable)
		{
			ResourceAvailable = false;
			if (Request.FilterDetails != null)
			{
				if (Request.ResourceName == null)
				{
					Summary.WriteWarning(this, String.Format("ResourceGroup name must be supplied in resource request from {0}",Request.ActivityName));
					return null;
				}

				IModel resourceGroup = this.GetByName(Request.ResourceName);
				if(resourceGroup== null)
				{
					return null;
				}

				// get list of children matching the conditions in filter
				// and return the lowest item that has enough time available
				ResourceAvailable = true;
				object resourceGroupObject = resourceGroup as object;
				switch (resourceGroupObject.GetType().ToString())
				{
					case "Models.WholeFarm.Resources.Labour":
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
						string errorMsg = "Resource cannot be filtered. Filtering not implemented for " + resourceGroupObject.GetType().ToString() + " from activity (" + Request.ActivityName + ")";
						Summary.WriteWarning(this, errorMsg);
						throw new Exception(errorMsg);
				}
			}
			else
			{
				return GetResourceItem(Request.ResourceName, Request.ResourceTypeName, out ResourceAvailable);
			}
		}

		/// <summary>
		/// Retrieve a ResourceType from a ResourceGroup with specified names
		/// </summary>
		/// <param name="ResourceGroupName">Name of the resource group</param>
		/// <param name="ResourceTypeName">Name of the resource item</param>
		/// <param name="ResourceAvailable">Determines whether the resource was found successfully. An empty return is therefore a real lack of resource such as no labour found</param>
		/// <returns>A reference to the item of type object</returns>
		public Model GetResourceItem(string ResourceGroupName, string ResourceTypeName, out bool ResourceAvailable)
		{
			ResourceAvailable = false;
			// locate specified resource
			Model resourceGroup = this.Children.Where(a => a.Name == ResourceGroupName).FirstOrDefault();
			if (resourceGroup != null)
			{
				Model resource = resourceGroup.Children.Where(a => a.Name == ResourceTypeName).FirstOrDefault();
				if (resource == null)
				{
					return null;
				}
				ResourceAvailable = true;
				return resource;
			}
			else
			{
				return null;
			}


			// Old method with error reporting removed to allow setup without resources.
			
			//ResourceAvailable = false;
			//if(ResourceGroupName==null)
			//{
			//	Summary.WriteWarning(this, "ResourceGroup name must be supplied");
			//	throw new Exception("Resource not specified!");
			//}
			//if (ResourceTypeName == null)
			//{
			//	Summary.WriteWarning(this, "ResourceType name must be supplied");
			//	throw new Exception("Resource group not specified!");
			//}

			//// locate specified resource
			//Model resourceGroup = this.Children.Where(a => a.Name == ResourceGroupName).FirstOrDefault();
			//if (resourceGroup != null)
			//{
			//	Model resource = resourceGroup.Children.Where(a => a.Name == ResourceTypeName).FirstOrDefault();
			//	if (resource == null)
			//	{
			//		Summary.WriteWarning(this, String.Format("Resource of name {0} not found in {1}", ((ResourceTypeName.Length == 0) ? "[Blank]" : ResourceTypeName), ResourceGroupName));
			//		throw new Exception("Resource not found!");
			//	}
			//	ResourceAvailable = true;
			//	return resource;
			//}
			//else
			//{
			//	Summary.WriteWarning(this, String.Format("No resource group named {0} found in Resources!", ((ResourceGroupName.Length == 0) ? "[Blank]" : ResourceGroupName)));
			//	throw new Exception("Resource group not found!");
			//}
		}

		/// <summary>
		/// Get the Resource Group for Fodder
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
					bool resourceAvailable = false;
					IModel model = this.GetResourceItem(request.ResourceName, request.ResourceTypeName, out resourceAvailable) as IModel;
					if (model != null)
					{
						// check if transmutations provided
						foreach (Transmutation trans in model.Children.Where(a => a.GetType() == typeof(Transmutation)))
						{
							// check if resources available for activity and transmutation
							double unitsNeeded = Math.Ceiling((request.Required - request.Available) / trans.AmountPerUnitPurchase);
							foreach (TransmutationCost transcost in trans.Children.Where(a => a.GetType() == typeof(TransmutationCost)))
							{
								double transmutationCost = unitsNeeded * transcost.CostPerUnit;

								// get transcost resource
								IResourceType transResource = null;
								if (transcost.ResourceName == "Labour")
								{
									// get by labour group filter under the transmutation cost
									ResourceRequest labourRequest = new ResourceRequest();
									labourRequest.ResourceName = "Labour";
									labourRequest.FilterDetails = transcost.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).ToList<object>();
									transResource = this.GetResourceItem(labourRequest, out resourceAvailable) as IResourceType;

									// put group name in the transcost resource type name

								}
								else
								{
									transResource = this.GetResourceItem(transcost.ResourceName, transcost.ResourceTypeName, out resourceAvailable) as IResourceType;
								}

								if (!QueryOnly)
								{
									//remove cost
									request.Reason = trans.Name + " " + trans.Parent.Name;
									// create new request for this transmutation cost
									ResourceRequest transRequest = new ResourceRequest();
									transRequest.Reason = trans.Name + " " + trans.Parent.Name;
									transRequest.ActivityName = trans.Name + " " + trans.Parent.Name;
									transRequest.Required = transmutationCost;
									transRequest.ResourceName = transcost.ResourceName;

									// used to pass request, but this is not the transmutation cost
									//transResource.Remove(request);
									transResource.Remove(transRequest);
								}
								else
								{
									double activityCost = requests.Where(a => a.ResourceName == transcost.ResourceName & a.ResourceTypeName == transcost.ResourceTypeName).Sum(a => a.Required);
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


	}
}
