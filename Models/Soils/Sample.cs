namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Soils.Standardiser;
    using Newtonsoft.Json;
    using System;

    /// <summary>The class represents a soil sample.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Physical))]
    public class Sample : Model
    {
        [Link]
        IPhysical physical = null;

        /// <summary>Constructor.</summary>
        public Sample() 
        { 
            this.Name = "Sample"; 
        }

        /// <summary>
        /// An enumeration for specifying soil water units
        /// </summary>
        public enum SWUnitsEnum
        {
            /// <summary>
            /// Volumetric mm/mm
            /// </summary>
            Volumetric,

            /// <summary>
            /// Gravimetric soil water
            /// </summary>
            Gravimetric,

            /// <summary>
            /// mm of water
            /// </summary>
            mm
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("cm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStringsCM(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Thickness</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>
        /// Gets or sets soil water. Units will be as specified by SWUnits
        /// </summary>
        [Description("SW")]
        [Summary]
        [Display(Format = "N3", ShowTotal = true)]
        public double[] SW { get; set; }

        /// <summary>
        /// Gets or sets the units of SW
        /// </summary>
        public SWUnitsEnum SWUnits { get; set; }

        /// <summary>
        /// Gets SW. Units: mm/mm.
        /// </summary>
        [Summary]
        [Display(Format = "N1", ShowTotal = true)]
        public double[] SWmm
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return MathUtilities.Multiply(this.SW, this.Thickness);
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return MathUtilities.Multiply(MathUtilities.Multiply(this.SW, Layers.BDMapped(physical, this.Thickness)), this.Thickness);
                    }
                    else
                    {
                        return this.SW;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets SW. Units: kg/kg.
        /// </summary>
        public double[] SWGravimetric
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return MathUtilities.Divide(this.SW, Layers.BDMapped(physical, this.Thickness));
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return this.SW;
                    }
                    else
                    {
                        return MathUtilities.Divide(MathUtilities.Divide(this.SW, Layers.BDMapped(physical, this.Thickness)), this.Thickness);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets SW. Units: mm/mm.
        /// </summary>
        public double[] SWVolumetric
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return this.SW;
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return MathUtilities.Multiply(this.SW, Layers.BDMapped(physical, this.Thickness));
                    }
                    else
                    {
                        return MathUtilities.Divide(this.SW, this.Thickness);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the soil associated with this sample
        /// </summary>
        private Soil Soil
        {
            get
            {
                return FindAncestor<Soil>();
            }
        }
    }
}
