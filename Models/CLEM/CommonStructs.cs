using System;
using System.Collections.Generic;

namespace Models.CLEM
{
    /// <summary>
    /// A list of labels used for communication between an activity and companion models
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="identifiers">List of available identifiers</param>
    /// <param name="measures">List of available measures</param>
    [Serializable]
    public struct LabelsForCompanionModels(List<string> identifiers, List<string> measures)
    {
        /// <summary>
        /// List of available identifiers
        /// </summary>
        public List<string> Identifiers = identifiers;
        
        /// <summary>
        /// List of available measures
        /// </summary>
        public List<string> Measures = measures;
    }

}