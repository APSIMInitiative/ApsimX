using System;
using Models.Agroforestry;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Zones
{
    /// <summary>A circular zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(AgroforestrySystem))]
    public class CircularZone : Zone
    {
        /// <summary>Radius of the zone.</summary>
        /// <value>The radius.</value>
        [Description("Outside radius of zone (m)")]
        public double Radius { get; set; }

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
                return (Math.PI * (Math.Pow(Radius, 2) - Math.Pow(Radius - Width, 2))) / 10000;
            }
            set
            {
            }
        }
    }
}
