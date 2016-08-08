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
    /// Store for all the food designated for animals to eat (eg. Forages and Supplements)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(DropAnywhere = true)]
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


        private IModel GetByName(string Name)
        {
            return Groups.Find(x => x.Name == Name);
        }



        /// <summary>
        /// Get the Resource Group for Fodder
        /// </summary>
        /// <returns></returns>
        public Fodder Fodder()
        {
            IModel model = GetByName("Fodder");
            return model as Fodder;
        }

        /// <summary>
        /// Get the Resource Group for FoodStore
        /// </summary>
        /// <returns></returns>
        public FoodStore FoodStore()
        {
            IModel model = GetByName("FoodStore");
            return model as FoodStore;
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
        /// Get the Resource Group for Pasture
        /// </summary>
        /// <returns></returns>
        public Pasture Pasture()
        {
            IModel model = GetByName("Pasture");
            return model as Pasture;
        }


        /// <summary>
        /// Get the Resource Group for Ruminant Herd
        /// </summary>
        /// <returns></returns>
        public RuminantHerd RuminantHerd()
        {
            IModel model = GetByName("RuminantHerd");
            return model as RuminantHerd;
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
