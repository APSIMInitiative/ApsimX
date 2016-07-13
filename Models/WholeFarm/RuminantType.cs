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
    public class RuminantType : Model
    {

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Starting Number")]
        public double StartingNumber { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Starting Weight (kg)")]
        public double StartingWeight { get; set; }


        /// <summary>
        ///  Creates a Land Item   
        /// </summary>
        /// <returns></returns>
        public RuminantItem CreateListItem()
        {
            RuminantItem ruminant = new RuminantItem();

            ruminant.Number = this.StartingNumber;
            ruminant.Weight = this.StartingWeight;

            return ruminant;
        }

    }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RuminantItem
    {

        /// <summary>
        /// Number
        /// </summary>
        public double Number { get; set; }

        /// <summary>
        /// Weight (kg)
        /// </summary>
        public double Weight { get; set; }

    }
}