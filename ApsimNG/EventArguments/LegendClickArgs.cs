// -----------------------------------------------------------------------
// <copyright file="LegendClickArgs.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.EventArguments
{
    using System;

    /// <summary>
    /// Arguments for a legend click
    /// </summary>
    public class LegendClickArgs : EventArgs
    {
        public int seriesIndex { get; set; }
        public bool controlKeyPressed { get; set; }
    }
}
