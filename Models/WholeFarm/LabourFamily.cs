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
    [ValidParent(DropAnywhere = true)]
    public class LabourFamily: Model
    {
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Current state of this resource.
        /// List of currently avialable days for each labour type.
        /// </summary>
        [XmlIgnore]
        public List<LabourFamilyItem> Family;


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Family = new List<LabourFamilyItem>();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
                //cast the generic IModel to a specfic model.
                LabourFamilyPerson personInit = childModel as LabourFamilyPerson;
                LabourFamilyItem person = personInit.CreateListItem();
                Family.Add(person);
            }
        }



        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            ResetAvailabilityEachMonth();
        }


        /// <summary>
        /// Reset the Available Labour (in days) in the current month 
        /// to the appropriate value for this month.
        /// </summary>
        private void ResetAvailabilityEachMonth()
        {
            int currentmonth = Clock.Today.Month;

            foreach (LabourFamilyItem person in Family)
            {
                person.AvailableDays = person.MaxLabourSupply[currentmonth - 1];
            }


        }


    }


}
