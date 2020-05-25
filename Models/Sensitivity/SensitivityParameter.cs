using System;

namespace Models.Sensitivity
{
    /// <summary>A encapsulation of a parameter to analyse</summary>
    [Serializable]
    public class Parameter
    {
        /// <summary>Name of parameter</summary>
        public string Name;

        /// <summary>Model path of parameter</summary>
        public string Path;

        /// <summary>Lower bound of parameter</summary>
        public double LowerBound;

        /// <summary>Upper bound of parameter</summary>
        public double UpperBound;
    }
}
