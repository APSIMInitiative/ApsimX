using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.Agroforestry
{
    /// <summary>
    /// Class to hold spatial parameters for tree proxy.
    /// </summary>
    [Serializable]
    public class TreeProxySpatial : Model
    {
        private List<TreeProxySpatialParameter> _parameters;

        /// <summary>
        /// Gets or sets the parameter data.
        /// </summary>
        [Display]
        public List<TreeProxySpatialParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                    CreateParametersUsingDefaults();
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }

        /// <summary>An instance of the tree proxy class.</summary>
        [JsonIgnore]
        public TreeProxy TreeProxyInstance { get; set; }

        /// <summary>Get the THCutoffs.</summary>
        public double[] THCutoffs => new double[] { 0, 0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6 };

        /// <summary>Get the shade values for all THCutoffs</summary>
        public double[] Shade
        {
            get
            {
                return new double[]
                {
                    Convert.ToDouble(Parameters[0].THCutoff0, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff05, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff1, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff15, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff2, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff25, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff3, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff4, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff5, CultureInfo.InvariantCulture),
                    Convert.ToDouble(Parameters[0].THCutoff6, CultureInfo.InvariantCulture)
                };
            }
        }

        /// <summary>Get the RLD values for a tree height cutoff.</summary>
        /// <param name="thCutoff">The tree height cutoff.</param>
        public double[] Rld(double thCutoff)
        {
            List<double> rld = new();
            for (int j = 1; j < Parameters.Count; j++)
            {
                if (thCutoff == 0 && !string.IsNullOrEmpty(Parameters[j].THCutoff0)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff0, CultureInfo.InvariantCulture));
                if (thCutoff == 0.5 && !string.IsNullOrEmpty(Parameters[j].THCutoff05)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff05, CultureInfo.InvariantCulture));
                if (thCutoff == 1 && !string.IsNullOrEmpty(Parameters[j].THCutoff1)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff1, CultureInfo.InvariantCulture));
                if (thCutoff == 1.5 && !string.IsNullOrEmpty(Parameters[j].THCutoff15)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff15, CultureInfo.InvariantCulture));
                if (thCutoff == 2 && !string.IsNullOrEmpty(Parameters[j].THCutoff2)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff2, CultureInfo.InvariantCulture));
                if (thCutoff == 2.5 && !string.IsNullOrEmpty(Parameters[j].THCutoff25)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff25, CultureInfo.InvariantCulture));
                if (thCutoff == 3 && !string.IsNullOrEmpty(Parameters[j].THCutoff3)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff3, CultureInfo.InvariantCulture));
                if (thCutoff == 4 && !string.IsNullOrEmpty(Parameters[j].THCutoff4)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff4, CultureInfo.InvariantCulture));
                if (thCutoff == 5 && !string.IsNullOrEmpty(Parameters[j].THCutoff5)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff5, CultureInfo.InvariantCulture));
                if (thCutoff == 6 && !string.IsNullOrEmpty(Parameters[j].THCutoff6)) rld.Add(Convert.ToDouble(Parameters[j].THCutoff6, CultureInfo.InvariantCulture));
            }
            return rld.ToArray();
        }

        /// <summary>Create parameters using default values.</summary>
        private void CreateParametersUsingDefaults()
        {
            // Get the first soil. For now we're assuming all soils have the same structure.
            var physical = TreeProxyInstance.FindInScope<Physical>();
            if (physical != null)
            {
                _parameters = new()
                {
                    new() { Name = "Shade (%)" },
                    new() { Name = "Root Length Density (cm/cm3)" },
                    new() { Name = "Depth (cm)" },
                };
                foreach (string s in SoilUtilities.ToDepthStringsCM(physical.Thickness))
                    _parameters.Add( new() { Name = s });
            }
        }
    }

    /// <summary>
    /// Class to hold spatial parameters for tree proxy.
    /// </summary>
    [Serializable]
    public class TreeProxySpatialParameter
    {
        /// <summary>Parameter.</summary>
        [Display(DisplayName = "Parameter")]
        public string Name { get; set; }

        /// <summary>Tree height cutoff 0.</summary>
        [Display(DisplayName = "0")]
        public string THCutoff0 { get; set; }

        /// <summary>Tree height cutoff 0.5</summary>
        [Display(DisplayName = "0.5")]
        public string THCutoff05 { get; set; }

        /// <summary>Tree height cutoff 1</summary>
        [Display(DisplayName = "1")]
        public string THCutoff1 { get; set; }

        /// <summary>Tree height cutoff 1.5</summary>
        [Display(DisplayName = "1.5")]
        public string THCutoff15 { get; set; }

        /// <summary>Tree height cutoff 2</summary>
        [Display(DisplayName = "2")]
        public string THCutoff2 { get; set; }

        /// <summary>Tree height cutoff 2.5</summary>
        [Display(DisplayName = "2.5")]
        public string THCutoff25 { get; set; }

        /// <summary>Tree height cutoff 3</summary>
        [Display(DisplayName = "3")]
        public string THCutoff3 { get; set; }

        /// <summary>Tree height cutoff 4</summary>
        [Display(DisplayName = "4")]
        public string THCutoff4 { get; set; }

        /// <summary>Tree height cutoff 5</summary>
        [Display(DisplayName = "5")]
        public string THCutoff5 { get; set; }

        /// <summary>Tree height cutoff 6</summary>
        [Display(DisplayName = "6")]
        public string THCutoff6 { get; set; }
    }
}