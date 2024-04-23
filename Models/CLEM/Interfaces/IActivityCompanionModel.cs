using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// A CLEM model able to be identified by the parent given a user specified identifier
    /// </summary>
    public interface IActivityCompanionModel : IModel
    {
        /// <summary>
        /// Identifier of this component 
        /// </summary>
        string Identifier { get; set; }

        /// <summary>
        /// Type of measure
        /// </summary>
        string Measure { get; set; }

        /// <summary>
        /// Method to prepare for the times step after parent activity preparation 
        /// </summary>
        public void PrepareForTimestep();

        /// <summary>
        /// Method to provide the resource requests from this comapnion model and pass to parent for processing
        /// </summary>
        /// <returns>A list of resource requests</returns>
        List<ResourceRequest> RequestResourcesForTimestep(double argument = 0);

        /// <summary>
        /// Perform a the task including creating resources by the companion model
        /// </summary>
        /// <returns></returns>
        void PerformTasksForTimestep(double argument = 0);

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers as stings</returns>
        List<string> ParentSuppliedIdentifiers();

        /// <summary>
        /// A method to return the list of unit types relavent to the parent activity
        /// </summary>
        /// <returns>A list of units as stings</returns>
        List<string> ParentSuppliedMeasures();
    }
}

