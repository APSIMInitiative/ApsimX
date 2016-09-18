//-----------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.Organs.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Models.Soils;
using Models.PMF;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{
    /// <summary>Describes a generic above ground organ of a pasture species</summary>
    [Serializable]
    public class PastureAboveGroundOrgan
    {
        /// <summary>Constructor, initialise tissues</summary>
        public PastureAboveGroundOrgan(int numTissues)
        {
            // Typically four tissues above ground, three live and one dead
            TissueCount = numTissues;
            Tissue = new GenericTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new GenericTissue();
        }

        /// <summary>The collection of tissues for this organ</summary>
        internal GenericTissue[] Tissue { get; set; }

        #region Organ specific characteristics  -----------------------------------------------------------------------

        /// <summary>Gets or sets the N concentration for optimum growth [kg/kg]</summary>
        internal double NConcOptimum = 0.04;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake [kg/kg]</summary>
        internal double NConcMaximum = 0.05;

        /// <summary>Gets or sets the minimum N concentration, structural N [kg/kg]</summary>
        internal double NConcMinimum = 0.012;

        /// <summary>Minimum DM amount of live tissues [kg/ha]</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Proportion of organ DM that is standing, available to harvest [0-1]</summary>
        internal double FractionStanding = 1.0;

        /// <summary>List of BiomassRemovalTypes with default biomass removal fractions for given removal types</summary>
        private Dictionary<string, OrganBiomassRemovalType> defaultRemovalFractions = new Dictionary<string, OrganBiomassRemovalType>();

        #endregion ----------------------------------------------------------------------------------------------------

        #region Organ properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>The number of tissue pools in this organ</summary>
        internal int TissueCount;

        /// <summary>Gets the total dry matter in this organ [kg/ha]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues [kg/ha]</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues [kg/ha]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The dry matter in the live (green) tissues available to harvest (kg/ha)</summary>
        internal double DMLiveHarvestable
        {
            get { return Math.Max(0.0, Math.Min(DMLive - MinimumLiveDM, DMLive * FractionStanding)); }
        }

        /// <summary>The dry matter in the dead tissues available to harvest (kg/ha)</summary>
        internal virtual double DMDeadHarvestable
        {
            get { return DMDead * FractionStanding; }
        }

        /// <summary>The total N amount in this tissue [kg/ha]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues [kg/ha]</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues [kg/ha]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>Gets the average N concentration in this organ [kg/kg]</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues [kg/kg]</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues [kg/kg]</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation [kg/ha]</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[TissueCount - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth [kg/ha]</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[TissueCount - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation [kg/ha]</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Gets the amount of luxury N remobilised into new growth [kg/ha]</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth [kg/ha]</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth [kg/ha]</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ [kg/ha]</summary>
        internal double DMSenesced
        {
            get { return Tissue[TissueCount - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ [kg/ha]</summary>
        internal double NSenesced
        {
            get { return Tissue[TissueCount - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ [kg/ha]</summary>
        internal double DMDetached
        {
            get { return Tissue[TissueCount - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ [kg/ha]</summary>
        internal double NDetached
        {
            get { return Tissue[TissueCount - 1].NTransferedOut; }
        }

        /// <summary>Gets the average digestibility of all biomass for this organ [kg/kg]</summary>
        internal double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of live biomass for this organ [kg/kg]</summary>
        internal double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMLive, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of dead biomass for this organ [kg/kg]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DigestibilityDead
        {
            get { return Tissue[TissueCount - 1].Digestibility; }
        }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Organ methods  ----------------------------------------------------------------------------------------

        /// <summary>Reset all amounts to zero in all tissues of this organ</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue)</summary>
        /// <param name="fraction">Fraction of tissues to kill</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
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

        /// <summary>Computes the DM and N amounts turned over for all tissues</summary>
        /// <param name="turnoverRate">The tunover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnedoverDM;
            double turnedoverN;

            // get amounts turned over
            for (int t = 0; t < TissueCount; t++)
            {
                turnedoverDM = Tissue[t].DM * turnoverRate[t];
                turnedoverN = Tissue[t].Namount * turnoverRate[t];
                Tissue[t].DMTransferedOut += turnedoverDM;
                Tissue[t].NTransferedOut += turnedoverN;

                if (t < TissueCount - 1)
                {
                    // pass amounts turned over from this tissue to the next (except last one)
                    Tissue[t + 1].DMTransferedIn += turnedoverDM;
                    Tissue[t + 1].NTransferedIn += turnedoverN;

                    // get the amounts remobilisable (luxury N)
                    double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                    Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t].FractionNLuxuryRemobilisable);
                }
                else
                {
                    // N transfered into dead tissue in excess of minimum N concentration is remobilisable
                    double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                    Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective</summary>
        /// <returns>A flag whether mass balance was maintained or not</returns>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < TissueCount; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
        }

        /// <summary>Adds a removal type to the defaultRemovalFractions</summary>
        /// <param name="typeName">The name of the removal type</param>
        /// <param name="removalFractions">Default removal fractions</param>
        internal void SetRemovalFractions(string typeName, OrganBiomassRemovalType removalFractions)
        {
            defaultRemovalFractions.Add(typeName, removalFractions);
        }

        /// <summary>Gets the default removal fractions for a given removal type</summary>
        /// <param name="typeName">The type of removal</param>
        /// <returns>The default removal fractions</returns>
        internal OrganBiomassRemovalType GetRemovalFractions(string typeName)
        {
            if (defaultRemovalFractions.ContainsKey(typeName))
                return defaultRemovalFractions[typeName];
            else
                return null;
        }

        #endregion ----------------------------------------------------------------------------------------------------

        /// <summary>Minimum significant difference between two values</summary>
        const double Epsilon = 0.000000001;
    }

    /// <summary>Describes a generic below ground organ of a pasture species</summary>
    [Serializable]
    public class PastureBelowGroundOrgan
    {
        /// <summary>Constructor, initialise tissues</summary>
        public PastureBelowGroundOrgan(int numTissues, int numLayers)
        {
            // Typically two tissues below ground, one live and one dead
            TissueCount = numTissues;
            Tissue = new RootTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new RootTissue(numLayers);
        }

        /// <summary>The collection of tissues for this organ</summary>
        internal RootTissue[] Tissue { get; set; }

        #region Root specific characteristics  ------------------------------------------------------------------------

        /// <summary>Gets or sets the N concentration for optimum growth [kg/kg]</summary>
        internal double NConcOptimum = 2.0;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake [kg/kg]</summary>
        internal double NConcMaximum  = 2.5;

        /// <summary>Gets or sets the minimum N concentration, structural N [kg/kg]</summary>
        internal double NConcMinimum = 0.6;

        /// <summary>Minimum DM amount of live tissues [kg/ha]</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Gets or sets the rooting depth [mm]</summary>
        internal double Depth { get; set; }

        /// <summary>Gets or sets the layer at the bottom of the root zone</summary>
        internal int BottomLayer { get; set; }

        /// <summary>Gets or sets the target (ideal) DM fractions for each layer [0-1]</summary>
        internal double[] TargetDistribution { get; set; }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Organ Properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>The number of tissue pools in this organ</summary>
        internal int TissueCount;

        /// <summary>Gets the total dry matter in this organ [kg/ha]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues [kg/ha]</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues [kg/ha]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue [kg/ha]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues [kg/ha]</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues [kg/ha]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>Gets the average N concentration in this organ [kg/kg]</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues [kg/kg]</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues [kg/kg]</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation [kg/ha]</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[TissueCount - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth [kg/ha]</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[TissueCount - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation [kg/ha]</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth [kg/ha]</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth [kg/ha]</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth [kg/ha]</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ [kg/ha]</summary>
        internal double DMSenesced
        {
            get { return Tissue[TissueCount - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ [kg/ha]</summary>
        internal double NSenesced
        {
            get { return Tissue[TissueCount - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ [kg/ha]</summary>
        internal double DMDetached
        {
            get { return Tissue[TissueCount - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ [kg/ha]</summary>
        internal double NDetached
        {
            get { return Tissue[TissueCount - 1].NTransferedOut; }
        }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Organ methods  ----------------------------------------------------------------------------------------

        /// <summary>Reset all amounts to zero in all tissues of this organ</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
                Array.Clear(Tissue[t].DMLayersTransferedIn, 0, Tissue[t].DMLayersTransferedIn.Length);
                Array.Clear(Tissue[t].NLayersTransferedIn, 0, Tissue[t].NLayersTransferedIn.Length);
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue)</summary>
        /// <param name="fraction">Fraction of tissues to kill</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[TissueCount - 1].DMLayer[layer] += Tissue[t].DMLayer[layer] * fraction;
                        Tissue[TissueCount - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer] * fraction;
                        Tissue[t].DMLayer[layer] *= fractionRemaining;
                        Tissue[t].NamountLayer[layer] *= fractionRemaining;
                    }
                }
            }
            else
            {
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[TissueCount - 1].DMLayer[layer] += Tissue[t].DMLayer[layer];
                        Tissue[TissueCount - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer];
                    }
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues</summary>
        /// <param name="turnoverRate">The tunover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnoverDM;
            double turnoverN;

            // get amounts turned over
            for (int t = 0; t < TissueCount; t++)
            {
                turnoverDM = Tissue[t].DM * turnoverRate[t];
                turnoverN = Tissue[t].Namount * turnoverRate[t];
                Tissue[t].DMTransferedOut += turnoverDM;
                Tissue[t].NTransferedOut += turnoverN;

                if (t < TissueCount - 1)
                {
                    // pass amounts turned over from this tissue to the next
                    Tissue[t + 1].DMTransferedIn += turnoverDM;
                    Tissue[t + 1].NTransferedIn += turnoverN;

                    // incoming stuff need to be given for each layer
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[t + 1].DMLayersTransferedIn[layer] = turnoverDM * Tissue[t].FractionWt[layer];
                        Tissue[t + 1].NLayersTransferedIn[layer] = turnoverN * Tissue[t].FractionWt[layer];
                    }

                    // get the amounts remobilisable (luxury N)
                    double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                    Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t + 1].FractionNLuxuryRemobilisable);
                }
                else
                {
                    // N transfered into dead tissue in excess of minimum N concentration is remobilisable
                    double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                    Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective</summary>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < TissueCount; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
        }

        #endregion ----------------------------------------------------------------------------------------------------

        /// <summary>Minimum significant difference between two values</summary>
        const double Epsilon = 0.000000001;
    }

    /// <summary>Describes a generic tissue of a pasture species</summary>
    [Serializable]
    public class GenericTissue
    {
        #region Basic properties  -------------------------------------------------------------------------------------

        // >> State amounts .......................................................................

        /// <summary>Gets or sets the dry matter weight [kg/ha]</summary>
        internal virtual double DM { get; set; }

        /// <summary>Gets or sets the nitrogen content [kg/ha]</summary>
        internal virtual double Namount { get; set; }

        /// <summary>Gets or sets the phosphorus content [kg/ha]</summary>
        internal virtual double Pamount { get; set; }

        // >> Amounts in and out ..................................................................

        /// <summary>Gets or sets the DM amount transfered into this tissue [kg/ha]</summary>
        internal double DMTransferedIn { get; set; }

        /// <summary>Gets or sets the DM amount transfered out of this tissue [kg/ha]</summary>
        internal double DMTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N transfered into this tissue [kg/ha]</summary>
        internal double NTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transfered out of this tissue [kg/ha]</summary>
        internal double NTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N available for remobilisation [kg/ha]</summary>
        internal double NRemobilisable { get; set; }

        /// <summary>Gets or sets the amount of N remobilised into new growth [kg/ha]</summary>
        internal double NRemobilised { get; set; }

        // >> Characteristics (parameters)  .......................................................

        /// <summary>Gets or sets the fraction of luxury N remobilisable per day [0-1]</summary>
        internal double FractionNLuxuryRemobilisable = 0.0;

        /// <summary>Gets or sets the sugar fraction on new growth, i.e. soluble carbohydrate [0-1]</summary>
        internal double FractionSugarNewGrowth = 0.0;

        /// <summary>Gets or sets the digestibility of cell walls [0-1]</summary>
        internal double DigestibilityCellWall = 0.5;

        /// <summary>Gets or sets the digestibility of proteins [0-1]</summary>
        internal double DigestibilityProtein = 1.0;

        #endregion ----------------------------------------------------------------------------------------------------

        #region Derived properties (get only)  ------------------------------------------------------------------------

        /// <summary>Gets the nitrogen concentration [kg/kg]</summary>
        internal double Nconc
        {
            get { return MathUtilities.Divide(Namount, DM, 0.0); }
            set { Namount = value * DM; }
        }

        /// <summary>Gets the phosphorus concentration [kg/kg]</summary>
        internal double Pconc
        {
            get { return MathUtilities.Divide(Pamount, DM, 0.0); }
            set { Pamount = value * DM; }
        }

        /// <summary>Gets the digestibility of this tissue [kg/kg]</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%</remarks>
        internal double Digestibility
        {
            get
            {
                double tissueDigestibility = 0.0;
                if (DM > 0.0)
                {
                    double cnTissue = DM * CarbonFractionInDM / Namount;
                    double ratio1 = CNratioCellWall / cnTissue;
                    double ratio2 = CNratioCellWall / CNratioProtein;
                    double fractionSugar = DMTransferedIn * FractionSugarNewGrowth / DM;
                    double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                    double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                    tissueDigestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);
                }

                return tissueDigestibility;
            }
        }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Tissue methods  ---------------------------------------------------------------------------------------

        /// <summary>Removes a fraction of remobilisable N for use into new growth</summary>
        /// <param name="fraction">fraction to remove [0-1]</param>
        internal void DoRemobiliseN(double fraction)
        {
            NRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective</summary>
        internal virtual void DoUpdateTissue()
        {
            DM += DMTransferedIn - DMTransferedOut;
            Namount += NTransferedIn - (NTransferedOut + NRemobilised);
        }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Constants  --------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>The C:N ratio of protein</summary>
        const double CNratioProtein = 3.5;

        /// <summary>The C:N ratio of cell wall</summary>
        const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values</summary>
        internal const double MyPrecision = 0.000000001;

        #endregion ----------------------------------------------------------------------------------------------------
    }

    /// <summary>Describes a root tissue of a pasture species</summary>
    [Serializable]
    internal class RootTissue : GenericTissue
    {
        /// <summary>Constructor, initialise array</summary>
        /// <param name="numLayers">Number of layers in the soil</param>
        public RootTissue(int numLayers)
        {
            nLayers = numLayers;
            DMLayer = new double[nLayers];
            NamountLayer = new double[nLayers];
            PamountLayer = new double[nLayers];
            DMLayersTransferedIn = new double[nLayers];
            NLayersTransferedIn = new double[nLayers];
        }

        /// <summary>The number of layers in the soil</summary>
        private int nLayers;

        #region Basic properties  -------------------------------------------------------------------------------------

        /// <summary>Gets or sets the dry matter weight [kg/ha]</summary>
        internal override double DM
        {
            get { return DMLayer.Sum(); }
            set
            {
                double[] prevRootFraction = FractionWt;
                for (int layer = 0; layer < nLayers; layer++)
                    DMLayer[layer] = value * prevRootFraction[layer];
            }
        }

        /// <summary>Gets or sets the DM amount for each layer [kg/ha]</summary>
        internal double[] DMLayer;

        /// <summary>Gets or sets the nitrogen content [kg/ha]</summary>
        internal override double Namount
        {
            get { return NamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    NamountLayer[layer] = value * FractionWt[layer];
            }
        }

        /// <summary>Gets or sets the N content for each layer [kg/ha]</summary>
        internal double[] NamountLayer;

        /// <summary>Gets or sets the phosphorus content [kg/ha]</summary>
        internal override double Pamount
        {
            get { return PamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    PamountLayer[layer] = value * FractionWt[layer];
            }
        }

        /// <summary>Gets or sets the P content for each layer [kg/ha]</summary>
        internal double[] PamountLayer { get; set; }

        /// <summary>Gets the dry matter fraction for each layer [0-1]</summary>
        internal double[] FractionWt
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = MathUtilities.Divide(DMLayer[layer], DM, 0.0);
                return result;
            }
        }

        /// <summary>Gets or sets the DM amount transfered into this tissue, for eac layer [kg/ha]</summary>
        internal double[] DMLayersTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transfered into this tissue, for eac layer [kg/ha]</summary>
        internal double[] NLayersTransferedIn { get; set; }

        #endregion ----------------------------------------------------------------------------------------------------

        #region Tissue methods  ---------------------------------------------------------------------------------------

        /// <summary>Updates the tissue state, make changes in DM and N effective</summary>
        internal override void DoUpdateTissue()
        {
            // removals first as they do not change distribution over the profile
            DM -= DMTransferedOut;
            Namount -= NTransferedOut;

            // additions need to consider distribution over the profile
            DMTransferedIn = DMLayersTransferedIn.Sum();
            NTransferedIn = NLayersTransferedIn.Sum();
            if ((DMTransferedIn > MyPrecision) && (NTransferedIn > MyPrecision))
            {
                for (int layer = 0; layer < nLayers; layer++)
                {
                    DMLayer[layer] += DMLayersTransferedIn[layer];
                    NamountLayer[layer] += NLayersTransferedIn[layer] - (NRemobilised * (NLayersTransferedIn[layer] / NTransferedIn));
                }
            }
        }

        #endregion ----------------------------------------------------------------------------------------------------
    }
}
