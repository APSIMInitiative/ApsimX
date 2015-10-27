using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using Models.Agroforestry;

namespace Models.Zones
{
    /// <summary>A circular zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
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
        /// Returns the distance from both edges of a zone and the midpoint.
        /// </summary>
        public double[] Distance
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
        [XmlIgnore]
        public override double Area
        {
            get
            {
                return (Math.PI * (Math.Pow(Radius,2) - Math.Pow(Radius-Width,2))) / 10000;
            }
            set
            {
            }
        }
    }
}
