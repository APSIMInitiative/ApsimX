using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Models;
using Models.Core;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models
{

    /// <summary>A multi-species pasture model</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AgPasture : Model, ICrop, ICanopy, IUptake
    {
        #region Links and event declarations

        /// <summary>The clock</summary>
        [Link]
        private Clock clock = null;

        /// <summary>The soil</summary>
        [Link]
        private Soils.Soil Soil = null;

        /// <summary>The met data</summary>
        [Link]
        private IWeather MetData = null;

        //Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void FOMLayerDelegate(Soils.FOMLayerType Data);
        /// <summary>Occurs when [incorp fom].</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void BiomassRemovedDelegate(PMF.BiomassRemovedType Data);
        /// <summary>Occurs when [biomass removed].</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void WaterChangedDelegate(PMF.WaterChangedType Data);
        /// <summary>Occurs when [water changed].</summary>
        public event WaterChangedDelegate WaterChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        /// <summary>Occurs when [nitrogen changed].</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion

        #region Inputs


        #endregion

        #region Model parameters (from user interface or via manager)

        // = General parameters  ==================================================================
        // * Parameters that are set via user interface -------------------------------------------

        /// <summary>The sward name</summary>
        private string swardName = "AgPasture";
        /// <summary>Gets or sets the name of the sward.</summary>
        /// <value>The name of the sward.</value>
        [Description("Sward name (as shown on the simulation tree)")]
        public string SwardName
        {
            get { return swardName; }
            set { swardName = value; }
        }

        /// <summary>The number species</summary>
        private int numSpecies = 1;
        /// <summary>Gets or sets the number species.</summary>
        /// <value>The number species.</value>
        [Description("Number of species")]
        public int NumSpecies
        {
            get { return numSpecies; }
            set { numSpecies = value; }
        }

        /// <summary>The water uptake source</summary>
        private string waterUptakeSource = "calc";
        /// <summary>Gets or sets the water uptake source.</summary>
        /// <value>The water uptake source.</value>
        [Description("Water uptake done by AgPasture (calc) or by apsim?")]
        public string WaterUptakeSource
        {
            get { return waterUptakeSource; }
            set { waterUptakeSource = value; }
        }

        // * Parameters that may be set via Manager  ----------------------------------------------
        /// <summary>The n uptake source</summary>
        private string nUptakeSource = "calc";
        /// <summary>Gets or sets the n uptake source.</summary>
        /// <value>The n uptake source.</value>
        [XmlIgnore]
        public string NUptakeSource
        {
            get { return nUptakeSource; }
            set { nUptakeSource = value; }
        }

        /// <summary>The alt_ n_uptake</summary>
        public string alt_N_uptake = "no";
        /// <summary>Gets or sets the use alternative n uptake.</summary>
        /// <value>The use alternative n uptake.</value>
        [XmlIgnore]
        public string UseAlternativeNUptake
        {
            get { return alt_N_uptake; }
            set { alt_N_uptake = value; }
        }

        // = Parameters for each species  =========================================================
        // * Inputs from user interface -----------------------------------------------------------
        /// <summary>The species name</summary>
        private string[] speciesName = new string[] { "Ryegrass", "WhiteClover", "Paspalum" };
        /// <summary>Gets or sets the name of the species.</summary>
        /// <value>The name of the species.</value>
        [Description("Name of pasture species")]
        public string[] SpeciesName
        {
            get { return speciesName; }
            set
            {
                int NSp = value.Length;
                speciesName = new string[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    speciesName[sp] = value[sp];
            }
        }

        /// <summary>The species n type</summary>
        private string[] speciesNType = new string[] { "grass", "legume", "grass" };
        /// <summary>Gets or sets the type of the species n.</summary>
        /// <value>The type of the species n.</value>
        [Description("Type of plant with respect to N fixation (legume/grass)")]
        public string[] SpeciesNType
        {
            get { return speciesNType; }
            set
            {
                int NSp = value.Length;
                speciesNType = new string[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    speciesNType[sp] = value[sp];
            }
        }

        /// <summary>The species c type</summary>
        private string[] speciesCType = new string[] { "C3", "C3", "C4" };
        /// <summary>Gets or sets the type of the species c.</summary>
        /// <value>The type of the species c.</value>
        [Description("Type of plant with respect to photosynthesis")]
        public string[] SpeciesCType
        {
            get { return speciesCType; }
            set
            {
                int NSp = value.Length;
                speciesCType = new string[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    speciesCType[sp] = value[sp];
            }
        }

        /// <summary>The ini d mshoot</summary>
        private double[] iniDMshoot = new double[] { 2000.0, 500.0, 500.0 };
        /// <summary>Gets or sets the initial dm shoot.</summary>
        /// <value>The initial dm shoot.</value>
        [Description("Initial above ground DM")]
        public double[] InitialDMShoot
        {
            get { return iniDMshoot; }
            set
            {
                int NSp = value.Length;
                iniDMshoot = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    iniDMshoot[sp] = value[sp];
            }
        }

        /// <summary>The ini d mroot</summary>
        private double[] iniDMroot = new double[] { 500.0, 250.0, 100.0 };
        /// <summary>Gets or sets the initial dm root.</summary>
        /// <value>The initial dm root.</value>
        [Description("Initial below ground DM")]
        public double[] InitialDMRoot
        {
            get { return iniDMroot; }
            set
            {
                int NSp = value.Length;
                iniDMroot = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    iniDMroot[sp] = value[sp];
            }
        }

        /// <summary>The ini root depth</summary>
        private double[] iniRootDepth = new double[] { 750.0, 350.0, 950.0 };
        /// <summary>Gets or sets the initial root depth.</summary>
        /// <value>The initial root depth.</value>
        [Description("Initial depth for roots")]
        public double[] InitialRootDepth
        {
            get { return iniRootDepth; }
            set
            {
                int NSp = value.Length;
                iniRootDepth = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    iniRootDepth[sp] = value[sp];
            }
        }

        // * Parameters that may be set via manager -----------------------------------------------

        /// <summary>The maximum photosynthesis rate</summary>
        private double[] maxPhotosynthesisRate = new double[] { 1.0, 1.0, 1.2 };
        /// <summary>Gets or sets the maximum photosynthesis rate.</summary>
        /// <value>The maximum photosynthesis rate.</value>
        [XmlIgnore]
        public double[] MaxPhotosynthesisRate
        {
            get { return maxPhotosynthesisRate; }
            set
            {
                int NSp = value.Length;
                maxPhotosynthesisRate = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    maxPhotosynthesisRate[sp] = value[sp];
            }
        }

        /// <summary>The maintenance respiration coef</summary>
        private double[] maintenanceRespirationCoef = new double[] { 3.0, 3.0, 3.0 };
        /// <summary>Gets or sets the maintenance respiration coef.</summary>
        /// <value>The maintenance respiration coef.</value>
        [XmlIgnore]
        public double[] MaintenanceRespirationCoef
        {
            get { return maintenanceRespirationCoef; }
            set
            {
                int NSp = value.Length;
                maintenanceRespirationCoef = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    maintenanceRespirationCoef[sp] = value[sp];
            }
        }

        /// <summary>The growth efficiency</summary>
        private double[] growthEfficiency = new double[] { 0.75, 0.75, 0.75 };
        /// <summary>Gets or sets the growth efficiency.</summary>
        /// <value>The growth efficiency.</value>
        [XmlIgnore]
        public double[] GrowthEfficiency
        {
            get { return growthEfficiency; }
            set
            {
                int NSp = value.Length;
                growthEfficiency = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    growthEfficiency[sp] = value[sp];
            }
        }

        /// <summary>The light extention coeff</summary>
        private double[] lightExtentionCoeff = new double[] { 0.5, 0.8, 0.6 };
        /// <summary>Gets or sets the light extention coeff.</summary>
        /// <value>The light extention coeff.</value>
        [XmlIgnore]
        public double[] LightExtentionCoeff
        {
            get { return lightExtentionCoeff; }
            set
            {
                int NSp = value.Length;
                lightExtentionCoeff = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    lightExtentionCoeff[sp] = value[sp];
            }
        }

        /// <summary>The growth tmin</summary>
        private double[] growthTmin = new double[] { 2.0, 4.0, 10.0 };
        /// <summary>Gets or sets the growth tmin.</summary>
        /// <value>The growth tmin.</value>
        [XmlIgnore]
        public double[] GrowthTmin
        {
            get { return growthTmin; }
            set
            {
                int NSp = value.Length;
                growthTmin = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    growthTmin[sp] = value[sp];
            }
        }

        /// <summary>The growth tmax</summary>
        private double[] growthTmax = new double[] { 32.0, 32.0, 40.0 };
        /// <summary>Gets or sets the growth tmax.</summary>
        /// <value>The growth tmax.</value>
        [XmlIgnore]
        public double[] GrowthTmax
        {
            get { return growthTmax; }
            set
            {
                int NSp = value.Length;
                growthTmax = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    growthTmax[sp] = value[sp];
            }
        }

        /// <summary>The growth topt</summary>
        private double[] growthTopt = new double[] { 20.0, 20.0, 22.0 };
        /// <summary>Gets or sets the growth topt.</summary>
        /// <value>The growth topt.</value>
        [XmlIgnore]
        public double[] GrowthTopt
        {
            get { return growthTopt; }
            set
            {
                int NSp = value.Length;
                growthTopt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    growthTopt[sp] = value[sp];
            }
        }

        /// <summary>The growth tq</summary>
        private double[] growthTq = new double[] { 1.75, 1.75, 2.0 };
        /// <summary>Gets or sets the growth tq.</summary>
        /// <value>The growth tq.</value>
        [XmlIgnore]
        public double[] GrowthTq
        {
            get { return growthTq; }
            set
            {
                int NSp = value.Length;
                growthTq = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    growthTq[sp] = value[sp];
            }
        }

        /// <summary>The heat onset t</summary>
        private double[] heatOnsetT = new double[] { 28.0, 28.0, 40.0 };
        /// <summary>Gets or sets the heat onset t.</summary>
        /// <value>The heat onset t.</value>
        [XmlIgnore]
        public double[] HeatOnsetT
        {
            get { return heatOnsetT; }
            set
            {
                int NSp = value.Length;
                heatOnsetT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    heatOnsetT[sp] = value[sp];
            }
        }

        /// <summary>The heat full t</summary>
        private double[] heatFullT = new double[] { 35.0, 35.0, 50.0 };
        /// <summary>Gets or sets the heat full t.</summary>
        /// <value>The heat full t.</value>
        [XmlIgnore]
        public double[] HeatFullT
        {
            get { return heatFullT; }
            set
            {
                int NSp = value.Length;
                heatFullT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    heatFullT[sp] = value[sp];
            }
        }

        /// <summary>The heat sum t</summary>
        private double[] heatSumT = new double[] { 30.0, 30.0, 50.0 };
        /// <summary>Gets or sets the heat sum t.</summary>
        /// <value>The heat sum t.</value>
        [XmlIgnore]
        public double[] HeatSumT
        {
            get { return heatSumT; }
            set
            {
                int NSp = value.Length;
                heatSumT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    heatSumT[sp] = value[sp];
            }
        }

        /// <summary>The cold onset t</summary>
        private double[] coldOnsetT = new double[] { 0.0, 0.0, 8.0 };
        /// <summary>Gets or sets the cold onset t.</summary>
        /// <value>The cold onset t.</value>
        [XmlIgnore]
        public double[] ColdOnsetT
        {
            get { return coldOnsetT; }
            set
            {
                int NSp = value.Length;
                coldOnsetT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    coldOnsetT[sp] = value[sp];
            }
        }

        /// <summary>The cold full t</summary>
        private double[] coldFullT = new double[] { -3.0, -3.0, 3.0 };
        /// <summary>Gets or sets the cold full t.</summary>
        /// <value>The cold full t.</value>
        [XmlIgnore]
        public double[] ColdFullT
        {
            get { return coldFullT; }
            set
            {
                int NSp = value.Length;
                coldFullT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    coldFullT[sp] = value[sp];
            }
        }

        /// <summary>The cold sum t</summary>
        private double[] coldSumT = new double[] { 20.0, 20.0, 50.0 };
        /// <summary>Gets or sets the cold sum t.</summary>
        /// <value>The cold sum t.</value>
        [XmlIgnore]
        public double[] ColdSumT
        {
            get { return coldSumT; }
            set
            {
                int NSp = value.Length;
                coldSumT = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    coldSumT[sp] = value[sp];
            }
        }

        /// <summary>The specific leaf area</summary>
        private double[] specificLeafArea = new double[] { 20.0, 20.0, 20.0 };
        /// <summary>Gets or sets the specific leaf area.</summary>
        /// <value>The specific leaf area.</value>
        [XmlIgnore]
        public double[] SpecificLeafArea
        {
            get { return specificLeafArea; }
            set
            {
                int NSp = value.Length;
                specificLeafArea = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    specificLeafArea[sp] = value[sp];
            }
        }

        /// <summary>The specific root length</summary>
        private double[] specificRootLength = new double[] { 75.0, 75.0, 75.0 };
        /// <summary>Gets or sets the length of the specific root.</summary>
        /// <value>The length of the specific root.</value>
        [XmlIgnore]
        public double[] SpecificRootLength
        {
            get { return specificRootLength; }
            set
            {
                int NSp = value.Length;
                specificRootLength = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    specificRootLength[sp] = value[sp];
            }
        }

        /// <summary>The maximum root fraction</summary>
        private double[] maxRootFraction = new double[] { 0.25, 0.25, 0.25 };
        /// <summary>Gets or sets the maximum root fraction.</summary>
        /// <value>The maximum root fraction.</value>
        [XmlIgnore]
        public double[] MaxRootFraction
        {
            get { return maxRootFraction; }
            set
            {
                int NSp = value.Length;
                maxRootFraction = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    maxRootFraction[sp] = value[sp];
            }
        }

        /// <summary>The allocation season f</summary>
        private double[] allocationSeasonF = new double[] { 0.8, 0.8, 0.8 };
        /// <summary>Gets or sets the allocation season f.</summary>
        /// <value>The allocation season f.</value>
        [XmlIgnore]
        public double[] AllocationSeasonF
        {
            get { return allocationSeasonF; }
            set
            {
                int NSp = value.Length;
                allocationSeasonF = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    allocationSeasonF[sp] = value[sp];
            }
        }

        /// <summary>The frac to leaf</summary>
        private double[] fracToLeaf = new double[] { 0.7, 0.56, 0.7 };
        /// <summary>Gets or sets the frac to leaf.</summary>
        /// <value>The frac to leaf.</value>
        [XmlIgnore]
        public double[] FracToLeaf
        {
            get { return fracToLeaf; }
            set
            {
                int NSp = value.Length;
                fracToLeaf = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    fracToLeaf[sp] = value[sp];
            }
        }

        /// <summary>The frac to stolon</summary>
        private double[] fracToStolon = new double[] { 0.0, 0.2, 0.0 };
        /// <summary>Gets or sets the frac to stolon.</summary>
        /// <value>The frac to stolon.</value>
        [XmlIgnore]
        public double[] FracToStolon
        {
            get { return fracToStolon; }
            set
            {
                int NSp = value.Length;
                fracToStolon = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    fracToStolon[sp] = value[sp];
            }
        }

        /// <summary>The turnover rate live2 dead</summary>
        private double[] turnoverRateLive2Dead = new double[] { 0.025, 0.025, 0.025 };
        /// <summary>Gets or sets the turnover rate live2 dead.</summary>
        /// <value>The turnover rate live2 dead.</value>
        [XmlIgnore]
        public double[] TurnoverRateLive2Dead
        {
            get { return turnoverRateLive2Dead; }
            set
            {
                int NSp = value.Length;
                turnoverRateLive2Dead = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    turnoverRateLive2Dead[sp] = value[sp];
            }
        }

        /// <summary>The turnover rate dead2 litter</summary>
        private double[] turnoverRateDead2Litter = new double[] { 0.11, 0.11, 0.11 };
        /// <summary>Gets or sets the turnover rate dead2 litter.</summary>
        /// <value>The turnover rate dead2 litter.</value>
        [XmlIgnore]
        public double[] TurnoverRateDead2Litter
        {
            get { return turnoverRateDead2Litter; }
            set
            {
                int NSp = value.Length;
                turnoverRateDead2Litter = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    turnoverRateDead2Litter[sp] = value[sp];
            }
        }

        /// <summary>The turnover rate root senescence</summary>
        private double[] turnoverRateRootSenescence = new double[] { 0.02, 0.02, 0.02 };
        /// <summary>Gets or sets the turnover rate root senescence.</summary>
        /// <value>The turnover rate root senescence.</value>
        [XmlIgnore]
        public double[] TurnoverRateRootSenescence
        {
            get { return turnoverRateRootSenescence; }
            set
            {
                int NSp = value.Length;
                turnoverRateRootSenescence = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    turnoverRateRootSenescence[sp] = value[sp];
            }
        }

        /// <summary>The mass flux tmin</summary>
        private double[] massFluxTmin = new double[] { 2.0, 3.0, 7.5 };
        /// <summary>Gets or sets the mass flux tmin.</summary>
        /// <value>The mass flux tmin.</value>
        [XmlIgnore]
        public double[] MassFluxTmin
        {
            get { return massFluxTmin; }
            set
            {
                int NSp = value.Length;
                massFluxTmin = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    massFluxTmin[sp] = value[sp];
            }
        }

        /// <summary>The mass flux topt</summary>
        private double[] massFluxTopt = new double[] { 20.0, 20.0, 22.0 };
        /// <summary>Gets or sets the mass flux topt.</summary>
        /// <value>The mass flux topt.</value>
        [XmlIgnore]
        public double[] MassFluxTopt
        {
            get { return massFluxTopt; }
            set
            {
                int NSp = value.Length;
                massFluxTopt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    massFluxTopt[sp] = value[sp];
            }
        }

        /// <summary>The mass flux w0</summary>
        private double[] massFluxW0 = new double[] { 2.0, 2.0, 2.0 };
        /// <summary>Gets or sets the mass flux w0.</summary>
        /// <value>The mass flux w0.</value>
        [XmlIgnore]
        public double[] MassFluxW0
        {
            get { return massFluxW0; }
            set
            {
                int NSp = value.Length;
                massFluxW0 = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    massFluxW0[sp] = value[sp];
            }
        }

        /// <summary>The mass flux wopt</summary>
        private double[] massFluxWopt = new double[] { 0.5, 0.5, 0.5 };
        /// <summary>Gets or sets the mass flux wopt.</summary>
        /// <value>The mass flux wopt.</value>
        [XmlIgnore]
        public double[] MassFluxWopt
        {
            get { return massFluxWopt; }
            set
            {
                int NSp = value.Length;
                massFluxWopt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    massFluxWopt[sp] = value[sp];
            }
        }

        /// <summary>The stock parameter</summary>
        private double[] stockParameter = new double[] { 0.05, 0.05, 0.05 };
        /// <summary>Gets or sets the stock parameter.</summary>
        /// <value>The stock parameter.</value>
        [XmlIgnore]
        public double[] StockParameter
        {
            get { return stockParameter; }
            set
            {
                int NSp = value.Length;
                stockParameter = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    stockParameter[sp] = value[sp];
            }
        }

        /// <summary>The digestibility live</summary>
        private double[] digestibilityLive = new double[] { 0.6, 0.6, 0.6 };
        /// <summary>Gets or sets the digestibility live.</summary>
        /// <value>The digestibility live.</value>
        [XmlIgnore]
        public double[] DigestibilityLive
        {
            get { return digestibilityLive; }
            set
            {
                int NSp = value.Length;
                digestibilityLive = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    digestibilityLive[sp] = value[sp];
            }
        }

        /// <summary>The digestibility dead</summary>
        private double[] digestibilityDead = new double[] { 0.2, 0.2, 0.2 };
        /// <summary>Gets or sets the digestibility dead.</summary>
        /// <value>The digestibility dead.</value>
        [XmlIgnore]
        public double[] DigestibilityDead
        {
            get { return digestibilityDead; }
            set
            {
                int NSp = value.Length;
                digestibilityDead = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    digestibilityDead[sp] = value[sp];
            }
        }

        /// <summary>The minimum green wt</summary>
        private double[] minimumGreenWt = new double[] { 300.0, 100.0, 100.0 };
        /// <summary>Gets or sets the minimum green wt.</summary>
        /// <value>The minimum green wt.</value>
        [Description("Minimum above ground green DM")]
        public double[] MinimumGreenWt
        {
            get { return minimumGreenWt; }
            set
            {
                int NSp = value.Length;
                minimumGreenWt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    minimumGreenWt[sp] = value[sp];
            }
        }

        /// <summary>The minimum dead wt</summary>
        private double[] minimumDeadWt = new double[] { 0.0, 0.0, 0.0 };
        /// <summary>Gets or sets the minimum dead wt.</summary>
        /// <value>The minimum dead wt.</value>
        [XmlIgnore]
        public double[] MinimumDeadWt
        {
            get { return minimumDeadWt; }
            set
            {
                int NSp = value.Length;
                minimumDeadWt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    minimumDeadWt[sp] = value[sp];
            }
        }

        /// <summary>The preference for green dm</summary>
        private double[] preferenceForGreenDM = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the preference for green dm.</summary>
        /// <value>The preference for green dm.</value>
        [XmlIgnore]
        public double[] PreferenceForGreenDM
        {
            get { return preferenceForGreenDM; }
            set
            {
                int NSp = value.Length;
                preferenceForGreenDM = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    preferenceForGreenDM[sp] = value[sp];
            }
        }

        /// <summary>The preference for dead dm</summary>
        private double[] preferenceForDeadDM = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the preference for dead dm.</summary>
        /// <value>The preference for dead dm.</value>
        [XmlIgnore]
        public double[] PreferenceForDeadDM
        {
            get { return preferenceForDeadDM; }
            set
            {
                int NSp = value.Length;
                preferenceForDeadDM = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    preferenceForDeadDM[sp] = value[sp];
            }
        }

        /// <summary>The leaf nopt</summary>
        private double[] leafNopt = new double[] { 4.0, 4.5, 3.0 };
        /// <summary>Gets or sets the leaf nopt.</summary>
        /// <value>The leaf nopt.</value>
        [XmlIgnore]
        public double[] LeafNopt
        {
            get { return leafNopt; }
            set
            {
                int NSp = value.Length;
                leafNopt = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    leafNopt[sp] = value[sp];
            }
        }

        /// <summary>The leaf nmax</summary>
        private double[] leafNmax = new double[] { 5.0, 5.5, 3.5 };
        /// <summary>Gets or sets the leaf nmax.</summary>
        /// <value>The leaf nmax.</value>
        [XmlIgnore]
        public double[] LeafNmax
        {
            get { return leafNmax; }
            set
            {
                int NSp = value.Length;
                leafNmax = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    leafNmax[sp] = value[sp];
            }
        }

        /// <summary>The leaf nmin</summary>
        private double[] leafNmin = new double[] { 1.2, 2.0, 0.5 };
        /// <summary>Gets or sets the leaf nmin.</summary>
        /// <value>The leaf nmin.</value>
        [XmlIgnore]
        public double[] LeafNmin
        {
            get { return leafNmin; }
            set
            {
                int NSp = value.Length;
                leafNmin = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    leafNmin[sp] = value[sp];
            }
        }

        /// <summary>The relative n stems</summary>
        private double[] relativeNStems = new double[] { 0.5, 0.5, 0.5 };
        /// <summary>Gets or sets the relative n stems.</summary>
        /// <value>The relative n stems.</value>
        [XmlIgnore]
        public double[] RelativeNStems
        {
            get { return relativeNStems; }
            set
            {
                int NSp = value.Length;
                relativeNStems = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    relativeNStems[sp] = value[sp];
            }
        }

        /// <summary>The relative n stolons</summary>
        private double[] relativeNStolons = new double[] { 0.0, 0.5, 0.0 };
        /// <summary>Gets or sets the relative n stolons.</summary>
        /// <value>The relative n stolons.</value>
        [XmlIgnore]
        public double[] RelativeNStolons
        {
            get { return relativeNStolons; }
            set
            {
                int NSp = value.Length;
                relativeNStolons = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    relativeNStolons[sp] = value[sp];
            }
        }

        /// <summary>The relative n roots</summary>
        private double[] relativeNRoots = new double[] { 0.5, 0.5, 0.5 };
        /// <summary>Gets or sets the relative n roots.</summary>
        /// <value>The relative n roots.</value>
        [XmlIgnore]
        public double[] RelativeNRoots
        {
            get { return relativeNRoots; }
            set
            {
                int NSp = value.Length;
                relativeNRoots = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    relativeNRoots[sp] = value[sp];
            }
        }

        /// <summary>The relative n stage2</summary>
        private double[] relativeNStage2 = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the relative n stage2.</summary>
        /// <value>The relative n stage2.</value>
        [XmlIgnore]
        public double[] RelativeNStage2
        {
            get { return relativeNStage2; }
            set
            {
                int NSp = value.Length;
                relativeNStage2 = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    relativeNStage2[sp] = value[sp];
            }
        }

        /// <summary>The relative n stage3</summary>
        private double[] relativeNStage3 = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the relative n stage3.</summary>
        /// <value>The relative n stage3.</value>
        [XmlIgnore]
        public double[] RelativeNStage3
        {
            get { return relativeNStage3; }
            set
            {
                int NSp = value.Length;
                relativeNStage3 = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    relativeNStage3[sp] = value[sp];
            }
        }

        /// <summary>The minimum n fixation</summary>
        private double[] minimumNFixation = new double[] { 0.0, 0.2, 0.0 };
        /// <summary>Gets or sets the minimum n fixation.</summary>
        /// <value>The minimum n fixation.</value>
        [XmlIgnore]
        public double[] MinimumNFixation
        {
            get { return minimumNFixation; }
            set
            {
                int NSp = value.Length;
                minimumNFixation = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    minimumNFixation[sp] = value[sp];
            }
        }

        /// <summary>The maximum n fixation</summary>
        private double[] maximumNFixation = new double[] { 0.0, 0.6, 0.0 };
        /// <summary>Gets or sets the maximum n fixation.</summary>
        /// <value>The maximum n fixation.</value>
        [XmlIgnore]
        public double[] MaximumNFixation
        {
            get { return maximumNFixation; }
            set
            {
                int NSp = value.Length;
                maximumNFixation = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    maximumNFixation[sp] = value[sp];
            }
        }

        /// <summary>The kappa2 remob</summary>
        private double[] kappa2Remob = new double[] { 0.0, 0.0, 0.0 };
        /// <summary>Gets or sets the kappa2 remob.</summary>
        /// <value>The kappa2 remob.</value>
        [XmlIgnore]
        public double[] Kappa2Remob
        {
            get { return kappa2Remob; }
            set
            {
                int NSp = value.Length;
                kappa2Remob = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    kappa2Remob[sp] = value[sp];
            }
        }
        /// <summary>The kappa3 remob</summary>
        private double[] kappa3Remob = new double[] { 0.0, 0.0, 0.0 };
        /// <summary>Gets or sets the kappa3 remob.</summary>
        /// <value>The kappa3 remob.</value>
        [XmlIgnore]
        public double[] Kappa3Remob
        {
            get { return kappa3Remob; }
            set
            {
                int NSp = value.Length;
                kappa3Remob = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    kappa3Remob[sp] = value[sp];
            }
        }

        /// <summary>The kappa4 remob</summary>
        private double[] kappa4Remob = new double[] { 0.0, 0.0, 0.0 };
        /// <summary>Gets or sets the kappa4 remob.</summary>
        /// <value>The kappa4 remob.</value>
        [XmlIgnore]
        public double[] Kappa4Remob
        {
            get { return kappa4Remob; }
            set
            {
                int NSp = value.Length;
                kappa4Remob = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    kappa4Remob[sp] = value[sp];
            }
        }

        /// <summary>The dillution coef n</summary>
        private double[] dillutionCoefN = new double[] { 0.5, 1.0, 0.5 };
        /// <summary>Gets or sets the dillution coef n.</summary>
        /// <value>The dillution coef n.</value>
        [XmlIgnore]
        public double[] DillutionCoefN
        {
            get { return dillutionCoefN; }
            set
            {
                int NSp = value.Length;
                dillutionCoefN = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    dillutionCoefN[sp] = value[sp];
            }
        }

        /// <summary>The GLF generic</summary>
        private double[] glfGeneric = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the GLF generic.</summary>
        /// <value>The GLF generic.</value>
        [XmlIgnore]
        public double[] GlfGeneric
        {
            get { return glfGeneric; }
            set
            {
                int NSp = value.Length;
                glfGeneric = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    glfGeneric[sp] = value[sp];
            }
        }

        /// <summary>The water stress factor</summary>
        private double[] waterStressFactor = new double[] { 1.0, 1.0, 1.0 };
        /// <summary>Gets or sets the water stress factor.</summary>
        /// <value>The water stress factor.</value>
        [XmlIgnore]
        public double[] WaterStressFactor
        {
            get { return waterStressFactor; }
            set
            {
                int NSp = value.Length;
                waterStressFactor = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    waterStressFactor[sp] = value[sp];
            }
        }

        /// <summary>The water logging factor</summary>
        private double[] waterLoggingFactor = new double[] { 0.1, 0.1, 0.1 };
        /// <summary>Gets or sets the water logging factor.</summary>
        /// <value>The water logging factor.</value>
        [XmlIgnore]
        public double[] WaterLoggingFactor
        {
            get { return waterLoggingFactor; }
            set
            {
                int NSp = value.Length;
                waterLoggingFactor = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    waterLoggingFactor[sp] = value[sp];
            }
        }

        /// <summary>The reference c o2</summary>
        private double[] referenceCO2 = new double[] { 380.0, 380.0, 380.0 };
        /// <summary>Gets or sets the reference c o2.</summary>
        /// <value>The reference c o2.</value>
        [XmlIgnore]
        public double[] ReferenceCO2
        {
            get { return referenceCO2; }
            set
            {
                int NSp = value.Length;
                referenceCO2 = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    referenceCO2[sp] = value[sp];
            }
        }

        /// <summary>The offset c o2 effect on photosynthesis</summary>
        private double[] offsetCO2EffectOnPhotosynthesis = new double[] { 700.0, 700.0, 150.0 };
        /// <summary>Gets or sets the offset c o2 effect on photosynthesis.</summary>
        /// <value>The offset c o2 effect on photosynthesis.</value>
        [XmlIgnore]
        public double[] OffsetCO2EffectOnPhotosynthesis
        {
            get { return offsetCO2EffectOnPhotosynthesis; }
            set
            {
                int NSp = value.Length;
                offsetCO2EffectOnPhotosynthesis = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    offsetCO2EffectOnPhotosynthesis[sp] = value[sp];
            }
        }

        /// <summary>The offset c o2 effect on nuptake</summary>
        private double[] offsetCO2EffectOnNuptake = new double[] { 600.0, 600.0, 600.0 };
        /// <summary>Gets or sets the offset c o2 effect on nuptake.</summary>
        /// <value>The offset c o2 effect on nuptake.</value>
        [XmlIgnore]
        public double[] OffsetCO2EffectOnNuptake
        {
            get { return offsetCO2EffectOnNuptake; }
            set
            {
                int NSp = value.Length;
                offsetCO2EffectOnNuptake = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    offsetCO2EffectOnNuptake[sp] = value[sp];
            }
        }

        /// <summary>The minimum c o2 effect on nuptake</summary>
        private double[] minimumCO2EffectOnNuptake = new double[] { 0.7, 0.7, 0.7 };
        /// <summary>Gets or sets the minimum c o2 effect on nuptake.</summary>
        /// <value>The minimum c o2 effect on nuptake.</value>
        [XmlIgnore]
        public double[] MinimumCO2EffectOnNuptake
        {
            get { return minimumCO2EffectOnNuptake; }
            set
            {
                int NSp = value.Length;
                minimumCO2EffectOnNuptake = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    minimumCO2EffectOnNuptake[sp] = value[sp];
            }
        }

        /// <summary>The exponent c o2 effect on nuptake</summary>
        private double[] exponentCO2EffectOnNuptake = new double[] { 2.0, 2.0, 2.0 };
        /// <summary>Gets or sets the exponent c o2 effect on nuptake.</summary>
        /// <value>The exponent c o2 effect on nuptake.</value>
        [XmlIgnore]
        public double[] ExponentCO2EffectOnNuptake
        {
            get { return exponentCO2EffectOnNuptake; }
            set
            {
                int NSp = value.Length;
                exponentCO2EffectOnNuptake = new double[NSp];
                for (int sp = 0; sp < NSp; sp++)
                    exponentCO2EffectOnNuptake[sp] = value[sp];
            }
        }


        // * Other parameters (changed via manager) -----------------------------------------------

        /// <summary>The root distribution method</summary>
        private string rootDistributionMethod = "ExpoLinear";
        //[Description("Root distribution method")]
        /// <summary>Gets or sets the root distribution method.</summary>
        /// <value>The root distribution method.</value>
        /// <exception cref="System.Exception">No valid method for computing root distribution was selected</exception>
        [XmlIgnore]
        public string RootDistributionMethod
        {
            get
            { return rootDistributionMethod; }
            set
            {
                switch (value.ToLower())
                {
                    case "homogenous":
                    case "userdefined":
                    case "expolinear":
                        rootDistributionMethod = value;
                        break;
                    default:
                        throw new Exception("No valid method for computing root distribution was selected");
                }
            }
        }

        /// <summary>The expo linear depth parameter</summary>
        private double expoLinearDepthParam = 0.12;
        /// <summary>Gets or sets the expo linear depth parameter.</summary>
        /// <value>The expo linear depth parameter.</value>
        [Description("Fraction of root depth where its proportion starts to decrease")]
        public double ExpoLinearDepthParam
        {
            get { return expoLinearDepthParam; }
            set
            {
                expoLinearDepthParam = value;
                if (expoLinearDepthParam == 1.0)
                    rootDistributionMethod = "Homogeneous";
            }
        }

        /// <summary>The expo linear curve parameter</summary>
        private double expoLinearCurveParam = 3.2;
        /// <summary>Gets or sets the expo linear curve parameter.</summary>
        /// <value>The expo linear curve parameter.</value>
        [Description("Exponent to determine mass distribution in the soil profile")]
        public double ExpoLinearCurveParam
        {
            get { return expoLinearCurveParam; }
            set
            {
                expoLinearCurveParam = value;
                if (expoLinearCurveParam == 0.0)
                    rootDistributionMethod = "Homogeneous";	// It is impossible to solve, but its limit is a homogeneous distribution
            }
        }

        /// <summary>The initial dm fractions_grass</summary>
        [XmlIgnore]
        public double[] initialDMFractions_grass = new double[] { 0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00 };
        /// <summary>The initial dm fractions_legume</summary>
        [XmlIgnore]
        public double[] initialDMFractions_legume = new double[] { 0.20, 0.25, 0.25, 0.00, 0.02, 0.04, 0.04, 0.00, 0.06, 0.12, 0.12 };

        /// <summary>The height mass function</summary>
        [XmlIgnore]
        public LinearInterpolation HeightMassFN = new LinearInterpolation
        {
            X = new double[5] { 0, 1000, 2000, 3000, 4000 },
            Y = new double[5] { 0, 25, 75, 150, 250 }
        };

        /// <summary>The FVPD function</summary>
        [XmlIgnore]
        public LinearInterpolation FVPDFunction = new LinearInterpolation
        {
            X = new double[3] { 0.0, 10.0, 50.0 },
            Y = new double[3] { 1.0, 1.0, 1.0 }
        };

        #endregion

        #region Output properties

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames
        {
            get
            {
                return null;
            }
        }


        /// <summary>
        /// Is the plant alive?
        /// </summary>
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status.</summary>
        /// <value>The plant status.</value>
        [Description("Plant status (dead, alive, etc)")]
        [Units("")]
        public string PlantStatus
        {
            get
            {
                if (p_Live) return "alive";
                else return "out";
            }
        }

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("Plant development stage number")]
        [Units("")]
        public int Stage
        {
            //An approximate of teh stages corresponding to that of other arable crops for management application settings.
            //Phenostage of the first species (ryegrass) is used for this approximation
            get
            {
                int cropStage = 0; //default as "phase out"
                if (p_Live)
                {
                    if (SP[0].phenoStage == 0)
                        cropStage = 1;    //"sowing & germination";
                    if (SP[0].phenoStage == 1)
                        cropStage = 3;    //"emergence";
                }
                return cropStage;
            }
        }

        /// <summary>Gets the name of the stage.</summary>
        /// <value>The name of the stage.</value>
        [Description("Plant development stage name")]
        [Units("")]
        public string StageName
        {
            get
            {
                string name = "out";
                if (p_Live)
                {
                    if (SP[0].phenoStage == 0)
                        name = "sowing";    //cropStage = 1 & 2
                    if (SP[0].phenoStage == 1)
                        name = "emergence"; // cropStage = 3
                }
                return name;
            }
        }

        /// <summary>Gets the total plant c.</summary>
        /// <value>The total plant c.</value>
        [Description("Total amount of C in plants")]
        [Units("kgDM/ha")]
        public double TotalPlantC
        {
            get { return 0.4 * (p_totalDM + p_rootMass); }
        }

        /// <summary>Gets the total plant wt.</summary>
        /// <value>The total plant wt.</value>
        [Description("Total dry matter weight of plants")]
        [Units("kgDM/ha")]
        public double TotalPlantWt
        {
            get { return (AboveGroundWt + BelowGroundWt); }
        }

        /// <summary>Gets the above ground wt.</summary>
        /// <value>The above ground wt.</value>
        [Description("Total dry matter weight of plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmshoot;
                return result;
            }
        }

        /// <summary>Gets the below ground wt.</summary>
        /// <value>The below ground wt.</value>
        [Description("Total dry matter weight of plants below ground")]
        [Units("kgDM/ha")]
        public double BelowGroundWt
        {
            get { return p_rootMass; }
        }

        /// <summary>Gets the standing plant wt.</summary>
        /// <value>The standing plant wt.</value>
        [Description("Total dry matter weight of standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingPlantWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf + SP[s].dmstem;
                return result;
            }
        }

        /// <summary>Gets the above ground live wt.</summary>
        /// <value>The above ground live wt.</value>
        [Description("Total dry matter weight of plants alive above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundLiveWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmgreen;
                return result;
            }
        }

        /// <summary>Gets the above ground dead wt.</summary>
        /// <value>The above ground dead wt.</value>
        [Description("Total dry matter weight of dead plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundDeadWt
        {
            get { return p_deadDM; }
        }

        /// <summary>Gets the leaf wt.</summary>
        /// <value>The leaf wt.</value>
        [Description("Total dry matter weight of plant's leaves")]
        [Units("kgDM/ha")]
        public double LeafWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf1 + SP[s].dmleaf2 + SP[s].dmleaf3 + SP[s].dmleaf4;
                return result;
            }
        }

        /// <summary>Gets the leaf live wt.</summary>
        /// <value>The leaf live wt.</value>
        [Description("Total dry matter weight of plant's leaves alive")]
        [Units("kgDM/ha")]
        public double LeafLiveWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf1 + SP[s].dmleaf2 + SP[s].dmleaf3;
                return result;
            }
        }

        /// <summary>Gets the leaf dead wt.</summary>
        /// <value>The leaf dead wt.</value>
        [Description("Total dry matter weight of plant's leaves dead")]
        [Units("kgDM/ha")]
        public double LeafDeadWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf4;
                return result;
            }
        }

        /// <summary>Gets the stem wt.</summary>
        /// <value>The stem wt.</value>
        [Description("Total dry matter weight of plant's stems")]
        [Units("kgDM/ha")]
        public double StemWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmstem1 + SP[s].dmstem2 + SP[s].dmstem3 + SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the stem live wt.</summary>
        /// <value>The stem live wt.</value>
        [Description("Total dry matter weight of plant's stems alive")]
        [Units("kgDM/ha")]
        public double StemLiveWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmstem1 + SP[s].dmstem2 + SP[s].dmstem3;
                return result;
            }
        }

        /// <summary>Gets the stem dead wt.</summary>
        /// <value>The stem dead wt.</value>
        [Description("Total dry matter weight of plant's stems dead")]
        [Units("kgDM/ha")]
        public double StemDeadWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the stolon wt.</summary>
        /// <value>The stolon wt.</value>
        [Description("Total dry matter weight of plant's stolons")]
        [Units("kgDM/ha")]
        public double StolonWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmstol1 + SP[s].dmstol2 + SP[s].dmstol3;
                return result;
            }
        }

        /// <summary>Gets the root wt.</summary>
        /// <value>The root wt.</value>
        [Description("Total dry matter weight of plant's roots")]
        [Units("kgDM/ha")]
        public double RootWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmroot;
                return result;
            }
        }

        /// <summary>Gets the plant gross potential growth wt.</summary>
        /// <value>The plant gross potential growth wt.</value>
        public double PlantGrossPotentialGrowthWt
        {
            get { return SP.Sum(x => x.Pgross) * 2.5; }
        }

        /// <summary>Gets the plant potential growth wt.</summary>
        /// <value>The plant potential growth wt.</value>
        [Description("Potential plant growth, correct for extreme temperatures")]
        [Units("kgDM/ha")]
        public double PlantPotentialGrowthWt
        {
            get { return p_dGrowthPot; }
        }

        /// <summary>Gets the plant growth no n limit.</summary>
        /// <value>The plant growth no n limit.</value>
        [Description("Potential plant growth, correct for temperature and water")]
        [Units("kgDM/ha")]
        public double PlantGrowthNoNLimit
        {
            get { return p_dGrowthW; }
        }

        /// <summary>Gets the plant growth wt.</summary>
        /// <value>The plant growth wt.</value>
        [Description("Actual plant growth (before littering)")]
        [Units("kgDM/ha")]
        public double PlantGrowthWt
        {
            //dm_daily_growth, including roots & before littering
            get { return p_dGrowth; }
        }

        /// <summary>Gets the plant effective growth wt.</summary>
        /// <value>The plant effective growth wt.</value>
        public double PlantEffectiveGrowthWt
        {
            get { return SP.Sum(x => x.dGrowth) - SP.Sum(x => x.dLitter) - SP.Sum(x => x.dRootSen); }
        }

        /// <summary>Gets the herbage growth wt.</summary>
        /// <value>The herbage growth wt.</value>
        [Description("Actual herbage (shoot) growth")]
        [Units("kgDM/ha")]
        public double HerbageGrowthWt
        {
            get { return p_dHerbage; }
        }

        /// <summary>Gets the litter deposition wt.</summary>
        /// <value>The litter deposition wt.</value>
        [Description("Dry matter amount of litter deposited onto soil surface")]
        [Units("kgDM/ha")]
        public double LitterDepositionWt
        {
            get { return p_dLitter; }
        }

        /// <summary>Gets the root senescence wt.</summary>
        /// <value>The root senescence wt.</value>
        [Description("Dry matter amount of senescent roots added to soil FOM")]
        [Units("kgDM/ha")]
        public double RootSenescenceWt
        {
            get { return p_dRootSen; }
        }

        /// <summary>Gets the plant remobilised c.</summary>
        /// <value>The plant remobilised c.</value>
        [Description("Plant C remobilisation")]
        [Units("kgC/ha")]
        public double PlantRemobilisedC
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Cremob;
                return result;
            }
        }

        /// <summary>Gets the harvestable wt.</summary>
        /// <value>The harvestable wt.</value>
        [Description("Total dry matter amount available for removal (leaf+stem)")]
        [Units("kgDM/ha")]
        public double HarvestableWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += Math.Max(0.0, SP[s].dmleaf_green + SP[s].dmstem_green - SP[s].dmgreenmin)
                             + Math.Max(0.0, SP[s].dmdead - SP[s].dmdeadmin);
                return result;
            }
        }

        /// <summary>Gets the harvest wt.</summary>
        /// <value>The harvest wt.</value>
        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kgDM/ha")]
        public double HarvestWt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmdefoliated;
                return result;
            }
        }

        /// <summary>Gets the la i_green.</summary>
        /// <value>The la i_green.</value>
        [Description("Leaf area index of green leaves")]
        [Units("m^2/m^2")]
        public double LAI_green
        {
            get { return p_greenLAI; }
        }

        /// <summary>Gets the la i_dead.</summary>
        /// <value>The la i_dead.</value>
        [Description("Leaf area index of dead leaves")]
        [Units("m^2/m^2")]
        public double LAI_dead
        {
            get { return p_deadLAI; }
        }

        /// <summary>Gets the la i_total.</summary>
        /// <value>The la i_total.</value>
        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double LAI_total
        {
            get { return p_totalLAI; }
        }

        /// <summary>Gets the cover_green.</summary>
        /// <value>The cover_green.</value>
        [Description("Fraction of soil covered by green leaves")]
        [Units("%")]
        public double Cover_green
        {
            get
            {
                if (p_greenLAI == 0) return 0;
                return (1.0 - Math.Exp(-p_lightExtCoeff * p_greenLAI));
            }

        }

        /// <summary>Gets the cover_dead.</summary>
        /// <value>The cover_dead.</value>
        [Description("Fraction of soil covered by dead leaves")]
        [Units("%")]
        public double Cover_dead
        {
            get
            {
                if (p_deadLAI == 0) return 0;
                return (1.0 - Math.Exp(-p_lightExtCoeff * p_deadLAI));
            }
        }

        /// <summary>Gets the cover_tot.</summary>
        /// <value>The cover_tot.</value>
        [Description("Fraction of soil covered by plants")]
        [Units("%")]
        public double Cover_tot
        {
            get
            {
                if (p_totalLAI == 0) return 0;
                return (1.0 - (Math.Exp(-p_lightExtCoeff * p_totalLAI)));
            }
        }

        /// <summary>Gets the total plant n.</summary>
        /// <value>The total plant n.</value>
        [Description("Total amount of N in plants")]
        [Units("kg/ha")]
        public double TotalPlantN
        {
            get { return (AboveGroundN + BelowGroundN); }
        }

        /// <summary>Gets the above ground n.</summary>
        /// <value>The above ground n.</value>
        [Description("Total amount of N in plants above ground")]
        [Units("kgN/ha")]
        public double AboveGroundN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nshoot;       //remoblised N is reported in stem
                return result;
            }
        }

        /// <summary>Gets the below ground n.</summary>
        /// <value>The below ground n.</value>
        [Description("Total amount of N in plants below ground")]
        [Units("kgN/ha")]
        public double BelowGroundN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nroot;
                return result;
            }
        }

        /// <summary>Gets the standing plant n.</summary>
        /// <value>The standing plant n.</value>
        [Description("Total amount of N in standing plants")]
        [Units("kgN/ha")]
        public double StandingPlantN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf + SP[s].Nstem;
                return result;
            }
        }

        /// <summary>Gets the standing plant n conc.</summary>
        /// <value>The standing plant n conc.</value>
        [Description("Average N concentration of standing plants")]
        [Units("kgN/kgDM")]
        public double StandingPlantNConc
        {
            get
            {
                double Namount = 0.0;
                double DMamount = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    Namount += SP[s].Nleaf + SP[s].Nstem;
                    DMamount += SP[s].dmleaf + SP[s].dmstem;
                }
                double result = Namount / DMamount;
                return result;
            }
        }

        /// <summary>Gets the above ground live n.</summary>
        /// <value>The above ground live n.</value>
        [Description("Total amount of N in plants alive above ground")]
        [Units("kgN/ha")]
        public double AboveGroundLiveN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ngreen;
                return result;
            }
        }

        /// <summary>Gets the above ground dead n.</summary>
        /// <value>The above ground dead n.</value>
        [Description("Total amount of N in dead plants above ground")]
        [Units("kgN/ha")]
        public double AboveGroundDeadN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ndead;
                return result;
            }
        }

        /// <summary>Gets the leaf n.</summary>
        /// <value>The leaf n.</value>
        [Description("Total amount of N in the plant's leaves")]
        [Units("kgN/ha")]
        public double LeafN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf;
                return result;
            }
        }

        /// <summary>Gets the stem n.</summary>
        /// <value>The stem n.</value>
        [Description("Total amount of N in the plant's stems")]
        [Units("kgN/ha")]
        public double StemN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nstem;
                return result;
            }
        }

        /// <summary>Gets the stolon n.</summary>
        /// <value>The stolon n.</value>
        [Description("Total amount of N in the plant's stolons")]
        [Units("kgN/ha")]
        public double StolonN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nstolon;
                return result;
            }
        }

        /// <summary>Gets the root n.</summary>
        /// <value>The root n.</value>
        [Description("Total amount of N in the plant's roots")]
        [Units("kgN/ha")]
        public double RootN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nroot;
                return result;
            }
        }

        /// <summary>Gets the leaf n conc.</summary>
        /// <value>The leaf n conc.</value>
        [Description("Average N concentration of leaves")]
        [Units("kgN/kgDM")]
        public double LeafNConc
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ncleaf1 * SP[s].dmleaf1
                             + SP[s].Ncleaf2 * SP[s].dmleaf2
                            + SP[s].Ncleaf3 * SP[s].dmleaf3
                            + SP[s].Ncleaf4 * SP[s].dmleaf4;
                result = result / LeafWt;
                return result;
            }
        }

        /// <summary>Gets the stem n conc.</summary>
        /// <value>The stem n conc.</value>
        [Description("Average N concentration in stems")]
        [Units("kgN/kgDM")]
        public double StemNConc
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ncstem1 * SP[s].dmstem1
                             + SP[s].Ncstem2 * SP[s].dmstem2
                             + SP[s].Ncstem3 * SP[s].dmstem3
                             + SP[s].Ncstem4 * SP[s].dmstem4;
                result = result / StemWt;
                return result;
            }
        }

        /// <summary>Gets the stolon n conc.</summary>
        /// <value>The stolon n conc.</value>
        [Description("Average N concentration in stolons")]
        [Units("kgN/kgDM")]
        public double StolonNConc
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ncstol1 * SP[s].dmstol1
                             + SP[s].Ncstol2 * SP[s].dmstol2
                             + SP[s].Ncstol3 * SP[s].dmstol3;
                result = result / StolonWt;
                return result;
            }
        }

        /// <summary>Gets the root n conc.</summary>
        /// <value>The root n conc.</value>
        [Description("Average N concentration in roots")]
        [Units("kgN/kgDM")]
        public double RootNConc
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ncroot * SP[s].dmroot;
                result = result / RootWt;
                return result;
            }
        }

        /// <summary>Gets the harvest n.</summary>
        /// <value>The harvest n.</value>
        [Description("Amount of N removed by harvest")]
        [Units("kgN/ha")]
        public double HarvestN
        {
            get
            {
                double result = 0.0;
                if (HarvestWt > 0.0)
                {
                    for (int s = 0; s < numSpecies; s++)
                        result += SP[s].Ndefoliated;
                }
                return result;
            }
        }

        /// <summary>Gets the herbage digestibility.</summary>
        /// <value>The herbage digestibility.</value>
        [Description("Average herbage digestibility")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get
            {
                if (!p_Live || (StemWt + LeafWt) <= 0)
                    return 0;

                double digest = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    digest += SP[s].digestHerbage * (SP[s].dmstem + SP[s].dmleaf) / (StemWt + LeafWt);  //(dm_stem + dm_leaf);
                return digest;
            }
        }

        /// <summary>Gets the defoliated digestibility.</summary>
        /// <value>The defoliated digestibility.</value>
        [Description("Average digestibility of harvested material")]
        [Units("0-1")]
        public double DefoliatedDigestibility
        {
            get { return p_harvestDigest; }
        }

        /// <summary>Gets the herbage me.</summary>
        /// <value>The herbage me.</value>
        [Description("Average ME of herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get
            {
                double me = 16 * HerbageDigestibility * (StemWt + LeafWt);
                return me;
            }
        }

        /// <summary>Gets the plant fixed n.</summary>
        /// <value>The plant fixed n.</value>
        [Description("Amount of atmospheric N fixed")]
        [Units("kgN/ha")]
        public double PlantFixedN
        {
            get { return p_Nfix; }
        }

        /// <summary>Gets the plant remobilised n.</summary>
        /// <value>The plant remobilised n.</value>
        [Description("Amount of N remobilised from senescing tissue")]
        [Units("kgN/ha")]
        public double PlantRemobilisedN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].remob2NewGrowth;
                return result;
            }
        }

        /// <summary>Gets the plant luxury n remobilised.</summary>
        /// <value>The plant luxury n remobilised.</value>
        [Description("Amount of luxury N remobilised")]
        [Units("kgN/ha")]
        public double PlantLuxuryNRemobilised
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].NFastRemob2 + SP[s].NFastRemob3;
                return result;
            }
        }

        /// <summary>Gets the plant remobilisable luxury n.</summary>
        /// <value>The plant remobilisable luxury n.</value>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kgN/ha")]
        public double PlantRemobilisableLuxuryN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].NLuxury2 + SP[s].NLuxury3;
                return result;
            }
        }

        /// <summary>Gets the litter deposition n.</summary>
        /// <value>The litter deposition n.</value>
        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kgN/ha")]
        public double LitterDepositionN
        {
            get { return p_dNLitter; }
        }

        /// <summary>Gets the root senescence n.</summary>
        /// <value>The root senescence n.</value>
        [Description("Amount of N added to soil FOM by senescent roots")]
        [Units("kgN/ha")]
        public double RootSenescenceN
        {
            get { return p_dNRootSen; }
        }

        /// <summary>Gets the nitrogen required luxury.</summary>
        /// <value>The nitrogen required luxury.</value>
        [Description("Plant nitrogen requirement with luxury uptake")]
        [Units("kgN/ha")]
        public double NitrogenRequiredLuxury
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    result += SP[s].NdemandLux;
                }
                return result;
            }
        }

        /// <summary>Gets the nitrogen required optimum.</summary>
        /// <value>The nitrogen required optimum.</value>
        [Description("Plant nitrogen requirement for optimum growth")]
        [Units("kgN/ha")]
        public double NitrogenRequiredOptimum
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    result += SP[s].NdemandOpt;
                }
                return result;
            }
        }

        /// <summary>Gets the plant growth n.</summary>
        /// <value>The plant growth n.</value>
        [Description("Nitrogen amount in new growth")]
        [Units("kgN/ha")]
        public double PlantGrowthN
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    result += SP[s].newGrowthN;
                }
                return result;
            }
        }

        /// <summary>Gets the plant growth nconc.</summary>
        /// <value>The plant growth nconc.</value>
        [Description("Nitrogen concentration in new growth")]
        [Units("kgN/kgDM")]
        public double PlantGrowthNconc
        {
            get
            {
                double result = 0.0;
                if (PlantGrowthWt > 0)
                    result = PlantGrowthN / PlantGrowthWt;
                else
                    result = 0.0;
                return result;
            }
        }

        /// <summary>Gets the nitrogen demand.</summary>
        /// <value>The nitrogen demand.</value>
        [Description("Plant nitrogen demand from soil")]
        [Units("kgN/ha")]
        public double NitrogenDemand
        {
            get { return p_soilNdemand; }
        }

        /// <summary>Gets the nitrogen supply.</summary>
        /// <value>The nitrogen supply.</value>
        [Description("Plant available nitrogen in soil")]
        [Units("kgN/ha")]
        public double NitrogenSupply
        {
            get { return p_soilNavailable; }
        }

        /// <summary>Gets the nitrogen supply layers.</summary>
        /// <value>The nitrogen supply layers.</value>
        [Description("Plant available nitrogen in soil layers")]
        [Units("kgN/ha")]
        public double[] NitrogenSupplyLayers
        {
            get { return SNSupply; }
        }

        /// <summary>Gets the nitrogen uptake.</summary>
        /// <value>The nitrogen uptake.</value>
        [Description("Plant nitrogen uptake")]
        [Units("kgN/ha")]
        public double NitrogenUptake
        {
            get { return p_soilNuptake; }
        }

        /// <summary>Gets the nitrogen uptake layers.</summary>
        /// <value>The nitrogen uptake layers.</value>
        [Description("Plant nitrogen uptake from soil layers")]
        [Units("kgN/ha")]
        public double[] NitrogenUptakeLayers
        {
            get { return SNUptake; }
        }

        /// <summary>Gets the gl function.</summary>
        /// <value>The gl function.</value>
        [Description("Plant growth limiting factor due to nitrogen stress")]
        [Units("0-1")]
        public double GLFn
        {
            get { return p_gfn; }
        }

        /// <summary>Gets the gl function concentration.</summary>
        /// <value>The gl function concentration.</value>
        [Description("Plant growth limiting factor due to plant N concentration")]
        [Units("0-1")]
        public double GLFnConcentration
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Ncfactor * SP[s].dmshoot;
                return (result / AboveGroundWt);
            }
        }

        /// <summary>Gets the dm to roots.</summary>
        /// <value>The dm to roots.</value>
        [Description("Dry matter allocated to roots")]
        [Units("kgDM/ha")]
        public double DMToRoots
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    result += (1 - SP[s].fShoot) * SP[s].dGrowth;
                }
                return result;
            }
        }

        /// <summary>Gets the dm to shoot.</summary>
        /// <value>The dm to shoot.</value>
        [Description("Dry matter allocated to shoot")]
        [Units("kgDM/ha")]
        public double DMToShoot
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                {
                    result += SP[s].fShoot * SP[s].dGrowth;
                }
                return result;
            }
        }

        /// <summary>Gets the fraction growth to root.</summary>
        /// <value>The fraction growth to root.</value>
        [Description("Fraction of growth allocated to roots")]
        [Units("0-1")]
        public double FractionGrowthToRoot
        {
            get
            {
                double result = 0.0;
                if (p_dGrowth > 0)
                    result = DMToRoots / p_dGrowth;
                return result;
            }
        }

        /// <summary>Gets the RLV.</summary>
        /// <value>The RLV.</value>
        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] Rlv
        {
            get
            {
                double[] rlv = new double[Soil.Thickness.Length];
                //double p_srl = 75000;           // specific root length (mm root/g root)
                //Compute the root length, total over the whole profile
                double Total_Rlength = p_rootMass * SP[0].SRL;   // m root/ha
                Total_Rlength *= 0.0000001;  // convert into mm root/mm2 soil)
                for (int layer = 0; layer < rlv.Length; layer++)
                {
                    rlv[layer] = RootFraction[layer] * Total_Rlength / Soil.Thickness[layer];    // mm root/mm3 soil
                }
                return rlv;
            }
        }

        /// <summary>The root fraction</summary>
        private double[] RootFraction;
        /// <summary>Gets the root wt fraction.</summary>
        /// <value>The root wt fraction.</value>
        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get { return RootFraction; }
        }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [Description("Plant water demand")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return p_waterDemand; }
        }

        /// <summary>Gets the water supply.</summary>
        /// <value>The water supply.</value>
        [Description("Plant available water in soil")]
        [Units("mm")]
        public double WaterSupply
        {
            get { return p_waterSupply; }
        }

        /// <summary>Gets the water supply layers.</summary>
        /// <value>The water supply layers.</value>
        [Description("Plant available water in soil layers")]
        [Units("mm")]
        public double[] WaterSupplyLayers
        {
            get { return SWSupply; }
        }

        /// <summary>Gets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Description("Plant water uptake")]
        [Units("mm")]
        public double WaterUptake
        {
            get { return p_waterUptake; }
        }

        /// <summary>Gets the water uptake layers.</summary>
        /// <value>The water uptake layers.</value>
        [Description("Plant water uptake from soil layers")]
        [Units("mm")]
        public double[] WaterUptakeLayers
        {
            get { return SWUptake; }
        }

        /// <summary>Gets the gl fwater.</summary>
        /// <value>The gl fwater.</value>
        [Description("Plant growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GLFwater
        {
            get { return p_gfwater; }
        }

        //**Stress factors
        /// <summary>Gets the gl ftemp.</summary>
        /// <value>The gl ftemp.</value>
        [Description("Plant growth limiting factor due to temperature")]
        [Units("0-1")]
        public double GLFtemp
        {
            get { return p_gftemp; }
        }

        /// <summary>Gets the gl FRGR.</summary>
        /// <value>The gl FRGR.</value>
        [Description("Generic plant growth limiting factor, used for other factors")]
        [Units("0-1")]
        public double GLFrgr
        {
            get
            {
                double p_Frgr = 0; //weighted value
                for (int s = 0; s < numSpecies; s++)
                {
                    double prop = 1.0 / numSpecies;
                    if (p_greenDM != 0.0)
                    {
                        prop = SP[s].dmgreen / AboveGroundLiveWt;
                    }
                    p_Frgr += SP[s].gfGen * prop;
                }
                return p_Frgr;
            }
        }


        //testing purpose
        /// <summary>Gets the plant stage1 wt.</summary>
        /// <value>The plant stage1 wt.</value>
        [Description("Dry matter of plant pools at stage 1 (young)")]
        [Units("kgN/ha")]
        public double PlantStage1Wt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf1 + SP[s].dmstem1 + SP[s].dmstol1;
                return result;
            }
        }

        /// <summary>Gets the plant stage2 wt.</summary>
        /// <value>The plant stage2 wt.</value>
        [Description("Dry matter of plant pools at stage 2 (developing)")]
        [Units("kgN/ha")]
        public double PlantStage2Wt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf2 + SP[s].dmstem2 + SP[s].dmstol2;
                return result;
            }
        }

        /// <summary>Gets the plant stage3 wt.</summary>
        /// <value>The plant stage3 wt.</value>
        [Description("Dry matter of plant pools at stage 3 (mature)")]
        [Units("kgN/ha")]
        public double PlantStage3Wt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf3 + SP[s].dmstem3 + SP[s].dmstol3;
                return result;
            }
        }

        /// <summary>Gets the plant stage4 wt.</summary>
        /// <value>The plant stage4 wt.</value>
        [Description("Dry matter of plant pools at stage 4 (senescent)")]
        [Units("kgN/ha")]
        public double PlantStage4Wt
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].dmleaf4 + SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the plant stage1 n.</summary>
        /// <value>The plant stage1 n.</value>
        [Description("N content of plant pools at stage 1 (young)")]
        [Units("kgN/ha")]
        public double PlantStage1N
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf1 + SP[s].Nstem1 + SP[s].Nstol1;
                return result;
            }
        }

        /// <summary>Gets the plant stage2 n.</summary>
        /// <value>The plant stage2 n.</value>
        [Description("N content of plant pools at stage 2 (developing)")]
        [Units("kgN/ha")]
        public double PlantStage2N
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf2 + SP[s].Nstem2 + SP[s].Nstol2;
                return result;
            }
        }

        /// <summary>Gets the plant stage3 n.</summary>
        /// <value>The plant stage3 n.</value>
        [Description("N content of plant pools at stage 3 (mature)")]
        [Units("kgN/ha")]
        public double PlantStage3N
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf3 + SP[s].Nstem3 + SP[s].Nstol3;
                return result;
            }
        }

        /// <summary>Gets the plant stage4 n.</summary>
        /// <value>The plant stage4 n.</value>
        [Description("N content of plant pools at stage 4 (senescent)")]
        [Units("kgN/ha")]
        public double PlantStage4N
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Nleaf4 + SP[s].Nstem4;
                return result;
            }
        }

        /// <summary>Gets the heightfrom dm.</summary>
        /// <value>The heightfrom dm.</value>
        private double HeightfromDM        // height calculation from DM, not output
        {
            get
            {
                double ht = (double)HeightMassFN.Value(p_greenDM + p_deadDM);
                if (ht < 20.0) ht = 20.0F;      // minimum = 20mm
                return ht;
            }

        }

        /// <summary>Gets the vp d_out.</summary>
        /// <value>The vp d_out.</value>
        [Description("Vapour pressure deficit")]
        [Units("kPa")]
        public double VPD_out              // VPD effect on Growth Interpolation Set
        {
            get { return VPD(); }
        }

        /// <summary>Gets the FVPD.</summary>
        /// <value>The FVPD.</value>
        [Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD              // VPD effect on Growth Interpolation Set
        {                               // mostly = 1 for crop/grass/forage
            get { return FVPDFunction.Value(VPD()); }
        }

        /// <summary>Gets the species green lai.</summary>
        /// <value>The species green lai.</value>
        [Description("Leaf area index of green leaves, for each species")]
        [Units("m^2/m^2")]
        public double[] SpeciesGreenLAI
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].greenLAI;
                return result;
            }
        }

        /// <summary>Gets the species dead lai.</summary>
        /// <value>The species dead lai.</value>
        [Description("Leaf area index of dead leaves, for each species")]
        [Units("m^2/m^2")]
        public double[] SpeciesDeadLAI
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].deadLAI;
                return result;
            }
        }

        /// <summary>Gets the species total lai.</summary>
        /// <value>The species total lai.</value>
        [Description("Total leaf area index, for each species")]
        [Units("m^2/m^2")]
        public double[] SpeciesTotalLAI
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].totalLAI;
                return result;
            }
        }

        /// <summary>Gets the species total wt.</summary>
        /// <value>The species total wt.</value>
        [Description("Total dry matter weight of plants for each plant species")]
        [Units("kgDM/ha")]
        public double[] SpeciesTotalWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmshoot + SP[s].dmroot;
                return result;
            }
        }

        /// <summary>Gets the species above ground wt.</summary>
        /// <value>The species above ground wt.</value>
        [Description("Dry matter weight of plants above ground, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesAboveGroundWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmshoot;
                return result;
            }
        }

        /// <summary>Gets the species below ground wt.</summary>
        /// <value>The species below ground wt.</value>
        [Description("Dry matter weight of plants below ground, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesBelowGroundWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmroot;
                return result;
            }
        }

        /// <summary>Gets the species standing wt.</summary>
        /// <value>The species standing wt.</value>
        [Description("Dry matter weight of standing herbage, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesStandingWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf + SP[s].dmstem;
                return result;
            }
        }

        /// <summary>Gets the species standing live wt.</summary>
        /// <value>The species standing live wt.</value>
        [Description("Dry matter weight of live standing plants parts for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesStandingLiveWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf_green + SP[s].dmstem_green;
                return result;
            }
        }

        /// <summary>Gets the species standing dead wt.</summary>
        /// <value>The species standing dead wt.</value>
        [Description("Dry matter weight of dead standing plants parts for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesStandingDeadWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf4 + SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the species leaf wt.</summary>
        /// <value>The species leaf wt.</value>
        [Description("Dry matter weight of leaves for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf1 + SP[s].dmleaf2 + SP[s].dmleaf3 + SP[s].dmleaf4;
                return result;
            }
        }

        /// <summary>Gets the species stem wt.</summary>
        /// <value>The species stem wt.</value>
        [Description("Dry matter weight of stems for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstem1 + SP[s].dmstem2 + SP[s].dmstem3 + SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the species stolon wt.</summary>
        /// <value>The species stolon wt.</value>
        [Description("Dry matter weight of stolons for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstol1 + SP[s].dmstol2 + SP[s].dmstol3;
                return result;
            }
        }

        /// <summary>Gets the species root wt.</summary>
        /// <value>The species root wt.</value>
        [Description("Dry matter weight of roots for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRootWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmroot;
                return result;
            }
        }

        /// <summary>Gets the species total n.</summary>
        /// <value>The species total n.</value>
        [Description("Total N amount for each plant species")]
        [Units("kgN/ha")]
        public double[] SpeciesTotalN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nshoot + SP[s].Nroot;
                return result;
            }
        }

        /// <summary>Gets the species standing n.</summary>
        /// <value>The species standing n.</value>
        [Description("N amount of standing herbage, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStandingN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf + SP[s].Nstem;
                return result;
            }
        }

        /// <summary>Gets the species leaf n.</summary>
        /// <value>The species leaf n.</value>
        [Description("N amount in the plant's leaves, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf1 + SP[s].Nleaf2 + SP[s].Nleaf3 + SP[s].Nleaf4;
                return result;
            }
        }

        /// <summary>Gets the species stem n.</summary>
        /// <value>The species stem n.</value>
        [Description("N amount in the plant's stems, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstem1 + SP[s].Nstem2 + SP[s].Nstem3 + SP[s].Nstem4;
                return result;
            }
        }

        /// <summary>Gets the species stolon n.</summary>
        /// <value>The species stolon n.</value>
        [Description("N amount in the plant's stolons, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstol1 + SP[s].Nstol2 + SP[s].Nstol3;
                return result;
            }
        }

        /// <summary>Gets the species roots n.</summary>
        /// <value>The species roots n.</value>
        [Description("N amount in the plant's roots, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRootsN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nroot;
                return result;
            }
        }

        /// <summary>Gets the species leaf n conc.</summary>
        /// <value>The species leaf n conc.</value>
        [Description("Average N concentration in leaves, for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesLeafNConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].Ncleaf1 * SP[s].dmleaf1
                            + SP[s].Ncleaf2 * SP[s].dmleaf2
                            + SP[s].Ncleaf3 * SP[s].dmleaf3
                            + SP[s].Ncleaf4 * SP[s].dmleaf4;
                    result[s] = result[s] / SP[s].dmleaf;
                }
                return result;
            }
        }

        /// <summary>Gets the species stem n conc.</summary>
        /// <value>The species stem n conc.</value>
        [Description("Average N concentration in stems, for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStemNConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].Ncstem1 * SP[s].dmstem1
                            + SP[s].Ncstem2 * SP[s].dmstem2
                            + SP[s].Ncstem3 * SP[s].dmstem3
                            + SP[s].Ncstem4 * SP[s].dmstem4;
                    result[s] = result[s] / SP[s].dmstem;
                }
                return result;
            }
        }

        /// <summary>Gets the species stolon n conc.</summary>
        /// <value>The species stolon n conc.</value>
        [Description("Average N concentration in stolons, for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStolonNConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].Ncstol1 * SP[s].dmstol1
                              + SP[s].Ncstol2 * SP[s].dmstol2
                              + SP[s].Ncstol3 * SP[s].dmstol3;
                    result[s] = result[s] / SP[s].dmstol;
                }
                return result;
            }
        }

        /// <summary>Gets the species root n conc.</summary>
        /// <value>The species root n conc.</value>
        [Description("Average N concentration in roots, for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesRootNConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].Ncroot * SP[s].dmroot;
                    result[s] = result[s] / SP[s].dmroot;
                }
                return result;
            }
        }

        /// <summary>Gets the species leaf stage1 wt.</summary>
        /// <value>The species leaf stage1 wt.</value>
        [Description("Dry matter weight of leaves at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage1Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf1;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage2 wt.</summary>
        /// <value>The species leaf stage2 wt.</value>
        [Description("Dry matter weight of leaves at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage2Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf2;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage3 wt.</summary>
        /// <value>The species leaf stage3 wt.</value>
        [Description("Dry matter weight of leaves at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage3Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf3;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage4 wt.</summary>
        /// <value>The species leaf stage4 wt.</value>
        [Description("Dry matter weight of leaves at stage 4 (dead) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage4Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmleaf4;
                return result;
            }
        }

        /// <summary>Gets the species stem stage1 wt.</summary>
        /// <value>The species stem stage1 wt.</value>
        [Description("Dry matter weight of stems at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage1Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstem1;
                return result;
            }
        }

        /// <summary>Gets the species stem stage2 wt.</summary>
        /// <value>The species stem stage2 wt.</value>
        [Description("Dry matter weight of stems at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage2Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstem2;
                return result;
            }
        }

        /// <summary>Gets the species stem stage3 wt.</summary>
        /// <value>The species stem stage3 wt.</value>
        [Description("Dry matter weight of stems at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage3Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstem3;
                return result;
            }
        }

        /// <summary>Gets the species stem stage4 wt.</summary>
        /// <value>The species stem stage4 wt.</value>
        [Description("Dry matter weight of stems at stage 4 (dead) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage4Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstem4;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage1 wt.</summary>
        /// <value>The species stolon stage1 wt.</value>
        [Description("Dry matter weight of stolons at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage1Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstol1;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage2 wt.</summary>
        /// <value>The species stolon stage2 wt.</value>
        [Description("Dry matter weight of stolons at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage2Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstol2;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage3 wt.</summary>
        /// <value>The species stolon stage3 wt.</value>
        [Description("Dry matter weight of stolons at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage3Wt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmstol3;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage1 n.</summary>
        /// <value>The species leaf stage1 n.</value>
        [Description("N amount in leaves at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage1N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf1;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage2 n.</summary>
        /// <value>The species leaf stage2 n.</value>
        [Description("N amount in leaves at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage2N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf2;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage3 n.</summary>
        /// <value>The species leaf stage3 n.</value>
        [Description("N amount in leaves at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage3N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf3;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage4 n.</summary>
        /// <value>The species leaf stage4 n.</value>
        [Description("N amount in leaves at stage 4 (dead) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLeafStage4N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nleaf4;
                return result;
            }
        }

        /// <summary>Gets the species stem stage1 n.</summary>
        /// <value>The species stem stage1 n.</value>
        [Description("N amount in stems at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage1N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstem1;
                return result;
            }
        }

        /// <summary>Gets the species stem stage2 n.</summary>
        /// <value>The species stem stage2 n.</value>
        [Description("N amount in stems at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage2N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstem2;
                return result;
            }
        }

        /// <summary>Gets the species stem stage3 n.</summary>
        /// <value>The species stem stage3 n.</value>
        [Description("N amount in stems at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage3N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstem3;
                return result;
            }
        }

        /// <summary>Gets the species stem stage4 n.</summary>
        /// <value>The species stem stage4 n.</value>
        [Description("N amount in stems at stage 4 (dead) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStemStage4N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstem4;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage1 n.</summary>
        /// <value>The species stolon stage1 n.</value>
        [Description("N amount in stolons at stage 1 (young) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage1N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstol1;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage2 n.</summary>
        /// <value>The species stolon stage2 n.</value>
        [Description("N amount in stolons at stage 2 (developing) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage2N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstol2;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage3 n.</summary>
        /// <value>The species stolon stage3 n.</value>
        [Description("N amount in stolons at stage 3 (mature) for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesStolonStage3N
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Nstol3;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage1 n conc.</summary>
        /// <value>The species leaf stage1 n conc.</value>
        [Description("N concentration of leaves at stage 1 (young) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesLeafStage1NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncleaf1;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage2 n conc.</summary>
        /// <value>The species leaf stage2 n conc.</value>
        [Description("N concentration of leaves at stage 2 (developing) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesLeafStage2NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncleaf2;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage3 n conc.</summary>
        /// <value>The species leaf stage3 n conc.</value>
        [Description("N concentration of leaves at stage 3 (mature) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesLeafStage3NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncleaf3;
                return result;
            }
        }

        /// <summary>Gets the species leaf stage4 n conc.</summary>
        /// <value>The species leaf stage4 n conc.</value>
        [Description("N concentration of leaves at stage 4 (dead) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesLeafStage4NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncleaf4;
                return result;
            }
        }

        /// <summary>Gets the species stem stage1 n conc.</summary>
        /// <value>The species stem stage1 n conc.</value>
        [Description("N concentration of stems at stage 1 (young) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStemStage1NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstem1;
                return result;
            }
        }

        /// <summary>Gets the species stem stage2 n conc.</summary>
        /// <value>The species stem stage2 n conc.</value>
        [Description("N concentration of stems at stage 2 (developing) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStemStage2NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstem2;
                return result;
            }
        }

        /// <summary>Gets the species stem stage3 n conc.</summary>
        /// <value>The species stem stage3 n conc.</value>
        [Description("N concentration of stems at stage 3 (mature) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStemStage3NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstem3;
                return result;
            }
        }

        /// <summary>Gets the species stem stage4 n conc.</summary>
        /// <value>The species stem stage4 n conc.</value>
        [Description("N concentration of stems at stage 4 (dead) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStemStage4NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstem4;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage1 n conc.</summary>
        /// <value>The species stolon stage1 n conc.</value>
        [Description("N concentration of stolons at stage 1 (young) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStolonStage1NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstol1;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage2 n conc.</summary>
        /// <value>The species stolon stage2 n conc.</value>
        [Description("N concentration of stolons at stage 2 (developing) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStolonStage2NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstol2;
                return result;
            }
        }

        /// <summary>Gets the species stolon stage3 n conc.</summary>
        /// <value>The species stolon stage3 n conc.</value>
        [Description("N concentration of stolons at stage 3 (mature) for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesStolonStage3NConc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ncstol3;
                return result;
            }
        }

        /// <summary>Gets the species growth wt.</summary>
        /// <value>The species growth wt.</value>
        [Description("Actual growth for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesGrowthWt
        {
            get
            {
                double[] result = new double[numSpecies];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dGrowth;
                return result;
            }
        }

        /// <summary>Gets the species litter wt.</summary>
        /// <value>The species litter wt.</value>
        [Description("Litter amount deposited onto soil surface, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesLitterWt
        {
            get
            {
                double[] result = new double[numSpecies];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dLitter;
                return result;
            }
        }

        /// <summary>Gets the species root senesced wt.</summary>
        /// <value>The species root senesced wt.</value>
        [Description("Amount of senesced roots added to soil FOM, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesRootSenescedWt
        {
            get
            {
                double[] result = new double[numSpecies];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dRootSen;
                return result;
            }
        }

        /// <summary>Gets the species harvestable wt.</summary>
        /// <value>The species harvestable wt.</value>
        [Description("Amount of dry matter harvestable for each species (leaf+stem)")]
        [Units("kgDM/ha")]
        public double[] SpeciesHarvestableWt
        {
            get
            {
                double[] result = new double[numSpecies];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = Math.Max(0.0, SP[s].dmleaf_green + SP[s].dmstem_green - SP[s].dmgreenmin)
                              + Math.Max(0.0, SP[s].dmdead - SP[s].dmdeadmin);
                return result;
            }
        }

        /// <summary>Gets the species harvest wt.</summary>
        /// <value>The species harvest wt.</value>
        [Description("Amount of plant dry matter removed by harvest, for each species")]
        [Units("kgDM/ha")]
        public double[] SpeciesHarvestWt
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dmdefoliated;
                return result;
            }
        }

        /// <summary>Gets the species harvest PCT.</summary>
        /// <value>The species harvest PCT.</value>
        [Description("Proportion in the dry matter harvested of each species")]
        [Units("%")]
        public double[] SpeciesHarvestPct
        {
            get
            {
                double[] result = new double[SP.Length];
                double myTotal = StandingPlantWt;
                for (int s = 0; s < numSpecies; s++)
                {
                    if (myTotal > 0.0)
                        result[s] = (SP[s].dmstem + SP[s].dmleaf) * 100 / myTotal;
                }
                return result;
            }
        }

        /// <summary>The fraction to harvest</summary>
        private double[] FractionToHarvest;
        /// <summary>Gets the species harvest fraction.</summary>
        /// <value>The species harvest fraction.</value>
        [Description("Fraction to harvest for each species")]
        [Units("0-1")]
        public double[] SpeciesHarvestFraction
        {
            get { return FractionToHarvest; }
        }

        /// <summary>Gets the species live dm turnover rate.</summary>
        /// <value>The species live dm turnover rate.</value>
        [Description("Rate of turnover for live DM, for each species")]
        [Units("0-1")]
        public double[] SpeciesLiveDMTurnoverRate
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].gama;
                }
                return result;
            }
        }

        /// <summary>Gets the species dead dm turnover rate.</summary>
        /// <value>The species dead dm turnover rate.</value>
        [Description("Rate of turnover for dead DM, for each species")]
        [Units("0-1")]
        public double[] SpeciesDeadDMTurnoverRate
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].gamad;
                }
                return result;
            }
        }

        /// <summary>Gets the species stolon dm turnover rate.</summary>
        /// <value>The species stolon dm turnover rate.</value>
        [Description("Rate of DM turnover for stolons, for each species")]
        [Units("0-1")]
        public double[] SpeciesStolonDMTurnoverRate
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].gamas;
                }
                return result;
            }
        }

        /// <summary>Gets the species root dm turnover rate.</summary>
        /// <value>The species root dm turnover rate.</value>
        [Description("Rate of DM turnover for roots, for each species")]
        [Units("0-1")]
        public double[] SpeciesRootDMTurnoverRate
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].gamar;
                }
                return result;
            }
        }

        /// <summary>Gets the species remobilised n.</summary>
        /// <value>The species remobilised n.</value>
        [Description("Amount of N remobilised from senesced material, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRemobilisedN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].remob2NewGrowth;
                }
                return result;
            }
        }

        /// <summary>Gets the species luxury n remobilised.</summary>
        /// <value>The species luxury n remobilised.</value>
        [Description("Amount of luxury N remobilised, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLuxuryNRemobilised
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].NFastRemob2 + SP[s].NFastRemob3;
                }
                return result;
            }
        }

        /// <summary>Gets the species remobilisable luxury n.</summary>
        /// <value>The species remobilisable luxury n.</value>
        [Description("Amount of luxury N potentially remobilisable, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRemobilisableLuxuryN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].NLuxury2 + SP[s].NLuxury3;
                }
                return result;
            }
        }

        /// <summary>Gets the species fixed n.</summary>
        /// <value>The species fixed n.</value>
        [Description("Amount of atmospheric N fixed, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesFixedN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].Nfix;
                }
                return result;
            }
        }

        /// <summary>Gets the species required n luxury.</summary>
        /// <value>The species required n luxury.</value>
        [Description("Amount of N required with luxury uptake, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRequiredNLuxury
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].NdemandLux;
                }
                return result;
            }
        }

        /// <summary>Gets the species required n optimum.</summary>
        /// <value>The species required n optimum.</value>
        [Description("Amount of N required for optimum growth, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesRequiredNOptimum
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].NdemandOpt;
                }
                return result;
            }
        }

        /// <summary>Gets the species demand n.</summary>
        /// <value>The species demand n.</value>
        [Description("Amount of N demaned from soil, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesDemandN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].soilNdemand;
                }
                return result;
            }
        }

        /// <summary>Gets the species growth n.</summary>
        /// <value>The species growth n.</value>
        [Description("Amount of N in new growth, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesGrowthN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].newGrowthN;
                }
                return result;
            }
        }

        /// <summary>Gets the species growth nconc.</summary>
        /// <value>The species growth nconc.</value>
        [Description("Nitrogen concentration in new growth, for each species")]
        [Units("kgN/kgDM")]
        public double[] SpeciesGrowthNconc
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    if (SP[s].dGrowth > 0)
                        result[s] = SP[s].newGrowthN / SP[s].dGrowth;
                    else
                        result[s] = 0.0;
                }
                return result;
            }
        }

        /// <summary>Gets the species uptake n.</summary>
        /// <value>The species uptake n.</value>
        [Description("Amount of N uptake, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesUptakeN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].soilNuptake;
                }
                return result;
            }
        }

        /// <summary>Gets the species litter n.</summary>
        /// <value>The species litter n.</value>
        [Description("Amount of N deposited as litter onto soil surface, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesLitterN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].dNLitter;
                }
                return result;
            }
        }

        /// <summary>Gets the species senesced n.</summary>
        /// <value>The species senesced n.</value>
        [Description("Amount of N from senesced roots added to soil FOM, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesSenescedN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                {
                    result[s] = SP[s].dNrootSen;
                }
                return result;
            }
        }

        /// <summary>Gets the species harvest n.</summary>
        /// <value>The species harvest n.</value>
        [Description("Amount of plant nitrogen removed by harvest, for each species")]
        [Units("kgN/ha")]
        public double[] SpeciesHarvestN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Ndefoliated;
                return result;
            }
        }

        /// <summary>Gets the species GLFN.</summary>
        /// <value>The species GLFN.</value>
        [Description("Growth limiting factor due to nitrogen, for each species")]
        [Units("0-1")]
        public double[] SpeciesGLFN
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].gfn;
                return result;
            }
        }

        /// <summary>Gets the species GLFT.</summary>
        /// <value>The species GLFT.</value>
        [Description("Growth limiting factor due to temperature, for each species")]
        [Units("0-1")]
        public double[] SpeciesGLFT
        {
            get
            {
                double[] result = new double[SP.Length];
                double Tmnw = 0.75 * MetData.MaxT + 0.25 * MetData.MinT;  // weighted Tmean
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].GFTemperature(Tmnw);
                return result;
            }
        }
        /// <summary>Gets the species GLFW.</summary>
        /// <value>The species GLFW.</value>
        [Description("Growth limiting factor due to water deficit, for each species")]
        [Units("0-1")]
        public double[] SpeciesGLFW
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].gfwater;
                return result;
            }
        }

        /// <summary>Gets the species irradiance top canopy.</summary>
        /// <value>The species irradiance top canopy.</value>
        [Description("Irridance on the top of canopy")]
        [Units("W.m^2/m^2")]
        public double[] SpeciesIrradianceTopCanopy
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].IL1;
                return result;
            }
        }

        /// <summary>Gets the species pot carbon assimilation.</summary>
        /// <value>The species pot carbon assimilation.</value>
        [Description("Potential C assimilation, corrected for extreme temperatures")]
        [Units("kgC/ha")]
        public double[] SpeciesPotCarbonAssimilation
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Pgross;
                return result;
            }
        }
        /// <summary>Gets the species carbon loss respiration.</summary>
        /// <value>The species carbon loss respiration.</value>
        [Description("Loss of C via respiration")]
        [Units("kgC/ha")]
        public double[] SpeciesCarbonLossRespiration
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = (double)SP[s].Resp_m;
                return result;
            }
        }

        /// <summary>Gets the species gross pot growth.</summary>
        /// <value>The species gross pot growth.</value>
        public double[] SpeciesGrossPotGrowth
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].Pgross * 2.5;
                return result;
            }
        }

        /// <summary>Gets the species net pot growth.</summary>
        /// <value>The species net pot growth.</value>
        public double[] SpeciesNetPotGrowth
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dGrowthPot;
                return result;
            }
        }

        /// <summary>Gets the species pot growth w.</summary>
        /// <value>The species pot growth w.</value>
        public double[] SpeciesPotGrowthW
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dGrowthW;
                return result;
            }
        }

        /// <summary>Gets the species actual growth.</summary>
        /// <value>The species actual growth.</value>
        public double[] SpeciesActualGrowth
        {
            get
            {
                double[] result = new double[SP.Length];
                for (int s = 0; s < numSpecies; s++)
                    result[s] = SP[s].dGrowth;
                return result;
            }
        }



        /// <summary>Gets the GPP.</summary>
        /// <value>The GPP.</value>
        [Description("Gross primary productivity")]
        [Units("kgDM/ha")]
        public double GPP
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Pgross * 2.5;   // assume 40% C in DM
                return result;
            }
        }

        /// <summary>Gets the NPP.</summary>
        /// <value>The NPP.</value>
        [Description("Net primary productivity")]
        [Units("kgDM/ha")]
        public double NPP
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += SP[s].Pgross * 0.75 - SP[s].Resp_m;
                result *= 2.5;   // assume 40% C in DM
                return result;
            }
        }

        /// <summary>Gets the napp.</summary>
        /// <value>The napp.</value>
        [Description("Net above-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NAPP
        {
            get
            {
                double result = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    result += (SP[s].Pgross * 0.75 - SP[s].Resp_m) * SP[s].fShoot;
                result *= 2.5;   // assume 40% C in DM
                return result;
            }
        }

        #endregion

        #region Internal variables

        /// <summary>Species in the simulated sward</summary>
        private Species[] SP;

        /// <summary>Constant needed for vapour pressure</summary>
        private const double SVPfrac = 0.66;

        /// <summary>The intercepted radn</summary>
        private double interceptedRadn;	// Intercepted Radn
        /// <summary>My light profile</summary>
        private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;

        /// <summary>The have initialised</summary>
        private bool HaveInitialised = false;

        //** Aggregated pasture parameters of all species (wiht a prefix 'p_')
        //p_d... variables are daily changes (delta)
        /// <summary>The P_D growth pot</summary>
        private double p_dGrowthPot;	  //daily growth potential
        /// <summary>The P_D growth w</summary>
        private double p_dGrowthW;		//daily growth with water-deficit incoprporated
        /// <summary>The P_D growth</summary>
        private double p_dGrowth;		 //daily growth
        /// <summary>The P_D herbage</summary>
        private double p_dHerbage;		//daily herbage (total standing DM) increae
        /// <summary>The P_D litter</summary>
        private double p_dLitter;		 //daily litter formation
        /// <summary>The P_D root sen</summary>
        private double p_dRootSen;		//daily root senescence
        /// <summary>The P_D n litter</summary>
        private double p_dNLitter;		//daily litter formation
        /// <summary>The P_D n root sen</summary>
        private double p_dNRootSen;	   //daily root senescence

        //p_... variables are pasture states at a given time (day)
        
        //private double p_fShoot;		  //actual fraction of dGrowth to shoot
        /// <summary>The p_height</summary>
        private double p_height;		  // Canopy height (mm)
        /// <summary>The p_green lai</summary>
        private double p_greenLAI;
        /// <summary>The p_dead lai</summary>
        private double p_deadLAI;
        /// <summary>The p_total lai</summary>
        private double p_totalLAI;
        /// <summary>The p_light ext coeff</summary>
        private double p_lightExtCoeff;
        /// <summary>The p_green dm</summary>
        private double p_greenDM;		 //green is the live aboveground herbage mass, kgDM/ha
        /// <summary>The p_dead dm</summary>
        private double p_deadDM;
        /// <summary>The p_total dm</summary>
        private double p_totalDM;

        /// <summary>The p_root mass</summary>
        private double p_rootMass;		//total root mass
        /// <summary>The p_root frontier</summary>
        private double p_rootFrontier;	//depth of root frontier

        //soil
        /// <summary>The p_bottom root layer</summary>
        private double p_bottomRootLayer;   //the soil layer just below root zone
        /// <summary>The p_soil ndemand</summary>
        private double p_soilNdemand;	   //plant N demand (shoot + root) for daily growth from soil (excludingfixation and remob)
        // private double p_soilNdemandMax;	//plant N demand with luxury uptake
        /// <summary>The p_soil navailable</summary>
        private double p_soilNavailable;	//Plant available N in soil kgN/ha, at the present day
        /// <summary>The p_soil nuptake</summary>
        private double p_soilNuptake;	   //Plant N uptake, daily
        /// <summary>The sn supply</summary>
        private double[] SNSupply;
        /// <summary>The sn uptake</summary>
        private double[] SNUptake;
        /// <summary>The p_ nfix</summary>
        private double p_Nfix = 0;
        /// <summary>The P_GFN</summary>
        private double p_gfn;			   // = effect of p_Nstress on growth

        /// <summary>The p_water demand</summary>
        private double p_waterDemand;   // Daily Soil Water Demand (mm)
        /// <summary>The p_water uptake</summary>
        private double p_waterUptake;   // Daily Soil Water uptake (mm)
        /// <summary>The p_water supply</summary>
        private double p_waterSupply;   // plant extractable soil moisture (mm)
        /// <summary>The sw supply</summary>
        private double[] SWSupply;
        /// <summary>The sw uptake</summary>
        private double[] SWUptake;
        /// <summary>The p_gfwater</summary>
        private double p_gfwater;	   // = effects of water stress on growth
        /// <summary>The p_gftemp</summary>
        private double p_gftemp;

        /// <summary>The p_harvest dm</summary>
        private double p_harvestDM;			  //daily harvested dm
        /// <summary>The p_harvest n</summary>
        private double p_harvestN;			   //daily harvested n
        /// <summary>The p_harvest digest</summary>
        private double p_harvestDigest;
        //private double p_herbageDigest;
        /// <summary>The p_ live</summary>
        private bool p_Live = true;			  //flag signialling crop is live (not killed)

        //temporary testing, will be removed later when IL1 can be get from micromet
        /// <summary>The canopies number</summary>
        private int canopiesNum = 1;			//number of canpy including this one
        /// <summary>The canopies radn</summary>
        private double[] canopiesRadn = null;   //Radn intercepted by canopies

        #endregion

        #region Initial and daily settings

        //----------------------------------------------------------------
        /// <summary>Initialise parameters</summary>
        private void InitParameters()
        {
            // zero out the global variables
            p_dGrowthPot = 0.0;
            p_dGrowthW = 0.0;
            p_dGrowth = 0.0;
            p_dHerbage = 0.0;
            p_height = 0.0;

            p_dLitter = 0.0;
            p_dRootSen = 0.0;
            p_dNLitter = 0.0;
            p_dNRootSen = 0.0;
            p_bottomRootLayer = 0;

            //Parameters for environmental factors
            p_soilNdemand = 0;
            p_soilNavailable = 0;
            p_soilNuptake = 0;
            p_gfn = 0;
            p_Nfix = 0.0;
            p_gftemp = 0.0;
            p_gfwater = 0.0;
            p_harvestN = 0.0;

            p_waterSupply = 0;
            p_waterDemand = 0;
            p_waterUptake = 0;
            p_gfwater = 0;

            p_rootFrontier = 0.0;
            p_rootMass = 0.0;
            p_greenLAI = 0.0;
            p_deadLAI = 0.0;
            p_greenDM = 0.0;
            p_deadDM = 0.0;

            SWSupply = new double[Soil.Thickness.Length];
            SWUptake = new double[Soil.Thickness.Length];
            SNSupply = new double[Soil.Thickness.Length];
            SNUptake = new double[Soil.Thickness.Length];

            // check that initialisation fractions have been supplied accordingly
            Array.Resize(ref initialDMFractions_grass, 11);
            Array.Resize(ref initialDMFractions_legume, 11);

            Species.iniDMFrac_grass = initialDMFractions_grass;
            Species.iniDMFrac_legume = initialDMFractions_legume;

            SP = new Species[numSpecies];
            for (int s = 0; s < numSpecies; s++)
            {
                InitSpeciesValues(s);
            }

            //Initialising the aggregated pasture parameters from initial valuses of each species
            double sum_lightExtCoeff = 0.0;
            for (int s = 0; s < numSpecies; s++)
            {
                //accumulate LAI of all species
                p_greenLAI += SP[s].greenLAI;
                p_deadLAI += SP[s].deadLAI;

                p_greenDM += SP[s].dmgreen;
                p_deadDM += SP[s].dmdead;

                //accumulate the sum for weighted average
                sum_lightExtCoeff += SP[s].lightExtCoeff * SP[s].totalLAI;

                //Set the deepest root frontier
                if (SP[s].rootDepth > p_rootFrontier)
                    p_rootFrontier = SP[s].rootDepth;

                p_rootMass += SP[s].dmroot;
            }
            p_totalLAI = p_greenLAI + p_deadLAI;
            p_totalDM = p_greenDM + p_deadDM;

            if (p_totalLAI == 0) { p_lightExtCoeff = 0.5; }
            else { p_lightExtCoeff = sum_lightExtCoeff / p_totalLAI; }

            // rlvp is no longer used in the calculations, it has been super-seeded by RootFraction (the proportion of roots mass in each layer)
            // The RootFraction should add up to 1.0 over the soil profile
            RootFraction = RootProfileDistribution();
        }

        /// <summary>
        /// Set parameter values that each species need to know
        /// - from pasture to species
        /// </summary>
        /// <param name="s">The s.</param>
        private void InitSpeciesValues(int s)
        {
            SP[s] = new Species(speciesName[s]);

            SP[s].isLegume = (speciesNType[s].ToLower() == "legume" ? true : false);
            SP[s].photoPath = speciesCType[s].ToUpper();
            SP[s].dmshoot = iniDMshoot[s];
            if (iniDMroot[s] >= 0.0)
                SP[s].dmroot = iniDMroot[s];
            else
                SP[s].dmroot = iniDMshoot[s] * maxRootFraction[s] / (1 + maxRootFraction[s]);
            SP[s].rootDepth = iniRootDepth[s];

            SP[s].Pm = maxPhotosynthesisRate[s];
            SP[s].maintRespiration = maintenanceRespirationCoef[s];
            SP[s].growthEfficiency = growthEfficiency[s];
            SP[s].lightExtCoeff = lightExtentionCoeff[s];
            SP[s].growthTmin = growthTmin[s];
            SP[s].growthTmax = growthTmax[s];
            SP[s].growthTopt = growthTopt[s];
            SP[s].growthTq = growthTq[s];
            SP[s].massFluxTmin = massFluxTmin[s];
            SP[s].massFluxTopt = massFluxTopt[s];
            SP[s].massFluxW0 = massFluxW0[s];
            SP[s].massFluxWopt = MassFluxWopt[s];
            SP[s].heatOnsetT = heatOnsetT[s];
            SP[s].heatFullT = heatFullT[s];
            SP[s].heatSumT = heatSumT[s];
            SP[s].coldOnsetT = coldOnsetT[s];
            SP[s].coldFullT = coldFullT[s];
            SP[s].coldSumT = coldSumT[s];
            SP[s].SLA = specificLeafArea[s];
            SP[s].SRL = specificRootLength[s];
            SP[s].maxSRratio = (1 - maxRootFraction[s]) / maxRootFraction[s];
            SP[s].allocationSeasonF = allocationSeasonF[s];
            SP[s].fLeaf = fracToLeaf[s];
            SP[s].fStolon = fracToStolon[s];
            SP[s].rateLive2Dead = TurnoverRateLive2Dead[s];
            SP[s].rateDead2Litter = TurnoverRateDead2Litter[s];
            SP[s].rateRootSen = TurnoverRateRootSenescence[s];
            SP[s].stockParameter = StockParameter[s];
            SP[s].digestLive = DigestibilityLive[s];
            SP[s].digestDead = DigestibilityDead[s];
            SP[s].dmgreenmin = MinimumGreenWt[s];
            SP[s].dmdeadmin = MinimumDeadWt[s];
            SP[s].NcleafOpt = LeafNopt[s] * 0.01;   // convert % to fraction
            SP[s].NcleafMax = LeafNmax[s] * 0.01;
            SP[s].NcleafMin = LeafNmin[s] * 0.01;
            SP[s].NcstemFr = RelativeNStems[s];
            SP[s].NcstolFr = RelativeNStolons[s];
            SP[s].NcrootFr = RelativeNRoots[s];
            SP[s].NcRel2 = RelativeNStage2[s];
            SP[s].NcRel3 = RelativeNStage3[s];
            SP[s].MinFix = MinimumNFixation[s];
            SP[s].MaxFix = MaximumNFixation[s];
            SP[s].NdilutCoeff = DillutionCoefN[s];
            SP[s].Kappa2 = Kappa2Remob[s];
            SP[s].Kappa3 = Kappa3Remob[s];
            SP[s].Kappa4 = Kappa4Remob[s];
            SP[s].gfGen = GlfGeneric[s];
            SP[s].referenceCO2 = ReferenceCO2[s];
            SP[s].CO2PmaxScale = offsetCO2EffectOnPhotosynthesis[s];
            SP[s].CO2NScale = OffsetCO2EffectOnNuptake[s];
            SP[s].CO2NMin = MinimumCO2EffectOnNuptake[s];
            SP[s].CO2NCurvature = ExponentCO2EffectOnNuptake[s];
            SP[s].waterStressFactor = WaterStressFactor[s];
            SP[s].soilSatFactor = WaterLoggingFactor[s];

            SP[s].InitValues();
        }

        //---------------------------------------------------------------------------
        /// <summary>Let species know weather conditions</summary>
        /// <returns></returns>
        private bool SetPastureToSpeciesData()
        {
            // Pass some data from Clock
            Species.simToday = clock.Today;

            //pass some metData to species
            Species.DayLength = MetData.DayLength;
            Species.localLatitude = MetData.Latitude;
            Species.Tmax = MetData.MaxT;
            Species.Tmin = MetData.MinT;
            Species.Tmean = 0.5 * (MetData.MaxT + MetData.MinT);
            Species.ambientCO2 = MetData.CO2;

            Species.PlantInterceptedRadn = interceptedRadn;
            Species.PlantCoverGreen = Cover_green;
            Species.PlantLightExtCoeff = p_lightExtCoeff;
            Species.PlantShootWt = AboveGroundWt;   //dm_shoot;

            //partition the intercepted radiation between species
            double sumCoverGreen = 0.0;
            for (int s = 0; s < numSpecies; s++)
                sumCoverGreen += SP[s].coverGreen;

            for (int s = 0; s < numSpecies; s++)
            {
                if (sumCoverGreen == 0)
                    SP[s].intRadnFrac = 0;
                else
                    SP[s].intRadnFrac = SP[s].coverGreen / sumCoverGreen;
            }

            return true;
        }

        //--------------------------------------------------------------------------
        /// <summary>
        /// Set drought stress factor to each species
        /// Worth more efforts in this area
        /// </summary>
        private void SetSpeciesLimitingFactors()
        {

            if (p_waterDemand == 0)
            {
                p_gfwater = 1.0;
                for (int s = 0; s < numSpecies; s++)
                    SP[s].gfwater = p_gfwater;
                return;								 //case (1) return
            }
            if (p_waterDemand > 0 && p_waterUptake == 0)
            {
                p_gfwater = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    SP[s].gfwater = p_gfwater;
                return;								 //case (2) return
            }

            p_gfwater = p_waterUptake / p_waterDemand;
            double spDepth = 0;			  // soil profile depth
            if (p_gfwater > 0.999)  //possible saturation
            {
                // calculate soil moisture content in root zone
                double SW = 0;	  //soil water content
                double Sat = 0;	 //water content at saturation
                double FC = 0;	  //water contenct at field capacity

                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    spDepth += Soil.Thickness[layer];
                    if (spDepth <= p_rootFrontier)
                    {
                        SW += Soil.Water[layer];
                        Sat += Soil.SoilWater.SATmm[layer];
                        FC += Soil.SoilWater.DULmm[layer];
                    }
                }
                if (SW > FC) //if saturated
                {
                    double accum_gfwater = 0;
                    p_greenLAI = 0;	 //update p_greenLAI before using it.
                    for (int s = 0; s < numSpecies; s++)
                    {
                        SP[s].gfwater = 1 - SP[s].soilSatFactor * (SW - FC) / (Sat - FC);
                        accum_gfwater += SP[s].gfwater * SP[s].greenLAI;   //weighted by greenLAI
                        p_greenLAI += SP[s].greenLAI;					  //FLi 19 Sept 2011 for avoiding error of an unupdated
                    }													  //p_greenLAI when using SWIM for waterUptake
                    if (p_greenLAI > 0)
                        p_gfwater = accum_gfwater / p_greenLAI;
                    else
                        p_gfwater = 1.0;
                    return;						 //case (3) return
                }
                //Reaching here is possible (SW < FC) even with a p_gfwater ==1	 //FLi 20 Oct 2012
                //not return, but go though to the case (4) below
            }

            //Original block Set specieS.gfwater = p_gfwater, to distinguish them later
            for (int s = 0; s < numSpecies; s++)
            {
                SP[s].gfwater = p_gfwater;
            }
            //Console.Out.WriteLine("gfwater4: " + p_gfwater);
            return;									 //case (4) return
        }

        //--------------------------------------------------------------------------
        /// <summary>plant growth and partitioning and tissue turnover</summary>
        private void GrowthAndPartition()
        {
            p_greenLAI = 0;
            p_deadLAI = 0;

            p_greenDM = 0.0;
            p_deadDM = 0.0;

            p_dHerbage = 0.0;
            p_rootMass = 0.0;

            p_dLitter = 0;
            p_dNLitter = 0;

            p_dRootSen = 0;
            p_dNRootSen = 0;

            for (int s = 0; s < numSpecies; s++)
            {
                SP[s].PartitionTurnover();

                p_greenLAI += SP[s].greenLAI;
                p_deadLAI += SP[s].deadLAI;

                p_greenDM += SP[s].dmgreen;
                p_deadDM += SP[s].dmdead;
                p_rootMass += SP[s].dmroot;

                p_dHerbage += (SP[s].dmshoot - SP[s].pS.dmshoot);

                p_dLitter += SP[s].dLitter;
                p_dNLitter += SP[s].dNLitter;

                p_dRootSen += SP[s].dRootSen;
                p_dNRootSen += SP[s].dNrootSen;
            }

            p_totalLAI = p_greenLAI + p_deadLAI;
            p_totalDM = p_greenDM + p_deadDM;

            //litter return to surface OM completely (frac = 1.0)
            DoSurfaceOMReturn(p_dLitter, p_dNLitter, 1.0);

            //Root FOM return
            DoIncorpFomEvent(p_dRootSen, p_dNRootSen);

        }

        #endregion

        # region Canopy interface
        /// <summary>Canopy type</summary>
        public string CanopyType { get { return SwardName; } }

        /// <summary>Gets the LAI (m^2/m^2)</summary>
        public double LAI { get { return p_greenLAI; } }

        /// <summary>Gets the maximum LAI (m^2/m^2)</summary>
        public double LAITotal { get { return p_totalLAI; } }

        /// <summary>Gets the cover green (0-1)</summary>
        public double CoverGreen { get { return Cover_green; } }

        /// <summary>Gets the cover total (0-1)</summary>
        public double CoverTotal { get { return Cover_tot; } }

        /// <summary>Gets the canopy height (mm)</summary>
        [Description("Sward average height")]                 //needed by micromet
        [Units("mm")]
        public double Height
        {
            get { return p_height; }
        }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Depth { get { return HeightfromDM; } }

        /// <summary>Gets  FRGR.</summary>
        public double FRGR 
        {
            get
            {
                p_gftemp = 0;	 //weighted average

                double Tday = 0.75 * MetData.MaxT + 0.25 * MetData.MinT; //Tday
                for (int s = 0; s < numSpecies; s++)
                {
                    double prop = 1.0 / numSpecies;
                    if (p_greenDM != 0.0)
                    {
                        prop = SP[s].dmgreen / AboveGroundLiveWt;   // dm_green;
                    }
                    p_gftemp += SP[s].GFTemperature(Tday) * prop;
                }

                double gft = 1;
                if (Tday < 20) gft = Math.Sqrt(p_gftemp);
                else gft = p_gftemp;
                // Note: p_gftemp is for gross photosysthsis.
                // This is different from that for net production as used in other APSIM crop models, and is
                // assumesd in calculation of temperature effect on transpiration (in micromet).
                // Here we passed it as sqrt - (Doing so by a comparison of p_gftemp and that
                // used in wheat). Temperature effects on NET produciton of forage species in other models
                // (e.g., grassgro) are not so significant for T = 10-20 degrees(C)

                //Also, have tested the consequences of passing p_Ncfactor in (different concept for gfwater),
                //coulnd't see any differnece for results
                return Math.Min(FVPD, gft);
                // RCichota, Jan/2014: removed AgPasture's Frgr from here, it is considered at the same level as nitrogen etc...
            }
        }

        /// <summary>MicroClimate supplies PotentialEP</summary>
        [XmlIgnore]
        public double PotentialEP
        {
            get
            {
                return p_waterDemand;
            }
            set
            {
                p_waterDemand = value;
            }
        }

        /// <summary>MicroClimate supplies LightProfile</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
        {
            get
            {
                return myLightProfile;
            }
            set
            {
                myLightProfile = value;
                canopiesNum = myLightProfile.Length;
                canopiesRadn = new double[canopiesNum];

                interceptedRadn = 0;
                for (int j = 0; j < canopiesNum; j++)
                {
                    interceptedRadn += myLightProfile[j].amount;
                }
                if (canopiesNum > 0)
                    canopiesRadn[0] = interceptedRadn;
            }
        }

        #endregion

        #region EventHandlers

        /// <summary>Eventhandeler - initialisation</summary>
        /// <exception cref="System.Exception">When working with multiple species, 'ValsMode' must ALWAYS be 'none'</exception>
        [EventSubscribe("Initialised")]
        private void Initialise() //overrides Sub init2()
        {
            InitParameters();			// Init parameters after reading the data

            if (MetData.StartDate != new DateTime(0))
                SetPastureToSpeciesData();		 // This is needed for the first day after knowing the number of species

            FractionToHarvest = new double[numSpecies];

            alt_N_uptake = alt_N_uptake.ToLower();
            if (alt_N_uptake == "yes")
                if (numSpecies > 1)
                    throw new Exception("When working with multiple species, 'ValsMode' must ALWAYS be 'none'");
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            HaveInitialised = false;
        }

        //---------------------------------------------------------------------
        /// <summary>EventHandeler - preparation befor the main process</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (!HaveInitialised)
            {
                Initialise();
                HaveInitialised = true;
            }

            for (int s = 0; s < numSpecies; s++)
                SP[s].DailyRefresh();
        }

        //---------------------------------------------------------------------
        /// <summary>Called when [do plant growth].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (!p_Live)
                return;

            //**Remember last status, and update root depth frontier (root depth mainly for annuals)
            for (int s = 0; s < numSpecies; s++)
            {
                SP[s].SetPrevPools(); //pool values yesterday is also retained in current state

                double spRootDepth = SP[s].rootGrowth();	//update root depth
                if (p_rootFrontier < spRootDepth)
                    p_rootFrontier = spRootDepth;
            }

            // clear FractionHarvest by assigning new
            FractionToHarvest = new double[numSpecies];

            // pass some global variables to each species
            SetPastureToSpeciesData();

            //** advance phenology
            int anyEmerged = 0;
            for (int s = 0; s < numSpecies; s++)
            {
                anyEmerged += SP[s].Phenology();
            }

            //**Potential growth
            p_dGrowthPot = 0.0;
            for (int s = 0; s < numSpecies; s++)
            {
                //p_dGrowthPot += SP[s].DailyGrowthPot();   // alternative way for calclating potential growth
                p_dGrowthPot += SP[s].DailyEMGrowthPot();   //pot here incorporated [N] effects
            }


            //**Calculate soil N available in root zone
            p_soilNavailable = calcPlantAvailableN();
            //p_soilNavailable = calcPlantExtractableN();   //need to do more validation/calibration for activating this
            //**Water supply & uptake
            if (WaterUptakeSource == "calc")
            {
                p_waterUptake = SWUptakeProcess();	  //actual uptake by all species
            }
            else
            {
                //Water uptake be calculated by other modules (e.g., SWIM) and got by [EventHandler]
            }
            SetSpeciesLimitingFactors();  // * root competition for water when SM is deficit: species-specific ?

            //**add drought effects (before considering other nutrient limitation)
            p_dGrowthW = 0;
            for (int s = 0; s < numSpecies; s++)
            {
                p_dGrowthW += SP[s].DailyGrowthW();
            }
            double nuptake = NBudgetAndUptake();

            //**actual daily growth
            p_dGrowth = 0;
            if (clock.Today.Day >= 30)
                p_dGrowth += 0.0;
            for (int s = 0; s < numSpecies; s++)
                p_dGrowth += SP[s].DailyGrowthAct();

            //**partitioning & turnover
            GrowthAndPartition();	   // litter returns to surfaceOM; Root returns to soil FOM dead in this turnover routines
        }

        //----------------------------------------------------------------------
        /// <summary>Onremove_crop_biomasses the specified rm.</summary>
        /// <param name="rm">The rm.</param>
        [EventSubscribe("RemoveCropBiomass")]
        private void Onremove_crop_biomass(RemoveCropBiomassType rm)
        {
            //Note: It is resposibility of the calling module to check the
            // amount of herbage in each pools of AbovegroundBiomassWt and set the
            // the correct amount in 'rm'.
            // No checking if the removing amount passed in are too much here

            const double gm2ha = 10;   // constant for conversion of g/m^2 to kg/ha,
            // rm.dm.dlt should be in g/m^2

            double dm_leaf_green = LeafLiveWt;
            double dm_stem_green = StemLiveWt;
            double dm_leaf_dead = LeafDeadWt;
            double dm_stem_dead = StemDeadWt;

            for (int s = 0; s < numSpecies; s++)	 // for accumulating the total DM & N removal of species from verious pools
            {
                SP[s].dmdefoliated = 0.0;
                SP[s].Ndefoliated = 0.0;
            }

            for (int i = 0; i < rm.dm.Length; i++)			  //for each pool
            {
                for (int j = 0; j < rm.dm[i].dlt.Length; j++)   //for each part
                {
                    if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "leaf")
                    {
                        for (int s = 0; s < numSpecies; s++)		   //for each species
                        {
                            if (dm_leaf_green != 0)			 //resposibility of other modules to check the amount
                            {
                                double rm_leaf = gm2ha * rm.dm[i].dlt[j] * SP[s].dmleaf_green / dm_leaf_green;
                                double rm_leaf1 = rm_leaf * SP[s].dmleaf1 / SP[s].dmleaf_green;
                                double rm_leaf2 = rm_leaf * SP[s].dmleaf2 / SP[s].dmleaf_green;
                                double rm_leaf3 = rm_leaf * SP[s].dmleaf3 / SP[s].dmleaf_green;
                                SP[s].dmleaf1 -= rm_leaf1;
                                SP[s].dmleaf2 -= rm_leaf2;
                                SP[s].dmleaf3 -= rm_leaf3;
                                SP[s].dmdefoliated += rm_leaf1 + rm_leaf2 + rm_leaf3;

                                SP[s].Nleaf1 -= SP[s].Ncleaf1 * rm_leaf1;
                                SP[s].Nleaf2 -= SP[s].Ncleaf2 * rm_leaf2;
                                SP[s].Nleaf3 -= SP[s].Ncleaf3 * rm_leaf3;
                                SP[s].Ndefoliated += SP[s].Ncleaf1 * rm_leaf1 + SP[s].Ncleaf2 * rm_leaf2 + SP[s].Ncleaf3 * rm_leaf3;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "stem")
                    {
                        for (int s = 0; s < numSpecies; s++)
                        {
                            if (dm_stem_green != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_stem = gm2ha * rm.dm[i].dlt[j] * SP[s].dmstem_green / dm_stem_green;
                                double rm_stem1 = rm_stem * SP[s].dmstem1 / SP[s].dmstem_green;
                                double rm_stem2 = rm_stem * SP[s].dmstem2 / SP[s].dmstem_green;
                                double rm_stem3 = rm_stem * SP[s].dmstem3 / SP[s].dmstem_green;
                                SP[s].dmstem1 -= rm_stem1;
                                SP[s].dmstem2 -= rm_stem2;
                                SP[s].dmstem3 -= rm_stem3;
                                SP[s].dmdefoliated += rm_stem1 + rm_stem2 + rm_stem3;

                                SP[s].Nstem1 -= SP[s].Ncstem1 * rm_stem1;
                                SP[s].Nstem2 -= SP[s].Ncstem2 * rm_stem2;
                                SP[s].Nstem3 -= SP[s].Ncstem3 * rm_stem3;
                                SP[s].Ndefoliated += SP[s].Ncstem1 * rm_stem1 + SP[s].Ncstem2 * rm_stem2 + SP[s].Ncstem3 * rm_stem3;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "leaf")
                    {
                        for (int s = 0; s < numSpecies; s++)
                        {
                            if (dm_leaf_dead != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_leaf4 = gm2ha * rm.dm[i].dlt[j] * SP[s].dmleaf4 / dm_leaf_dead;
                                SP[s].dmleaf4 -= rm_leaf4;
                                SP[s].dmdefoliated += rm_leaf4;

                                SP[s].Ndefoliated += SP[s].Ncleaf4 * rm_leaf4;
                                SP[s].Nleaf4 -= SP[s].Ncleaf4 * rm_leaf4;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "stem")
                    {
                        for (int s = 0; s < numSpecies; s++)
                        {
                            if (dm_stem_dead != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_stem4 = gm2ha * rm.dm[i].dlt[j] * SP[s].dmstem4 / dm_stem_dead;
                                SP[s].dmstem4 -= rm_stem4;
                                SP[s].dmdefoliated += rm_stem4;

                                SP[s].Nstem4 -= SP[s].Ncstem4 * rm_stem4;
                                SP[s].Ndefoliated += SP[s].Ncstem4 * rm_stem4;
                            }
                        }
                    }
                }
            }

            p_harvestDM = 0;
            p_harvestN = 0;
            for (int s = 0; s < numSpecies; s++)
            {
                p_harvestDM += SP[s].dmdefoliated;
                p_harvestN += SP[s].Ndefoliated;
                SP[s].updateAggregated();
            }

            //In this routine of no selection among species, the removed tissue from different species
            //will be in proportion with exisisting mass of each species.
            //The digetibility below is an approximation (= that of pasture swards).
            //It is more reasonable to calculate it organ-by-organ for each species, then put them together.
            p_harvestDigest = HerbageDigestibility;

        }

        //----------------------------------------------------------------------
        /// <summary>Harvests the specified type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="amount">The amount.</param>
        public void Harvest(String type, double amount)  //Being called not by Event
        {
            GrazeType GZ = new GrazeType();
            GZ.amount = amount;
            GZ.type = type;
            OnGraze(GZ);
        }

        /// <summary>Grazes the specified type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="amount">The amount.</param>
        public void Graze(string type, double amount)
        {
            if ((!p_Live) || p_totalDM == 0)
                return;

            // get the amount that can potentially be removed
            double AmountRemovable = 0.0;
            for (int s = 0; s < numSpecies; s++)
                AmountRemovable += Math.Max(0.0, SP[s].dmleaf_green + SP[s].dmstem_green - SP[s].dmgreenmin) + Math.Max(0.0, SP[s].dmdead - SP[s].dmdeadmin);
            AmountRemovable = Math.Max(0.0, AmountRemovable);

            // get the amount required to remove
            double AmountRequired = 0.0;
            if (type.ToLower() == "SetResidueAmount".ToLower())
            {
                // Remove all DM above given residual amount
                AmountRequired = Math.Max(0.0, StandingPlantWt - amount);
            }
            else if (type.ToLower() == "SetRemoveAmount".ToLower())
            {
                // Attempt to remove a given amount
                AmountRequired = Math.Max(0.0, amount);
            }
            else
            {
                Console.WriteLine("  AgPasture - Method to set amount to remove not recognized, command will be ignored");
            }
            // get the actual amount to remove
            double AmountToRemove = Math.Min(AmountRequired, AmountRemovable);

            p_harvestDM = AmountToRemove;
            p_harvestN = 0.0;
            p_harvestDigest = 0.0;

            // get the amounts to remove by species:
            double FractionNotRemoved = 0.0;
            if (AmountRemovable > 0.0)
                FractionNotRemoved = Math.Max(0.0, (AmountRemovable - AmountToRemove) / AmountRemovable);
            double[] TempWeights = new double[numSpecies];
            double[] TempAmounts = new double[numSpecies];
            double TempTotal = 0.0;
            if (AmountRequired > 0.0)
            {
                // get the weights for each species, consider preference and available DM
                double TotalPreference = 0.0;
                for (int s = 0; s < numSpecies; s++)
                    TotalPreference += preferenceForGreenDM[s] + preferenceForDeadDM[s];
                for (int s = 0; s < numSpecies; s++)
                {
                    TempWeights[s] = preferenceForGreenDM[s] + preferenceForDeadDM[s];
                    TempWeights[s] += (TotalPreference - TempWeights[s]) * (1 - FractionNotRemoved);
                    TempAmounts[s] = Math.Max(0.0, SP[s].dmleaf_green + SP[s].dmstem_green - SP[s].dmgreenmin) + Math.Max(0.0, SP[s].dmdead - SP[s].dmdeadmin);
                    TempTotal += TempAmounts[s] * TempWeights[s];
                }

                // get the actual amounts to remove for each species
                for (int s = 0; s < numSpecies; s++)
                {
                    if (TempTotal > 0.0)
                        FractionToHarvest[s] = Math.Max(0.0, Math.Min(1.0, TempWeights[s] * TempAmounts[s] / TempTotal));
                    else
                        FractionToHarvest[s] = 0.0;
                    p_harvestN += SP[s].RemoveDM(AmountToRemove * FractionToHarvest[s], preferenceForGreenDM[s], preferenceForDeadDM[s]);

                    // get digestibility of harvested material
                    p_harvestDigest += SP[s].digestDefoliated * SP[s].dmdefoliated / AmountToRemove;
                }
            }
        }

        //----------------------------------------------------------------------
        /// <summary>Called when [graze].</summary>
        /// <param name="GZ">The gz.</param>
        [EventSubscribe("Graze")]
        private void OnGraze(GrazeType GZ)
        {
            Graze(GZ.type, GZ.amount);
        }

        //----------------------------------------------------------
        /// <summary>Called when [water uptakes calculated].</summary>
        /// <param name="SoilWater">The soil water.</param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
        {
            // Gets the water uptake for each layer as calculated by an external module (SWIM)
            p_waterUptake = 0;
            for (int i_Crop = 0; i_Crop != SoilWater.Uptakes.Length; i_Crop++)
            {
                string MyName = SoilWater.Uptakes[i_Crop].Name;
                if (MyName == SwardName)
                {
                    int length = SoilWater.Uptakes[i_Crop].Amount.Length;
                    for (int layer = 0; layer < length; layer++)
                    {
                        SWUptake[layer] = SoilWater.Uptakes[i_Crop].Amount[layer];
                        p_waterUptake += SoilWater.Uptakes[i_Crop].Amount[layer];
                    }
                }
            }
        }

        //----------------------------------------------------------------------        
        /// <summary>Sows the plant</summary>
        /// <param name="cultivar"></param>
        /// <param name="population"></param>
        /// <param name="depth"></param>
        /// <param name="rowSpacing"></param>
        /// <param name="maxCover"></param>
        /// <param name="budNumber"></param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {
            /*SowType is our type and is defined like this:
            <type name="Sow">
              <field name="Cultivar" kind="string" />
              <field name="Population" kind="double" />
              <field name="Depth" kind="double" />
              <field name="MaxCover" kind="double" />
              <field name="BudNumber" kind="double" />
            </type>
            */

            p_Live = true;
            ResetZero();
            for (int s = 0; s < numSpecies; s++)
                SP[s].SetInGermination();

        }

        //----------------------------------------------------------------------
        /// <summary>Called when [kill crop].</summary>
        /// <param name="PKill">The p kill.</param>
        [EventSubscribe("KillCrop")]
        private void OnKillCrop(KillCropType PKill)
        {
            double frac = PKill.KillFraction;
            //always complete kill for pasture, ignore fraction

            //Above_ground part returns to surface OM comletey (frac = 1.0)
            DoSurfaceOMReturn(p_totalDM, AboveGroundN, 1.0);	//n_shoot

            //Incorporate root mass in soil fresh organic matter
            DoIncorpFomEvent(p_rootMass, BelowGroundN);		 //n_root);

            ResetZero();

            p_Live = false;
        }

        //-----------------------------------------------------------------------
        /// <summary>Resets the zero.</summary>
        private void ResetZero()
        {
            //shoot
            p_greenLAI = 0;
            p_deadLAI = 0;
            p_totalLAI = 0;
            p_greenDM = 0;
            p_deadDM = 0;
            p_totalDM = 0;
            p_height = 0;

            //root
            p_rootMass = 0;
            p_rootFrontier = 0;

            //daily changes
            p_dGrowthPot = p_dGrowthW = p_dGrowth = p_dHerbage = 0;   //daily DM increase
            p_dLitter = p_dNLitter = 0;
            p_dRootSen = p_dNRootSen = 0;

            p_waterDemand = p_waterUptake = 0;
            p_soilNdemand = p_soilNuptake = 0;

            //species (ignore fraction)
            for (int s = 0; s < numSpecies; s++)
                SP[s].ResetZero();

        }
        //-----------------------------------------------------------------------
        /// <summary>Calculates the plant available n.</summary>
        /// <returns></returns>
        private double calcPlantAvailableN()
        {
            p_soilNavailable = 0;
            double spDepth = 0;		 // depth before next soil layer
            int layer = 0;
            for (layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                if (spDepth <= p_rootFrontier)
                {
                    /* an approach for controlling N uptake
                    const double KNO3 = 0.1F;
                    const double KNH4 = 0.1F;
                    double swaf = 1.0;
                    swaf = (sw_dep[layer] - ll[layer]) / (DUL[layer] - ll[layer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    p_soilNavailable += (no3[layer] * KNO3 + nh4[layer] * KNH4 ) * swaf;
                    SNSupply[layer] = (no3[layer] * KNO3 + nh4[layer] * KNH4 ) * (double)swaf;
                    */
                    //original below
                    p_soilNavailable += (Soil.NO3N[layer] + Soil.NH4N[layer]);
                    SNSupply[layer] = (Soil.NO3N[layer] + Soil.NH4N[layer]);
                }
                else
                {
                    p_bottomRootLayer = layer;
                    break;
                }

                spDepth += Soil.Thickness[layer];

            }

            if (p_bottomRootLayer == 0 && layer > 0)
                p_bottomRootLayer = layer - 1;

            return p_soilNavailable;
        }

        //-----------------------------------------------------------------------
        /// <summary>Calculates the plant extractable n.</summary>
        /// <returns></returns>
        private double calcPlantExtractableN()	// not all minN is extractable
        {
            p_soilNavailable = 0;
            double spDepth = 0;		 // depth before next soil layer
            int layer = 0;
            SoilCrop soilCrop = this.Soil.Crop(Name) as SoilCrop;
                    
            for (layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                if (spDepth <= p_rootFrontier)
                {
                    //an approach for controlling N uptake
                    const double KNO3 = 0.1;
                    const double KNH4 = 0.1;
                    double swaf = 1.0;
                    swaf = (Soil.Water[layer] - soilCrop.LL[layer]) / (Soil.SoilWater.DUL[layer] - soilCrop.LL[layer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    p_soilNavailable += (Soil.NO3N[layer] * KNO3 + Soil.NH4N[layer] * KNH4) * Math.Pow(swaf, 0.25);
                    SNSupply[layer] = (Soil.NO3N[layer] * KNO3 + Soil.NH4N[layer] * KNH4) * Math.Pow(swaf, 0.25);

                    //original below
                    //p_soilNavailable += (no3[layer] + nh4[layer]);
                    //SNSupply[layer] = (no3[layer] + nh4[layer]);
                }
                else
                {
                    p_bottomRootLayer = layer;
                    break;
                }

                spDepth += Soil.Thickness[layer];

            }

            if (p_bottomRootLayer == 0 && layer > 0)
                p_bottomRootLayer = layer - 1;

            return p_soilNavailable;
        }

        // RCichota, Jun 2014: cleaned up and add consideration for remobilisation of luxury N
        /// <summary>ns the budget and uptake.</summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private double NBudgetAndUptake()
        {
            //1) Get the total N demand (species by species)
            p_Nfix = 0.0;
            double p_Ndemand = 0.0;
            double p_NdemandOpt = 0.0;
            for (int s = 0; s < numSpecies; s++)
            {
                p_Nfix += SP[s].CalcNdemand();       //minimum N fixation
                p_NdemandOpt += SP[s].NdemandOpt;    //demand for optimum [N]
                p_Ndemand += SP[s].NdemandLux;       //demand for luxury [N]
            }

            //2) Update Nfix of legume species under N stress
            double Nstress = 1.0;
            if (p_Ndemand > 0.0 && (p_Ndemand > p_soilNavailable + p_Nfix))
                Nstress = p_soilNavailable / (p_Ndemand - p_Nfix);

            for (int s = 0; s < numSpecies; s++)
            {
                if ((SP[s].isLegume) && (Nstress < 0.99))  //more fixation under Nstress
                {
                    double newNfix = (SP[s].MaxFix - (SP[s].MaxFix - SP[s].MinFix) * Nstress) * SP[s].NdemandLux;
                    double moreNfix = Math.Max(0.0, (newNfix - SP[s].Nfix));
                    SP[s].Nfix = newNfix;
                    p_Nfix += moreNfix;
                }
            }
            
            //3) Get N remobilised and calculate N demand from soil
            p_soilNdemand = 0.0;
            for (int s = 0; s < numSpecies; s++)
            {
                if (SP[s].NdemandLux <= SP[s].Nremob + SP[s].Nfix)
                {
                    // Nremob and Nfix are able to supply all N - note: Nfix = 0 for non-legumes
                    SP[s].remob2NewGrowth = Math.Max(0.0, SP[s].NdemandLux - SP[s].Nfix);
                    SP[s].Nremob -= SP[s].remob2NewGrowth;
                    SP[s].soilNdemand = 0.0;
                }
                else
                {
                    // not enough N within the plant, uptake is needed
                    SP[s].remob2NewGrowth = SP[s].Nremob;
                    SP[s].Nremob = 0.0;
                    SP[s].soilNdemand = SP[s].NdemandLux - (SP[s].Nfix + SP[s].remob2NewGrowth);
                }
                SP[s].newGrowthN = SP[s].remob2NewGrowth + SP[s].Nfix;
                p_soilNdemand += SP[s].soilNdemand;
            }

            //4) Compute soil N uptake, newGrowthN and N limitation factor
            p_soilNuptake = 0.0;
            p_gfn = 0.0;
            for (int s = 0; s < numSpecies; s++)
            {
                if (SP[s].soilNdemand == 0.0)
                {
                    SP[s].soilNuptake = 0.0;
                    SP[s].NFastRemob3 = 0.0;
                    SP[s].NFastRemob2 = 0.0;
                    SP[s].gfn = 1.0;
                }
                else
                {
                    if (p_soilNavailable >= p_soilNdemand)
                    {
                        // soil can supply all remaining N needed
                        SP[s].soilNuptake = SP[s].soilNdemand;
                        SP[s].NFastRemob3 = 0.0;
                        SP[s].NFastRemob2 = 0.0;
                        SP[s].newGrowthN += SP[s].soilNuptake;
                        SP[s].gfn = 1.0;
                    }
                    else
                    {
                        // soil cannot supply all N needed. Get the available N and partition between species
                        SP[s].soilNuptake = p_soilNavailable * SP[s].soilNdemand / p_soilNdemand;
                        SP[s].newGrowthN += SP[s].soilNuptake;

                        // check whether demand for optimum growth is satisfied
                        if (SP[s].NdemandOpt > SP[s].newGrowthN)
                        {
                            // plant still needs more N for optimum growth (luxury uptake is ignored), check whether luxury N in plants can be used
                            double Nmissing = SP[s].NdemandOpt - SP[s].newGrowthN;
                            if (Nmissing <= SP[s].NLuxury2 + SP[s].NLuxury3)
                            {
                                // There is luxury N that can be used for optimum growth, first from tissue 3
                                if (Nmissing <= SP[s].NLuxury3)
                                {
                                    SP[s].NFastRemob3 = Nmissing;
                                    SP[s].NFastRemob2 = 0.0;
                                    Nmissing = 0.0;
                                }
                                else
                                {
                                    SP[s].NFastRemob3 = SP[s].NLuxury3;
                                    Nmissing -= SP[s].NLuxury3;

                                    // remaining from tissue 2
                                    SP[s].NFastRemob2 = Nmissing;
                                    Nmissing = 0.0;
                                }
                            }
                            else
                            {
                                // N luxury is not enough for optimum growth, use up all there is
                                if (SP[s].NLuxury2 + SP[s].NLuxury3 > 0)
                                {
                                    SP[s].NFastRemob3 = SP[s].NLuxury3;
                                    SP[s].NFastRemob2 = SP[s].NLuxury2;
                                    Nmissing -= (SP[s].NLuxury3 + SP[s].NLuxury2);
                                }
                            }
                            SP[s].newGrowthN += SP[s].NFastRemob3 + SP[s].NFastRemob2;
                        }
                        else
                        {
                            // N supply is enough for optimum growth, although luxury uptake is not fully accomplished
                            SP[s].NFastRemob3 = 0.0;
                            SP[s].NFastRemob2 = 0.0;
                        }
                        SP[s].gfn = Math.Min(1.0, Math.Max(0.0, SP[s].newGrowthN / SP[s].NdemandOpt));
                    }
                }
                p_soilNuptake += SP[s].soilNuptake;

                //weighted average of species gfn
                if (p_dGrowthW == 0)
                {
                    p_gfn = 1;
                }
                else
                {
                    p_gfn += SP[s].gfn * SP[s].dGrowthW / p_dGrowthW;
                }
                if (SP[s].gfn < 1.0)
                    p_soilNavailable += 0;
            }

            //5) Actual uptake, remove N from soil
            double soilNremoved = 0;
            if (nUptakeSource == "calc")
            {
                soilNremoved = SNUptakeProcess();               //N remove from soil
            }
            else
            {
                // N uptake calculated by other modules (e.g., SWIM)
                string msg = "Only one option for N uptake is implemented in AgPasture. Please specify N uptake source as default \"calc\".";
                throw new Exception(msg);
            }

            return soilNremoved;

        }

        #endregion //Eventhandlers

        #region Functions

        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">The soil state</param>
        /// <returns></returns>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            throw new NotImplementedException();
        }
        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">The soil state</param>
        /// <returns></returns>
        public List<ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetSWUptake(List<ZoneWaterAndN> info)
        { }
        /// <summary>
        /// Set the  uptanke for today
        /// </summary>
        public void SetNUptake(List<ZoneWaterAndN> info)
        { }

        //===============================================
        /// <summary>
        /// water uptake processes ...
        /// Rainss Notes 20010707
        /// - Should this be done per species? Is using the root frontier an acceptable solution?
        /// - Plant2 breaks this into two parts: WaterSupply and DoWaterUptake
        /// </summary>
        /// <returns></returns>
        private double SWUptakeProcess()
        {
            SoilCrop soilCrop = this.Soil.Crop(Name) as SoilCrop;

            //find out soil available water
            p_waterSupply = 0;
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                SWSupply[layer] = Math.Max(0.0, soilCrop.KL[layer] * (Soil.Water[layer] - soilCrop.LL[layer] * (Soil.Thickness[layer])))
                                * LayerFractionForRoots(layer, p_rootFrontier);
                if (layer < p_bottomRootLayer)
                    p_waterSupply += SWSupply[layer];
            }

            //uptake in proportion
            PMF.WaterChangedType WaterUptake = new PMF.WaterChangedType();
            WaterUptake.DeltaWater = new double[Soil.Thickness.Length];
            //double[] SWUptake = new double[dlayer.Length];
            double Fraction = Math.Min(1.0, p_waterDemand / p_waterSupply);
            double actualUptake = 0.0;
            for (int layer = 0; layer < p_bottomRootLayer; layer++)
            {   //water are taken up only in top layers that root can reach.
                SWUptake[layer] = SWSupply[layer] * Fraction;
                actualUptake += SWUptake[layer];
                WaterUptake.DeltaWater[layer] = -SWUptake[layer];
            }

            if (WaterChanged != null)
                WaterChanged.Invoke(WaterUptake);

            return actualUptake;
        }

        /// <summary>Compute the distribution of roots in the soil profile (sum is equal to one)</summary>
        /// <returns>The proportion of root mass in each soil layer</returns>
        /// <exception cref="System.Exception">
        /// No valid method for computing root distribution was selected
        /// or
        /// Could not calculate root distribution
        /// </exception>
        private double[] RootProfileDistribution()
        {
            int nLayers = Soil.Thickness.Length;
            double[] result = new double[nLayers];
            double sumProportion = 0;

            switch (rootDistributionMethod.ToLower())
            {
                case "homogeneous":
                    {
                        // homogenous distribution over soil profile (same root density throughout the profile)
                        double DepthTop = 0;
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            if (DepthTop >= p_rootFrontier)
                                result[layer] = 0.0;
                            else if (DepthTop + Soil.Thickness[layer] <= p_rootFrontier)
                                result[layer] = 1.0;
                            else
                                result[layer] = (p_rootFrontier - DepthTop) / Soil.Thickness[layer];
                            sumProportion += result[layer] * Soil.Thickness[layer];
                            DepthTop += Soil.Thickness[layer];
                        }
                        break;
                    }
                case "userdefined":
                    {
                        // distribution given by the user
                        // Option no longer available
                        break;
                    }
                case "expolinear":
                    {
                        // distribution calculated using ExpoLinear method
                        //  Considers homogeneous distribution from surface down to a fraction of root depth (p_ExpoLinearDepthParam)
                        //   below this depth, the proportion of root decrease following a power function (exponent = p_ExpoLinearCurveParam)
                        //   if exponent is one than the proportion decreases linearly.
                        double DepthTop = 0;
                        double DepthFirstStage = p_rootFrontier * expoLinearDepthParam;
                        double DepthSecondStage = p_rootFrontier - DepthFirstStage;
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            if (DepthTop >= p_rootFrontier)
                                result[layer] = 0.0;
                            else if (DepthTop + Soil.Thickness[layer] <= DepthFirstStage)
                                result[layer] = 1.0;
                            else
                            {
                                if (DepthTop < DepthFirstStage)
                                    result[layer] = (DepthFirstStage - DepthTop) / Soil.Thickness[layer];
                                if ((expoLinearDepthParam < 1.0) && (expoLinearCurveParam > 0.0))
                                {
                                    double thisDepth = Math.Max(0.0, DepthTop - DepthFirstStage);
                                    double Ftop = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, expoLinearCurveParam) / (expoLinearCurveParam + 1);
                                    thisDepth = Math.Min(DepthTop + Soil.Thickness[layer] - DepthFirstStage, DepthSecondStage);
                                    double Fbottom = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, expoLinearCurveParam) / (expoLinearCurveParam + 1);
                                    result[layer] += Math.Max(0.0, Fbottom - Ftop) / Soil.Thickness[layer];
                                }
                                else if (DepthTop + Soil.Thickness[layer] <= p_rootFrontier)
                                    result[layer] += Math.Min(DepthTop + Soil.Thickness[layer], p_rootFrontier) - Math.Max(DepthTop, DepthFirstStage) / Soil.Thickness[layer];
                            }
                            sumProportion += result[layer];
                            DepthTop += Soil.Thickness[layer];
                        }
                        break;
                    }
                default:
                    {
                        throw new Exception("No valid method for computing root distribution was selected");
                    }
            }
            if (sumProportion > 0)
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = result[layer] / sumProportion;
            else
                throw new Exception("Could not calculate root distribution");
            return result;
        }

        /// <summary>Compute how much of the layer is actually explored by roots</summary>
        /// <param name="layer">The layer.</param>
        /// <param name="root_depth">The root_depth.</param>
        /// <returns>Fraction of layer explored by roots</returns>
        private double LayerFractionForRoots(int layer, double root_depth)
        {
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double fraction_in_layer = 0;
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += Soil.Thickness[i];
            depth_to_layer_top = depth_to_layer_bottom - Soil.Thickness[layer];
            fraction_in_layer = (root_depth - depth_to_layer_top) / (depth_to_layer_bottom - depth_to_layer_top);

            return Math.Min(1.0, Math.Max(0.0, fraction_in_layer));
        }

        /// <summary>Nitrogen uptake process</summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private double SNUptakeProcess()
        {
            //Uptake from the root_zone
            Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
            NUptake.Sender = Name;
            NUptake.SenderType = "Plant";
            NUptake.DeltaNO3 = new double[Soil.Thickness.Length];
            NUptake.DeltaNH4 = new double[Soil.Thickness.Length];

            double Fraction = 0;
            if (p_soilNavailable > 0)
            {
                Fraction = Math.Min(1.0, p_soilNuptake / p_soilNavailable);
            }

            double n_uptake = 0;

            if (alt_N_uptake == "yes")
            {
                double
                    uptake_multiplier = double.MaxValue,
                    totSWUptake = SWUptake.Sum();

                double[]
                    availableNH4_bylayer = new double[Soil.Thickness.Length],
                    availableNO3_bylayer = new double[Soil.Thickness.Length],
                    diffNH4_bylayer = new double[Soil.Thickness.Length],
                    diffNO3_bylayer = new double[Soil.Thickness.Length];

                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    double
                        totN = Soil.NH4N[layer] + Soil.NO3N[layer],
                        fracH2O = SWUptake[layer] / totSWUptake;

                    if (totN > 0)
                    {
                        availableNH4_bylayer[layer] = fracH2O * Soil.NH4N[layer] / totN;
                        availableNO3_bylayer[layer] = fracH2O * Soil.NO3N[layer] / totN;

                        //if we have no3 and nh4 in this layer then calculate our uptake multiplier, otherwise set it to 0
                        //the idea behind the multiplier is that it allows us to calculate the max amount of N we can extract
                        //without forcing any of the layers below 0 AND STILL MAINTAINING THE RATIO as calculated with fracH2O
                        //NOTE: it doesn't matter whether we use nh4 or no3 for this calculation, we will get the same answer regardless
                        uptake_multiplier = Soil.NH4N[layer] * Soil.NO3N[layer] > 0 ? Math.Min(uptake_multiplier, Soil.NH4N[layer] / availableNH4_bylayer[layer]) : 0;
                    }
                    else
                    {
                        availableNH4_bylayer[layer] = 0;
                        availableNO3_bylayer[layer] = 0;
                    }
                }

                //adjust availability values with the multiplier we just calculated
                availableNH4_bylayer = availableNH4_bylayer.Select(x => x * uptake_multiplier).ToArray();
                availableNO3_bylayer = availableNO3_bylayer.Select(x => x * uptake_multiplier).ToArray();

                //calculate how much no3/nh4 will be left in the soil layers (diff_nxx[layer] = nxx[layer] - availableNH4_bylayer[layer])
                diffNH4_bylayer = Soil.NH4N.Select((x, layer) => Math.Max(0, x - availableNH4_bylayer[layer])).ToArray();
                diffNO3_bylayer = Soil.NO3N.Select((x, layer) => Math.Max(0, x - availableNO3_bylayer[layer])).ToArray();

                //adjust this by the sum of all leftover so we get a ratio we can use later
                double sum_diff = diffNH4_bylayer.Sum() + diffNO3_bylayer.Sum();
                diffNH4_bylayer = diffNH4_bylayer.Select(x => x / sum_diff).ToArray();
                diffNO3_bylayer = diffNO3_bylayer.Select(x => x / sum_diff).ToArray();

                double
                    //available N from our 'withwater' calcs (still some left in the 'diff' arrays if this isn't enough)
                    avail_withwater = availableNH4_bylayer.Sum() + availableNO3_bylayer.Sum(),
                    //if not enough N was available via the 'withwater' calcs this will be positive and will require more from the 'diffs' we calculated
                    shortfall_withwater = p_soilNuptake - avail_withwater;

                if (shortfall_withwater > 0)
                {
                    //this cap should not be needed because shortfall is already capped via the math.min in the scaled_demand calcs (leave it here though)
                    double scaled_diff = Math.Min(shortfall_withwater / avail_withwater, 1);

                    availableNH4_bylayer = availableNH4_bylayer.Select((x, layer) => x + shortfall_withwater * diffNH4_bylayer[layer]).ToArray();
                    availableNO3_bylayer = availableNO3_bylayer.Select((x, layer) => x + shortfall_withwater * diffNO3_bylayer[layer]).ToArray();
                }

                NUptake.DeltaNH4 = availableNH4_bylayer.Select(x => x * -1).ToArray();
                NUptake.DeltaNO3 = availableNO3_bylayer.Select(x => x * -1).ToArray();

                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                    n_uptake += SNUptake[layer] = (NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer]) * -1;

                double[] diffs = NUptake.DeltaNO3.Select((x, i) => Math.Max(Soil.NO3N[i] + x + 0.00000001, 0)).ToArray();
                if (diffs.Any(x => x == 0))
                    throw new Exception();

            }

            /*if (ValsMode == "withwater")
            {
                NUptake.DeltaNO3 = SP[0].availableNO3_bylayer.Select(x => x * -1).ToArray();
                NUptake.DeltaNH4 = SP[0].availableNH4_bylayer.Select(x => x * -1).ToArray();

                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                    SNUptake[layer] = SP[0].availableNO3_bylayer[layer] + SP[0].availableNH4_bylayer[layer];
                n_uptake = SNUptake.Sum();
            }*/
            else
            {
                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                {   //N are taken up only in top layers that root can reach (including buffer Zone).
                    n_uptake += (Soil.NO3N[layer] + Soil.NH4N[layer]) * Fraction;
                    SNUptake[layer] = (Soil.NO3N[layer] + Soil.NH4N[layer]) * Fraction;

                    NUptake.DeltaNO3[layer] = -Soil.NO3N[layer] * Fraction;
                    NUptake.DeltaNH4[layer] = -Soil.NH4N[layer] * Fraction;
                }
            }

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptake);
            return n_uptake;
        }

        /// <summary>return plant litter to surface organic matter poor</summary>
        /// <param name="amtDM">The amt dm.</param>
        /// <param name="amtN">The amt n.</param>
        /// <param name="frac">The frac.</param>
        private void DoSurfaceOMReturn(Double amtDM, Double amtN, Double frac)
        {
            if (BiomassRemoved != null)
            {
                Single dDM = (Single)amtDM;

                PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
                String[] type = new String[1];
                Single[] dltdm = new Single[1];
                Single[] dltn = new Single[1];
                Single[] dltp = new Single[1];
                Single[] fraction = new Single[1];

                type[0] = "grass";
                dltdm[0] = dDM;				 // kg/ha
                dltn[0] = (Single)amtN;		 // dDM * (Single)dead_nconc;
                dltp[0] = dltn[0] * 0.3F;	   //just a stub here, no P budgeting process in this module
                fraction[0] = (Single)frac;

                BR.crop_type = "grass";
                BR.dm_type = type;
                BR.dlt_crop_dm = dltdm;
                BR.dlt_dm_n = dltn;
                BR.dlt_dm_p = dltp;
                BR.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(BR);
            }
        }

        /// <summary>return scenescent roots into fresh organic matter pool in soil</summary>
        /// <param name="rootSen">The root sen.</param>
        /// <param name="NinRootSen">The nin root sen.</param>
        private void DoIncorpFomEvent(double rootSen, double NinRootSen)
        {
            Soils.FOMLayerLayerType[] fomLL = new Soils.FOMLayerLayerType[Soil.Thickness.Length];

            // ****  RCichota, Jun, 2014 change how RootFraction (rlvp) is used in here ****************************************
            // root senesced are returned to soil (as FOM) considering return is proportional to root mass

            double dAmtLayer = 0.0; //amount of root litter in a layer
            double dNLayer = 0.0;
            for (int i = 0; i < Soil.Thickness.Length; i++)
            {
                dAmtLayer = rootSen * RootFraction[i];
                dNLayer = NinRootSen * RootFraction[i];

                Soils.FOMType fom = new Soils.FOMType();
                fom.amount = dAmtLayer;
                fom.N = dNLayer;// 0.03F * amt;	// N in dead root
                fom.C = 0.40 * dAmtLayer;	//40% of OM is C. Actually, 'C' is not used, as shown in DataTypes.xml
                fom.P = 0;			  //to consider later
                fom.AshAlk = 0;		 //to consider later

                Soils.FOMLayerLayerType Layer = new Soils.FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0;	   //not used
                Layer.LabileP = 0;   //not used

                fomLL[i] = Layer;
            }

            if (IncorpFOM != null)
            {
                Soils.FOMLayerType FomLayer = new Soils.FOMLayerType();
                FomLayer.Type = "agpasture";
                FomLayer.Layer = fomLL;
                IncorpFOM.Invoke(FomLayer);
            }
        }

        #endregion //Functions

        #region Utilities
        //-----------------------------------------------------------------
        /// <summary>The following helper functions [VDP and svp] are for calculating Fvdp</summary>
        /// <returns></returns>
        private double VPD()
        {
            double VPDmint = svp(MetData.MinT) - MetData.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = svp(MetData.MaxT) - MetData.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            double vdp = SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            return vdp;
        }
        /// <summary>SVPs the specified temporary.</summary>
        /// <param name="temp">The temporary.</param>
        /// <returns></returns>
        private double svp(double temp)  // from Growth.for documented in MicroMet
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        #endregion //Utility
    }

    //================================================================================
    // One species
    //================================================================================
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Species
    {
        /// <summary>The p s</summary>
        internal DMPools pS;				//for remember the status of previous day
        //constants
        /// <summary>The c d2 c</summary>
        const double CD2C = 12.0 / 44.0;	//convert CO2 into C
        /// <summary>The c2 dm</summary>
        const double C2DM = 2.5;			//C to DM convertion
        /// <summary>The d m2 c</summary>
        const double DM2C = 0.4;			//DM to C converion
        /// <summary>The n2 protein</summary>
        const double N2Protein = 6.25;	  //this is for plants... (higher amino acids)
        /// <summary>The c2 n_protein</summary>
        const double C2N_protein = 3.5;	 //C:N in remobilised material
        //const double growthTref = 20.0;	  //reference temperature

        //internal static WeatherFile.NewMetType MetData = new WeatherFile.NewMetType();	//climate data applied to all species
        // From MetData
        /// <summary>The tmean</summary>
        internal static double Tmean;
        /// <summary>The tmax</summary>
        internal static double Tmax;
        /// <summary>The tmin</summary>
        internal static double Tmin;
        /// <summary>The ambient c o2</summary>
        internal static double ambientCO2;
        /// <summary>The day length</summary>
        internal static double DayLength;
        /// <summary>The local latitude</summary>
        internal static double localLatitude;

        // From Clock
        /// <summary>The sim today</summary>
        internal static DateTime simToday;

        // From Others
        /// <summary>The plant intercepted radn</summary>
        internal static double PlantInterceptedRadn;						  //total Radn intecepted by pasture
        /// <summary>The plant cover green</summary>
        internal static double PlantCoverGreen;
        /// <summary>The plant light ext coeff</summary>
        internal static double PlantLightExtCoeff;					//k of mixed pasture
        /// <summary>The plant shoot wt</summary>
        internal static double PlantShootWt;

        /// <summary>The int radn frac</summary>
        internal double intRadnFrac;	 //fraction of Radn intercepted by this species = intRadn/Radn

        /// <summary>The species name</summary>
        public string speciesName;
        /// <summary>The micromet type</summary>
        internal string micrometType = "grass";

        /// <summary>The is annual</summary>
        internal bool isAnnual = false;		//Species type (1=annual,0=perennial)
        /// <summary>The is legume</summary>
        public bool isLegume;		//Legume (0=no,1=yes)
        /// <summary>The photo path</summary>
        public string photoPath;	   //Phtosynthesis pathways: 3=C3, 4=C4; //no consideration for CAM(=3)

        /// <summary>The ini dm frac_grass</summary>
        internal static double[] iniDMFrac_grass;
        /// <summary>The ini dm frac_legume</summary>
        internal static double[] iniDMFrac_legume;

        //annual species parameters
        /// <summary>The day emerg</summary>
        public int dayEmerg = 0; 		//Earlist day of emergence (for annuals only)
        /// <summary>The mon emerg</summary>
        public int monEmerg = 0;		//Earlist month of emergence (for annuals only)
        /// <summary>The day anth</summary>
        public int dayAnth = 0;			//Earlist day of anthesis (for annuals only)
        /// <summary>The mon anth</summary>
        public int monAnth = 0;			//Earlist month of anthesis (for annuals only)
        /// <summary>The days to mature</summary>
        public int daysToMature = 0;	//Days from anthesis to maturity (for annuals only)
        /// <summary>The days emg to anth</summary>
        internal int daysEmgToAnth = 0;   //Days from emergence to Anthesis (calculated, annual only)
        /// <summary>The pheno stage</summary>
        internal int phenoStage = 1;  //pheno stages: 0 - pre_emergence, 1 - vegetative, 2 - reproductive
        /// <summary>The pheno factor</summary>
        internal double phenoFactor = 1;
        /// <summary>The daysfrom emergence</summary>
        internal int daysfromEmergence = 0;   //days
        /// <summary>The daysfrom anthesis</summary>
        internal int daysfromAnthesis = 0;	//days

        /// <summary>The b sown</summary>
        internal bool bSown = false;
        /// <summary>The dd sfrom sowing</summary>
        internal double DDSfromSowing = 0;

        /// <summary>The d root depth</summary>
        public int dRootDepth = 50;		//Daily root growth (mm)
        /// <summary>The maximum root depth</summary>
        public int maxRootDepth = 900;	//Maximum root depth (mm)
        /// <summary>The allocation season f</summary>
        public double allocationSeasonF; //factor for different biomass allocation among seasons
        /// <summary>The ndilut coeff</summary>
        public double NdilutCoeff;
        /// <summary>The root depth</summary>
        public double rootDepth;	   //current root depth (mm)
        //**public int rootFnType;		//Root function 0=default 1=Ritchie 2=power_law 3=proportional_depth

        /// <summary>The growth tmin</summary>
        public double growthTmin;   //Minimum temperature (grtmin) - originally 0
        /// <summary>The growth tmax</summary>
        public double growthTmax;   //Maximum temperature (grtmax) - originally 30
        /// <summary>The growth topt</summary>
        public double growthTopt;   //Optimum temperature (grtopt) - originally 20
        /// <summary>The growth tq</summary>
        public double growthTq;		//Temperature n (grtemn) --fyl: q curvature coefficient, 1.5 for c3 & 2 for c4 in IJ

        /// <summary>The heat onset t</summary>
        public double heatOnsetT;			//onset tempeature for heat effects
        /// <summary>The heat full t</summary>
        public double heatFullT;			//full temperature for heat effects
        /// <summary>The heat sum t</summary>
        public double heatSumT;			//temperature sum for recovery - sum of (25-mean)
        /// <summary>The cold onset t</summary>
        public double coldOnsetT;		  //onset tempeature for cold effects
        /// <summary>The cold full t</summary>
        public double coldFullT;			//full tempeature for cold effects
        /// <summary>The cold sum t</summary>
        public double coldSumT;			//temperature sum for recovery - sum of means
        /// <summary>The pm</summary>
        public double Pm;					//reference leaf co2 g/m^2/s maximum
        /// <summary>The maint respiration</summary>
        public double maintRespiration;	//in %
        /// <summary>The growth efficiency</summary>
        public double growthEfficiency;


        /// <summary>The high temporary effect</summary>
        private double highTempEffect = 1;  //fraction of growth rate due to high temp. effect
        /// <summary>The low temporary effect</summary>
        private double lowTempEffect = 1;   //fraction of growth rate due to low temp. effect
        /// <summary>The accum t</summary>
        private double accumT = 0;		  //accumulated temperature from previous heat strike = sum of '25-MeanT'(>0)
        /// <summary>The accum t low</summary>
        private double accumTLow = 0;	   //accumulated temperature from previous cold strike = sum of MeanT (>0)

        /// <summary>The mass flux tmin</summary>
        public double massFluxTmin;			//grfxt1	Mass flux minimum temperature
        /// <summary>The mass flux topt</summary>
        public double massFluxTopt;			//grfxt2	Mass flux optimum temperature
        /// <summary>The mass flux w0</summary>
        public double massFluxW0;			//grfw1		Mass flux scale factor at GLFwater=0 (must be > 1)
        /// <summary>The mass flux wopt</summary>
        public double massFluxWopt;		 //grfw2		Mass flux optimum temperature

        /// <summary>The sla</summary>
        public double SLA;				//Specific leaf area (m2/kg dwt)
        /// <summary>The SRL</summary>
        public double SRL;				//Specific root length
        /// <summary>The light ext coeff</summary>
        public double lightExtCoeff;	//Light extinction coefficient
        /// <summary>The light ext coeff_ref</summary>
        private double lightExtCoeff_ref;
        /// <summary>The rue</summary>
        public double rue;			  //radiaiton use efficiency
        /// <summary>The maximum assimi rate</summary>
        public double maxAssimiRate;	//Maximum Assimulation rate at reference temp & daylength (20C & 12Hrs)
        /// <summary>The rate live2 dead</summary>
        public double rateLive2Dead;	//Decay coefficient between live and dead
        /// <summary>The rate dead2 litter</summary>
        public double rateDead2Litter;	//Decay coefficient between dead and litter
        /// <summary>The rate root sen</summary>
        public double rateRootSen;	  //Decay reference root senescence rate (%/day)
        /// <summary>The stock parameter</summary>
        public double stockParameter;   //Stock influence parameter
        /// <summary>The maximum s rratio</summary>
        public double maxSRratio;	   //Shoot-Root ratio maximum
        /// <summary>The leaf rate</summary>
        public double leafRate;		 //reference leaf appearance rate without stress
        /// <summary>The f leaf</summary>
        public double fLeaf;			//Fixed growth partition to leaf (0-1)
        /// <summary>The f stolon</summary>
        public double fStolon;			//Fixed growth partition to stolon (0-1)

        /// <summary>The digest live</summary>
        public double digestLive;   //Digestibility of live plant material (0-1)
        /// <summary>The digest dead</summary>
        public double digestDead;   //Digestibility of dead plant material (0-1)

        //CO2
        /// <summary>The reference c o2</summary>
        public double referenceCO2;
        /// <summary>The c o2 pmax scale</summary>
        public double CO2PmaxScale;
        /// <summary>The c o2 n scale</summary>
        public double CO2NScale;
        /// <summary>The c o2 n minimum</summary>
        public double CO2NMin;
        /// <summary>The c o2 n curvature</summary>
        public double CO2NCurvature;

        //water
        //private double swuptake;
        //private double swdemandFrac;
        /// <summary>The water stress factor</summary>
        public double waterStressFactor;
        /// <summary>The soil sat factor</summary>
        public double soilSatFactor;

        //Nc - N concentration
        /// <summary>The ncstem fr</summary>
        public double NcstemFr;   //stem Nc as % of leaf Nc
        /// <summary>The ncstol fr</summary>
        public double NcstolFr;   //stolon Nc as % of leaf Nc
        /// <summary>The ncroot fr</summary>
        public double NcrootFr;   //root Nc as % of leaf Nc

        /// <summary>The nc rel2</summary>
        public double NcRel2;     //N concentration in tissue 2 relative to tissue 1
        /// <summary>The nc rel3</summary>
        public double NcRel3;     //N concentration in tissue 3 relative to tissue 1

        //current
        /// <summary>The ncleaf1</summary>
        internal double Ncleaf1;	//leaf 1  (critical N %)
        /// <summary>The ncleaf2</summary>
        internal double Ncleaf2;	//leaf 2
        /// <summary>The ncleaf3</summary>
        internal double Ncleaf3;	//leaf 3
        /// <summary>The ncleaf4</summary>
        internal double Ncleaf4;	//leaf dead
        /// <summary>The ncstem1</summary>
        internal double Ncstem1;	//sheath and stem 1
        /// <summary>The ncstem2</summary>
        internal double Ncstem2;	//sheath and stem 2
        /// <summary>The ncstem3</summary>
        internal double Ncstem3;	//sheath and stem 3
        /// <summary>The ncstem4</summary>
        internal double Ncstem4;	//sheath and stem dead
        /// <summary>The ncstol1</summary>
        internal double Ncstol1;	//stolon 1
        /// <summary>The ncstol2</summary>
        internal double Ncstol2;	//stolon 2
        /// <summary>The ncstol3</summary>
        internal double Ncstol3;	//stolon 3
        /// <summary>The ncroot</summary>
        internal double Ncroot;		//root
        /// <summary>The nclitter</summary>
        internal double Nclitter;	//Litter pool

        //Max, Min & Opt = critical N
        /// <summary>The ncleaf opt</summary>
        public double NcleafOpt;	//leaf   (critical N %)
        /// <summary>The ncstem opt</summary>
        internal double NcstemOpt;	//sheath and stem
        /// <summary>The ncstol opt</summary>
        internal double NcstolOpt;	//stolon
        /// <summary>The ncroot opt</summary>
        internal double NcrootOpt;	//root
        /// <summary>The ncleaf maximum</summary>
        public double NcleafMax;	//leaf  (critical N %)
        /// <summary>The ncstem maximum</summary>
        internal double NcstemMax;	//sheath and stem
        /// <summary>The ncstol maximum</summary>
        internal double NcstolMax;	//stolon
        /// <summary>The ncroot maximum</summary>
        internal double NcrootMax;	//root
        /// <summary>The ncleaf minimum</summary>
        public double NcleafMin;
        /// <summary>The ncstem minimum</summary>
        internal double NcstemMin;
        /// <summary>The ncstol minimum</summary>
        internal double NcstolMin;
        /// <summary>The ncroot minimum</summary>
        internal double NcrootMin;
        /// <summary>The maximum fix</summary>
        public double MaxFix;   //N-fix fraction when no soil N available, read in later
        /// <summary>The minimum fix</summary>
        public double MinFix;   //N-fix fraction when soil N sufficient

        //N in each pool (calculated as dm * Nc)
        /// <summary>The nleaf1</summary>
        internal double Nleaf1 = 0;	//leaf 1 (kg/ha)
        /// <summary>The nleaf2</summary>
        internal double Nleaf2 = 0;	//leaf 2 (kg/ha)
        /// <summary>The nleaf3</summary>
        internal double Nleaf3 = 0;	//leaf 3 (kg/ha)
        /// <summary>The nleaf4</summary>
        internal double Nleaf4 = 0;	//leaf dead (kg/ha)
        /// <summary>The nstem1</summary>
        internal double Nstem1 = 0;	//sheath and stem 1 (kg/ha)
        /// <summary>The nstem2</summary>
        internal double Nstem2 = 0;	//sheath and stem 2 (kg/ha)
        /// <summary>The nstem3</summary>
        internal double Nstem3 = 0;	//sheath and stem 3 (kg/ha)
        /// <summary>The nstem4</summary>
        internal double Nstem4 = 0;	//sheath and stem dead (kg/ha)
        /// <summary>The nstol1</summary>
        internal double Nstol1 = 0;	//stolon 1 (kg/ha)
        /// <summary>The nstol2</summary>
        internal double Nstol2 = 0;	//stolon 2 (kg/ha)
        /// <summary>The nstol3</summary>
        internal double Nstol3 = 0;	//stolon 3 (kg/ha)
        /// <summary>The nroot</summary>
        internal double Nroot = 0;	//root (kg/ha)
        /// <summary>The nlitter</summary>
        internal double Nlitter = 0;	//Litter pool (kg/ha)

        //calculated
        //DM
        /// <summary>The dmtotal</summary>
        internal double dmtotal;	  //=dmgreen + dmdead
        /// <summary>The dmgreen</summary>
        internal double dmgreen;
        /// <summary>The dmgreenmin</summary>
        internal double dmgreenmin;
        /// <summary>The dmdead</summary>
        internal double dmdead;
        /// <summary>The dmdeadmin</summary>
        internal double dmdeadmin;
        /// <summary>The dmshoot</summary>
        internal double dmshoot;
        /// <summary>The dmleaf</summary>
        internal double dmleaf;
        /// <summary>The dmstem</summary>
        internal double dmstem;
        /// <summary>The dmleaf_green</summary>
        internal double dmleaf_green;
        /// <summary>The dmstem_green</summary>
        internal double dmstem_green;
        /// <summary>The dmstol_green</summary>
        internal double dmstol_green;
        /// <summary>The dmstol</summary>
        internal double dmstol;
        /// <summary>The dmroot</summary>
        internal double dmroot;

        /// <summary>The dmleaf1</summary>
        internal double dmleaf1;	//leaf 1 (kg/ha)
        /// <summary>The dmleaf2</summary>
        internal double dmleaf2;	//leaf 2 (kg/ha)
        /// <summary>The dmleaf3</summary>
        internal double dmleaf3;	//leaf 3 (kg/ha)
        /// <summary>The dmleaf4</summary>
        internal double dmleaf4;	//leaf dead (kg/ha)
        /// <summary>The dmstem1</summary>
        internal double dmstem1;	//sheath and stem 1 (kg/ha)
        /// <summary>The dmstem2</summary>
        internal double dmstem2;	//sheath and stem 2 (kg/ha)
        /// <summary>The dmstem3</summary>
        internal double dmstem3;	//sheath and stem 3 (kg/ha)
        /// <summary>The dmstem4</summary>
        internal double dmstem4;	//sheath and stem dead (kg/ha)
        /// <summary>The dmstol1</summary>
        internal double dmstol1;	//stolon 1 (kg/ha)
        /// <summary>The dmstol2</summary>
        internal double dmstol2;	//stolon 2 (kg/ha)
        /// <summary>The dmstol3</summary>
        internal double dmstol3;	//stolon 3 (kg/ha)
        /// <summary>The dmlitter</summary>
        internal double dmlitter;	//Litter pool (kg/ha)

        /// <summary>The dmdefoliated</summary>
        internal double dmdefoliated = 0.0;
        /// <summary>The ndefoliated</summary>
        internal double Ndefoliated = 0.0;
        /// <summary>The digest herbage</summary>
        internal double digestHerbage;
        /// <summary>The digest defoliated</summary>
        internal double digestDefoliated;
        //LAI
        /// <summary>The green lai</summary>
        internal double greenLAI; //sum of 3 pools
        /// <summary>The dead lai</summary>
        internal double deadLAI;  //pool dmleaf4
        /// <summary>The total lai</summary>
        internal double totalLAI;
        //N plant
        /// <summary>The nshoot</summary>
        internal double Nshoot;	//above-ground total N (kg/ha)
        /// <summary>The nleaf</summary>
        internal double Nleaf;	//leaf N
        /// <summary>The nstem</summary>
        internal double Nstem;	//stem N
        /// <summary>The ngreen</summary>
        internal double Ngreen;	//live N
        /// <summary>The ndead</summary>
        internal double Ndead;	//in standing dead (kg/ha)
        /// <summary>The nstolon</summary>
        internal double Nstolon;	//stolon

        //internal double NremobMax;  //maximum N remob of the day
        /// <summary>The nremob</summary>
        internal double Nremob = 0;	   //N remobiliesd N during senesing
        /// <summary>The cremob</summary>
        internal double Cremob = 0;
        /// <summary>The nleaf3 remob</summary>
        internal double Nleaf3Remob = 0;
        /// <summary>The nstem3 remob</summary>
        internal double Nstem3Remob = 0;
        /// <summary>The nstol3 remob</summary>
        internal double Nstol3Remob = 0;
        /// <summary>The nroot remob</summary>
        internal double NrootRemob = 0;
        /// <summary>The remob2 new growth</summary>
        internal double remob2NewGrowth = 0;
        /// <summary>The new growth n</summary>
        internal double newGrowthN = 0;	//N plant-soil
        /// <summary>The ndemand lux</summary>
        internal double NdemandLux;	  //N demand for new growth
        /// <summary>The ndemand opt</summary>
        internal double NdemandOpt;
        //internal double NdemandMax;   //luxury N demand for new growth
        /// <summary>The nfix</summary>
        internal double Nfix;		 //N fixed by legumes

        /// <summary>The kappa2</summary>
        internal double Kappa2 = 0.0;
        /// <summary>The kappa3</summary>
        internal double Kappa3 = 0.0;
        /// <summary>The kappa4</summary>
        internal double Kappa4 = 0.0;
        /// <summary>The n luxury2</summary>
        internal double NLuxury2;		       // luxury N (above Nopt) potentially remobilisable
        /// <summary>The n luxury3</summary>
        internal double NLuxury3;		       // luxury N (above Nopt)potentially remobilisable
        /// <summary>The n fast remob2</summary>
        internal double NFastRemob2 = 0.0;   // amount of luxury N remobilised from tissue 2
        /// <summary>The n fast remob3</summary>
        internal double NFastRemob3 = 0.0;   // amount of luxury N remobilised from tissue 3

        //internal double soilNAvail;   //N available to this species
        /// <summary>The soil ndemand</summary>
        internal double soilNdemand;  //N demand from soil (=Ndemand-Nremob-Nfixed)
        //internal double soilNdemandMax;   //N demand for luxury uptake
        /// <summary>The soil nuptake</summary>
        internal double soilNuptake;  //N uptake of the day

        //growth limiting factors
        /// <summary>The gfwater</summary>
        internal double gfwater;  //from water stress
        /// <summary>The gftemp</summary>
        internal double gftemp;   //from temperature
        /// <summary>The GFN</summary>
        internal double gfn;	  //from N deficit
        /// <summary>The gf gen</summary>
        internal double gfGen;
        /// <summary>The ncfactor</summary>
        internal double Ncfactor;
        //internal double fNavail2Max; //demand/Luxruy uptake

        //calculated, species delta
        /// <summary>The d growth pot</summary>
        internal double dGrowthPot;	//daily growth potential
        /// <summary>The d growth w</summary>
        internal double dGrowthW;	  //daily growth with water-deficit incorporated
        /// <summary>The d growth</summary>
        internal double dGrowth;	   //daily growth
        /// <summary>The d growth root</summary>
        internal double dGrowthRoot;   //daily root growth
        /// <summary>The d growth herbage</summary>
        internal double dGrowthHerbage; //daily growth shoot

        /// <summary>The d litter</summary>
        internal double dLitter;	   //daily litter production
        /// <summary>The d n litter</summary>
        internal double dNLitter;	  //N in dLitter
        /// <summary>The d root sen</summary>
        internal double dRootSen;	  //daily root sennesce
        /// <summary>The d nroot sen</summary>
        internal double dNrootSen;	 //N in dRootSen

        /// <summary>The f shoot</summary>
        internal double fShoot;		 //actual fraction of dGrowth to shoot

        // transfer coefficients 
        /// <summary>The gama</summary>
        public double gama = 0.0;	// from tissue 1 to 2, then 3 then 4
        /// <summary>The gamas</summary>
        public double gamas = 0.0;	// for stolons
        /// <summary>The gamad</summary>
        public double gamad = 0.0;	// from dead to litter
        /// <summary>The gamar</summary>
        public double gamar = 0.0;	// for roots (to dead/FOM)

        /// <summary>The leaf preference</summary>
        internal double leafPref = 1;	//leaf preference
        // internal double accumtotalnewG = 0;
        // internal double accumtotalnewN = 0;
        /// <summary>The i l1</summary>
        internal double IL1;
        /// <summary>The pgross</summary>
        internal double Pgross;
        /// <summary>The resp_m</summary>
        internal double Resp_m;

        //Species ------------------------------------------------------------
        /// <summary>Initializes a new instance of the <see cref="Species"/> class.</summary>
        /// <param name="name">The name.</param>
        public Species(string name)
        {
            speciesName = name;
        }

        /// <summary>Initializes the values.</summary>
        public void InitValues()
        {
            pS = new DMPools();
            Nremob = 0.0;
            Cremob = 0;
            Nfix = 0.0;
            Ncfactor = 0.0;
            NdemandLux = 0.0;
            soilNdemand = 0.0;
            soilNuptake = 0.0;
            dmdefoliated = 0.0;
            Ndefoliated = 0;
            digestHerbage = 0;
            digestDefoliated = 0;
            dLitter = 0.0;
            dNLitter = 0.0;
            dRootSen = 0.0;
            dNrootSen = 0.0;
            gfn = 0.0;
            gftemp = 0.0;
            gfwater = 0.0;
            phenoFactor = 1.0;
            intRadnFrac = 0.0;
            newGrowthN = 0.0;
            Pgross = 0.0;
            NdemandOpt = 0.0;
            remob2NewGrowth = 0.0;
            Resp_m = 0.0;
            NrootRemob = 0.0;
            IL1 = 0.0;

            if (isAnnual) //calulate days from Emg to Antheis
                CalcDaysEmgToAnth();

            lightExtCoeff_ref = lightExtCoeff;

            leafPref = 1;
            if (isLegume) leafPref = 1.5;		//Init DM (is partitioned to different pools)

            dmtotal = dmshoot + dmroot;
            dmlitter = 0.0;

            if (dmtotal == 0.0) phenoStage = 0;
            else phenoStage = 1;

            if (isLegume)
            {
                dmleaf1 = iniDMFrac_legume[0] * dmshoot;
                dmleaf2 = iniDMFrac_legume[1] * dmshoot;
                dmleaf3 = iniDMFrac_legume[2] * dmshoot;
                dmleaf4 = iniDMFrac_legume[3] * dmshoot;
                dmstem1 = iniDMFrac_legume[4] * dmshoot;
                dmstem2 = iniDMFrac_legume[5] * dmshoot;
                dmstem3 = iniDMFrac_legume[6] * dmshoot;
                dmstem4 = iniDMFrac_legume[7] * dmshoot;
                dmstol1 = iniDMFrac_legume[8] * dmshoot;
                dmstol2 = iniDMFrac_legume[9] * dmshoot;
                dmstol3 = iniDMFrac_legume[10] * dmshoot;
            }
            else
            {
                dmleaf1 = iniDMFrac_grass[0] * dmshoot;
                dmleaf2 = iniDMFrac_grass[1] * dmshoot;
                dmleaf3 = iniDMFrac_grass[2] * dmshoot;
                dmleaf4 = iniDMFrac_grass[3] * dmshoot;
                dmstem1 = iniDMFrac_grass[4] * dmshoot;
                dmstem2 = iniDMFrac_grass[5] * dmshoot;
                dmstem3 = iniDMFrac_grass[6] * dmshoot;
                dmstem4 = iniDMFrac_grass[7] * dmshoot;
                dmstol1 = iniDMFrac_grass[8] * dmshoot;
                dmstol2 = iniDMFrac_grass[9] * dmshoot;
                dmstol3 = iniDMFrac_grass[10] * dmshoot;
            }

            //init N
            NcstemOpt = NcleafOpt * NcstemFr; 	//stem
            NcstolOpt = NcleafOpt * NcstolFr; 	//stolon
            NcrootOpt = NcleafOpt * NcrootFr; 	//root

            NcstemMax = NcleafMax * NcstemFr; //sheath and stem
            NcstolMax = NcleafMax * NcstolFr;	//stolon
            NcrootMax = NcleafMax * NcrootFr;	//root

            NcstemMin = NcleafMin * NcstemFr;
            NcstolMin = NcleafMin * NcstolFr;
            NcrootMin = NcleafMin * NcrootFr;

            //init as optimum
            Ncleaf1 = NcleafOpt;
            Ncleaf2 = NcleafOpt; //optimum now is the optimum of green leaf [N]
            Ncleaf3 = NcleafOpt;
            Ncleaf4 = NcleafMin; //this could become much small depending on [N] in green tisssue

            Ncstem1 = NcstemOpt; //stem [N] is 50% of the leaf [N]
            Ncstem2 = NcstemOpt;
            Ncstem3 = NcstemOpt;
            Ncstem4 = NcstemMin;

            Ncstol1 = NcstolOpt;
            Ncstol2 = NcstolOpt;
            Ncstol3 = NcstolOpt;

            Ncroot = NcrootOpt;
            Nclitter = NcleafMin;  //init as same [N]

            //Init total N in each pool
            Nleaf1 = dmleaf1 * Ncleaf1; //convert % to fraction [i.e., 4% ->0.02]
            Nleaf2 = dmleaf2 * Ncleaf2;
            Nleaf3 = dmleaf3 * Ncleaf3;
            Nleaf4 = dmleaf4 * Ncleaf4;
            Nstem1 = dmstem1 * Ncstem1;
            Nstem2 = dmstem2 * Ncstem2;
            Nstem3 = dmstem3 * Ncstem3;
            Nstem4 = dmstem4 * Ncstem4;
            Nstol1 = dmstol1 * Ncstol1;
            Nstol2 = dmstol2 * Ncstol2;
            Nstol3 = dmstol3 * Ncstol3;
            Nroot = dmroot * Ncroot;
            Nlitter = dmlitter * Nclitter;

            //calculated, DM and LAI,  species-specific
            updateAggregated();   // agregated properties, such as p_totalLAI

            dGrowthPot = 0.0;	   // daily growth potential
            dGrowthW = 0.0;		  // daily growth actual
            dGrowth = 0.0;		  // daily growth actual
            dGrowthRoot = 0.0;	  // daily root growth
            fShoot = 1;			// actual fraction of dGrowth allocated to shoot
        }

        //Species -----------------------
        /// <summary>Dailies the refresh.</summary>
        public void DailyRefresh()
        {
            dmdefoliated = 0;
            Ndefoliated = 0;
            digestHerbage = 0;
            digestDefoliated = 0;
        }

        //Species -----------------------------
        /// <summary>Removes the dm.</summary>
        /// <param name="AmountToRemove">The amount to remove.</param>
        /// <param name="PrefGreen">The preference green.</param>
        /// <param name="PrefDead">The preference dead.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">  AgPasture - removal of DM resulted in loss of mass balance</exception>
        public double RemoveDM(double AmountToRemove, double PrefGreen, double PrefDead)
        {

            // check existing amount and what is harvestable
            double PreRemovalDM = dmshoot;
            double PreRemovalN = Nshoot;
            double AmountRemovable = Math.Max(0.0, dmleaf_green + dmstem_green - dmgreenmin) + Math.Max(0.0, dmleaf4 + dmstem4 - dmdeadmin);

            // get the weights for each pool, consider preference and available DM
            double FractionNotRemoved = 0.0;
            if (AmountRemovable > 0)
                FractionNotRemoved = Math.Max(0.0, (AmountRemovable - AmountToRemove) / AmountRemovable);

            double TempPrefGreen = PrefGreen + (PrefDead * (1 - FractionNotRemoved));
            double TempPrefDead = PrefDead + (PrefGreen * (1 - FractionNotRemoved));
            double TempRemovableGreen = Math.Max(0.0, dmleaf_green + dmstem_green - dmgreenmin);
            double TempRemovableDead = Math.Max(0.0, dmleaf4 + dmstem4 - dmdeadmin);

            // get partiton between dead and live materials
            double TempTotal = TempRemovableGreen * TempPrefGreen + TempRemovableDead * TempPrefDead;
            double FractionToHarvestGreen = 0.0;
            double FractionToHarvestDead = 0.0;
            if (TempTotal > 0.0)
            {
                FractionToHarvestGreen = TempRemovableGreen * TempPrefGreen / TempTotal;
                FractionToHarvestDead = TempRemovableDead * TempPrefDead / TempTotal;
            }

            // get amounts removed
            double RemovingGreenDM = AmountToRemove * FractionToHarvestGreen;
            double RemovingDeadDM = AmountToRemove * FractionToHarvestDead;
            // Fraction of DM remaining in the field
            double FractionRemainingGreen = 1.0;
            if (dmleaf_green + dmstem_green > 0.0)
                FractionRemainingGreen -= RemovingGreenDM / (dmleaf_green + dmstem_green);
            double FractionRemainingDead = 1.0;
            if (dmleaf4 + dmstem4 > 0.0)
                FractionRemainingDead -= RemovingDeadDM / (dmleaf4 + dmstem4);
            FractionRemainingGreen = Math.Max(0.0, Math.Min(1.0, FractionRemainingGreen));
            FractionRemainingDead = Math.Max(0.0, Math.Min(1.0, FractionRemainingDead));

            // get digestibility of DM being harvested
            digestDefoliated = calcDigestability();

            // update the various pools
            dmleaf1 = FractionRemainingGreen * dmleaf1;
            dmleaf2 = FractionRemainingGreen * dmleaf2;
            dmleaf3 = FractionRemainingGreen * dmleaf3;
            dmleaf4 = FractionRemainingDead * dmleaf4;
            dmstem1 = FractionRemainingGreen * dmstem1;
            dmstem2 = FractionRemainingGreen * dmstem2;
            dmstem3 = FractionRemainingGreen * dmstem3;
            dmstem4 = FractionRemainingDead * dmstem4;
            //No stolon remove

            // N remove
            Nleaf1 = FractionRemainingGreen * Nleaf1;
            Nleaf2 = FractionRemainingGreen * Nleaf2;
            Nleaf3 = FractionRemainingGreen * Nleaf3;
            Nleaf4 = FractionRemainingDead * Nleaf4;
            Nstem1 = FractionRemainingGreen * Nstem1;
            Nstem2 = FractionRemainingGreen * Nstem2;
            Nstem3 = FractionRemainingGreen * Nstem3;
            Nstem4 = FractionRemainingDead * Nstem4;

            //Nremob is also removed proportionally (not sensitive?)
            double PreRemovalNRemob = Nremob;
            Nremob = FractionRemainingGreen * Nremob;

            // update Luxury N pools
            NLuxury2 *= FractionRemainingGreen;
            NLuxury3 *= FractionRemainingGreen;

            // update variables
            updateAggregated();

            // check balance and set outputs
            double NremobRemove = PreRemovalNRemob - Nremob;
            dmdefoliated = PreRemovalDM - dmshoot;
            pS.dmdefoliated = dmdefoliated;
            Ndefoliated = PreRemovalN - Nshoot;
            if (Math.Abs(dmdefoliated - AmountToRemove) > 0.00001)
                throw new Exception("  AgPasture - removal of DM resulted in loss of mass balance");

            return Ndefoliated;
        }

        /// <summary>Remove_originals the specified amt.</summary>
        /// <param name="amt">The amt.</param>
        /// <returns></returns>
        public double Remove_original(double amt)
        {
            //double pRest = 1 - (amt/dmtotal);
            double pRest = 1 - (amt / (dmstem + dmleaf));
            if (pRest < 0)
                return 0;

            dmdefoliated = amt;

            // Mar2011: If removing the specified 'amt' would result in a 'dmgreen' less than specified 'dmgreenmin',
            // then less green tissue (pool1-3 of leaf+stem) and more standing dead (pool4), will be removed
            // This is especially necessaery for semi-arid grassland
            double pRest_green = pRest;
            double pRest_dead = pRest;
            if (pRest * (dmleaf_green + dmstem_green) + dmstol_green < dmgreenmin)
            {
                pRest_green = (dmgreenmin - dmstol_green) / (dmleaf_green + dmstem_green);
                double amt_dead_remove = amt - (1 - pRest_green) * (dmleaf_green + dmstem_green);
                pRest_dead = (dmstem4 + dmleaf4 - amt_dead_remove) / (dmstem4 + dmleaf4);
                if (pRest_dead < 0.0) pRest_dead = 0.0;   //this is impossible
            }

            //double standingDead =dmleaf4 + dmstem4;
            //double deadFrac = standingDead /(dmleaf+dmstem);
            //digestDefoliated = (1-deadFrac) * digestLive + deadFrac * digestDead;
            digestDefoliated = calcDigestability(); //because the defoliateion of different parts is in proportion to biomass


            // 1)Removing without preference   Mar2011: using different pRest for maintain a 'dmgreenmin'
            dmleaf1 = pRest_green * dmleaf1;
            dmleaf2 = pRest_green * dmleaf2;
            dmleaf3 = pRest_green * dmleaf3;
            dmleaf4 = pRest_dead * dmleaf4;
            dmstem1 = pRest_green * dmstem1;
            dmstem2 = pRest_green * dmstem2;
            dmstem3 = pRest_green * dmstem3;
            dmstem4 = pRest_dead * dmstem4;
            //No stolon remove

            double preNshoot = Nshoot; //before remove
            //N remove
            Nleaf1 = pRest_green * Nleaf1;
            Nleaf2 = pRest_green * Nleaf2;
            Nleaf3 = pRest_green * Nleaf3;
            Nleaf4 = pRest_dead * Nleaf4;
            Nstem1 = pRest_green * Nstem1;
            Nstem2 = pRest_green * Nstem2;
            Nstem3 = pRest_green * Nstem3;
            Nstem4 = pRest_dead * Nstem4;

            //Nremob also been emoved proportionally (not sensiive?)
            double preNremob = Nremob;
            Nremob = pRest * Nremob;
            double NremobRemove = preNremob - Nremob;

            updateAggregated();

            double removeN = preNshoot - Nshoot;
            Ndefoliated = Math.Max(0.0, removeN);

            return removeN;
        }

        //Species ------------------------------------------------------------
        /// <summary>Calculates the digestability.</summary>
        /// <returns></returns>
        public double calcDigestability()
        {
            if ((dmleaf + dmstem) <= 0)
            {
                digestHerbage = 0;
                return digestHerbage;
            }

            double fSugar = 0.5 * dGrowth / dmgreen;	//dmgreen: live shoots including leaves/stems/stolons
            double CNp = 3.5;						   //CN ratio of protein
            double CNw = 100;						   //CN ratio of cell wall

            //Live
            double digestabilityLive = 0;
            if (dmgreen > 0 & Ngreen > 0)
            {
                double CNlive = 0.4 * dmgreen / Ngreen;								//CN ratio of live shoots
                double fProteinLive = (CNw / CNlive - (1 - fSugar)) / (CNw / CNp - 1); //Fraction of protein in liveing shoots
                double fWallLive = 1 - fSugar - fProteinLive;						  //Fraction of cell wall in living shoots
                digestabilityLive = fSugar + fProteinLive + digestLive * fWallLive;
            }

            //Dead
            double digestabilityDead = 0;
            double standingDead = dmleaf4 + dmstem4;		//Not including stolons here for stolons are not grazed
            if (standingDead > 0 && Ndead > 0)
            {
                double CNdead = 0.4 * dmdead / Ndead;					   //CN ratio of standing dead;
                double fProteinDead = (CNw / CNdead - 1) / (CNw / CNp - 1); //Fraction of protein in standing dead
                double fWallDead = 1 - fProteinDead;						//Fraction of cell wall in standing dead
                digestabilityDead = fProteinDead + digestDead * fWallDead;
            }

            double deadFrac = standingDead / (dmleaf + dmstem);
            digestHerbage = (1 - deadFrac) * digestabilityLive + deadFrac * digestabilityDead;

            return digestHerbage;
        }
        //Species ------------------------------------------------------------
        /// <summary>Updates the aggregated.</summary>
        /// <returns></returns>
        /// <exception cref="System.Exception">Loss of mass balance of shoot plant dry matter</exception>
        public double updateAggregated()   //update DM, N & LAI
        {
            //DM
            dmleaf = dmleaf1 + dmleaf2 + dmleaf3 + dmleaf4;
            dmstem = dmstem1 + dmstem2 + dmstem3 + dmstem4;
            dmstol = dmstol1 + dmstol2 + dmstol3;
            dmshoot = dmleaf + dmstem + dmstol;

            dmleaf_green = dmleaf1 + dmleaf2 + dmleaf3;
            dmstem_green = dmstem1 + dmstem2 + dmstem3;
            dmstol_green = dmstol1 + dmstol2 + dmstol3;

            dmgreen = dmleaf1 + dmleaf2 + dmleaf3
                    + dmstem1 + dmstem2 + dmstem3
                    + dmstol1 + dmstol2 + dmstol3;

            dmdead = dmleaf4 + dmstem4;

            if (Math.Abs((dmgreen + dmdead) - dmshoot) > 0.001)
                throw new Exception("Loss of mass balance of shoot plant dry matter");

            dmtotal = dmshoot + dmroot;

            //N
            Nleaf = Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4;
            Nstem = Nstem1 + Nstem2 + Nstem3 + Nstem4;// +Nremob;  //separately handled, not reported in stem
            Nstolon = Nstol1 + Nstol2 + Nstol3;

            Nshoot = Nleaf + Nstem + Nstolon;   //shoot

            Ngreen = Nleaf1 + Nleaf2 + Nleaf3
                    + Nstem1 + Nstem2 + Nstem3
                    + Nstol1 + Nstol2 + Nstol3;
            Ndead = Nleaf4 + Nstem4;

            //LAI								   //0.0001: kg/ha->kg/m2; SLA: m2/kg
            greenLAI = 0.0001 * dmleaf_green * SLA + 0.0001 * dmstol * 0.3 * SLA;   //insensitive? assuming Mass2GLA = 0.3*SLA

            // Resilence after unfovoured conditions
            // Consider cover will be bigger for the same amount of DM when DM is low due to
            // - light extinction coefficient will be bigger - plant leaves will be more plate than in dense high swards
            // - more parts will turn green for photosysntheses?
            // - quick response of plant shoots to fovoured conditions after release of stress
            if (!isLegume && dmgreen < 1000)
            {
                greenLAI += 0.0001 * dmstem_green * SLA * Math.Sqrt((1000 - dmgreen) / 1000);
            }

            deadLAI = 0.0001 * dmleaf4 * SLA;
            totalLAI = greenLAI + deadLAI;

            return totalLAI;

        }

        //Species --------------------------------------------
        /// <summary>Roots the growth.</summary>
        /// <returns></returns>
        public double rootGrowth()
        {
            if (isAnnual)
            {
                rootDepth = 50 + (maxRootDepth - 50) * daysfromEmergence / daysEmgToAnth;
                //considering root distribution change, here?
            }
            return rootDepth;  // no root depth change for pereniel pasture
        }

        //Species -------------------------------------------------
        /// <summary>Calculates the days emg to anth.</summary>
        /// <returns></returns>
        public int CalcDaysEmgToAnth()
        {
            int numbMonths = monAnth - monEmerg;  //emergence & anthesis in the same calendar year: monEmerg < monAnth
            if (monEmerg >= monAnth)			  //...across the calendar year
                numbMonths += 12;

            daysEmgToAnth = (int)(30.5 * numbMonths + (dayAnth - dayEmerg));

            return daysEmgToAnth;
        }

        //Species -------------------------------------------------------------
        /// <summary>Phenologies this instance.</summary>
        /// <returns></returns>
        public int Phenology()
        {
            const double DDSEmergence = 150;   // to be an input parameter

            if (bSown && phenoStage == 0)			//  before emergence
            {
                DDSfromSowing += Tmean;
                if (DDSfromSowing > DDSEmergence)
                {
                    phenoStage = 1;
                    DDSfromSowing = 0;
                    SetEmergentState();	  //Initial states at 50% emergence

                }
            }

            return phenoStage;
        }

        //Species -------------------------------------------------------------
        /// <summary>Sets the state of the emergent.</summary>
        /// <returns></returns>
        private double SetEmergentState()
        {
            dmleaf1 = 10;   //(kg/ha)
            dmleaf2 = 20;
            dmleaf3 = 20;
            dmleaf4 = 0;
            if (!isLegume)
            {
                dmstem1 = 5;
                dmstem2 = 10;
                dmstem3 = 0;
                dmstem4 = 0;
                dmroot = 50;
            }
            else
            {
                dmstol1 = 5;
                dmstol2 = 10;
                dmstol3 = 0;
                dmroot = 25;
            }
            dmlitter = 0;

            //Init total N in each pool
            Nleaf1 = dmleaf1 * Ncleaf1;
            Nleaf2 = dmleaf2 * Ncleaf2;
            Nleaf3 = dmleaf3 * Ncleaf3;
            Nleaf4 = dmleaf4 * Ncleaf4;
            Nstem1 = dmstem1 * Ncstem1;
            Nstem2 = dmstem2 * Ncstem2;
            Nstem3 = dmstem3 * Ncstem3;
            Nstem4 = dmstem4 * Ncstem4;
            Nstol1 = dmstol1 * Ncstol1;
            Nstol2 = dmstol2 * Ncstol2;
            Nstol3 = dmstol3 * Ncstol3;
            Nroot = dmroot * Ncroot;
            Nlitter = dmlitter * Nclitter;

            //calculated, DM and LAI,  species-specific
            updateAggregated();   // agregated properties, such as p_totalLAI

            dGrowthPot = 0.0;	   // daily growth potential
            dGrowthW = 0.0;		 // daily growth considering only water deficit
            dGrowth = 0.0;		  // daily growth actual
            dGrowthRoot = 0.0;	  // daily root growth
            fShoot = 1;			  // actual fraction of dGrowth allocated to shoot

            return dmtotal;	   // total shoot mass

        }

        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public ZoneWaterAndN GetPotSWUptake(ZoneWaterAndN info)
        {
            return info;
        }

        /// <summary>Dailies the em growth pot.</summary>
        /// <returns></returns>
        public double DailyEMGrowthPot()
        {
            //annual phebology
            if (isAnnual)
            {
                bool moreGrowth = annualPhenology();
                if (!moreGrowth)
                    return dGrowthPot = 0;
            }

            //
            if (phenoStage == 0 || greenLAI == 0) //Before gemination
                return dGrowthPot = 0;

            const double alfa = 0.01;				 //P_al, leaf gross photosynthesis rate: mg co2/J
            const double theta = 0.8;				 //P_th, curvature parameter: J /kg/s

            //following parometers are from input (.xml)
            double maint_coeff = 0.01 * maintRespiration;  //reference maintnance respiration as % of live weight
            double Yg = growthEfficiency;				  //default =0.75; //Efficiency of plant photosynthesis growth)
            //Pm is an input

            //Add temp effects to Pm
            double Tday = Tmean + 0.5 * (Tmax - Tmean);

            double Pm_mean = Pm * GFTemperature(Tmean) * PCO2Effects() * PmxNeffect();  //Dec10: added CO2 & [N]effects
            double Pm_day = Pm * GFTemperature(Tday) * PCO2Effects() * PmxNeffect();	//Dec10: added CO2 & [N]effects

            double tau = 3600 * DayLength;//conversion of hour to seconds //  tau := 0.0036 * hours ;
            //IL_1 := k_light * 1.33333 * 0.5 * light/tau;  // flat bit - goes with Pm_day
            //FYL: k_light*light/tau = Irridance intercepted by 1 LAI on 1 m^2 ground: J/(m^2 ground)/s

            //IL:  irridance on the top of canopy, with unit: J/(m^2 LAI)/(m^2 ground)/second.  PAR = 0.5*Radn; 1 MJ = 10^6 J

            //IL1 = 1.33333 * 0.5 * PIntRadn / (PCoverGreen*coverRF) * PLightExtCoeff * 1000000 / tau;
            IL1 = 1.33333 * 0.5 * PlantInterceptedRadn * 1000000 * PlantLightExtCoeff / tau;					//ignore putting 2 species seperately for now
            double IL2 = IL1 / 2;					  //IL for early & late period of a day

            //Photosynthesis per LAI under full irridance at the top of the canopy
            double Pl1 = (0.5 / theta) * (alfa * IL1 + Pm_day
                         - Math.Sqrt((alfa * IL1 + Pm_day) * (alfa * IL1 + Pm_day) - 4 * theta * alfa * IL1 * Pm_day));
            double Pl2 = (0.5 / theta) * (alfa * IL2 + Pm_mean
                         - Math.Sqrt((alfa * IL2 + Pm_mean) * (alfa * IL2 + Pm_mean) - 4 * theta * alfa * IL2 * Pm_mean));
            
            //Upscaling from 'per LAI' to 'per ground area'
            double carbon_m2 = 0.000001 * CD2C * 0.5 * tau * (Pl1 + Pl2) * PlantCoverGreen * intRadnFrac / lightExtCoeff;
            //tau: per second => per day; 0.000001: mg/m^2=> kg/m^2_ground/day;
            //only 'intRadnFrac' portion for this species;
            //using lightExeCoeff (species, result in a lower yield with ample W & N)

            carbon_m2 *= 1;// coverRF;					   //coverRF == 1 when puting species together

            Pgross = 10000 * carbon_m2;				 //10000: 'kg/m^2' =>'kg/ha'

            //Add extreme temperature effects;
            double ExtremeFactor = HeatEffect() * ColdEffect();
            Pgross *= ExtremeFactor;	  // in practice only one temp stress factor is < 1

            //Maintenance respiration
            double Teffect = 0;						 //Add temperature effects on respi
            if (Tmean > growthTmin)
            {
                if (Tmean < growthTopt)
                {
                    Teffect = GFTemperature(Tmean);
                    //Teffect = Math.Pow(Teffect, 1.5);
                }
                else
                {
                    //Teffect = 1;
                    Teffect = Tmean / growthTopt;		// Using growthTopt (e.g., 20 C) as reference, and set maximum
                    if (Teffect > 1.25) Teffect = 1.25;  // Resp_m
                }   //The extreme high temperatue (heat) effect is added separately
            }


            double YgFactor = 1.0;
            //Ignore [N] effects in potential growth here
            Resp_m = maint_coeff * Teffect * PmxNeffect() * (dmgreen + dmroot) * DM2C;	   //converting DM to C	(kg/ha)
            //Dec10: added [N] effects here

            // ** C budget is not explicitly done here as in EM
            Cremob = 0;					 // Nremob* C2N_protein;	// No carbon budget here
            // Nu_remob[elC] := C2N_protein * Nu_remob[elN];
            // need to substract CRemob from dm rutnover?
            dGrowthPot = Yg * YgFactor * (Pgross + Cremob - Resp_m);	 //Net potential growth (C) of the day (excluding growth respiration)
            dGrowthPot = Math.Max(0.0, dGrowthPot);
            //double Resp_g = Pgross * (1 - Yg) / Yg;
            //dGrowthPot *= PCO2Effects();					  //multiply the CO2 effects. Dec10: This ihas been now incoporated in Pm/leaf area above

            //convert C to DM
            dGrowthPot *= C2DM;

            // phenologically related reduction of annual species (from IJ)
            if (isAnnual)
                dGrowthPot = annualSpeciesReduction();

            return dGrowthPot;

        }

        //Species --------------------------------------------------------------
        // phenology of anuual species
        /// <summary>Annuals the phenology.</summary>
        /// <returns></returns>
        public bool annualPhenology()
        {
            if (simToday.Month == monEmerg && simToday.Day == dayEmerg)
                phenoStage = 1;		 //vegetative stage
            else if (simToday.Month == monAnth && simToday.Day == dayAnth)
                phenoStage = 2;		 //reproductive

            if (phenoStage == 0)		//before emergence
            {
                dGrowthPot = 0;
                return false;		   //no growth
            }

            if (phenoStage == 1)		//vege
            {
                daysfromEmergence++;
                return true;
            }

            if (phenoStage == 2)
            {
                daysfromAnthesis++;
                if (daysfromAnthesis >= daysToMature)
                {
                    phenoStage = 0;
                    daysfromEmergence = 0;
                    daysfromAnthesis = 0;
                    dGrowthPot = 0;
                    return false;	   // Flag no growth after mature
                }
                return true;
            }
            return true;
        }


        //Species --------------------------------------------------------------
        // phenologically related reduction of annual species
        /// <summary>Annuals the species reduction.</summary>
        /// <returns></returns>
        public double annualSpeciesReduction()
        {
            double rFactor = 1;  // reduction factor of annual species
            if (phenoStage == 1 && daysfromEmergence < 60)  //decline at the begining due to seed bank effects ???
            {
                rFactor = 0.5 + 0.5 * daysfromEmergence / 60;
            }
            else if (phenoStage == 2)					   //decline of photosynthesis when approaching maturity
            {
                rFactor = 1.0 - (double)daysfromAnthesis / daysToMature;
            }
            dGrowthPot *= rFactor;
            return dGrowthPot;
        }



        //Species --------------------------------------------------------------
        //Plant photosynthesis increase to eleveated [CO2]
        /// <summary>Pcs the o2 effects.</summary>
        /// <returns></returns>
        public double PCO2Effects()
        {
            if (Math.Abs(ambientCO2 - referenceCO2) < 0.5)
                return 1.0;

            double Kp = CO2PmaxScale; //700; for C3 plants & 150 for C4
            if (photoPath == "C4")
                Kp = 150;

            double Fp = (ambientCO2 / (Kp + ambientCO2)) * ((referenceCO2 + Kp) / referenceCO2);
            return Fp;
        }

        //Species --------------------------------------------------------------
        // Plant nitrogen [N] decline to elevated [CO2]
        /// <summary>Ncs the o2 effects.</summary>
        /// <returns></returns>
        public double NCO2Effects()
        {
            if (Math.Abs(ambientCO2 - referenceCO2) < 0.5)
                return 1.0;

            double L = CO2NMin;		 // 0.7 - lamda: same for C3 & C4 plants
            double Kn = CO2NScale;	  // 600 - ppm,   when CO2 = 600ppm, Fn = 0.5*(1+lamda);
            double Qn = CO2NCurvature;  //2 - curveture factor

            double interm = Math.Pow((Kn - referenceCO2), Qn);
            double Fn = (L + (1 - L) * interm / (interm + Math.Pow((ambientCO2 - referenceCO2), Qn)));
            return Fn;
        }

        //Species --------------------------------------------------------------
        //Canopy conductiance decline to elevated [CO2]
        /// <summary>Conductances the c o2 effects.</summary>
        /// <returns></returns>
        public double ConductanceCO2Effects()
        {
            if (Math.Abs(ambientCO2 - referenceCO2) < 0.5)
                return 1.0;
            //Hard coded here, not used, should go to Micromet!
            double Gmin = 0.2;	  //Fc = Gmin when CO2->unlimited
            double Gmax = 1.25;	 //Fc = Gmax when CO2 = 0;
            double beta = 2.5;	  //curvature factor,

            double Fc = Gmin + (Gmax - Gmin) * (1 - Gmin) * Math.Pow(referenceCO2, beta) /
                               ((Gmax - 1) * Math.Pow(ambientCO2, beta) + (1 - Gmin) * Math.Pow(referenceCO2, beta));
            return Fc;
        }

        //Species ---------------------------------------------------------------
        //Calculate species N demand for potential growth (soilNdemand);
        /// <summary>Calculates the ndemand.</summary>
        /// <returns></returns>
        public double CalcNdemand()
        {
            fShoot = NewGrowthToShoot();
            double fL = UpdatefLeaf(); //to consider more dm to leaf when DM is lower?

            double toRoot = dGrowthW * (1.0 - fShoot);
            double toStol = dGrowthW * fShoot * fStolon;
            double toLeaf = dGrowthW * fShoot * fLeaf;
            double toStem = dGrowthW * fShoot * (1.0 - fStolon - fLeaf);

            //N demand for new growth (kg/ha)
            NdemandOpt = toRoot * NcrootOpt + toStol * NcstolOpt + toLeaf * NcleafOpt + toStem * NcstemOpt;
            NdemandOpt *= NCO2Effects();	//reduce the demand under elevated [co2],
            //this will reduce the N stress under N limitation for the same soilN

            //N demand for new growth assuming luxury uptake to max [N]
            NdemandLux = toRoot * NcrootMax + toStol * NcstolMax + toLeaf * NcleafMax + toStem * NcstemMax;
            //Ndemand *= NCO2Effects();	   //luxary uptake not reduce

            //even with sufficient soil N available
            if (isLegume)
                Nfix = MinFix * NdemandLux;
            else
                Nfix = 0.0;

            return Nfix;
        }


        //------------------------------------------
        /// <summary>Updatefs the leaf.</summary>
        /// <returns></returns>
        public double UpdatefLeaf()
        {
            //temporary, need to do as interpolatiopon set
            double fL = 1.0;   //fraction of shoot goes to leaf
            if (isLegume)
            {
                if (dmgreen > 0 && (dmstol / dmgreen) > fStolon)
                    fL = 1.0;
                else if (PlantShootWt < 2000)
                    fL = fLeaf + (1 - fLeaf) * PlantShootWt / 2000;
                else
                    fL = fLeaf;
            }
            else //grasses
            {
                if (PlantShootWt < 2000)
                    fL = fLeaf + (1 - fLeaf) * PlantShootWt / 2000;
                else
                    fL = fLeaf;
            }
            return fL;
        }

        //Species -------------------------------------------------------------
        /// <summary>Dailies the growth w.</summary>
        /// <returns></returns>
        public double DailyGrowthW()
        {
            Ncfactor = PmxNeffect();

            // NcFactor were addeded in Pm and Resp_m, Dec 10
            //  dGrowthW = dGrowthPot * Math.Min(gfwater, Ncfactor);
            dGrowthW = dGrowthPot * Math.Pow(gfwater, waterStressFactor);

            return dGrowthW;
        }

        //Species -------------------------------------------------------------
        /// <summary>Dailies the growth act.</summary>
        /// <returns></returns>
        public double DailyGrowthAct()
        {
            double gfnit = 0.0;
            if (isLegume)
                gfnit = gfn;						   //legume no dilution, but reducing more DM (therefore LAI)
            else
                gfnit = Math.Pow(gfn, NdilutCoeff);	// more DM growth than N limited, due to dilution (typically NdilutCoeff = 0.5)

            dGrowth = dGrowthW * Math.Min(gfnit, gfGen);
            return dGrowth;

            //RCichota, Jan/2014: updated the function, added account for Frgr
        }

        //Species -------------------------------------------------------------
        /// <summary>PMXs the neffect.</summary>
        /// <returns></returns>
        public double PmxNeffect()
        {
            double Fn = NCO2Effects();

            double Nleaf_green = 0;
            double effect = 1.0;
            if (!isAnnual)  //  &&and FVegPhase and ( VegDay < 10 ) ) then  // need this or it never gets going
            {
                Nleaf_green = Nleaf1 + Nleaf2 + Nleaf3;
                if (dmleaf_green > 0)
                {
                    double Ncleaf_green = Nleaf_green / dmleaf_green;
                    if (Ncleaf_green < NcleafOpt * Fn)	 //Fn
                    {
                        if (Ncleaf_green > NcleafMin)
                        {
                            //effect = Math.Min(1.0, Ncleaf_green / NcleafOpt*Fn);
                            effect = Math.Min(1.0, (Ncleaf_green - NcleafMin) / (NcleafOpt * Fn - NcleafMin));
                        }
                        else
                        {
                            effect = 0;
                        }
                    }
                }
            }
            return effect;
        }

        //Species -------------------------------------------------------------
        /// <summary>ns the fix cost.</summary>
        /// <returns></returns>
        public double NFixCost()
        {
            double costF = 1.0;	//  redcuiton fraction of net prodcution as cost of N-fixining
            if (!isLegume || Nfix == 0 || NdemandLux == 0)	  //  happens when plant has no growth
            { return costF; }

            double actFix = Nfix / NdemandLux;
            costF = 1 - 0.24 * (actFix - MinFix) / (MaxFix - MinFix);
            if (costF < 0.76)
                costF = 0.76;
            return costF;
        }



        //Species -------------------------------------------------------------
        /// <summary>Partitions the turnover.</summary>
        /// <returns></returns>
        public double PartitionTurnover()
        {
            double GFT = GFTemperature();	   // Temperature response

            //Leaf appearance rate is modified by temp & water stress
            double rateLeaf = leafRate * GFT * (Math.Pow(gfwater, 0.33333));  //why input is 3
            if (rateLeaf < 0.0) rateLeaf = 0.0;
            if (rateLeaf > 1.0) rateLeaf = 1.0;

            if (dGrowth > 0.0)				  // if no net growth, then skip "partition" part
            {
                //Not re-calculate fShoot for avoiding N-inbalance

                //New growth is allocated to the 1st pools
                //fLeaf & fStolon: fixed partition to leaf & stolon.
                //Fractions [eq.4.13]
                double toRoot = 1.0 - fShoot;
                double toStol = fShoot * fStolon;
                double toLeaf = fShoot * fLeaf;
                double toStem = fShoot * (1.0 - fStolon - fLeaf);

                //checking
                double ToAll = toLeaf + toStem + toStol + toRoot;
                if (Math.Abs(ToAll - 1.0) > 0.01)
                { /*Console.WriteLine("checking partitioning fractions");*/ }

                //Assign the partitioned growth to the 1st tissue pools
                dmleaf1 += toLeaf * dGrowth;
                dmstem1 += toStem * dGrowth;
                dmstol1 += toStol * dGrowth;
                dmroot += toRoot * dGrowth;
                dGrowthHerbage = (toLeaf + toStem + toStol) * dGrowth;

                //partitioing N based on not only the DM, but also [N] in plant parts
                double Nsum = toLeaf * NcleafMax + toStem * NcstemMax + toStol * NcstolMax + toRoot * NcrootMax;
                double toLeafN = toLeaf * NcleafMax / Nsum;
                double toStemN = toStem * NcstemMax / Nsum;
                double toStolN = toStol * NcstolMax / Nsum;
                double toRootN = toRoot * NcrootMax / Nsum;

                Nleaf1 += toLeafN * newGrowthN;
                Nstem1 += toStemN * newGrowthN;
                Nstol1 += toStolN * newGrowthN;
                Nroot += toRootN * newGrowthN;

                double leftoverNremob = Nremob * Kappa4;  // fraction of Nremob not used, added to dead tissue
                if (leftoverNremob > 0)
                {
                    double DMsum = dmleaf4 + dmstem;
                    Nleaf4 += leftoverNremob * dmleaf4 / DMsum;
                    Nstem4 += leftoverNremob * dmstem4 / DMsum;
                }

                // check whether luxury N was remobilised during N balance
                if (NFastRemob2 + NFastRemob3 > 0.0)
                {
                    // partition any used N into plant parts (by N content)
                    if (NFastRemob2 > 0.0)
                    {
                        Nsum = Nleaf2 + Nstem2 + Nstol2;
                        Nleaf2 -= NFastRemob2 * Nleaf2 / Nsum;
                        Nstem2 -= NFastRemob2 * Nstem2 / Nsum;
                        Nstol2 -= NFastRemob2 * Nstol2 / Nsum;
                    }
                    if (NFastRemob3 > 0.0)
                    {
                        Nsum = Nleaf3 + Nstem3 + Nstol3;
                        Nleaf3 -= NFastRemob3 * Nleaf3 / Nsum;
                        Nstem3 -= NFastRemob3 * Nstem3 / Nsum;
                        Nstol3 -= NFastRemob3 * Nstol3 / Nsum;
                    }
                }
                // accumtotalnewN += newGrowthN;

            }  //end of "partition" block

            //**Tussue turnover among the 12 standing biomass pools
            //The rates are affected by water and temperature factor
            double gftt = GFTempTissue();
            double gfwt = GFWaterTissue();

            gama = gftt * gfwt * rateLive2Dead;
            gamas = gama;									//for stolon of legumes
            //double gamad = gftt * gfwt * rateDead2Litter;
            double SR = 0;  //stocking rate affacting transfer of dead to little (default as 0 for now)
            gamad = rateDead2Litter * Math.Pow(gfwater, 3) * digestDead / 0.4 + stockParameter * SR;

            gamar = gftt * (2 - gfwater) * rateRootSen;  //gfwt * rateRootSen;


            if (gama == 0.0) //if gama ==0 due to gftt or gfwt, then skip "turnover" part
            {
                //no new little or root senensing
                dLitter = 0;
                dNLitter = 0;
                dRootSen = 0;
                dNrootSen = 0;
                //Nremob = Nremob; //no change
                //Nroot = Nroot;
            }
            else
            {
                if (isAnnual)
                {
                    if (phenoStage == 1)		//vege
                    {
                        double Kv = (double)daysfromEmergence / daysEmgToAnth;
                        gama *= Kv;
                        gamar *= Kv;
                    }
                    else if (phenoStage == 2)	//repro
                    {
                        double Kr = (double)daysfromAnthesis / daysToMature;
                        gama = 1 - (1 - gama) * (1 - Kr * Kr);
                    }
                }

                // get daily defoliation: Fd = fraction of defoliation
                double Fd = 0;								  //TODO with animal module later
                if (pS.dmdefoliated != 0 && pS.dmshoot != 0)
                    Fd = pS.dmdefoliated / (pS.dmdefoliated + pS.dmshoot);

                //gamar = gamar + Fd * Fd * (1 - gamar);
                //**Nov 09: Decided not to reduce root mass mmediately in a high proportion according to defoliation,
                //**Gradual process is more reasonable, and this results in a very smmall difference in predicting prodution

                if (isLegume) gamas = gama + Fd * (1 - gama);   //increase stolon senescence

                //if today's turnover will result in a dmgreen < dmgreen_minimum, then adjust the rate,
                //double dmgreenToBe = dmgreen + dGrowth - gamad * (pS.dmleaf4 + pS.dmstem4 + pS.dmstol3);
                //Possibly to skip this for annuals to allow them to die - phenololgy-related?
                double dmgreenToBe = dmgreen + dGrowth - gama * (pS.dmleaf3 + pS.dmstem3 + pS.dmstol3);
                if (dmgreenToBe < dmgreenmin)
                {
                    double preDMgreen = pS.dmgreen;
                    if (gama > 0.0)
                    {
                        if (dmgreen + dGrowth < dmgreenmin)
                        {
                            gama = 0;
                            gamas = 0;
                            //  gamad = 0;
                            gamar = 0;
                        }
                        else
                        {
                            double gama_adj = (dmgreen + dGrowth - dmgreenmin) / (pS.dmleaf3 + pS.dmstem3 + pS.dmstol3);
                            gamar = gamar * gama_adj / gama;
                            gamad = gamad * gama_adj / gama;
                            gama = gama_adj;
                        }
                    }
                }
                if (dmroot < 0.5 * dmgreenmin)		  //set a minimum root too
                    gamar = 0;

                //Do actual DM turnover
                dmleaf1 = dmleaf1 - 2 * gama * pS.dmleaf1;				//except dmleaf1, other pool dm* = pS.dm*
                dmleaf2 = dmleaf2 - gama * pS.dmleaf2 + 2 * gama * pS.dmleaf1;
                dmleaf3 = dmleaf3 - gama * pS.dmleaf3 + gama * pS.dmleaf2;
                dmleaf4 = dmleaf4 - gamad * pS.dmleaf4 + gama * pS.dmleaf3;
                dGrowthHerbage -= gamad * pS.dmleaf4;

                dmstem1 = dmstem1 - 2 * gama * pS.dmstem1;
                dmstem2 = dmstem2 - gama * pS.dmstem2 + 2 * gama * pS.dmstem1;
                dmstem3 = dmstem3 - gama * pS.dmstem3 + gama * pS.dmstem2;
                dmstem4 = dmstem4 - gamad * pS.dmstem4 + gama * pS.dmstem3;
                dGrowthHerbage -= gamad * pS.dmstem4;

                dmstol1 = dmstol1 - 2 * gamas * pS.dmstol1;
                dmstol2 = dmstol2 - gamas * pS.dmstol2 + 2 * gamas * pS.dmstol1;
                dmstol3 = dmstol3 - gamas * pS.dmstol3 + gamas * pS.dmstol2;
                dGrowthHerbage -= gamas * pS.dmstol3;

                dRootSen = gamar * pS.dmroot;
                dmroot = dmroot - dRootSen;// -Resp_root;

                //Previous: N (assuming that Ncdead = Ncleaf4, Ncstem4 or Nclitter):  Nc --[N]
                double Nleaf1to2 = Ncleaf1 * 2 * gama * pS.dmleaf1;
                double Nleaf2to3 = Ncleaf2 * gama * pS.dmleaf2;
                double Nleaf3to4 = Ncleaf4 * gama * pS.dmleaf3;		 //Ncleaf = NcleafMin: [N] in naturally scenescend tissue
                double Nleaf3Remob = (Ncleaf3 - Ncleaf4) * gama * pS.dmleaf3;
                double Nleaf4toL = Ncleaf4 * gamad * pS.dmleaf4;		//to litter
                Nleaf1 = Nleaf1 - Nleaf1to2;
                Nleaf2 = Nleaf2 + Nleaf1to2 - Nleaf2to3;
                Nleaf3 = Nleaf3 + Nleaf2to3 - Nleaf3to4 - Nleaf3Remob;
                Nleaf4 = Nleaf4 + Nleaf3to4 - Nleaf4toL;

                if (dmleaf1 != 0) { Ncleaf1 = Nleaf1 / dmleaf1; }
                if (dmleaf2 != 0) { Ncleaf2 = Nleaf2 / dmleaf2; }
                if (dmleaf3 != 0) { Ncleaf3 = Nleaf3 / dmleaf3; }
                if (dmleaf4 != 0) { Ncleaf4 = Nleaf4 / dmleaf4; }

                double Nstem1to2 = Ncstem1 * 2 * gama * pS.dmstem1;
                double Nstem2to3 = Ncstem2 * gama * pS.dmstem2;
                double Nstem3to4 = Ncstem4 * gama * pS.dmstem3;
                double Nstem3Remob = (Ncstem3 - Ncstem4) * gama * pS.dmstem3;
                double Nstem4toL = Ncstem4 * gamad * pS.dmstem4;   //to litter

                Nstem1 = Nstem1 - Nstem1to2;
                Nstem2 = Nstem2 + Nstem1to2 - Nstem2to3;
                Nstem3 = Nstem3 + Nstem2to3 - Nstem3to4 - Nstem3Remob;
                Nstem4 = Nstem4 + Nstem3to4 - Nstem4toL;

                if (dmstem1 != 0) { Ncstem1 = Nstem1 / dmstem1; }
                if (dmstem2 != 0) { Ncstem2 = Nstem2 / dmstem2; }
                if (dmstem3 != 0) { Ncstem3 = Nstem3 / dmstem3; }
                if (dmstem4 != 0) { Ncstem4 = Nstem4 / dmstem4; }

                double Nstol1to2 = Ncstol1 * 2 * gamas * pS.dmstol1;
                double Nstol2to3 = Ncstol2 * gamas * pS.dmstol2;
                double Nstol3Remob = 0.5 * (Ncstol3 - NcstolMin) * gamas * pS.dmstol3;	   //gamas is acelerated by defoliation
                double Nstol3toL = Ncstol3 * gamas * pS.dmstol3 - Nstol3Remob;

                Nstol1 = Nstol1 - Nstol1to2;
                Nstol2 = Nstol2 + Nstol1to2 - Nstol2to3;
                Nstol3 = Nstol3 + Nstol2to3 - Nstol3toL - Nstol3Remob;

                if (dmstol1 != 0) { Ncstol1 = Nstol1 / dmstol1; } //grass has no stolon
                if (dmstol2 != 0) { Ncstol2 = Nstol2 / dmstol2; }
                if (dmstol3 != 0) { Ncstol3 = Nstol3 / dmstol3; }

                //rootN
                NrootRemob = 0.5 * (Ncroot - NcrootMin) * dRootSen;	//acelerated by defoliation, the N remob smaller
                dNrootSen = Ncroot * dRootSen - NrootRemob;
                Nroot = Nroot - Ncroot * dRootSen;			  // (Ncroot goes to both Remob & FOM in soil)
                if (dmroot != 0) Ncroot = Nroot / dmroot;	   // dmroot==0 this should not happen

                dLitter = gamad * (pS.dmleaf4 + pS.dmstem4) + gamas * pS.dmstol3;

                double leftoverNremob = Nremob;
                dNLitter = Nleaf4toL + Nstem4toL + Nstol3toL + leftoverNremob;	//Nremob of previous day after newgrowth, go to litter

                // remobilised and remobilisable N (these will be used tomorrow)
                Nremob = Nleaf3Remob + Nstem3Remob + Nstol3Remob + NrootRemob;
                NLuxury2 = Math.Max(0.0, Nleaf2 - dmleaf2 * NcleafOpt * NcRel2)
                         + Math.Max(0.0, Nstem2 - dmstem2 * NcstemOpt * NcRel2)
                         + Math.Max(0.0, Nstol2 - dmstol2 * NcstolOpt * NcRel2);
                NLuxury3 = Math.Max(0.0, Nleaf3 - dmleaf3 * NcleafOpt * NcRel3)
                         + Math.Max(0.0, Nstem3 - dmstem3 * NcstemOpt * NcRel3)
                         + Math.Max(0.0, Nstol3 - dmstol3 * NcstolOpt * NcRel3);
                // only a fraction of luxury N is available for remobilisation:
                NLuxury2 *= Kappa2;
                NLuxury3 *= Kappa3;

                //Sugar remobilisation and C balance:
                Cremob = 0;// not explicitely considered

            }  //end of "turnover" block

            updateAggregated();

            calcDigestability();

            return dGrowth;
        }


        //Species ------------------------------------------------------------------
        /// <summary>News the growth to shoot.</summary>
        /// <returns></returns>
        private double NewGrowthToShoot()
        {
            //The input maxSRratio (maximum percentage allocated to roots = 20%) was converted into
            //the real ratio (=4) at the beginning when setting specific values
            double GFmin = Math.Min(gfwater, gfn);	  //To consider other nutrients later

            //Variable maxSR - maximum shoot/root ratio accoding to phenoloty
            double maxSR = maxSRratio;
            // fac: Assuming the new growth partition is towards a shoot:root ratio of 'maxSR' during reproductive stage,
            //	  then the partition will be towards a lower shoot:root ratio of (frac*maxSRratio) during vegetative stage

            double minF = allocationSeasonF;	//default = 0.8;
            double fac = 1.0;				   //day-to-day fraction of reduction
            int doy = simToday.Day + (int)((simToday.Month - 1) * 30.5);

            // double pd = 4*Math.PI* doy/365;
            // double toRoot = 1/(1 + maxSRratio);
            // toRoot = toRoot + 0.25*maxSRratio * Math.Sin(pd);

            int doyC = 232;			 // Default as in South-hemisphere
            if (localLatitude > 0)		   // If it is in North-hemisphere.
                doyC = doyC - 181;

            int doyF = doyC + 35;   //75
            int doyD = doyC + 95;   // 110;
            int doyE = doyC + 125;  // 140;
            if (doyE > 365) doyE = doyE - 365;

            if (doy > doyC)
            {
                if (doy <= doyF)
                    fac = minF + (1 - minF) * (doy - doyC) / (doyF - doyC);
                else if (doy <= doyD)
                    fac = 1.0;
                else if (doy <= doyE)
                    fac = 1 - (1 - minF) * (doy - doyD) / (doyE - doyD);
            }
            else
            {
                fac = minF;
                if (doyE < doyC && doy <= doyE)	//only happens in south hemisphere
                    fac = 1 - (1 - minF) * (365 + doy - doyD) / (doyE - doyD);

            }
            maxSR = 1.25 * fac * maxSRratio;	//maxR is bigger in reproductive stage (i.e., less PHT going to root)
            //fac = 0.8 ~ 1; i.e., maxSR = 1.0 ~ 1.25 of maxSRratio (i.e., SRratio may be 1.25 times of specified maxSRratio during reproductive stage)

            phenoFactor = fac;
            //calculate shoot:root partitioning: fShoot = fraction to shoot [eq.4.12c]
            if (pS.dmroot > 0.00001)					//pS is the previous state (yesterday)
            {
                double SRratio = dmgreen / pS.dmroot;
                if (SRratio > maxSR) SRratio = maxSR;

                double param = GFmin * maxSR * maxSR / SRratio;
                fShoot = param / (1.0 + param);
            }
            else
            {
                fShoot = 1.0;
            }


            /* resistance after drought
             *
             * if (gfwater > 0.5 && dayCounter >= 5 && sumGFW < 1)
            {
                fShoot = 1;
                sumGFW = 0;
            }
            else
            {
                dayCounter++;
                sumGFW +=gfwater;
            }*/

            if (fShoot / (1 - fShoot) < maxSR)  // Set daily minimum fraction to shoot (i.e., maximum to root)
                fShoot = maxSR / (1 + maxSR);   // as the specified that the system maxSR towards to (useful under stress)

            if (dmgreen < pS.dmroot)  //this may happen under stress. There may be CHTs move up too
                fShoot = 1.0;

            return fShoot;
        }

        //Species -------------------------------------------------------------------
        /// <summary>Gets the cover green.</summary>
        /// <value>The cover green.</value>
        public double coverGreen
        {
            get { return (1.0 - Math.Exp(-lightExtCoeff * greenLAI)); }
        }
        //Species -------------------------------------------------------------------
        /// <summary>Gets the cover dead.</summary>
        /// <value>The cover dead.</value>
        public double coverDead
        {
            get { return (1.0 - Math.Exp(-lightExtCoeff * deadLAI)); }
        }
        //Species -------------------------------------------------------------------
        /// <summary>Gets the cover tot.</summary>
        /// <value>The cover tot.</value>
        public double coverTot
        {
            get { return (1.0 - (Math.Exp(-lightExtCoeff * totalLAI))); }
        }

        //Species ---------------------------------------------------------------------
        /// <summary>Gfs the temperature.</summary>
        /// <returns></returns>
        public double GFTemperature()
        {
            if (photoPath == "C4") gftemp = GFTempC4();
            else gftemp = GFTempC3();			   //CAM path ?
            return gftemp;
        }
        /// <summary>Gfs the temperature.</summary>
        /// <param name="T">The t.</param>
        /// <returns></returns>
        public double GFTemperature(double T)	   //passing T
        {
            if (photoPath == "C4") gftemp = GFTempC4(T);
            else gftemp = GFTempC3(T);
            return gftemp;
        }
        //Species -------------------------------------------------
        // Photosynthesis temperature response curve for C3 plants
        /// <summary>Gfs the temporary c3.</summary>
        /// <returns></returns>
        public double GFTempC3()
        {
            double gft3 = 0.0;
            if (Tmean > growthTmin && Tmean < growthTmax)
            {
                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((Tmean - growthTmin), growthTq) * (Tmax - Tmean);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft3 = val1 / val2;

                if (gft3 < 0.0) gft3 = 0.0;
                if (gft3 > 1.0) gft3 = 1.0;
            }
            return gft3;
        }
        //Species -------------------------------------------------
        // Photosynthesis temperature response curve for C3 plants, passing T
        /// <summary>Gfs the temporary c3.</summary>
        /// <param name="T">The t.</param>
        /// <returns></returns>
        public double GFTempC3(double T)
        {
            double gft3 = 0.0;
            if (T > growthTmin && T < growthTmax)
            {
                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft3 = val1 / val2;

                if (gft3 < 0.0) gft3 = 0.0;
                if (gft3 > 1.0) gft3 = 1.0;
            }
            return gft3;
        }

        //Species ---------------------------------------------
        // Photosynthesis temperature response curve for C4 plants
        /// <summary>Gfs the temporary c4.</summary>
        /// <returns></returns>
        public double GFTempC4()
        {
            double gft4 = 0.0;		  // Assign value 0 for the case of T < Tmin

            if (Tmean > growthTmin)		 // same as GFTempC3 for [Tmin,Topt], but T as Topt if T > Topt
            {
                if (Tmean > growthTopt)
                    Tmean = growthTopt;

                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((Tmean - growthTmin), growthTq) * (Tmax - Tmean);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft4 = val1 / val2;

                if (gft4 < 0.0) gft4 = 0.0;
                if (gft4 > 1.0) gft4 = 1.0;
            }
            return gft4;
        }

        //Species ---------------------------------------------
        // Photosynthesis temperature response curve for C4 plants, passing T
        /// <summary>Gfs the temporary c4.</summary>
        /// <param name="T">The t.</param>
        /// <returns></returns>
        public double GFTempC4(double T)
        {
            double gft4 = 0.0;		  // Assign value 0 for the case of T < Tmin

            if (T > growthTmin)		 // same as GFTempC3 for [Tmin,Topt], but T as Topt if T > Topt
            {
                if (T > growthTopt)
                    T = growthTopt;

                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft4 = val1 / val2;

                if (gft4 < 0.0) gft4 = 0.0;
                if (gft4 > 1.0) gft4 = 1.0;
            }
            return gft4;
        }

        //Species ---------------------------------------------
        // Heat effect: reduction = (MaxT-28)/35, recovery after accumulating 50C of (meanT-25)
        /// <summary>Heats the effect.</summary>
        /// <returns></returns>
        private double HeatEffect()
        {
            //constants are now set from interface
            //recover from the previous high temp. effect
            double recoverF = 1.0;

            if (highTempEffect < 1.0)
            {
                if (25 - Tmean > 0)
                {
                    accumT += (25 - Tmean);
                }

                if (accumT < heatSumT)
                {
                    recoverF = highTempEffect + (1 - highTempEffect) * accumT / heatSumT;
                }
            }

            //possible new high temp. effect
            double newHeatF = 1.0;
            if (Tmax > heatFullT)
            {
                newHeatF = 0;
            }
            else if (Tmax > heatOnsetT)
            {
                newHeatF = (Tmax - heatOnsetT) / (heatFullT - heatOnsetT);
            }

            // If this new high temp. effect is compounded with the old one &
            // re-start of the recovery from the new effect
            if (newHeatF < 1.0)
            {
                highTempEffect = recoverF * newHeatF;
                accumT = 0;
                recoverF = highTempEffect;
            }

            return recoverF;
        }

        //Species ---------------------------------------------
        // Cold effect: reduction, recovery after accumulating 20C of meanT
        /// <summary>Colds the effect.</summary>
        /// <returns></returns>
        private double ColdEffect()
        {
            //recover from the previous high temp. effect
            double recoverF = 1.0;
            if (lowTempEffect < 1.0)
            {
                if (Tmean > 0)
                {
                    accumTLow += Tmean;
                }

                if (accumTLow < coldSumT)
                {
                    recoverF = lowTempEffect + (1 - lowTempEffect) * accumTLow / coldSumT;
                }
            }

            //possible new low temp. effect
            double newColdF = 1.0;
            if (Tmin < coldFullT)
            {
                newColdF = 0;
            }
            else if (Tmin < coldOnsetT)
            {
                newColdF = (Tmin - coldFullT) / (coldOnsetT - coldFullT);
            }

            // If this new cold temp. effect happens when serious cold effect is still on,
            // compound & then re-start of the recovery from the new effect
            if (newColdF < 1.0)
            {
                lowTempEffect = newColdF * recoverF;
                accumTLow = 0;
                recoverF = lowTempEffect;
            }

            return recoverF;
        }

        //Species ----------------------------------------------------------
        // Tissue turnover rate's response to water stress (eq. 4.15h)
        /// <summary>Gfs the water tissue.</summary>
        /// <returns></returns>
        public double GFWaterTissue()
        {
            double gfwt = 1.0;

            if (gfwater < massFluxWopt)
                gfwt = 1 + (massFluxW0 - 1.0) * ((massFluxWopt - gfwater) / massFluxWopt);

            if (gfwt < 1.0) gfwt = 1.0;
            if (gfwt > massFluxW0) gfwt = massFluxW0;
            return gfwt;
        }

        //Species ------------------------------------------------------
        // Tissue turnover rate's response to temperature (eq 4.15f)
        // Tissue turnover: Tmin=5, Topt=20 - same for C3 & C4 plants ?
        /// <summary>Gfs the temporary tissue.</summary>
        /// <returns></returns>
        public double GFTempTissue()
        {
            double gftt = 0.0;		//default as T < massFluxTmin
            if (Tmean > massFluxTmin && Tmean <= massFluxTopt)
            {
                gftt = (Tmean - massFluxTmin) / (massFluxTopt - massFluxTmin);
            }
            else if (Tmean > massFluxTopt)
            {
                gftt = 1.0;
            }
            return gftt;
        }
        // Species ----------------------------------------------------------------------
        /// <summary>Resets the zero.</summary>
        public void ResetZero()  //kill this crop
        {
            //Reset dm pools
            dmleaf1 = dmleaf2 = dmleaf3 = dmleaf4 = 0;	//(kg/ha)
            dmstem1 = dmstem2 = dmstem3 = dmstem4 = 0;	//sheath and stem
            dmstol1 = dmstol2 = dmstol3 = 0;
            dmroot = 0;
            dmlitter = 0;

            dmdefoliated = 0;

            //Reset N pools
            Nleaf1 = Nleaf2 = Nleaf3 = Nleaf4 = 0;
            Nstem1 = Nstem2 = Nstem3 = Nstem4 = 0;
            Nstol1 = Nstol2 = Nstol3 = Nroot = Nlitter = 0;

            phenoStage = 0;

            if (updateAggregated() > 0.0)  //return totalLAI = 0
            {
                Console.WriteLine("Plant is not completely killed.");
            }
        }


        //Species ---------------------------------------------------------
        /// <summary>Sets the in germination.</summary>
        public void SetInGermination()
        {
            bSown = true;
            phenoStage = 0; //before germination
        }

        //Species ---------------------------------------------------------
        /// <summary>Sets the previous pools.</summary>
        /// <returns></returns>
        public bool SetPrevPools()
        {
            pS.dmleaf1 = dmleaf1;
            pS.dmleaf2 = dmleaf2;
            pS.dmleaf3 = dmleaf3;
            pS.dmleaf4 = dmleaf4;
            pS.dmstem1 = dmstem1;
            pS.dmstem2 = dmstem2;
            pS.dmstem3 = dmstem3;
            pS.dmstem4 = dmstem4;
            pS.dmstol1 = dmstol1;
            pS.dmstol2 = dmstol2;
            pS.dmstol3 = dmstol3;
            pS.dmlitter = dmlitter;
            pS.dmroot = dmroot;
            pS.dmleaf_green = dmleaf_green;
            pS.dmstem_green = dmstem_green;
            pS.dmstol_green = dmstol_green;
            pS.dmleaf = dmleaf;
            pS.dmstem = dmstem;
            pS.dmstol = dmstol;
            pS.dmshoot = dmshoot;
            pS.dmgreen = dmgreen;
            pS.dmdead = dmdead;
            pS.dmtotal = dmtotal;

            return true;
        }

    } //class Species

    //------ RemoveCropBiomassdm ------
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RemoveCropBiomassdmType
    {
        /// <summary>The pool</summary>
        public string pool = "";
        /// <summary>The part</summary>
        public string[] part;
        /// <summary>The DLT</summary>
        public double[] dlt;
    }

    //------ RemoveCropBiomass ------
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RemoveCropBiomassType
    {
        /// <summary>The dm</summary>
        public RemoveCropBiomassdmType[] dm;
    }

    //------ Sow ------
    /// <summary>
    /// 
    /// </summary>
    public class SowType
    {
        /// <summary>The cultivar</summary>
        public string Cultivar = "";
        /// <summary>The plants</summary>
        public double plants;
        /// <summary>The sowing_depth</summary>
        public double sowing_depth;
        /// <summary>The row_spacing</summary>
        public double row_spacing;
        /// <summary>The skip row</summary>
        public double SkipRow;
        /// <summary>The skip plant</summary>
        public double SkipPlant;
        /// <summary>The establishment</summary>
        public string Establishment = "";
        /// <summary>The crop_class</summary>
        public string crop_class = "";
        /// <summary>The tiller_no_fertile</summary>
        public string tiller_no_fertile = "";
        /// <summary>The skip</summary>
        public string Skip = "";
        /// <summary>The plants_pm</summary>
        public double plants_pm;
        /// <summary>The ratoon</summary>
        public int Ratoon;
        /// <summary>The sbdur</summary>
        public int sbdur;
        /// <summary>The NPLH</summary>
        public double nplh;
        /// <summary>The nh</summary>
        public double nh;
        /// <summary>The NPLSB</summary>
        public double nplsb;
        /// <summary>The NPLDS</summary>
        public double nplds;
    }

    //------ Graze ------
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class GrazeType
    {
        /// <summary>The sender</summary>
        public string sender = "";
        /// <summary>The amount</summary>
        public double amount;
        /// <summary>The type</summary>
        public string type = "";
    }

    //------ KillCrop ------
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class KillCropType
    {
        /// <summary>The kill fraction</summary>
        public double KillFraction;
    }

    //DMPools =================================================
    //for remember the pool status of previous day
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DMPools
    {
        /// <summary>The dmleaf1</summary>
        public double dmleaf1;
        /// <summary>The dmleaf2</summary>
        public double dmleaf2;
        /// <summary>The dmleaf3</summary>
        public double dmleaf3;
        /// <summary>The dmleaf4</summary>
        public double dmleaf4;
        /// <summary>The dmstem1</summary>
        public double dmstem1;
        /// <summary>The dmstem2</summary>
        public double dmstem2;
        /// <summary>The dmstem3</summary>
        public double dmstem3;
        /// <summary>The dmstem4</summary>
        public double dmstem4;
        /// <summary>The dmstol1</summary>
        public double dmstol1;
        /// <summary>The dmstol2</summary>
        public double dmstol2;
        /// <summary>The dmstol3</summary>
        public double dmstol3;
        /// <summary>The dmlitter</summary>
        public double dmlitter;
        /// <summary>The dmroot</summary>
        public double dmroot;

        /// <summary>The dmleaf</summary>
        public double dmleaf;
        /// <summary>The dmstem</summary>
        public double dmstem;
        /// <summary>The dmleaf_green</summary>
        public double dmleaf_green;
        /// <summary>The dmstem_green</summary>
        public double dmstem_green;
        /// <summary>The dmstol_green</summary>
        public double dmstol_green;
        /// <summary>The dmstol</summary>
        public double dmstol;
        /// <summary>The dmshoot</summary>
        public double dmshoot;
        /// <summary>The dmgreen</summary>
        public double dmgreen;
        /// <summary>The dmdead</summary>
        public double dmdead;
        /// <summary>The dmtotal</summary>
        public double dmtotal;
        /// <summary>The dmdefoliated</summary>
        public double dmdefoliated;
        /// <summary>The nremob</summary>
        public double Nremob;

        /// <summary>Initializes a new instance of the <see cref="DMPools"/> class.</summary>
        public DMPools() { }


    } //class DMPools

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LinearInterpolation
    {
        //public string[] XYs;

        /// <summary>The x</summary>
        public double[] X;
        /// <summary>The y</summary>
        public double[] Y;

        /*
        [EventSubscribe("Initialised")]
        public void OnInitialised()
        {

            X = new double[XYs.Length];
            Y = new double[XYs.Length];
            for (int i = 0; i < XYs.Length; i++)
            {
                string[] XYBits = XYs[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (XYBits.Length != 2)
                    throw new Exception("Invalid XY coordinate for function. Value: " + XYs[i]);
                X[i] = Convert.ToDouble(XYBits[0]);
                Y[i] = Convert.ToDouble(XYBits[1]);
            }
        } */
        /// <summary>Values the specified d x.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double Value(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
