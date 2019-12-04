// -----------------------------------------------------------------------
// <copyright file="IStockView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Interfaces
{
    using System;
    using Models.GrazPlan;

    public delegate void NormalWeightDelegate(AnimalParamSet mainParams,
                                                  SingleGenotypeInits[] BreedInfo,
                                                  int iGenotype,
                                                  GrazType.ReproType Repro,
                                                  int AgeDays,
                                                  double dLowBC, double dHighBC,
                                                  out double LowWt, out double HighWt);

    /// <summary>
    /// Interface for a supplement view.
    /// </summary>
    public interface IStockView
    {
        SingleGenotypeInits[] Genotypes { get; set; }

        void SetValues();
        void SetGenoParams(AnimalParamSet animalParams);
        
        event NormalWeightDelegate OnCalcNormalWeight;

        event EventHandler<GenotypeInitArgs> GetGenoParams;
    }

    /// <summary>
    /// 
    /// </summary>
    public class GenotypeInitArgs : EventArgs
    {
        public AnimalParamSet ParamSet { get; set; }
        public SingleGenotypeInits[] Genotypes { get; set; }
        public int Index { get; set; }
    }

}