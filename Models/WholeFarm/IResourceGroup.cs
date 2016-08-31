using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models.WholeFarm
{
    /// <summary>
    /// Interface for the parent of a Resource Type.
    /// </summary>
    public interface IResourceGroup
    {

        //event EventHandler ResourceChanged;


        /// <summary>
        /// Initialise the variables that store the current state of the resource.
        /// </summary>
        /// <param name="Name"></param>
        IResourceType GetByName(string Name);


        /// <summary>
        /// Add this Amount to the existing Amount.
        /// </summary>
        /// <param name="Names"></param>
        /// <param name="AddAmounts"></param>
        void Add(string[] Names, double[] AddAmounts);

        /// <summary>
        /// Remove this Amount to the existing Amount
        /// </summary>
        /// <param name="Names"></param>
        /// <param name="RemoveAmounts">nb. This is a positive value not a negative value.</param>
        void Remove(string[] Names, double[] RemoveAmounts);

        /// <summary>
        /// Set the amount to this new value.
        /// </summary>
        /// <param name="Names"></param>
        /// <param name="NewAmounts"></param>
        void Set(string[] Names, double[] NewAmounts);

    }
}
