using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Interfaces
{
    internal interface ICanHandleIdentifiableChildModels
    {
        /// <summary>
        /// A method to get a list of activity specified labels for a generic type T 
        /// </summary>
        /// <param name="type">The type of model to return labels for</param>
        /// <returns>A LabelsForIdentifiableChildren containing all labels</returns>
        public LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels(string type);
    }
}
