using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a fodder type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Fodder))]
    public class FodderType : Model
    {

        /// <summary>
        /// Is this Fodder a Forage?
        /// </summary>
        [Description("Is this a Forage?")]
        public bool IsForage { get; set; }


        /// <summary>
        /// Is this Fodder a Supplement?
        /// </summary>
        [Description("Is this a Supplement?")]
        public bool IsSupplement { get; set; }


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
        [Description("Starting Age of Fodder (Months)")]
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
        public FodderItem CreateListItem()
        {
            FodderItem fodder = new FodderItem();

            fodder.IsForage = this.IsForage;
            fodder.IsSupplement = this.IsSupplement;
            fodder.DryMatter  = this.DryMatter;
            fodder.DMD = this.DMD;
            fodder.Nitrogen = this.Nitrogen;
            fodder.Age = this.StartingAge;
            fodder.Amount = this.StartingAmount;

            return fodder;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class FodderItem
    {

        /// <summary>
        /// Is this Fodder a Forage?
        /// </summary>
        public bool IsForage;


        /// <summary>
        /// Is this Fodder a Supplement?
        /// </summary>
        public bool IsSupplement;


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
        /// Age of this Fodder (Months)
        /// </summary>
        public double Age { get; set; }


        /// <summary>
        /// Amount (kg)
        /// </summary>
        public double Amount { get; set; }


    }
}