using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a Home Food Store type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(FoodStore))]
    public class FoodStoreType : Model
    {



        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        [Description("Dry Matter (%)")]
        public double DryMatter { get; set; }


        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        [Description("DMD (%)")]
        public double DMD { get; set; }



        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        public double Nitrogen { get; set; }



        /// <summary>
        /// Starting Age of the Fodder (Months)
        /// </summary>
        [Description("Starting Age of Human Food (Months)")]
        public double StartingAge { get; set; }


        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }







        /// <summary>
        ///  Creates a Fodder Item   
        /// </summary>
        /// <returns></returns>
        public FoodStoreItem CreateListItem()
        {
            FoodStoreItem food = new FoodStoreItem();

            food.DryMatter  = this.DryMatter;
            food.DMD = this.DMD;
            food.Nitrogen = this.Nitrogen;
            food.Age = this.StartingAge;
            food.Amount = this.StartingAmount;

            return food;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class FoodStoreItem
    {



        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        public double DryMatter;


        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        public double DMD;


        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        public double Nitrogen;


        /// <summary>
        /// Age of this Human Food (Months)
        /// </summary>
        public double Age { get; set; }


        /// <summary>
        /// Amount (kg)
        /// </summary>
        public double Amount { get; set; }


    }
}