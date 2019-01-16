using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using Models.Agroforestry;

namespace Models.Zones
{
    /// <summary>A strip crop zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(AgroforestrySystem))]
    [ScopedModel]
    public class StripCropZone : Zone
    {
        /// <summary>Length of the zone.</summary>
        /// <value>The length.</value>
        [Description("Length of zone (m)")]
        public double Length
        {
            get
            {
                if (Apsim.Children(this, typeof(RectangularZone)).Count() != 2)
                    throw new Exception("StripCropZone " + Name + " must have two children of type RectangularZone");
                if ((Children[0] as RectangularZone).Length != (Children[1] as RectangularZone).Length)
                    throw new Exception("StripCropZone " + Name + " must have two children of type RectangularZone with the same length");

                return (Children[0] as RectangularZone).Length;
            }
        }

        /// <summary>Width of the zone.</summary>
        /// <value>The width.</value>
        [Description("Width of zone (m)")]
        public double Width
        { 
            get
            {
                if (Apsim.Children(this, typeof(RectangularZone)).Count() != 2)
                    throw new Exception("StripCropZone " + Name + " must have two children of type RectangularZone");
                return (Children[0] as RectangularZone).Width + (Children[1] as RectangularZone).Width;
            }
        }

        /// <summary>Slope of the zone.</summary>
        /// <value>The slope.</value>
        [Description("Slope of zone ")]
        public override double Slope
        {
            get
            {
                if (Apsim.Children(this, typeof(RectangularZone)).Count() != 2)
                    throw new Exception("StripCropZone " + Name + " must have two children of type RectangularZone");
                if ((Children[0] as RectangularZone).Slope != (Children[1] as RectangularZone).Slope)
                    throw new Exception("StripCropZone " + Name + " must have two children of type RectangularZone with the same length");

                return (Children[0] as RectangularZone).Slope;
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
