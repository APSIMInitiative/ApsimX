using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.Core
{
    /// <summary>A circular zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CircularZone : Zone
    {
        /// <summary>Radius of the zone.</summary>
        /// <value>The radius.</value>
        [Description("Length of zone (m)")]
        public double Radius { get; set; }

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
                return (Math.PI * (Math.Pow(Radius,2) - Math.Pow(Radius-Width,2))) / 10000;
            }
            set
            {
            }
        }
    }
}
