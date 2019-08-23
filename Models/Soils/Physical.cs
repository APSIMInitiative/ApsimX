namespace Models.Soils
{
    using Models.Core;
    using System;

    /// <summary>A model for capturing physical soil parameters</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Physical : Model
    {
        /// <summary>Gets or sets the thickness.</summary>
        [Description("Depth")]
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Particle size clay.</summary>
        [Summary]
        [Description("Clay")]
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }

        /// <summary>Gets or sets the bd.</summary>
        [Summary]
        [Description("BD")]
        [Units("g/cc")]
        [Display(Format = "N2")]
        public double[] BD { get; set; }

        /// <summary>Gets or sets the air dry.</summary>
        [Summary]
        [Description("Air dry")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] AirDry { get; set; }

        /// <summary>Gets or sets the l L15.</summary>
        [Summary]
        [Description("LL15")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] LL15 { get; set; }

        /// <summary>Gets or sets the dul.</summary>
        [Summary]
        [Description("DUL")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] DUL { get; set; }

        /// <summary>Gets or sets the sat.</summary>
        [Summary]
        [Description("SAT")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SAT { get; set; }

        /// <summary>Gets or sets the ks.</summary>
        [Summary]
        [Description("KS")]
        [Units("mm/day")]
        [Display(Format = "N1")]
        public double[] KS { get; set; }

        /// <summary>Gets or sets the bd metadata.</summary>
        public string[] BDMetadata { get; set; }

        /// <summary>Gets or sets the air dry metadata.</summary>
        public string[] AirDryMetadata { get; set; }
        
        /// <summary>Gets or sets the l L15 metadata.</summary>
        public string[] LL15Metadata { get; set; }
        
        /// <summary>Gets or sets the dul metadata.</summary>
        public string[] DULMetadata { get; set; }
        
        /// <summary>Gets or sets the sat metadata.</summary>
        public string[] SATMetadata { get; set; }
        
        /// <summary>Gets or sets the ks metadata.</summary>
        public string[] KSMetadata { get; set; }
    }
}
