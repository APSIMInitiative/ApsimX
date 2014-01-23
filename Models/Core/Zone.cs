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
    public class Zone : ModelCollection
    {
        /// <summary>
        /// Area of the zone.
        /// </summary>
        [Description("Area of zone (ha)")]
        public double Area { get; set; }

        /// <summary>
        /// A list of child models.
        /// </summary>
        [XmlElement(typeof(Simulation))]
        [XmlElement(typeof(Simulations))]
        [XmlElement(typeof(Zone))]
        [XmlElement(typeof(Models.Graph.Graph))]
        [XmlElement(typeof(Models.PMF.Plant))]
        [XmlElement(typeof(Models.PMF.Slurp.Slurp))]
        [XmlElement(typeof(Models.Soils.Soil))]
        [XmlElement(typeof(Models.SurfaceOM.SurfaceOrganicMatter))]
        [XmlElement(typeof(Clock))]
        [XmlElement(typeof(DataStore))]
        [XmlElement(typeof(Fertiliser))]
        [XmlElement(typeof(Input))]
        [XmlElement(typeof(Irrigation))]
        [XmlElement(typeof(Manager))]
        [XmlElement(typeof(MicroClimate))]
        [XmlElement(typeof(Operations))]
        [XmlElement(typeof(Report))]
        [XmlElement(typeof(Summary))]
        [XmlElement(typeof(NullSummary))]
        [XmlElement(typeof(Tests))]
        [XmlElement(typeof(WeatherFile))]
        [XmlElement(typeof(Log))]
        [XmlElement(typeof(Models.Factorial.Experiment))]
        [XmlElement(typeof(Memo))]
        [XmlElement(typeof(Folder))]
        public List<Model> Children { get; set; }

         /// <summary>
        /// Add a model to the Models collection. Will throw if model cannot be added.
        /// </summary>
        public override void AddModel(Model model)
        {
            base.AddModel(model);
            EnsureNameIsUnique(model);
        }


    }
}