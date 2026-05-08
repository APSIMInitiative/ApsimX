// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;
using APSIM.Core;
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// A stored supplement name and quantity
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Supplement))]
    public class StoreType : Model, ISuppInfo
    {

        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Gets a value indicating whether this is a roughage.</summary>
        [Description("Is this a roughage?")]
        [Display(Type = DisplayType.DropDown, Values = "GetIsRoughageValues")]
        [Separator("Roughage Options")]
        public bool IsRoughage { get; set; }

        /// <summary>Gets or sets the amount of supplement.</summary>
        [Description("Amount of supplement (kg)")]
        [Units("kg")]
        [Separator("General Supplement Options")]
        public double Stored { get; set; }

        /// <summary>Gets the dry matter content (0-1).</summary>
        [Description("Dry matter content (0-1)")]
        public double DMContent { get; set; }

        /// <summary>Gets the dry matter digestibility (0-1).</summary>
        [Description("Dry matter digestibility (0-1)")]
        public double DMD { get; set; }

        /// <summary>Gets the metabolizable energy content (MJ/kg DM).</summary>
        [Description("Metabolizable energy content (MJ/kg DM)")]

        public double MEContent { get; set; }

        /// <summary>Gets the crude protein concentration (0-1).</summary>
        [Description("Crude protein concentration (0-1)")]
        public double CPConc { get; set; }

        /// <summary>Gets the protein degradability (0-1).</summary>
        [Description("Protein degradability (0-1)")]

        public double ProtDg { get; set; }

        /// <summary>Gets the phosphorus concentration (0-1).</summary>
        [Description("Phosphorus concentration (0-1)")]
        public double PConc { get; set; }

        /// <summary>Gets the sulphur concentration (0-1).</summary>
        [Description("Sulphur concentration (0-1)")]
        public double SConc { get; set; }

        /// <summary>Gets the ether extract concentration (0-1).</summary>
        [Description("Ether extract concentration (0-1)")]
        public double EEConc { get; set; }

        /// <summary>Gets the ADIP to crude protein ratio (0-1).</summary>
        [Description("ADIP to crude protein ratio (0-1)")]
        public double ADIP2CP { get; set; }

        /// <summary>Gets the ash alkalinity (mol/kg DM).</summary>
        public double AshAlk { get; set; }

        /// <summary>Gets the maximum passage rate (0-1).</summary>
        public double MaxPassage { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is a roughage.
        /// </summary>
        /// <returns></returns>
        public string[] GetIsRoughageValues()
        {
            return new string[] { bool.FalseString, bool.TrueString };
        }

        /// <summary>Called when the model is created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();
            if(Node != null && Node.Parent != null)
                (Node.Parent.Model as Supplement)?.AddToStore(this);
        }
    }
}
