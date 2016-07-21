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
    [ValidParent(ParentType = typeof(Pasture))]
    public class PastureType : Model
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
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }







        /// <summary>
        ///  Creates a Fodder Item   
        /// </summary>
        /// <returns></returns>
        public PastureItem CreateListItem()
        {
            PastureItem pasture = new PastureItem();

            pasture.DryMatter  = this.DryMatter;
            pasture.DMD = this.DMD;
            pasture.Nitrogen = this.Nitrogen;
            pasture.Amount = this.StartingAmount;

            return pasture;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PastureItem
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
        /// Amount (kg)
        /// </summary>
        public double Amount { get; set; }


    }
}