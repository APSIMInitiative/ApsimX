// -----------------------------------------------------------------------
// <copyright file="SoilCrop.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Soils
{
    using System;
    using System.Xml.Serialization;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A soil crop parameterization class.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(typeof(Water))]
    public class SoilCrop : Model, ISoilCrop
    {
        /// <summary>
        /// Gets the parent soil
        /// </summary>
        [XmlIgnore]
        private Soil Soil
        {
            get
            {
                return Apsim.Parent(this, typeof(Soil)) as Soil;
            }
        }

        /// <summary>
        /// Gets the associated depths
        /// </summary>
        [Summary]
        [XmlIgnore]
        [Units("cm")]
        [Description("Depth")]
        public string[] Depth
        {
            get
            {
                return Soil.ToDepthStrings(Soil.Thickness);
            }
        }

        /// <summary>
        /// Gets or sets the crop lower limit
        /// </summary>
        [Summary]
        [Description("LL")]
        [Units("mm/mm")]
        public double[] LL { get; set; }

        /// <summary>
        /// Gets the plant available water by layer
        /// </summary>
        [Summary]
        [Description("PAWC")]
        [Display(Format = "N1", ShowTotal = true)]
        [Units("mm")]
        public double[] PAWC
        {
            get
            {
                Soil parentSoil = Soil;
                if (parentSoil != null)
                    return MathUtilities.Multiply(Soil.CalcPAWC(parentSoil.Thickness, this.LL, parentSoil.DUL, this.XF), parentSoil.Thickness);
                else
                    return new double[0];
            }
        }

        /// <summary>
        /// Gets or sets the KL value.
        /// </summary>
        [Summary]
        [Description("KL")]
        [Display(Format = "N2")]
        [Units("mm/mm")]
        public double[] KL { get; set; }

        /// <summary>
        /// Gets or sets the exploration factor
        /// </summary>
        [Summary]
        [Description("XF")]
        [Display(Format = "N1")]
        [Units("mm/mm")]
        public double[] XF { get; set; }

        /// <summary>
        /// Gets or sets the metadata for crop lower limit
        /// </summary>
        public string[] LLMetadata { get; set; }

        /// <summary>
        /// Gets or sets the metadata for KL
        /// </summary>
        public string[] KLMetadata { get; set; }

        /// <summary>
        /// Gets or sets the meta data for the exploration factor
        /// </summary>
        public string[] XFMetadata { get; set; }
    }
}