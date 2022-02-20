using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Interfaces
{
    internal interface ICanHandleIdentifiableChildModels
    {
        /// <summary>
        /// A method to get a list of activity specific identifiers for a given type of Identifiable child model
        /// </summary>
        /// <typeparam name="T">Identifiable child model type</typeparam>
        /// <returns>A list of identifiers as strings</returns>
        List<string> DefineIdentifiableChildModelIdentifiers<T>() where T : IIdentifiableChildModel;
    }
}
