using System;
using Models.Core;
namespace Models.Sensitivity
{
    /// <summary>A encapsulation of a parameter to analyse</summary>
    [Serializable]
    public class Parameter
    {
        /// <summary>Name of parameter</summary>
        [Display]
        public string Name { get; set; }

        /// <summary>Model path of parameter</summary>
        [Display]
        public string Path { get; set; }

        /// <summary>Lower bound of parameter</summary>
        [Display]
        public double LowerBound { get; set; }

        /// <summary>Upper bound of parameter</summary>
        [Display]
        public double UpperBound { get; set; }
    }
}
