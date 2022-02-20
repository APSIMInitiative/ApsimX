using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Interfaces
{
    internal interface ICanHandleIdentifiableChildModels
    {
        /// <summary>
        /// A method to return the list of identifiers provided by the parent activity for the given identifiable child model type
        /// </summary>
        /// <typeparam name="T">Type of identifiable child model</typeparam>
        /// <returns>List of identifiers provided</returns>
        List<string> IdentifiableChildModelIdentifiers<T>() where T : IIdentifiableChildModel;
    }
}
