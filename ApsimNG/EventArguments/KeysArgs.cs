// -----------------------------------------------------------------------
// <copyright file="KeysArgs.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.EventArguments
{
    using System;

    /// <summary>
    /// The editor view asks the presenter for context items. This structure
    /// is used to do that
    /// </summary>
    public class KeysArgs : EventArgs
    {
        /// <summary>
        /// The name of the object that needs context items.
        /// </summary>
        public ConsoleKey Keys;
    } 
}
