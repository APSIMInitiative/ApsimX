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
        StockGeno[] Genotypes { get; set; }

        void SetValues();
        void SetGenoParams(TAnimalParamSet animalParams);

        event EventHandler<GenotypeInitArgs> GetGenoParams;
    }

    public class GenotypeInitArgs : EventArgs
    {
        public TAnimalParamSet ParamSet;
        public StockGeno[] Genotypes;
        public int index;
    }

}