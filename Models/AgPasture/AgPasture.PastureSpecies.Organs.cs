//-----------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.Organs.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Models.Core;
using Models.Soils;
using Models.PMF;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{

    #region Plant organs and tissues  ---------------------------------------------------------------------------------

    /// <summary>
    /// Defines a basic generic above-ground organ of a pasture species
    /// </summary>
    /// <remarks>
    /// Each organ (leaf, stem, etc) is defined as a collection of tissues (limited to four)
    /// Leaves and stems have four tissues, stolons three and roots only one.
    /// Three tissues are alive (growing, developed and mature), the fourth represents dead material
    /// Each organ has its own nutrient concentration thresholds (min, max, opt)
    /// Each tissue has a record of DM and nutrient amounts, from which N concentration is computed
    /// The organ has methods to output DM and nutrient as total, live, and dead tissues
    /// The tissue has method to output is digestibility
    /// </remarks>
    [Serializable]
    public class GenericAboveGroundOrgan
    {
        /// <summary>Constructor, Initialise tissues</summary>
        public GenericAboveGroundOrgan(int numTissues)
        {
            DoInitialisation(numTissues);
        }

        /// <summary>Actuallly initialise the tissues</summary>
        internal void DoInitialisation(int numTissues)
        {
            // generally 4 tisues, three live, last is dead material
            TissueCount = numTissues;
            Tissue = new GenericTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new GenericTissue();
        }

        /// <summary>the collection of tissues for this organ</summary>
        internal GenericTissue[] Tissue { get; set; }

        #region Organ Properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>Number of tissue pools to create</summary>
        internal int TissueCount;

        /// <summary>N concentration for optimal growth [g/g]</summary>
        internal double NConcOptimum
        {
            get { return Tissue[0].NConcOptimum; }
            set
            {
                foreach (GenericTissue tissue in Tissue)
                    tissue.NConcOptimum = value;
            }
        }

        /// <summary>Maximum N concentration, for luxury uptake [g/g]</summary>
        internal double NConcMaximum = 0.06;

        /// <summary>Minimum N concentration, structural N [g/g]</summary>
        internal double NConcMinimum = 0.012;

        /// <summary>Fraction of new growth that is soluble carbohydrate, i.e. sugars [0-1]</summary>
        internal double SugarFraction { get; set; } = 0.0;

        /// <summary>The total dry matter in this organ [g/m^2]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the live (green) tissues [g/m^2]</summary>
        internal double DMGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue [g/m^2]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the live (green) tissues [g/m^2]</summary>
        internal double NGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>The average N concentration in this organ [g/g]</summary>
        internal double NconcTotal
        {
            get
            {
                return MathUtilities.Divide(NTotal, DMTotal, 0.0);
            }
        }

        /// <summary>The average N concentration in the live tissues [g/g]</summary>
        internal double NconcGreen
        {
            get
            {
                return MathUtilities.Divide(NGreen, DMGreen, 0.0);
            }
        }

        /// <summary>The average N concentration in dead tissues [g/g]</summary>
        internal double NconcDead
        {
            get
            {
                return MathUtilities.Divide(NDead, DMDead, 0.0);
            }
        }

        /// <summary>The amount of N available for remobilisation from senescence [g/g]</summary>
        internal double NRemobilisable = 0.0;

        /// <summary>The amount of luxury N available for remobilisation [g/g]</summary>
        internal double NRemobilisableLuxury
        {
            get
            {
                double luxNAmount = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    luxNAmount += Tissue[t].NLuxury;
                }

                return luxNAmount;
            }
        }

        /// <summary>The average digestibility of all biomass for this organ [g/g]</summary>
        internal double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;
                }

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>The average digestibility of live biomass for this organ [g/g]</summary>
        internal double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;
                }

                return MathUtilities.Divide(digestableDM, DMGreen, 0.0);
            }
        }

        /// <summary>The average digestibility of dead biomass for this organ [g/g]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DigestibilityDead
        {
            get { return Tissue[TissueCount - 1].Digestibility; }
        }

        /// <summary>Reset</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < TissueCount - 1; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tiisue)</summary>
        /// <param name="fraction">Fraction of organ tissues to kill</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (fraction < 1.0)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    Tissue[TissueCount - 1].DM += Tissue[t].DM * fraction;
                    Tissue[TissueCount - 1].Namount += Tissue[t].Namount * fraction;
                    Tissue[t].DM *= fractionRemaining;
                    Tissue[t].Namount *= fractionRemaining;
                }
            }
            else
            {
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    Tissue[TissueCount - 1].DM += Tissue[t].DM;
                    Tissue[TissueCount - 1].Namount += Tissue[t].Namount;
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        internal double[] DoTissueTurnover(double[] turnoverRate)
        {
            double[] fromToDM = new double[TissueCount];
            double[] fromToN = new double[TissueCount];
            double[] detachment = new double[2];

            // get amounts turned over
            for (int t = 0; t < TissueCount; t++)
            {
                fromToDM[t] = Tissue[t].DM * turnoverRate[t];
                fromToN[t] = fromToDM[t] * Tissue[t].Nconc;
                if (t == TissueCount - 2)
                {
                    // turnover from live to dead only uses minimum conc (remaining is remobilisable)
                    NRemobilisable = fromToDM[t] * (Tissue[t].Nconc - NConcMinimum);
                    fromToN[t] -= NRemobilisable;
                }
            }

            // update tissue pools (note that growth goes into tissue 0 and it has been considered already)
            Tissue[0].DM -= fromToDM[0];
            Tissue[0].Namount -= fromToN[0];
            for (int t = 1; t < TissueCount; t++)
            {
                Tissue[t].DM += fromToDM[t - 1] - fromToDM[t];
                Tissue[t].Namount += fromToN[t - 1] - fromToN[t];
            }

            // get the amount detached today
            detachment[0] = fromToDM[TissueCount - 1];
            detachment[1] = fromToN[TissueCount - 1];

            return detachment;
        }

        #endregion

        /// <summary>Defines a generic plant tissue</summary>
        internal class GenericTissue
        {
            /// <summary>The dry matter amount [g/m^2]</summary>
            internal double DM { get; set; } = 0.0;

            /// <summary>The N content [g/m^2]</summary>
            internal double Namount { get; set; } = 0.0;

            /// <summary>The N content [g/m^2]</summary>
            internal double NConcOptimum { get; set; } = 0.04;

            /// <summary>Minimum N concentration, structural N [g/g]</summary>
            internal double FractionNLuxuryRemobilisable = 0.0;

            /// <summary>The P content [g/m^2]</summary>
            internal double Pamount { get; set; } = 0.0;

            /// <summary>The nitrogen concentration [g/g]</summary>
            internal double Nconc
            {
                get { return MathUtilities.Divide(Namount, DM, 0.0); }
                set { Namount = value * DM; }
            }

            /// <summary>The nitrogen amount above optimum concentration [g/g]</summary>
            internal double NLuxury
            {
                get { return Math.Max(0.0, FractionNLuxuryRemobilisable * ((DM * NConcOptimum) - Namount)); }
            }

            /// <summary>The phosphorus concentration [g/g]</summary>
            internal double Pconc
            {
                get { return MathUtilities.Divide(Pamount, DM, 0.0); }
                set { Pamount = value * DM; }
            }

            /// <summary>The digestibility of cell walls [0-1]</summary>
            internal double DigestibilityCellWall { get; set; } = 0.5;

            /// <summary>The digestibility of proteins [0-1]</summary>
            internal double DigestibilityProtein { get; set; } = 1.0;

            /// <summary>The amount of soluble carbohydrate, i.e. sugars [g/m^2]</summary>
            internal double DMSugar { get; set; } = 0.0;

            /// <summary>The dry matter amount [g/g]</summary>
            /// <remarks>Digestibility of sugars is assumed to be 100%</remarks>
            internal double Digestibility
            {
                get
                {
                    double tissueDigestibility = 0.0;
                    if (DM > 0.0)
                    {
                        double fractionSugar = DMSugar / DM;
                        double cnTissue = DM * CarbonFractionInDM / Namount;
                        double ratio1 = CNratioCellWall / cnTissue;
                        double ratio2 = CNratioCellWall / CNratioProtein;
                        double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                        double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                        tissueDigestibility = fractionSugar
                                            + (fractionProtein * DigestibilityProtein)
                                            + (fractionCellWall * DigestibilityCellWall);
                    }

                    return tissueDigestibility;
                }
            }

            #region Constants  --------------------------------------------------------------------

            /// <summary>Average carbon content in plant dry matter</summary>
            const double CarbonFractionInDM = 0.4;

            /// <summary>The C:N ratio of protein</summary>
            const double CNratioProtein = 3.5;

            /// <summary>The C:N ratio of cell wall</summary>
            const double CNratioCellWall = 100.0;

            #endregion
        }
    }

    /// <summary>
    /// Defines a generic root organ of a pasture species
    /// </summary>
    /// <remarks>
    /// Contains the same properties of a BaseOrgan, extended by adding N thresholds
    /// and especially by defining the tissues as arrays (to store values by soil layer)
    /// </remarks>
    [Serializable]
    internal class GenericBelowGroundOrgan
    {
        /// <summary>Constructor, Initialise tissues</summary>
        public GenericBelowGroundOrgan(int numTissues, int numLayers)
        {
            DoInitialisation(numTissues, numLayers);
        }

        /// <summary>Actuallly initialise the tissues</summary>
        internal void DoInitialisation(int numTissues, int numLayers)
        {
            // Two tisses for root, live and dead
            TissueCount = numTissues;
            Tissue = new RootTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new RootTissue(numLayers);
        }

        /// <summary>the collection of tissues for this organ</summary>
        internal RootTissue[] Tissue { get; set; }

        #region Organ Properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>Number of tissue pools to create</summary>
        internal int TissueCount;

        /// <summary>N concentration for optimal growth [g/g]</summary>
        internal double NConcOptimum
        {
            get { return Tissue[0].NConcOptimum; }
            set
            {
                foreach (RootTissue tissue in Tissue)
                    tissue.NConcOptimum = value;
            }
        }

        /// <summary>Maximum N concentration, for luxury uptake [g/g]</summary>
        internal double NConcMaximum = 0.02;

        /// <summary>Minimum N concentration, structural N [g/g]</summary>
        internal double NConcMinimum = 0.01;

        /// <summary>The total dry matter in this organ [g/m^2]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the live tissues [g/m^2]</summary>
        internal double DMGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue [g/m^2]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the live tissues [g/m^2]</summary>
        internal double NGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>The average N concentration in this organ [g/g]</summary>
        internal double NconcTotal
        {
            get
            {
                return MathUtilities.Divide(NTotal, DMTotal, 0.0);
            }
        }

        /// <summary>The average N concentration in the live tissues [g/g]</summary>
        internal double NconcGreen
        {
            get
            {
                return MathUtilities.Divide(NGreen, DMGreen, 0.0);
            }
        }

        /// <summary>The average N concentration in dead tissues [g/g]</summary>
        internal double NconcDead
        {
            get
            {
                return MathUtilities.Divide(NDead, DMDead, 0.0);
            }
        }

        /// <summary>The amount of N available for remobilisation from senescence [g/g]</summary>
        internal double NRemobilisable = 0.0;

        /// <summary>The amount of luxury N available for remobilisation [g/g]</summary>
        internal double NRemobilisableLuxury
        {
            get
            {
                double luxNAmount = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    luxNAmount += Tissue[t].NLuxury;
                }

                return luxNAmount;
            }
        }

        /// <summary>The rooting depth [mm]</summary>
        internal double Depth;

        /// <summary>The layer at the bottom of the root zone</summary>
        internal int BottomLayer;

        /// <summary>The target (ideal) dry matter fraction by layer [0-1]</summary>
        internal double[] TargetDistribution;

        /// <summary>Reset</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < TissueCount - 1; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tiisue)</summary>
        /// <param name="fraction">Fraction of organ tissues to kill</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (fraction < 1.0)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    Tissue[TissueCount - 1].DM += Tissue[t].DM * fraction;
                    Tissue[TissueCount - 1].Namount += Tissue[t].Namount * fraction;
                    Tissue[t].DM *= fractionRemaining;
                    Tissue[t].Namount *= fractionRemaining;
                }
            }
            else
            {
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    Tissue[TissueCount - 1].DM += Tissue[t].DM;
                    Tissue[TissueCount - 1].Namount += Tissue[t].Namount;
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        internal double[] DoTissueTurnover(double[] turnoverRate)
        {
            double[] fromToDM = new double[TissueCount];
            double[] fromToN = new double[TissueCount];
            double[] detachment = new double[2];

            // get amounts turned over
            for (int t = 0; t < TissueCount; t++)
            {
                fromToDM[t] = Tissue[t].DM * turnoverRate[t];
                // turnover from live to dead only uses minimum conc (remaining is remobilisable)
                NRemobilisable = fromToDM[t] * (Tissue[t].Nconc - NConcMinimum);
                fromToN[t] -= NRemobilisable;
            }

            // update tissue pools (note that growth goes into tissue 0 and it has been considered already)
            Tissue[0].DM -= fromToDM[0];
            Tissue[0].Namount -= fromToN[0];
            for (int t = 1; t < TissueCount; t++)
            {
                Tissue[t].DM += fromToDM[t - 1] - fromToDM[t];
                Tissue[t].Namount += fromToN[t - 1] - fromToN[t];
            }

            // get the amount detached today
            detachment[0] = fromToDM[TissueCount - 1];
            detachment[1] = fromToN[TissueCount - 1];

            return detachment;
        }

        #endregion

        /// <summary>Defines a generic root tissue</summary>
        internal class RootTissue
        {
            /// <summary>Constructor, initialise array</summary>
            /// <param name="numLayers">Number of layers in the soil</param>
            public RootTissue(int numLayers)
            {
                nlayers = numLayers;
                DMLayer = new double[nlayers];
                NamountLayer = new double[nlayers];
                PamountLayer = new double[nlayers];
            }

            /// <summary>The number of layers in the soil</summary>
            private int nlayers;

            /// <summary>The dry matter amount [g/m^2]</summary>
            internal double DM
            {
                get { return DMLayer.Sum(); }
                set
                {
                    double[] prevRootFraction = FractionWt;
                    for (int layer = 0; layer < nlayers; layer++)
                        DMLayer[layer] = value * prevRootFraction[layer];
                }
            }

            /// <summary>The dry matter amount by layer [g/m^2]</summary>
            internal double[] DMLayer;

            /// <summary>The N content [g/m^2]</summary>
            internal double Namount
            {
                get { return NamountLayer.Sum(); }
                set
                {
                    for (int layer = 0; layer < nlayers; layer++)
                        NamountLayer[layer] = value * FractionWt[layer];
                }
            }

            /// <summary>The N content by layer [g/m^2]</summary>
            internal double[] NamountLayer;

            /// <summary>The N content [g/m^2]</summary>
            internal double NConcOptimum { get; set; } = 0.015;

            /// <summary>Minimum N concentration, structural N [g/g]</summary>
            internal double FractionNLuxuryRemobilisable = 0.0;

            /// <summary>The P content amount [g/m^2]</summary>
            internal double Pamount
            {
                get { return PamountLayer.Sum(); }
                set
                {
                    for (int layer = 0; layer < nlayers; layer++)
                        PamountLayer[layer] = value * FractionWt[layer];
                }
            }

            /// <summary>The P content by layer [g/m^2]</summary>
            internal double[] PamountLayer;

            /// <summary>The nitrogen concentration [g/g]</summary>
            internal double Nconc
            {
                get { return MathUtilities.Divide(Namount, DM, 0.0); }
                set { Namount = value * DM; }
            }

            /// <summary>The nitrogen amount above optimum concentration [g/g]</summary>
            internal double NLuxury
            {
                get { return Math.Max(0.0, FractionNLuxuryRemobilisable * ((DM * NConcOptimum) - Namount)); }
            }

            /// <summary>The phosphorus concentration [g/g]</summary>
            internal double Pconc
            {
                get { return MathUtilities.Divide(Pamount, DM, 0.0); }
                set { Pamount = value * DM; }
            }

            /// <summary>The dry matter fraction by layer [0-1]</summary>
            internal double[] FractionWt
            {
                get
                {
                    double[] result = new double[nlayers];
                    for (int layer = 0; layer < nlayers; layer++)
                        result[layer] = MathUtilities.Divide(DMLayer[layer], DM, 0.0);
                    return result;
                }
            }
        }
    }

    #endregion
}
