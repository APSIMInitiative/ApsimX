using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>A soil crop parameterization class.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Physical))]
    public class SoilCrop : Model
    {
        /// <summary>Depth strings (mm/mm)</summary>
        [Display]
        [Summary]
        [Units("mm")]
        public string[] Depth => (Parent as Physical).Depth;

        /// <summary>Crop lower limit (mm/mm)</summary>
        [Summary]
        [Display(Format = "N3")]
        [Units("mm/mm")]
        public double[] LL { get; set; }

        /// <summary>Crop lower limit (mm)</summary>
        [Units("mm")]
        public double[] LLmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return MathUtilities.Multiply(LL, soilPhysical.Thickness);
            }
        }

        /// <summary>The KL value.</summary>
        [Summary]
        [Units("/day")]
        [Display(Format = "N3")]
        public double[] KL { get; set; }

        /// <summary>The exploration factor</summary>
        [Summary]
        [Units("0-1")]
        [Display(Format = "N1")]
        public double[] XF { get; set; }

        /// <summary>The metadata for crop lower limit</summary>
        public string[] LLMetadata { get; set; }

        /// <summary>The metadata for KL</summary>
        public string[] KLMetadata { get; set; }

        /// <summary>The meta data for the exploration factor</summary>
        public string[] XFMetadata { get; set; }

        /// <summary>Return the plant available water CAPACITY at standard thickness.</summary>
        [Units("mm/mm")]
        public double[] PAWC
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return SoilUtilities.CalcPAWC(soilPhysical.Thickness, LL, soilPhysical.DUL, XF);
            }
        }

        /// <summary>Return the plant available water CAPACITY at standard thickness.</summary>
        [Display(DisplayName = "PAWC", Format = "N1")]
        [Units("mm")]
        public double[] PAWCmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return MathUtilities.Multiply(PAWC, soilPhysical.Thickness);
            }
        }

        /// <summary>Return the plant available water (SW-CLL).</summary>
        [Units("mm/mm")]
        public double[] PAW
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                var water = FindInScope<Water>();
                if (water == null)
                    return null;
                return SoilUtilities.CalcPAWC(soilPhysical.Thickness, LL, water.Volumetric, XF);
            }
        }

        /// <summary>Return the plant available water (SW-CLL) (mm).</summary>
        [Units("mm")]
        public double[] PAWmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;

                return MathUtilities.Multiply(PAW, soilPhysical.Thickness);
            }
        }
    }
}