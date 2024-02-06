﻿using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant weight tracking
    /// </summary>
    public class RuminantInfoWeight
    {
        private double live = 0;

        /// <summary>
        /// Track protein weight (kg)
        /// </summary>
        public RuminantTrackingItem Protein { get; set; } = new();

        /// <summary>
        /// Track fat weight (kg)
        /// </summary>
        public RuminantTrackingItem Fat { get; set; } = new();

        /// <summary>
        /// Track base weight (kg)
        /// </summary>
        public RuminantTrackingItem Base { get; set; } = new();

        /// <summary>
        /// Track conceptus weight (kg)
        /// </summary>
        public RuminantTrackingItem Conceptus { get; set; } = new();

        /// <summary>
        /// Track wool weight (kg)
        /// </summary>
        public RuminantTrackingItem Wool { get; set; } = new();

        /// <summary>
        /// Track wool weight (kg)
        /// </summary>
        public RuminantTrackingItem Cashmere { get; set; } = new();

        /// <summary>
        /// Current weight of individual  (kg)
        /// Live is calculated on change in base weight that assumes conceptus and wool weight have been updated for the time step
        /// This should be true as these are either doen early (birth) on activitiy (shear) or in the growth model before weight gain.
        /// </summary>
        public double Live { get { return live; } }

        /// <summary>
        /// Previous weight of individual  (kg)
        /// </summary>
        public double Previous { get { return Base.Previous + Conceptus.Previous + Wool.Previous; } }

        /// <summary>
        /// Current weight of individual  (kg)
        /// </summary>
        public double Gain { get { return Live - Previous; } }

        /// <summary>
        /// Empty body mass change  (kg)
        /// </summary>
        public double EmptyBodyMassChange { get; private set; }

        /// <summary>
        /// Highest weight attained  (kg)
        /// </summary>
        public double HighestAttained { get; private set; }

        /// <summary>
        /// Adult equivalent
        /// </summary>
        public double AdultEquivalent { get; private set; }

        /// <summary>
        /// Weight at birth (kg)
        /// </summary>
        public double AtBirth { get; private set; }

        /// <summary>
        /// Normalised weight for current age
        /// </summary>
        public double NormalisedForAge { get; private set; }

        /// <summary>
        /// Set the normal weight for age of individual
        /// </summary>
        /// <param name="normalWeight">Normal weight</param>
        public void SetNormalWeightForAge(double normalWeight)
        {
            NormalisedForAge = normalWeight;
        }

        /// <summary>
        /// The current live weight as a proportion of highest weight achieved
        /// </summary>
        public double ProportionOfHighWeight { get { return HighestAttained == 0 ? 1 : Live / HighestAttained; } }

        /// <summary>
        /// Standard reference weight
        /// </summary>
        public double StandardReferenceWeight { get; private set; }

        /// <summary>
        /// Set the standard reference weight of individual
        /// </summary>
        /// <param name="srw">SRW to assign</param>
        public void SetStandardReferenceWeight(double srw)
        {
            StandardReferenceWeight = srw;
        }

        /// <summary>
        /// Relative size based on highest weight achieved (High weight / standard reference weight)
        /// </summary>
        public double RelativeSizeByHighWeight { get { return HighestAttained / StandardReferenceWeight; } }

        /// <summary>
        /// Relative condition (base weight / normalised weight)
        /// Does not include conceptus weight in pregnant females.
        /// </summary>
        public double RelativeCondition { get { return Base.Amount / NormalisedForAge; } }

        /// <summary>
        /// Body condition
        /// </summary>
        [FilterByProperty]
        public double BodyCondition { get { return Base.Amount / NormalisedForAge; } }

        /// <summary>
        /// The current weight as a proportion of High weight achieved
        /// </summary>
        [FilterByProperty]
        public double ProportionOfNormalisedWeight { get { return NormalisedForAge == 0 ? 1 : Live / NormalisedForAge; } }

        /// <summary>
        /// Relative size (normalised weight / standard reference weight)
        /// </summary>
        [FilterByProperty]
        public double RelativeSize { get { return  NormalisedForAge / StandardReferenceWeight; }  }

        /// <summary>
        /// Adjust weight
        /// </summary>
        /// <param name="change">The amount to change</param>
        /// <param name="individual">THe individual to change</param>
        public void Adjust(double change, Ruminant individual)
        {
            EmptyBodyMassChange = change / individual.Parameters.General.EBW2LW_CG18;

            Base.Adjust(change);

            UpdateLiveWeight();

            if (individual.Sex == Sex.Female)
            {
                (individual as RuminantFemale).UpdateHighWeightWhenNotPregnant(Live);
            }

            AdultEquivalent = Math.Pow(Live, 0.75) / Math.Pow(individual.Parameters.General.BaseAnimalEquivalent, 0.75);
            HighestAttained = Math.Max(HighestAttained, Live);
        }

        /// <summary>
        /// Method to recalculate the live weight based on current base, conceptus and wool weights.
        /// </summary>
        public void UpdateLiveWeight()
        {
            live = Base.Amount + Conceptus.Amount + Wool.Amount;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInfoWeight(double weightAtBirth)
        {
            AtBirth = weightAtBirth;
        }
    }
}
