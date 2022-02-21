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
        /// <typeparam name="T">Identifiable child model type</typeparam>
        /// <returns>A LabelsForIdentifiableChildren containing all labels</returns>
        public LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels<T>() where T : IIdentifiableChildModel;
    }
}
