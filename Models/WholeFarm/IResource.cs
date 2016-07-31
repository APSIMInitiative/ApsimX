using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models.WholeFarm
{
    interface IResource
    {

        //event EventHandler ResourceChanged;

        /// <summary>
        /// Add this Amount to the existing Amount.
        /// </summary>
        /// <param name="AddAmount"></param>
        void Add(double AddAmount);

        /// <summary>
        /// Remove this Amount to the existing Amount
        /// </summary>
        /// <param name="RemoveAmount">nb. This is a positive value not a negative value.</param>
        void Remove(double RemoveAmount);

        /// <summary>
        /// Set the amount to this new value.
        /// </summary>
        void Set(double NewAmount);

        /// <summary>
        /// Initialise the variables that store the current state of the resource.
        /// </summary>
        void Initialise();
    }
}
