using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using DocumentFormat.OpenXml.Office.CustomXsn;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Core
{

    /// <summary>
    /// A generic system that can have children
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Agroforestry.AgroforestrySystem))]
    public class Zone : Model, IZone, IScopedModel, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { protected get; set; }

        /// <summary>
        /// Link to summary, for error/warning reporting.
        /// </summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Area of the zone.</summary>
        [Description("Area of zone (ha)")]
        virtual public double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        [Description("Slope angle (degrees)")]
        virtual public double Slope { get; set; }

        /// <summary>Angle of the aspect, from north (degrees).</summary>
        [Description("Aspect (degrees from north)")]
        public double AspectAngle { get; set; }

        /// <summary>Local altitude (meters above sea level).</summary>
        [Description("Local altitude (meters above sea level)")]
        public double Altitude { get; set; } = 50;

        /// <summary>Tha amount of incomming radiation (MJ)</summary>
        [Units("MJ/m^2/day")]
        public double IncidentRadiation
        {
            get
            {
                Simulation parentSim = Structure.FindParents<Simulation>().FirstOrDefault();
                double radn = (double)parentSim.Node.Get("[Weather].Radn");
                return radn * Area * 10000;
            }
        }

        ///<summary>What kind of canopy</summary>
        [Description("Strip crop Radiation Interception Model")]
        [Display(Type = DisplayType.CanopyTypes)]
        virtual public string CanopyType { get; set; }

        /// <summary>Return a list of plant models.</summary>
        [JsonIgnore]
        public List<IPlant> Plants { get { return Structure.FindChildren<IPlant>().ToList(); } }

        /// <summary>Return a list of canopies.</summary>
        [JsonIgnore]
        public List<ICanopy> Canopies { get { return Structure.FindChildren<ICanopy>(recurse: true).ToList(); } }

        /// <summary>Return the index of this paddock</summary>
        public int Index { get { return Parent.Children.IndexOf(this); } }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Area <= 0)
                throw new Exception("Zone area must be greater than zero.  See Zone: " + Name);
            Validate();
            CheckSensibility();
        }

        /// <summary>
        /// Ensure that child zones' total area does not exceed this zone's area.
        /// </summary>
        private void Validate()
        {
            Zone[] subPaddocks = Children.OfType<Zone>().ToArray();
            double totalSubzoneArea = subPaddocks.Sum(z => z.Area);
            if (totalSubzoneArea > Area)
                throw new Exception($"Error in zone {this.FullPath}: total area of child zones ({totalSubzoneArea} ha) exceeds that of parent ({Area} ha)");
        }

        /// <summary>
        /// Check the sensibility of the zone. Write any warnings to the summary log.
        /// </summary>
        private void CheckSensibility()
        {
            if (Structure.Find<MicroClimate>() == null)
                summary.WriteMessage(this, "MicroClimate not found", MessageType.Warning);
        }

        /// <summary>
        /// Called when the model has been newly created in memory whether from
        /// cloning or deserialisation.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            Validate();
            base.OnCreated();
        }
    }
}