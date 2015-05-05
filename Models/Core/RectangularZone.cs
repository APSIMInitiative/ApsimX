using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.Core
{
    /// <summary>A generic system that can have children</summary>
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
        /// Return the area of the zone.
        /// </summary>
        [XmlIgnore]
        public new double Area
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
