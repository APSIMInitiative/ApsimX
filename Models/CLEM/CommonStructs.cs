using System;
using System.Collections.Generic;

namespace Models.CLEM
{
    /// <summary>
    /// A list of labels used for communication between an activity and companion models
    /// </summary>
    [Serializable]
    public struct LabelsForCompanionModels
    {
        /// <summary>
        /// List of available identifiers
        /// </summary>
        public List<string> Identifiers;
        /// <summary>
        /// List of available measures
        /// </summary>
        public List<string> Measures;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="measures"></param>
        public LabelsForCompanionModels(List<string> identifiers, List<string> measures)
        {
            Identifiers = identifiers;
            Measures = measures;
        }
    }

}