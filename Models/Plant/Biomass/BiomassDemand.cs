using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF
{
    /// <summary>
    /// Class that holds the functions for calculating Structura, Metabolic and Storage demands
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassDemand : Model
    {
        /// <summary>The lag duration</summary>
        [Link]
        [Units("g/m2")]
        public IFunction Structural = null;
        /// <summary>The growth duration</summary>
        [Link]
        [Units("g/m2")]
        public IFunction Storage = null;
        /// <summary>The growth duration</summary>
        [Link]
        [Units("g/m2")]
        public IFunction Metabolic = null;
        
    }
}
