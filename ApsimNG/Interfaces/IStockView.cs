// -----------------------------------------------------------------------
// <copyright file="IStockView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Interfaces
{
    using System;
    using Models.GrazPlan;


    /// <summary>
    /// Interface for a supplement view.
    /// </summary>
    public interface IStockView
    {
        TStockGeno[] Genotypes { get; set; }

        void SetValues();
    }

 
}