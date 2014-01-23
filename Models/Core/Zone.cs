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
        public List<Model> Children { get; set; }

         /// <summary>
        /// Add a model to the Models collection. Will throw if model cannot be added.
        /// </summary>
        public override void AddModel(Model model)
        {
            base.AddModel(model);
            EnsureNameIsUnique(model);
        }

        /// <summary>
        /// If the specified model has a settable name property then ensure it has a unique name.
        /// Otherwise don't do anything.
        /// </summary>
        private string EnsureNameIsUnique(object Model)
        {
            string OriginalName = Utility.Reflection.Name(Model);
            string NewName = OriginalName;
            int Counter = 0;
            object Child = Models.FirstOrDefault(m => m.Name == NewName);
            while (Child != null && Child != Model && Counter < 10000)
            {
                Counter++;
                NewName = OriginalName + Counter.ToString();
                Child = Models.FirstOrDefault(m => m.Name == NewName);
            }
            if (Counter == 1000)
                throw new Exception("Cannot create a unique name for model: " + OriginalName);
            Utility.Reflection.SetName(Model, NewName);
            return NewName;
        }
    }
}