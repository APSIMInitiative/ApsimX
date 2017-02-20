using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{
	///<summary>
	/// WholeFarm Activity base model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public abstract class WFActivityBase: WFModel
	{
		private List<ResourceRequest> requestList;

		[Link]
		private Resources Resources = null;

		/// <summary>
		/// Method to cascade calls for resources for all activities in the UI tree. 
		/// Responds to WFGetResourcesRequired in the Activity model holing top level list of activities
		/// </summary>
		public void GetResourcesForAllActivities()
		{
			// Get resources needed and use substitution if needed and provided, then move through children getting their resources.
			GetResourcesRequired();

			// get resources required for all children of type WFActivityBase
			foreach (WFActivityBase child in Children.Where(a => a.GetType() == typeof(WFActivityBase)))
			{
				child.GetResourcesForAllActivities();
			}
		}

		/// <summary>
		/// Method to get this time steps current required resources for this activity. 
		/// </summary>
		public void GetResourcesRequired()
		{
			// determine what resources are needed
			requestList = DetermineResourcesNeeded();

			// check resource amounts available
			foreach (ResourceRequest request in requestList)
			{
				// get resource
				IResourceType resource = Resources.GetResourceItem(request.ResourceName, request.ResourceTypeName) as IResourceType;

				if (resource != null)
				{
					// get amount available
					request.Available = Math.Max(resource.Amount, request.Required);
				}
				else
				{
					// if resource does not exist in simulation assume unlimited resource available
					request.Available = request.Required;
				}
			}

			// are all resources available
			List<ResourceRequest> shortfallRequests = requestList.Where(a => a.Required - a.Available > 0).ToList();
			if(shortfallRequests.Count() > 0)
			{
				foreach (ResourceRequest shortrequest in shortfallRequests)
				{
					if(shortrequest.AllowTransmutation)
					{
						//
					}
				}
			}

			if (PerformWithPartialResources)
			{
				// take resources if available or happy to do IsPartial 

				// undertake activity here if provided or store resources obtained and await activity event
				PerformActivity();
			}
		}


		/// <summary>
		/// Allow activity to proceed with less resources than requested
		/// </summary>
		public bool PerformWithPartialResources { get; set; }

		/// <summary>
		/// Method to perform activity tasks if expected as soon as resources are available
		/// </summary>
		public abstract void PerformActivity();

		/// <summary>
		/// Abstract method to determine list of resources and amounts needed. 
		/// </summary>
		public abstract List<ResourceRequest> DetermineResourcesNeeded();
	}

	///<summary>
	/// Resource request for Resource from a ResourceType
	///</summary> 
	public class ResourceRequest
	{
		///<summary>
		/// Name of resource being requested 
		///</summary> 
		public string ResourceName { get; set; }
		///<summary>
		/// Name of resource being requested 
		///</summary> 
		public string ResourceTypeName { get; set; }
		///<summary>
		/// Amount required 
		///</summary> 
		public double Required { get; set; }
		///<summary>
		/// Amount available
		///</summary> 
		public double Available { get; set; }
		///<summary>
		/// Request details such as groups and filtering options
		///</summary> 
		public List<object> RequestDetails { get; set; }
		///<summary>
		/// Allow transmutation
		///</summary> 
		public bool AllowTransmutation { get; set; }
		///<summary>
		/// Allow transmutation
		///</summary> 
		public bool TransmutationSuccessful { get; set; }
		///<summary>
		/// Query only request
		///</summary> 
		public bool QueryOnly { get; set; }
		///<summary>
		/// ResourceRequest constructor
		///</summary> 
		public ResourceRequest()
		{
			// default values
			TransmutationSuccessful = false;
			AllowTransmutation = false;
		}
	}

}
