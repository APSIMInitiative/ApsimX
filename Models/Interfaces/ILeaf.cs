// -----------------------------------------------------------------------
// <copyright file="ICanopy.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System;

    /// <summary>This interface describes interface for leaf interaction with Structure.</summary>
    public interface IHasStructure
    {
        /// <summary>
        /// 
        /// </summary>
        bool CohortsInitialised { get; }
        /// <summary>
        /// 
        /// </summary>
        double PlantAppearedLeafNo { get; }
        /// <summary>
        /// 
        /// </summary>
        double InitialisedCohortNo { get; }
        /// <summary>
        /// 
        /// </summary>
        double AppearedCohortNo { get; }

        /// <summary>
        /// 
        /// </summary>
        int TipsAtEmergence { get; }

        /// <summary>
        /// 
        /// </summary>
        int CohortsAtInitialisation { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ProportionRemoved"></param>
        void DoThin(double ProportionRemoved);

        /// <summary>Apex number by age</summary>
        /// <param name="age">Threshold age</param>
        double ApexNumByAge(double age);
    }
}