using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;



namespace Models.WholeFarm
{

    ///<summary>
    /// Manger for all resources available to the model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Resources: Model
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
        private List<IModel> Groups;

		[Link]
		ISummary Summary = null;

		private IModel GetByName(string Name)
        {
            return Groups.Find(x => x.Name == Name);
        }

		private IModel GetByType(Type type)
		{
			return Groups.Find(x => x.GetType() == type);
		}

		/// <summary>
		/// Retrieve a ResourceType from a ResourceGroup with specified names
		/// </summary>
		/// <param name="ResourceGroupName">Name of the resource group</param>
		/// <param name="ResourceTypeName">Name of the resource item</param>
		/// <returns>A reference to the item of type object</returns>
		public Model GetResourceItem(string ResourceGroupName, string ResourceTypeName)
		{
			if(ResourceGroupName==null)
			{
				Summary.WriteWarning(this, "ResourceGroup name must be supplied");
				return null;
			}
			if (ResourceTypeName == null)
			{
				Summary.WriteWarning(this, "ResourceType name must be supplied");
				return null;
			}

			// locate specified resource
			Model resourceGroup = this.Children.Where(a => a.Name == ResourceGroupName).FirstOrDefault();
			if (resourceGroup != null)
			{
				Model resource = resourceGroup.Children.Where(a => a.Name == ResourceTypeName).FirstOrDefault();
				if (resource == null)
				{
					Summary.WriteWarning(this, String.Format("Resource of name {0} not found in {1}", ((ResourceTypeName.Length == 0) ? "[Blank]" : ResourceTypeName), ResourceGroupName));
					throw new Exception("Resource not found!");
				}
				return resource;
			}
			else
			{
				Summary.WriteWarning(this, String.Format("No resource group named {0} found in Resources!", ((ResourceGroupName.Length == 0) ? "[Blank]" : ResourceGroupName)));
				throw new Exception("Resource group not found!");
			}
		}

		/// <summary>
		/// Get the Resource Group for Fodder
		/// </summary>
		/// <returns></returns>
		public AnimalFoodStore AnimalFoodStore()
        {
			return GetByType(typeof(AnimalFoodStore)) as AnimalFoodStore;
//			IModel model = GetByName("AnimalFoodStore");
//           return model as AnimalFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for FoodStore
        /// </summary>
        /// <returns></returns>
        public HumanFoodStore HumanFoodStore()
        {
            IModel model = GetByName("HumanFoodStore");
            return model as HumanFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for Labour Family
        /// </summary>
        /// <returns></returns>
        public LabourFamily LabourFamily()
        {
            IModel model = GetByName("LabourFamily");
            return model as LabourFamily;
        }

        /// <summary>
        /// Get the Resource Group for Labour Hired
        /// </summary>
        /// <returns></returns>
        public LabourHired LabourHired()
        {
            IModel model = GetByName("LabourHired");
            return model as LabourHired;
        }

        /// <summary>
        /// Get the Resource Group for Land
        /// </summary>
        /// <returns></returns>
        public Land Land()
        {
            IModel model = GetByName("Land");
            return model as Land;
        }

        /// <summary>
        /// Get the Resource Group for the GrazeFoodStore
        /// </summary>
        /// <returns></returns>
        public GrazeFoodStore GrazeFoodStore()
        {
            IModel model = GetByName("GrazeFoodStore");
            return model as GrazeFoodStore;
        }

        /// <summary>
        /// Get the Resource Group for Ruminant Herd
        /// </summary>
        /// <returns></returns>
        public RuminantHerd RuminantHerd()
        {
            IModel model = GetByName("Ruminants");
            return model as RuminantHerd;
        }

		/// <summary>
		/// Get the Resource Group for Finances
		/// </summary>
		/// <returns></returns>
		public Finance FinanceResource()
		{
			return GetByType(typeof(Finance)) as Finance;
//			IModel model = GetByName("Finances");
//			return model as Finance;
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Groups = Apsim.Children(this, typeof(IModel));
        }



    }


}
