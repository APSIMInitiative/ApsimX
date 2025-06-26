using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
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
    [ScopedModel]
    public class Zone : Model, IZone
    {
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

        /// <summary>Tha amount of incomming radiation (MJ/m2)</summary>
        [Units("MJ/m^2/day")]
        public double IncidentRadiation
        {
            get
            {
                Simulation parentSim = this.FindAllAncestors<Simulation>().FirstOrDefault();
                double radn = (double)parentSim.Get("[Weather].Radn");
                return radn;
            }
        }

        ///<summary>What kind of canopy</summary>
        [Description("Strip crop Radiation Interception Model")]
        [Display(Type = DisplayType.CanopyTypes)]
        virtual public string CanopyType { get; set; }

        /// <summary>Return a list of plant models.</summary>
        [JsonIgnore]
        public List<IPlant> Plants { get { return FindAllChildren<IPlant>().ToList(); } }

        /// <summary>Return a list of canopies.</summary>
        [JsonIgnore]
        public List<ICanopy> Canopies { get { return FindAllDescendants<ICanopy>().ToList(); } }

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

        /// <summary>Gets the value of a variable or model.</summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locator.Get(namePath);
        }

        /// <summary>Get the underlying variable object for the given path.</summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locator.GetObject(namePath);
        }

        /// <summary>Sets the value of a variable. Will throw if variable doesn't exist.</summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locator.Set(namePath, value);
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
            if (FindInScope<MicroClimate>() == null)
                summary.WriteMessage(this, "MicroClimate not found", MessageType.Warning);
        }

        /// <summary>
        /// Called when the model has been newly created in memory whether from
        /// cloning or deserialisation.
        /// </summary>
        public override void OnCreated(Node node)
        {
            base.OnCreated(node);
            Validate();
            base.OnCreated(node);
        }
    }
}