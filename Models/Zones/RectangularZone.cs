using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using Models.Agroforestry;

namespace Models.Zones
{
    /// <summary>A rectangular zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
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
                if (Parent is StaticForestrySystem)
                    return (Parent as StaticForestrySystem).GetDistanceFromTrees(this);
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
                return Length * Width / 10000;
            }
            set
            {
            }
        }
    }
}
