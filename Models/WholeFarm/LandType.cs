using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for land
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Land))]
    public class LandType : Model
    {

        /// <summary>
        /// Total Area (ha)
        /// </summary>
        [Description("Land Area (ha)")]
        public double LandArea { get; set; }


        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        [Description("Buildings - proportion taken up with bldgs, paths (%)")]
        public double UnusablePortion { get; set; }

        /// <summary>
        /// Portion Bunded (%)
        /// </summary>
        [Description("Portion bunded (%)")]
        public double BundedPortion { get; set; }

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        [Description("Soil Type (1-5)")]
        public int SoilType { get; set; }

        /// <summary>
        /// Fertility - N Decline Yield
        /// </summary>
        [Description("Fertility - N Decline yld")]
        public double NDecline { get; set; }


        /// <summary>
        ///  Creates a Land Item   
        /// </summary>
        /// <returns></returns>
        public LandItem CreateListItem()
        {
            LandItem land = new LandItem();

            land.LandArea = this.LandArea;
            land.UnusablePortion = this.UnusablePortion;
            land.BundedPortion = this.BundedPortion;
            land.SoilType = this.SoilType;
            land.NDecline = this.NDecline;

            land.AreaAvailable = this.LandArea;
            land.AreaUsed = 0;

            return land;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LandItem
    {


        /// <summary>
        /// Total Area (ha)
        /// </summary>
        public double LandArea;


        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        public double UnusablePortion;

        /// <summary>
        /// Portion Bunded (%)
        /// </summary>
        public double BundedPortion;

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        public int SoilType;

        /// <summary>
        /// Fertility - N Decline Yield
        /// </summary>
        public double NDecline;


        //TODO: turn these two below into properties with getters and setters that change each other as well.

        /// <summary>
        /// Area not currently being used (ha)
        /// </summary>
        public double AreaAvailable;

        /// <summary>
        /// Area already used (ha)
        /// </summary>
        public double AreaUsed;
    }
}