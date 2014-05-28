using System.Xml.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Reflection;
using System.Linq;

namespace Models.Core
{


    //=========================================================================
    /// <summary>
    /// A generic system that can have children
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class Zone : Model
    {
        /// <summary>
        /// Area of the zone.
        /// </summary>
        [Description("Area of zone (ha)")]
        public double Area { get; set; }




    }
}