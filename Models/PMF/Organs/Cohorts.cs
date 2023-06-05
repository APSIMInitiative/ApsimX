using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Represents a perennial leaf cohort.
    /// </summary>
    [Serializable]
    public class PerennialLeafCohort
    {
        /// <summary>
        /// Age of the leaf.
        /// </summary>
        public double Age { get; set; } = 0;

        /// <summary>
        /// Leaf area.
        /// </summary>
        public double Area { get; set; } = 0;

        /// <summary>
        /// Area of dead leaf.
        /// </summary>
        public double AreaDead { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public bool IsSenesced { get; set; }

        /// <summary>
        /// Live biomass of the leaf.
        /// </summary>
        public Biomass Live { get; private set; } = new Biomass();

        /// <summary>
        /// Dead biomass of the leaf.
        /// </summary>
        /// <returns></returns>
        public Biomass Dead { get; private set; } = new Biomass();

        /// <summary>
        /// Senesced biomass.
        /// </summary>
        public Biomass Senesced { get; private set; } = new Biomass();
    }

    /// <summary>
    /// Encapsulates a collection of perennial leaf cohorts.
    /// </summary>
    [Serializable]
    public class Cohorts
    {
        /// <summary>
        /// The leaves in the cohort.
        /// </summary>
        private List<PerennialLeafCohort> leaves = new List<PerennialLeafCohort>();

        /// <summary>
        /// Running total of the live biomass of all leaves.
        /// </summary>
        private Biomass live = new Biomass();

        /// <summary>
        /// Running total of the dead biomass of all leaves.
        /// </summary>
        private Biomass dead = new Biomass();

        /// <summary>
        /// Total dead Lai.
        /// </summary>
        [Units("m^2/m^2")]
        public double LaiDead { get; private set; }

        /// <summary>
        /// Total live Lai.
        /// </summary>
        [Units("m^2/m^2")]
        public double Lai { get; private set; }

        /// <summary>
        /// Change total leaf area.
        /// </summary>
        /// <param name="value">New Lai value.</param>
        public void SetLai(double value)
        {
            if (Lai > 0)
            {
                var delta = Lai - value;
                var prop = delta / Lai;
                foreach (var L in leaves)
                {
                    var amountToRemove = L.Area * prop;
                    L.Area -= amountToRemove;
                    L.AreaDead += amountToRemove;
                }
                Lai -= value;
                LaiDead += value;
            }
        }

        /// <summary>
        /// Get the total live biomass of the cohort.
        /// </summary>
        public Biomass GetLive()
        {
            // Return a copy of the live biomass, so that the caller
            // can't modify it.
            return new Biomass(live);
        }

        /// <summary>
        /// Get the total dead biomass of the cohort.
        /// </summary>
        /// <returns></returns>
        public Biomass GetDead()
        {
            // Return a copy of the dead biomass, so that the caller
            // can't modify it.
            return new Biomass(dead);
        }

        /// <summary>
        /// Add new leaf material to the last leaf in the list.
        /// </summary>
        /// <param name="structuralMass">Structural biomass to add to the leaf.</param>
        /// <param name="storageMass">Storage biomass to add to the leaf.</param>
        /// <param name="structuralN">Sturctural N to add to the leaf.</param>
        /// <param name="storageN">Storage N to add to the leaf..</param>
        /// <param name="sla">Specific leaf area.</param>
        public void AddNewLeafMaterial(double structuralMass, double storageMass, double structuralN, double storageN, double sla)
        {
            foreach (Biomass biomass in new[] { leaves[leaves.Count - 1].Live, live })
            {
                biomass.StructuralWt += structuralMass;
                biomass.StorageWt += storageMass;
                biomass.StructuralN += structuralN;
                biomass.StorageN += storageN;
            }
            leaves[leaves.Count - 1].Area += (structuralMass + storageMass) * sla;
            Lai += (structuralMass + storageMass) * sla;
        }

        /// <summary>
        /// Reduce all live leaves' size by a given fraction.
        /// </summary>
        /// <param name="liveFraction">The fraction by which to reduce the size of live leaves.</param>
        /// <param name="deadFraction">The fraction by whith to reduce the size of dead leaves.</param>
        public void ReduceLeavesUniformly(double liveFraction, double deadFraction)
        {
            foreach (PerennialLeafCohort leaf in leaves)
            {
                leaf.Live.Multiply(liveFraction);
                leaf.Dead.Multiply(deadFraction);
                leaf.Area *= liveFraction;
                leaf.AreaDead *= deadFraction;
            }
            live.Multiply(liveFraction);
            dead.Multiply(deadFraction);
            Lai *= liveFraction;
            LaiDead *= deadFraction;
        }

        /// <summary>
        /// Reduce non-structural biomass in all leaves by the given fraction.
        /// </summary>
        /// <param name="fraction">Fraction of non-structural biomass to remove from each leaf.</param>
        public void ReduceNonStructuralWt(double fraction)
        {
            foreach (PerennialLeafCohort leaf in leaves)
            {
                leaf.Live.StorageWt *= fraction;
                leaf.Live.MetabolicWt *= fraction;
            }
            live.StorageWt *= fraction;
            live.MetabolicWt *= fraction;
        }

        /// <summary>
        /// Get the total value of all the leaves which match
        /// a given condition.
        /// </summary>
        /// <param name="predicate">The condition - value will be taken for all leaves matching this predicate.</param>
        /// <param name="selector">The property value of each leaf to be summed.</param>
        internal double SelectWhere(Func<PerennialLeafCohort, bool> predicate, Func<PerennialLeafCohort, double> selector)
        {
            return leaves.Where(predicate).Select(selector).Sum();
        }

        /// <summary>
        /// Senesce any leaves matching the given condition.
        /// </summary>
        /// <param name="predicate">Any leaves matching this predicate will be senesced.</param>
        public void SenesceWhere(Func<PerennialLeafCohort, bool> predicate)
        {
            foreach (PerennialLeafCohort leaf in leaves.Where(predicate))
            {
                leaf.IsSenesced = true;

                if (leaf.Live.Wt > 0)
                {
                    // Move leaf biomass from live to dead pool.
                    live.Subtract(leaf.Live);
                    dead.Add(leaf.Live);

                    // Update the leaf's internal biomass pools.
                    leaf.Dead.Add(leaf.Live);
                    leaf.Senesced.SetTo(leaf.Live);
                    leaf.Live.Clear();

                    // Move leaf area into dead area.
                    leaf.AreaDead += leaf.Area;
                    LaiDead += leaf.Area;
                    Lai -= leaf.Area;
                    leaf.Area = 0;
                }
            }
        }

        /// <summary>
        /// Detach any leaves older than the specified age.
        /// </summary>
        /// <param name="predicate">Any leaves matching this predicate will be deatched..</param>
        public Biomass DetachWhere(Func<PerennialLeafCohort, bool> predicate)
        {
            Biomass detached = new Biomass();

            foreach (PerennialLeafCohort leaf in leaves.Where(predicate))
            {
                detached.Add(leaf.Dead);

                // Need to check this. The assumption here is that leaves will
                // be senesced before they are detached. If this assumption
                // doesn't hold up, mass balance will be violated.
                if (leaf.IsSenesced)
                {
                    LaiDead -= leaf.AreaDead;
                    dead.Subtract(leaf.Senesced);
                }
            }
            leaves.RemoveAll(l => predicate(l));
            return detached;
        }

        /// <summary>
        /// Kill all leaves by a given fraction.
        /// </summary>
        /// <param name="fraction">The fraction to be removed from the leaves.</param>
        public void KillLeavesUniformly(double fraction)
        {
            Biomass loss = new Biomass();
            LaiDead += Lai * fraction;
            foreach (PerennialLeafCohort leaf in leaves)
            {
                loss.SetTo(leaf.Live);
                loss.Multiply(fraction);
                dead.Add(loss);
                live.Subtract(loss);
                leaf.Dead.Add(loss);
                leaf.Live.Subtract(loss);
                leaf.AreaDead += leaf.Area * fraction;
                leaf.Area *= (1 - fraction);
            }
            Lai *= (1 - fraction);
        }

        /// <summary>
        /// Add a leaf to the cohort.
        /// </summary>
        /// <param name="initialMass">Initial mass of the leaf.</param>
        /// <param name="minNConc">Minimum N concentration of the new leaf.</param>
        /// <param name="maxNConc">Maximum N concentration in the new leaf.</param>
        /// <param name="sla">Specific leaf area of the new leaf.</param>
        public void AddLeaf(double initialMass, double minNConc, double maxNConc, double sla)
        {
            leaves.Add(new PerennialLeafCohort());
            if (leaves.Count == 1)
            {
                AddNewLeafMaterial(initialMass,
                                   storageMass: 0,
                                   structuralN: initialMass * minNConc,
                                   storageN: initialMass * (maxNConc - minNConc),
                                   sla: sla);
            }
        }

        /// <summary>
        /// Clear the leaf cohorts.
        /// </summary>
        public void Clear()
        {
            leaves.Clear();
            live.Clear();
            dead.Clear();
            Lai = 0;
            LaiDead = 0;
        }

        /// <summary>
        /// Increase age of all leaves by the given amount.
        /// </summary>
        /// <param name="delta">Amount by which to increase leaf age.</param>
        public void IncreaseAge(double delta)
        {
            foreach (PerennialLeafCohort L in leaves)
                L.Age += delta;
        }

        /// <summary>
        /// Retranslocate biomass from leaves.
        /// </summary>
        /// <param name="removal">Amount which needs to be retranslocated.</param>
        public void DoBiomassRetranslocation(double removal)
        {
            if (removal == 0)
                return;

            foreach (PerennialLeafCohort leaf in leaves)
            {
                double delta = Math.Min(leaf.Live.StorageWt, removal);
                if (delta > 0)
                {
                    leaf.Live.StorageWt -= delta;
                    live.StorageWt -= delta;
                    removal -= delta;
                }
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
            if (removal == 0)
                return;

            foreach (PerennialLeafCohort leaf in leaves)
            {
                double delta = Math.Min(leaf.Live.StorageN, removal);
                if (delta > 0)
                {
                    leaf.Live.StorageN -= delta;
                    live.StorageN -= delta;
                    removal -= delta;
                }
            }
            if (MathUtilities.IsGreaterThan(removal, 0))
                throw new Exception("Insufficient Storage N to account for Retranslocation and Reallocation in Perrenial Leaf");
        }
    }
}
