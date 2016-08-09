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
    [ValidParent(ParentType = typeof(Resources))]
    public class Fodder: Model
    {


        /// <summary>
        /// List of all the Fodder Types in this Resource Group.
        /// </summary>
        [XmlIgnore]
        public List<FodderType> Items;


        /// <summary>
        /// Returns the Fodder with the given name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public FodderType GetByName(string Name)
        {
            return Items.Find(x => x.Name == Name);
        }



        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<FodderType>();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
                //cast the generic IModel to a specfic model.
                FodderType fodder = childModel as FodderType;
                Items.Add(fodder);
            }
        }

    }


}
