using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// A CLEM model able to be identified by the parent given a user specified identifier
    /// </summary>
    public interface IIdentifiableChildModel: IModel
    {
        /// <summary>
        /// Identifier of this component 
        /// </summary>
        string Identifier { get; set; }
    }
}

