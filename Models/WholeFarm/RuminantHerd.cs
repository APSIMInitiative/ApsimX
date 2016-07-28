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
    /// Parent model of Land Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class RuminantHerd: Model
    {


        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> Herd;


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Herd = new List<Ruminant>();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
                //cast the generic IModel to a specfic model.
                RuminantType ruminantType = childModel as RuminantType;
                Herd.AddRange(ruminantType.CreateIndividuals());
            }
        }

    }


}
