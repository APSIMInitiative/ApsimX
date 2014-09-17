// -----------------------------------------------------------------------
// <copyright file="SoilCropOilPalm.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Soils
{
    using System;
    using System.Xml.Serialization;
    using Models.Core;

    /// <summary>
    /// A soil crop interface
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class SoilCropOilPalm : Model, ISoilCrop
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
        /// Gets or sets the KL value.
        /// </summary>
        [Description("KL")]
        [Display(Format = "N2")]
        [Units("mm/mm")]
        public double[] KL { get; set; }

        /// <summary>
        /// Gets or sets the exploration factor
        /// </summary>
        [Description("XF")]
        [Display(Format = "N1")]
        [Units("mm/mm")]
        public double[] XF { get; set; }

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