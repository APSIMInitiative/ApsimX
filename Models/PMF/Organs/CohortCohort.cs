using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Encapsulates a collection of perennial leaf cohorts.
    /// </summary>
    [Serializable]
    public class CohortCohort
    {
        [Serializable]
        private class PerrenialLeafCohort
        {
            public double Age { get; set; } = 0;
            public double Area { get; set; } = 0;
            public double AreaDead { get; set; } = 0;
            public Biomass Live = new Biomass();
            public Biomass Dead = new Biomass();
        }

        private List<PerrenialLeafCohort> Leaves = new List<PerrenialLeafCohort>();

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get
            {
                double lai = 0;
                foreach (PerrenialLeafCohort L in Leaves)
                    lai = lai + L.AreaDead;
                return lai;
            }
        }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                double lai = 0;
                foreach (PerrenialLeafCohort L in Leaves)
                    lai = lai + L.Area;
                return lai;
            }
            set
            {
                var totalLeafArea = Leaves.Sum(x => x.Area);
                if (totalLeafArea > 0)
                {
                    var delta = totalLeafArea - value;
                    var prop = delta / totalLeafArea;
                    foreach (var L in Leaves)
                    {
                        var amountToRemove = L.Area * prop;
                        L.Area -= amountToRemove;
                        L.AreaDead += amountToRemove;
                    }
                }
            }
        }

        // Update methods

        /// <summary>
        /// Add new leaf material to the last leaf in the list.
        /// </summary>
        /// <param name="StructuralWt"></param>
        /// <param name="StorageWt"></param>
        /// <param name="StructuralN"></param>
        /// <param name="StorageN"></param>
        /// <param name="SLA"></param>
        public void AddNewLeafMaterial(double StructuralWt, double StorageWt, double StructuralN, double StorageN, double SLA)
        {
            Leaves[Leaves.Count - 1].Live.StructuralWt += StructuralWt;
            Leaves[Leaves.Count - 1].Live.StorageWt += StorageWt;
            Leaves[Leaves.Count - 1].Live.StructuralN += StructuralN;
            Leaves[Leaves.Count - 1].Live.StorageN += StorageN;
            Leaves[Leaves.Count - 1].Area += (StructuralWt + StorageWt) * SLA;
        }

        /// <summary>
        /// Reduce all live leaves' size by a given fraction.
        /// </summary>
        /// <param name="fraction">The fraction by which to reduce the size of the leaves.</param>
        public void ReduceLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Live.Multiply(fraction);
                L.Area *= fraction;
            }
        }

        /// <summary>
        /// Reduce the size of all dead leaves by a given fraction.
        /// </summary>
        /// <param name="fraction">The fraction by which to reduce the dead leaves' size.</param>
        public void ReduceDeadLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Dead.Multiply(fraction);
                L.AreaDead *= fraction;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fraction"></param>
        public void RespireLeafFraction(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Live.StorageWt *= (1 - fraction);
                L.Live.MetabolicWt *= (1 - fraction);
            }
        }

        /// <summary>
        /// Get the senescing leaf biomass.
        /// </summary>
        /// <param name="residenceTime">Leaf residence time.</param>
        public Biomass GetSenescingLeafBiomass(double residenceTime)
        {
            Biomass Senescing = new Biomass();
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= residenceTime)
                    Senescing.Add(L.Live);
            return Senescing;
        }

        /// <summary>
        /// Do leaf senescence.
        /// </summary>
        public void SenesceLeaves(double residenceTime)
        {
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= residenceTime)
                {
                    L.Dead.Add(L.Live);
                    L.AreaDead += L.Area;
                    L.Live.Clear();
                    L.Area = 0;
                }
        }

        /// <summary>
        /// Kill all leaves by a given fractin.
        /// </summary>
        /// <param name="fraction">The fraction to be removed from the leaves.</param>
        public void KillLeavesUniformly(double fraction)
        {
            Biomass Loss = new Biomass();

            foreach (PerrenialLeafCohort L in Leaves)
            {            
                Loss.SetTo(L.Live);
                Loss.Multiply(fraction);
                L.Dead.Add(Loss);
                L.Live.Subtract(Loss);
                L.AreaDead += L.Area * fraction;
                L.Area *= (1 - fraction);
            }
        }

        /// <summary>
        /// Detach all leaves.
        /// </summary>
        /// <returns></returns>
        public Biomass DetachLeaves(double residenceTime, double detachmentTime)
        {
            Biomass Detached = new Biomass();

            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= (residenceTime + detachmentTime))
                    Detached.Add(L.Dead);
            Leaves.RemoveAll(L => L.Age >= (residenceTime + detachmentTime));
            return Detached;
        }

        /// <summary>
        /// Add a leaf to the cohort.
        /// </summary>
        public void AddLeaf(double initialMass, double minNConc, double maxNConc, double sla)
        {
            Leaves.Add(new PerrenialLeafCohort());
            if (Leaves.Count == 1)
            {
                AddNewLeafMaterial(initialMass,
                                   StorageWt: 0,
                                   StructuralN: initialMass * minNConc,
                                   StorageN: initialMass * (maxNConc - minNConc),
                                   SLA: sla);
            }
        }

        /// <summary>
        /// Clear the leaf cohorts.
        /// </summary>
        public void Clear()
        {
            Leaves.Clear();
        }

        /// <summary>
        /// Increase age of all leaves by the given amount.
        /// </summary>
        /// <param name="delta">Amount by which to increase leaf age.</param>
        public void IncreaseAge(double delta)
        {
            foreach (PerrenialLeafCohort L in Leaves)
                L.Age+=delta;
        }

        /// <summary>
        /// Retranslocate biomass from leaves.
        /// </summary>
        /// <param name="removal">Amount which needs to be retranslocated.</param>
        public void DoBiomassRetranslocation(double removal)
        {
            foreach (PerrenialLeafCohort leaf in Leaves)
            {
                double delta = Math.Min(leaf.Live.StorageWt, removal);
                leaf.Live.StorageWt -= delta;
                removal -= delta;
            }
            if (MathUtilities.IsGreaterThan(removal, 0))
                throw new Exception("Insufficient Storage N to account for Retranslocation and Reallocation in Perrenial Leaf");
        }

        /// <summary>
        /// Retranslocate nitrogen from leaves.
        /// </summary>
        /// <param name="removal">Amount of nitrogen to be retranslocated.</param>
        public void DoNitrogenRetranslocation(double removal)
        {
            foreach (PerrenialLeafCohort leaf in Leaves)
            {
                double delta = Math.Min(leaf.Live.StorageN, removal);
                leaf.Live.StorageN -= delta;
                removal -= delta;
            }
            if (MathUtilities.IsGreaterThan(removal, 0))
                throw new Exception("Insufficient Storage N to account for Retranslocation and Reallocation in Perrenial Leaf");
        }
    }
}