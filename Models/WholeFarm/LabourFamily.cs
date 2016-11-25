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
    /// Parent model of Labour Person models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Resources))]
    public class LabourFamily: Model
    {
        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<LabourFamilyType> Items;

		/// <summary>
		/// Name of each column in the grid. Used as the column header.
		/// </summary>
		[Description("Allow individuals to age")]
		public bool AllowAging { get; set; }

		/// <summary>
		/// Returns the family member with the given name.
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public LabourFamilyType GetByName(string Name)
		{
			return Items.Find(x => x.Name == Name);
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<LabourFamilyType>();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
                //cast the generic IModel to a specfic model.
                LabourFamilyType labour = childModel as LabourFamilyType;
                Items.Add(labour);
            }
        }


    }


}
