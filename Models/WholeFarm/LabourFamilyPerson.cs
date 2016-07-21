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
    ///  who is a family member.
    /// eg. AdultMale, AdultFemale etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourFamily))]
    public class LabourFamilyPerson : Model
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
        /// Does this family member do Non Farm labour ?
        /// </summary>
        [Description("Does Non Farm Labour ?")]
        public bool DoesNonFarmLabour { get; set; }

        /// <summary>
        /// If this family member does Non Farm labour
        /// then what is their default Non Farm pay rate.
        /// </summary>
        [Description("Default Non Farm Pay rate")]
        public double DefaultNonFarmPayRate { get; set; }




        /// <summary>
        ///  Creates a Labour Person who is a family member.   
        /// </summary>
        /// <returns></returns>
        public LabourFamilyItem CreateListItem()
        {
            LabourFamilyItem person = new LabourFamilyItem();
            person.Age = this.InitialAge;
            person.Gender = this.Gender;
            person.MaxLabourSupply = this.MaxLabourSupply;

            person.DoesNonFarmLabour = this.DoesNonFarmLabour;
            person.DefaultNonFarmPayRate = this.DefaultNonFarmPayRate;

            return person;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LabourFamilyItem : LabourItem
    {

        /// <summary>
        /// Does this family member do Non Farm labour ?
        /// </summary>
        public bool DoesNonFarmLabour;

        /// <summary>
        /// If this family member does Non Farm labour
        /// then what is their default Non Farm pay rate.
        /// </summary>
        public double DefaultNonFarmPayRate;
    }
}