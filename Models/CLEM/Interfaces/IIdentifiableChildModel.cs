using System;
using System.Collections.Generic;
using System.Text;
using Models.CLEM.Resources;
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

        /// <summary>
        /// Type of measure
        /// </summary>
        string Units { get; set; }

        /// <summary>
        /// Determines whether a shortfall of this child request will affect the activity if possible
        /// </summary>
        bool ShortfallAffectsActivity { get; set; }

        /// <summary>
        /// Get the resource requests from the identifiable child
        /// </summary>
        /// <param name="activityMetric">The current metric provided by the Activity</param>
        /// <returns></returns>
        List<ResourceRequest> GetResourceRequests(double activityMetric);

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers as stings</returns>
        List<string> ParentSuppliedIdentifiers();

        /// <summary>
        /// A method to return the list of unit types relavent to the parent activity
        /// </summary>
        /// <returns>A list of units as stings</returns>
        List<string> ParentSuppliedUnits();

    }
}

