using System;
using Models.Agroforestry;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Zones
{
    /// <summary>A rectangular zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(AgroforestrySystem))]
    public class RectangularZone : Zone
    {
        /// <summary>Length of the zone.</summary>
        /// <value>The length.</value>
        [Description("Length of zone (m)")]
        public double Length { get; set; }

        /// <summary>Width of the zone.</summary>
        /// <value>The width.</value>
        [Description("Width of zone (m)")]
        public double Width { get; set; }

        /// <summary>
        /// Returns the distance from edge of system
        /// </summary>
        public double Distance
        {
            get
            {
                if (Parent is AgroforestrySystem)
                    return (Parent as AgroforestrySystem).GetDistanceFromTrees(this);
                throw new ApsimXException(this, "Not implemented for this system");
            }
        }

        /// <summary>
        /// Return the area of the zone.
        /// </summary>
        [JsonIgnore]
        public override double Area
        {
            get
            {
                return Length * Width / 10000;
            }
            set
            {
            }
        }

        ///<summary>What kind of canopy</summary>
        [Description("Strip crop Radiation Interception Model")]
        [Display(Type = DisplayType.CanopyTypes)]
        public override string CanopyType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        public RectangularZone(double length, double width)
        {
            Length = length;
            Width = width;
        }
    }
}
