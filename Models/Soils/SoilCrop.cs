namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>A soil crop parameterization class.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.NewGridView.glade")]
    [PresenterName("UserInterface.Presenters.NewGridPresenter")]
    [ValidParent(ParentType = typeof(Physical))]
    public class SoilCrop : Model, ITabularData
    {
        /// <summary>Crop lower limit (mm/mm)</summary>
        [Summary]
        [Description("LL")]
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
        [Description("KL")]
        [Display(Format = "N2")]
        [Units("/day")]
        public double[] KL { get; set; }

        /// <summary>The exploration factor</summary>
        [Summary]
        [Description("XF")]
        [Display(Format = "N1")]
        [Units("0-1")]
        public double[] XF { get; set; }

        /// <summary>The metadata for crop lower limit</summary>
        public string[] LLMetadata { get; set; }

        /// <summary>The metadata for KL</summary>
        public string[] KLMetadata { get; set; }

        /// <summary>The meta data for the exploration factor</summary>
        public string[] XFMetadata { get; set; }

        /// <summary>Return the plant available water CAPACITY at standard thickness.</summary>
        [Description("PAWC")]
        [Display(Format = "N2", ShowTotal = true)]
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
        [Display(Format = "N2", ShowTotal = true)]
        [Units("mm")]
        public double[] PAWCmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return  MathUtilities.Multiply(PAWC, soilPhysical.Thickness);
            }
        }

        /// <summary>Tabular data. Called by GUI.</summary>
        public TabularData GetTabularData()
        {
            return new TabularData(Name, new TabularData.Column[]
            {
                new TabularData.Column("Depth", new VariableProperty(Parent, Parent.GetType().GetProperty("Depth")), readOnly:true),
                new TabularData.Column("LL", new VariableProperty(this, GetType().GetProperty("LL"))),
                new TabularData.Column("KL", new VariableProperty(this, GetType().GetProperty("KL"))),
                new TabularData.Column("XF", new VariableProperty(this, GetType().GetProperty("XF"))),
                new TabularData.Column("PAWC", new VariableProperty(this, GetType().GetProperty("PAWCmm")), units: $"{PAWCmm.Sum():F1} mm")
            });
        }

    }
}