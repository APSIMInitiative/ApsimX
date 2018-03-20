// -----------------------------------------------------------------------
// <copyright file="ICanopy.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System;

    /// <summary>This interface describes interface for leaf interaction with Structure.</summary>
    public interface ILeaf
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
        int InitialisedCohortNo { get; }
        /// <summary>
        /// 
        /// </summary>
        int AppearedCohortNo { get; }

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

        /// <summary>
        /// Method to remove 
        /// </summary>
       void RemoveHighestLeaf();
        
        /// <summary>
        /// Method to zero leaf numbembers
        /// </summary>
       void Reset();

        /// <summary>
        /// Then number of cohorts on the apex that are yet to expand
        /// </summary>
        int ApicalCohortNo { get; }
    }
}