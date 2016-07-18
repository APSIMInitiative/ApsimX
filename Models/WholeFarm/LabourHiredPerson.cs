using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a person who can do labour 
    ///  who is a hired in to work on the farm.
    /// eg. AdultMale, AdultFemale etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourHired))]
    public class LabourHiredPerson : Model
    {

        /// <summary>
        /// Age in years.
        /// </summary>
        [Description("Initial Age")]
        public double InitialAge { get; set; }

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Name of each column in the grid. Used as the column header.
        /// </summary>
        [Description("Column Names")]
        public string[] ColumnNames { get; set; }

        /// <summary>
        /// Maximum Labour Supply (in days) for each month of the year. 
        /// </summary>
        [Description("Max Labour Supply (in days) for each month of the year")]
        public double[] MaxLabourSupply { get; set; }





        /// <summary>
        /// If this labour is hired in to work on the farm,
        /// then what is their pay rate.
        /// </summary>
        [Description("Hired in Pay rate")]
        public double HiredInPayRate { get; set; }




        /// <summary>
        ///  Creates a Labour Person who is hired in to work on the farm   
        /// </summary>
        /// <returns></returns>
        public LabourHiredItem CreateListItem()
        {
            LabourHiredItem person = new LabourHiredItem();
            person.Age = this.InitialAge;
            person.Gender = this.Gender;
            person.MaxLabourSupply = this.MaxLabourSupply;

            person.HiredInPayRate = this.HiredInPayRate;

            return person;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LabourHiredItem : LabourItem
    {
        /// <summary>
        /// If this labour is hired in to work on the farm,
        /// then what is their pay rate.
        /// </summary>
        [XmlIgnore]
        public double HiredInPayRate;
    }
}