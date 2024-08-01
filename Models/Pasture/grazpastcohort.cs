using System;
using System.Linq;
using StdUnits;
using static Models.GrazPlan.GrazType;

namespace Models.GrazPlan
{
    /// <summary>
    /// TPastureCohort class
    /// Herbage cohort within a pasture population
    /// </summary>
    public class TPastureCohort
    {
        /// <summary></summary>
        private readonly TPasturePopulation Owner;

        /// <summary></summary>
        private TPastureParamSet Params;

        // Temporary variables -----------------------------------------------------

        /// <summary>New specific area. ptLEAF..ptSEED</summary>
        private readonly double[] FNewSpecificArea = new double[GrazType.ptSTEM + 1];

        /// <summary></summary>
        private double FPotAssimilation;

        /// <summary></summary>
        private double[,] FPotRootTransloc = new double[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];         // [EFFR..OLDR,1..MaxSoilLayers]

        /// <summary></summary>
        private double FPotRootTranslocSum;

        /// <summary>Biomass translocated from belowground</summary>
        private readonly double[,] FRootTransloc = new double[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];   // [EFFR..OLDR,1..MaxSoilLayers]

        /// <summary></summary>
        private readonly double[] FPotStemTransloc = new double[GrazType.HerbClassNo + 1];

        /// <summary></summary>
        private double FPotStemTranslocSum;

        /// <summary>Potential net growth of the plant part. ptLEAF..ptSEED</summary>
        private readonly double[] FPotPartNetGrowth = new double[GrazType.ptSEED + 1];

        /// <summary></summary>
        public double[] FStemTransloc = new double[GrazType.HerbClassNo + 1];

        /// <summary>Maintenance respiration rates   g/g/d. ptLEAF..ptSEED</summary>
        private readonly double[] FMaintRespRate = new double[GrazType.ptSEED + 1];

        /// <summary>Maintenance respiration rates g/m^2/ds</summary>
        private readonly double[,] FShootMaintResp = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];     // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        private readonly double[,] FRootMaintResp = new double[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];      // [EFFR..OLDR, 1..MaxSoilLayers]

        /// <summary>Growth respiration rate  g/g</summary>
        private double FGrowthRespRate;

        /// <summary>Net growth incl.translocation,g/m^2/d. ptLEAF..ptSEED</summary>
        public double[] FPartNetGrowth = new double[GrazType.ptSEED + 1];

        /// <summary></summary>
        public GrazType.DM_Pool[,] FShootNetGrowth = new GrazType.DM_Pool[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];   // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        public GrazType.DM_Pool[,] FRootNetGrowth = new GrazType.DM_Pool[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];    // [EFFR..OLDR,  1..MaxSoilLayers]
        private readonly GrazType.DM_Pool FSeedNetGrowth = new DM_Pool();

        /// <summary></summary>
        public int FMaxRootLayer;

        /// <summary></summary>
        public double[] FRootExtension = new double[GrazType.MaxSoilLayers + 1];

        /// <summary>Highest digestibility class for plant part. ptLEAF..ptSEED</summary>
        public int[] FHighestDMDClass = new int[GrazType.ptSTEM + 1];

        /// <summary>Lowest digestibility class for plant part. ptLEAF..ptSTEM</summary>
        public int[] FLowestDMDClass = new int[GrazType.ptSTEM + 1];

        /// <summary></summary>
        public double[,] FNewShootDistn = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];            // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        private readonly double[,] FNewRootDistn = new double[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];   // [EFFR..OLDR, 1..MaxSoilLayers]

        /// <summary>
        /// Death or fall                   g/g/d
        /// </summary>
        private readonly double[,] FShootLossRate = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];  // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary>Death, fall, etc                g/m^2</summary>
        public double[,] FBiomassExitGM2 = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];           // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary>
        /// Net respiration loss            g/m^2
        /// </summary>
        private readonly double[,] FBiomassRespireGM2 = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];  // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        private double FRootAgingRate;

        /// <summary></summary>
        private readonly double[] FRootLossRate = new double[GrazType.OLDR + 1];                                    // EFFR..OLDR

        /// <summary></summary>
        private double FRootRelocRate;

        /// <summary>Digestibility decline         (g/g)/d</summary>
        public double[,] FDMDDecline = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];               // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary>Change in FH</summary>
        private double FDeltaFrost;

        /// <summary>Change in SI</summary>
        private double FDelta_SI;

        /// <summary></summary>
        public TNutrientInfo[] FNutrientInfo = new TNutrientInfo[Enum.GetNames(typeof(GrazType.TPlantElement)).Length + 1]; // [TPlantElement] of TNutrientInfo

        // State variables ---------------------------------------------------------

        /// <summary>COMP(j, k)</summary>
        public int Status;

        /// <summary>B(j, k, p, d)     g/m^2</summary>
        public GrazType.DM_Pool[,] Herbage;

        /// <summary>Specific leaf and stem areas m^2/g</summary>
        public double[,] SpecificArea;

        /// <summary>B(j, k, root, m, a)  g/m^2</summary>
        public GrazType.DM_Pool[,] Roots = new GrazType.DM_Pool[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];     // [TOTAL..OLDR, TOTAL..MaxSoilLayers]

        /// <summary>SSR0 maximum stem translocation g/m^2</summary>
        public double StemReserve;

        /// <summary>RD rooting depth mm</summary>
        public double RootDepth;

        /// <summary>FH frost-hardening factor oC</summary>
        public double FrostFactor;

        /// <summary>SI seedling stress index      0 - 1</summary>
        public double SeedlStress;

        /// <summary>EI establishment index -</summary>
        public double EstabIndex;

        /// <summary>Moisture content of dead in mm water. ptLEAF..ptSTEM</summary>
        public double[] DeadMoisture = new double[GrazType.ptSTEM + 1];

        /// <summary>Average relative moisture content of dead plant part. ptLEAF..ptSTEM</summary>
        public double[] RelMoisture = new double[GrazType.ptSTEM + 1];

        // Reporting variables -----------------------------------------------------}

        /// <summary>Values of growth limiting factors</summary>
        public double[] LimitFactors = new double[Enum.GetNames(typeof(PastureUtil.TGrowthLimit)).Length];

        /// <summary>Gross assimilation rate g/m^2/d </summary>
        public double Assimilation;

        /// <summary></summary>
        public double RootTranslocSum;

        /// <summary></summary>
        public double StemTranslocSum;

        /// <summary></summary>
        public double[] fMaintRespiration = new double[GrazType.ptSEED + 1];        // TOTAL..ptSEED

        /// <summary></summary>
        public double[] fGrowthRespiration = new double[GrazType.ptSEED + 1];       // TOTAL..ptSEED

        /// <summary></summary>
        public double fR2S_Target;

        /// <summary>Allocation pattern g/g of plant part. ptLEAF..ptSEED</summary>
        public double[] Allocation = new double[GrazType.ptSEED + 1];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="status">Plant component status</param>
        public TPastureCohort(TPasturePopulation owner, int status) : base()
        {
            // initialse data structures
            this.SpecificArea = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];          // [ptLEAF..ptSTEM, TOTAL..HerbClassNo]
            this.Herbage = new GrazType.DM_Pool[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];     // [TOTAL..ptSTEM, TOTAL..HerbClassNo]

            for (int i = 0; i <= ptSTEM; i++)
            {
                for (int j = 0; j <= HerbClassNo; j++)
                {
                    this.FShootNetGrowth[i, j] = new DM_Pool();
                    this.Herbage[i, j] = new DM_Pool();
                }
            }

            for (int i = 0; i <= OLDR; i++)
            {
                for (int j = 0; j <= MaxSoilLayers; j++)
                {
                    this.Roots[i, j] = new DM_Pool();
                    this.FRootNetGrowth[i, j] = new DM_Pool();
                }
            }

            for (int i = 0; i < this.FNutrientInfo.Length; i++)
            {
                this.FNutrientInfo[i] = new TNutrientInfo();
            }

            this.Owner = owner;
            this.Status = status;
            this.SetParameters();
        }

        /// <summary>
        /// Initialise the pool structure
        /// </summary>
        /// <param name="growthPool"></param>
        private void Zero2D_DMPool(ref DM_Pool[,] growthPool)
        {
            for (int i = 0; i <= OLDR; i++)
            {
                for (int j = 0; j <= MaxSoilLayers; j++)
                {
                    PastureUtil.ZeroPool(ref growthPool[i, j]);
                }
            }
        }

        /// <summary>
        /// Ratio of (mass in a pool):(mass of all pools in the same plant part)
        /// Used to compute net growth when NPP of a plant part is negative
        /// </summary>
        /// <param name="part">Leaf, stem, root or total part</param>
        /// <param name="idx"></param>
        /// <param name="jdx"></param>
        /// <returns></returns>
        public double PartFraction(int part, int idx, int jdx = GrazType.TOTAL)
        {
            double Result;

            if ((part == ptLEAF) || (part == GrazType.ptSTEM))
            {
                Result = PastureUtil.Div0(this.Herbage[part, idx].DM, this.Herbage[part, TOTAL].DM);
            }
            else if (part == GrazType.ptROOT)
            {
                Result = PastureUtil.Div0(this.Roots[idx, jdx].DM, this.Roots[TOTAL, TOTAL].DM);
            }
            else
            {
                Result = 0.0;
            }

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="newPool"></param>
        /// <param name="specArea"></param>
        public void AddHerbage(int part, int DMD, ref DM_Pool newPool, double specArea)
        {
            if (newPool.DM > 0.0)
            {
                this.SpecificArea[part, DMD] = WeightAverage(this.SpecificArea[part, DMD],
                                                              this.Herbage[part, DMD].DM,
                                                              specArea,
                                                              newPool.DM);
            }

            this.Owner.AddPool(newPool, ref this.Herbage[part, DMD], true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="flux"></param>
        /// <param name="destCohort"></param>
        /// <param name="destDMD"></param>
        public void MoveHerbage(int part,
                                int DMD,
                                double flux,
                                ref TPastureCohort destCohort,
                                int destDMD)
        {
            if (destCohort == null)
            {
                destCohort = this;
            }

            destCohort.SpecificArea[part, destDMD] = WeightAverage(destCohort.SpecificArea[part, destDMD],
                                                                    destCohort.Herbage[part, destDMD].DM,
                                                                    this.SpecificArea[part, DMD],
                                                                    flux);
            this.Owner.MovePool(flux, ref this.Herbage[part, DMD], ref destCohort.Herbage[part, destDMD]);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="value"></param>
        public void SetHerbageDM(int part, int DMD, double value)
        {
            this.Owner.ResizePool(ref this.Herbage[part, DMD], value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GetHerbageNutr(int part, int DMD, TPlantElement elem)
        {
            if (this.Owner.FElements.Contains(elem))
            {
                return this.Herbage[part, DMD].Nu[(int)elem];
            }
            else
            {
                return this.Herbage[part, DMD].DM * this.GetHerbageConc(part, DMD, elem);
            }
        }

        /// <summary>
        /// Set the nutrient value for this herbage part.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <param name="value"></param>
        public void SetHerbageNutr(int part, int DMD, TPlantElement elem, double value)
        {
            this.Herbage[part, DMD].Nu[(int)elem] = value;
        }

        /// <summary>
        /// * Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GetHerbageConc(int part, int DMD, TPlantElement elem)
        {
            double protConc;
            int idx;
            double result;

            if (this.Owner.FElements.Contains(elem))
            {
                result = PastureUtil.Div0(this.Herbage[part, DMD].Nu[(int)elem], this.Herbage[part, DMD].DM);
            }
            else
            {
                if (DMD != TOTAL)
                {
                    protConc = this.Params.Protein[DMD];
                }
                else
                {
                    protConc = 0.0;
                    for (idx = 1; idx <= HerbClassNo; idx++)
                    {
                        protConc += this.Herbage[part, idx].DM * this.Params.Protein[idx];
                    }

                    protConc = PastureUtil.Div0(protConc, this.Herbage[part, TOTAL].DM);
                }

                switch (elem)
                {
                    case TPlantElement.N:
                        result = protConc / N2Protein;
                        break;
                    case TPlantElement.P:
                        result = protConc / N2Protein * PastureUtil.DEF_P2N;
                        break;
                    case TPlantElement.S:
                        result = protConc / N2Protein * PastureUtil.DEF_S2N;
                        break;
                    default:
                        result = 0.0;
                        break;
                }
            }

            return result;
        }

        //public void setRootDM(int iAge, int iLayer, double fValue)

        /// <summary>
        /// Get the average nutrient content of the roots in a layer
        /// </summary>
        /// <param name="age"></param>
        /// <param name="layer">Soil layer 1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <returns></returns>
        public double GetRootNutr(int age, int layer, TPlantElement elem)
        {
            double result;

            if (this.Owner.FElements.Contains(elem))
            {
                result = this.Roots[age, layer].Nu[(int)elem];
            }
            else
            {
                if (this.Roots[age, layer].DM != 0)
                {
                    result = this.Roots[age, layer].DM * this.GetRootConc(age, layer, elem);
                }
                else
                {
                    result = 0;
                }
            }

            return result;
        }

        /// <summary>
        /// Set the nutrient value of this root cohort
        /// </summary>
        /// <param name="age"></param>
        /// <param name="layer">1..n</param>
        /// <param name="elem">N,P,S</param>
        /// <param name="value">New value</param>
        public void SetRootNutr(int age, int layer, TPlantElement elem, double value)
        {
            this.Roots[age, layer].Nu[(int)elem] = value;
        }

        /// <summary>
        /// Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="age"></param>
        /// <param name="layer">1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <returns></returns>
        private double GetRootConc(int age, int layer, TPlantElement elem)
        {
            double result;
            if (this.Owner.FElements.Contains(elem))
            {
                result = PastureUtil.Div0(this.Roots[age, layer].Nu[(int)elem], this.Roots[age, layer].DM);
            }
            else
            {
                switch (elem)
                {
                    case TPlantElement.N:
                        result = this.Params.NutrConcK[1, (int)TPlantElement.N, ptROOT];
                        break;
                    case TPlantElement.P:
                        result = this.Params.NutrConcK[1, (int)TPlantElement.N, ptROOT] * PastureUtil.DEF_P2N;
                        break;
                    case TPlantElement.S:
                        result = this.Params.NutrConcK[1, (int)TPlantElement.N, ptROOT] * PastureUtil.DEF_S2N;
                        break;
                    default:
                        result = 0.0;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        public void SetParameters()
        {
            int iPart, iDMD;

            this.Params = this.Owner.Params;

            if (this.Params != null)
            {
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    this.FHighestDMDClass[iPart] = this.NewHerbageClass(iPart);
                    if ((this.Status >= stSEEDL) && (this.Status <= stSENC))
                    {
                        this.FLowestDMDClass[iPart] = this.DMDToClass(Math.Min(this.Params.MatureK[2, iPart], this.Params.MatureK[3, iPart]), true);
                    }
                    else
                    {
                        this.FLowestDMDClass[iPart] = HerbClassNo;
                    }

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        this.FNewShootDistn[iPart, iDMD] = this.NewShootFraction(iPart, iDMD);
                    }
                }

                var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>();
                foreach (var Elem in values)
                {
                    if (this.Owner.FElements.Contains(Elem))
                    {
                        for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                        {
                            for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FLowestDMDClass[iPart]; iDMD++)
                            {
                                this.FNutrientInfo[(int)Elem].fMaxShootConc[iPart, iDMD] = this.MaxNutrientConc(iPart, iDMD, Elem);
                                this.FNutrientInfo[(int)Elem].fMinShootConc[iPart, iDMD] = this.MinNutrientConc(iPart, iDMD, Elem);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Returns the default distribution across DMD classes of a herbage component
        ///  of given average DMD
        ///  * Implemented as a procedure, not a method, so that it can be used in
        /// dialogs
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="green"></param>
        /// <param name="phenoStage"></param>
        /// <param name="part"></param>
        /// <param name="DMDValue"></param>
        /// <returns></returns>
        public double[] DefaultDigClassPropns(TPastureParamSet Params, bool green, PastureUtil.TDevelopType phenoStage, int part, double DMDValue)
        {
            double maxDMDValue,
            minDMDValue,
            relDMDValue;
            double threshold;
            double[] relLimits = new double[HerbClassNo + 1]; // [0..HerbClassNo]
            double[] cumValues = new double[HerbClassNo + 1]; // [0..HerbClassNo]
            double _A, _B;
            double delta;
            double testDMDValue;
            int iDMD;
            double[] result = new double[HerbClassNo + 1]; // [0..HerbClassNo]

            maxDMDValue = this.Params.MatureK[1, part];
            if (!green)
            {
                minDMDValue = PastureUtil.HerbageDMD[12];
            }
            else if (!((phenoStage == PastureUtil.TDevelopType.Reproductive)
                       || (phenoStage == PastureUtil.TDevelopType.SprayTopped)
                       || (phenoStage == PastureUtil.TDevelopType.Senescent)
                       || (phenoStage == PastureUtil.TDevelopType.Dormant)))
            {
                minDMDValue = this.Params.MatureK[2, part];
            }
            else
            {
                minDMDValue = this.Params.MatureK[3, part];
            }

            minDMDValue = PastureUtil.HerbageDMD[this.DMDToClass(minDMDValue, true)];
            relDMDValue = (DMDValue - minDMDValue) / (maxDMDValue - minDMDValue);
            threshold = (PastureUtil.HerbageDMD[this.DMDToClass(maxDMDValue, false)] - minDMDValue) / (maxDMDValue - minDMDValue) + 1.0E-7;
            threshold = Math.Min(threshold, 1.0);

            if ((relDMDValue > 0.0) && (relDMDValue < threshold))
            {
                for (iDMD = 0; iDMD <= HerbClassNo; iDMD++)
                {
                    relLimits[iDMD] = Math.Max(0.0, Math.Min((PastureUtil.DMDLimits[iDMD] - minDMDValue) / (maxDMDValue - minDMDValue), 1.0));
                }

                _A = 60.0 * relDMDValue * (StdMath.Sqr(relDMDValue) - relDMDValue + 1.0 / 3.0);
                _B = (1.0 - relDMDValue) / relDMDValue * _A;
                delta = 0.1;
                bool test; // = false;
                do
                {
                    for (iDMD = 0; iDMD <= HerbClassNo; iDMD++)
                    {
                        cumValues[iDMD] = StdMath.CumBeta(relLimits[iDMD], _A, _B);
                    }

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        result[iDMD] = cumValues[iDMD - 1] - cumValues[iDMD];
                    }

                    testDMDValue = 0.0;
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        testDMDValue += PastureUtil.HerbageDMD[iDMD] * result[iDMD];
                    }

                    if (((testDMDValue < DMDValue) && (delta > 0.0))
                     || ((testDMDValue > DMDValue) && (delta < 0.0)))
                    {
                        delta = -0.5 * delta;
                    }

                    _B = _B * (1.0 + delta);
                    test = (Math.Abs(testDMDValue - DMDValue) < 1.0E-7) || (Math.Abs(delta) < 1.0E-6);
                } while (test == false);            // until(Math.Abs(fTestDMD - fDMD) < 1.0E-7) || (Math.Abs(fDelta) < 1.0E-6);
            }
            else if (relDMDValue >= threshold)
            {
                PastureUtil.FillArray(result, 0.0);
                iDMD = this.DMDToClass(maxDMDValue, false);
                result[iDMD] = Math.Max(0.0, Math.Min(1.0 - (PastureUtil.HerbageDMD[iDMD] - DMDValue) / PastureUtil.CLASSWIDTH, 1.0));
                result[iDMD + 1] = 1.0 - result[iDMD];
            }
            else
            {
                PastureUtil.FillArray(result, 0.0);
                iDMD = this.DMDToClass(minDMDValue, true);
                result[iDMD] = Math.Max(0.0, Math.Min(1.0 - (DMDValue - PastureUtil.HerbageDMD[iDMD]) / PastureUtil.CLASSWIDTH, 1.0));
                result[iDMD - 1] = 1.0 - result[iDMD];
            }

            return result;
        }

        /// <summary>
        /// Initialise the cohort from the init values
        /// </summary>
        /// <param name="green"></param>
        /// <param name="dry"></param>
        /// <param name="isGreen"></param>
        public void ReadFromValue(GreenInit green, DryInit dry, bool isGreen)
        {
            int[] partMap = { 0, ptLEAF, ptSTEM };                              // [1..2]
            int[] RootMap = { 0, GrazType.EFFR, GrazType.OLDR };                // [1..2]

            int DMDCount;
            double upperDMD;
            double lowerDMD;
            double meanDMD;
            double mass;
            double[] nutrConc = new double[4];                 // [TPlantElement]
            double specArea;
            double[] propns = new double[HerbClassNo + 1];
            DM_Pool newPool;
            double effPropn;
            int part, DMD;
            int layer, age;
            uint idx;
            int jdx, kdx;
            Herbage[] subValue;

            for (int i = 0; i <= ptSTEM; i++)
            {
                for (int j = 0; j <= HerbClassNo; j++)
                {
                    ZeroDMPool(ref this.Herbage[i, j]);
                }
            }

            if (isGreen)
                subValue = green.herbage;
            else
                subValue = dry.herbage;

            if (subValue != null)
            {
                var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();

                for (idx = 0; idx <= Math.Min(1, subValue.Length-1); idx++)
                {
                    part = partMap[idx + 1];
                    DMDCount = (int)subValue[idx].dmd.Length;

                    if (DMDCount > 1)
                    {
                        // DMD distribution given...
                        for (jdx = 0; jdx < DMDCount - 1; jdx++)
                        {
                            upperDMD = subValue[idx].dmd[jdx];                // DMD distribution in descending order
                            lowerDMD = subValue[idx].dmd[jdx + 1];
                            mass = PastureUtil.ReadMass(subValue[idx].weight[jdx], "kg/ha");

                            foreach (var Elem in values)
                            {
                                nutrConc[(int)Elem] = 0.0;
                                if (Elem == TPlantElement.N)
                                {
                                    if ((subValue[idx].n_conc != null) && (subValue[idx].n_conc.Length >= jdx))
                                        nutrConc[(int)Elem] = subValue[idx].n_conc[jdx];
                                }
                                if (Elem == TPlantElement.S)
                                {
                                    if ((subValue[idx].s_conc != null) && (subValue[idx].s_conc.Length >= jdx))
                                        nutrConc[(int)Elem] = subValue[idx].s_conc[jdx];
                                }
                                if (Elem == TPlantElement.P)
                                {
                                    if ((subValue[idx].p_conc != null) && (subValue[idx].p_conc.Length >= jdx))
                                        nutrConc[(int)Elem] = subValue[idx].p_conc[jdx];
                                }
                            }

                            specArea = 0.0;                                                        // The zero value will be replaced with a default (that depends on environment) during simulation
                            if (subValue[idx].spec_area != null)
                            {
                                kdx = Math.Min(jdx, subValue[idx].spec_area.Length);
                                if (kdx > 0)
                                {
                                    specArea = subValue[idx].spec_area[kdx] / PastureUtil.M2_CM2;
                                }
                            }

                            if ((mass > 0.0) && (upperDMD > lowerDMD))
                            {
                                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                                {
                                    propns[DMD] = (Math.Min(upperDMD, PastureUtil.DMDLimits[DMD - 1]) - Math.Max(lowerDMD, PastureUtil.DMDLimits[DMD]))
                                                     / (upperDMD - lowerDMD);
                                    if (propns[DMD] > 1.0E-5)
                                    {
                                        newPool = this.Owner.MakeNewPool(part, propns[DMD] * mass);

                                        foreach (var Elem in values)
                                        {
                                            if (!this.Owner.FElements.Contains(Elem))
                                            {
                                                newPool.Nu[(int)Elem] = 0.0;
                                            }
                                            else if (nutrConc[(int)Elem] > 0.0)
                                            {
                                                newPool.Nu[(int)Elem] = newPool.DM * nutrConc[(int)Elem];
                                            }
                                            else
                                            {
                                                newPool.Nu[(int)Elem] = newPool.DM * this.DefaultNutrConc(part, DMD, Elem);
                                            }
                                        }

                                        this.AddHerbage(part, DMD, ref newPool, specArea);
                                    }
                                }
                            }
                        } // loop over entries for this part
                    } // iDMDCount > 1
                    else if (DMDCount == 1)
                    {
                        // Average DMD & nutrients given
                        meanDMD = subValue[idx].dmd[0];
                        mass = PastureUtil.ReadMass(subValue[idx].weight[0], "kg/ha");
                        foreach (var Elem in values)
                        {
                            nutrConc[(int)Elem] = 0.0;
                            if (Elem == TPlantElement.N)
                            {
                                if ((subValue[idx].n_conc != null) && (subValue[idx].n_conc.Length >= 0))
                                    nutrConc[(int)Elem] = subValue[idx].n_conc[0];
                            }
                            if (Elem == TPlantElement.S)
                            {
                                if ((subValue[idx].s_conc != null) && (subValue[idx].s_conc.Length >= 0))
                                    nutrConc[(int)Elem] = subValue[idx].s_conc[0];
                            }
                            if (Elem == TPlantElement.P)
                            {
                                if ((subValue[idx].p_conc != null) && (subValue[idx].p_conc.Length >= 0))
                                    nutrConc[(int)Elem] = subValue[idx].p_conc[0];
                            }
                        }

                        specArea = 0.0;
                        if ((subValue[idx].spec_area != null) && (subValue[idx].spec_area.Length > 0))
                        {
                            specArea = subValue[idx].spec_area[0] / PastureUtil.M2_CM2;
                        }

                        if (mass > 0.0)
                        {
                            propns = this.DefaultDigClassPropns(this.Params, isGreen, this.Owner.Phenology, part, meanDMD);
                            for (DMD = 1; DMD <= HerbClassNo; DMD++)
                            {
                                newPool = this.Owner.MakeNewPool(part, propns[DMD] * mass);

                                foreach (var Elem in values)
                                {
                                    if (!this.Owner.FElements.Contains(Elem))
                                    {
                                        newPool.Nu[(int)Elem] = 0.0;
                                    }
                                    else if (nutrConc[(int)Elem] > 0.0)
                                    {
                                        newPool.Nu[(int)Elem] = newPool.DM * this.DefaultDMD_Nutr(part, DMD, Elem, nutrConc[(int)Elem], propns);
                                    }
                                    else
                                    {
                                        newPool.Nu[(int)Elem] = newPool.DM * this.DefaultNutrConc(part, DMD, Elem);
                                    }
                                }

                                this.AddHerbage(part, DMD, ref newPool, specArea);
                            }
                        } // fMass > 0.0
                    } // iDMDCount = 1
                } // loop over plant parts

                this.ComputeTotals();                                                               // Total shoot values are needed for root defaults, so compute here

                if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
                {
                    this.RootDepth = green.rt_dep;
                    if (green.rt_dep == 0)
                        this.RootDepth = this.Owner.MaxRootingDepth; // Roots -------------------------------

                    double[][] root_wts;
                    root_wts = green.root_wt;                                            // "root_wt[i][j]" is age i and layer j
                    if ((root_wts != null) && (root_wts.Length >= 2))                    // Effective & old roots given separately
                    {
                        for (idx = 0; idx <= 1; idx++)
                        {
                            age = RootMap[idx+1];

                            if (root_wts[idx].Length > 1)
                            {
                                for (layer = 0; layer < root_wts[idx].Length; layer++)
                                {
                                    this.Roots[age, layer] = this.Owner.MakeNewPool(ptROOT, PastureUtil.ReadMass(root_wts[idx][layer], "kg/ha"));
                                }
                            }
                            else if (root_wts[idx].Length == 1)
                            {
                                this.SetDefaultRoots(age, PastureUtil.ReadMass(root_wts[idx][0], "kg/ha"), this.RootDepth);
                            }
                        }
                    }
                    else if ((root_wts != null) && (root_wts.Length == 1))
                    {
                        // Total over root age classes
                        effPropn = this.DefaultPropnEffRoots();

                        if (root_wts[0].Length > 1)                                                // Layers, default age distribution
                        {
                            for (layer = 0; layer < root_wts[1].Length; layer++)
                            {
                                mass = PastureUtil.ReadMass(root_wts[layer][0], "kg/ha");
                                this.Roots[EFFR, layer] = this.Owner.MakeNewPool(ptROOT, mass * effPropn);
                                this.Roots[OLDR, layer] = this.Owner.MakeNewPool(ptROOT, mass * (1.0 - effPropn));
                            }
                        }
                        else if (root_wts[0].Length == 1)
                        {
                            // Grand total root mass only
                            this.SetDefaultRoots(TOTAL, PastureUtil.ReadMass(root_wts[0][0], "kg/ha"), this.RootDepth);
                        }
                    }
                    else
                    {
                        // No value given - compute a default root mass & distribution
                        this.ComputeAllocation();
                        this.SetDefaultRoots(TOTAL, this.fR2S_Target * this.Herbage[TOTAL, TOTAL].DM, this.RootDepth);
                    }

                    this.ComputeTotals();                                                                       // Compute marginal totals for roots

                    this.FrostFactor = this.Params.DeathK[6] * green.frosts;    // Other state variables ---------------

                    if (!((this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive) && (this.Owner.DegDays >= this.Params.DevelopK[6])))
                    {
                        this.StemReserve = 0.0;
                    }
                    else if (green.stem_reloc != -999.0)
                    {
                        // if stem_reloc has been set
                        this.StemReserve = PastureUtil.ReadMass(green.stem_reloc, "kg/ha");
                    }
                    else
                    {
                        this.StemReserve = this.Params.TranslocK[3] * this.Herbage[ptSTEM, TOTAL].DM;
                    }

                    if (this.Status == stSEEDL)
                    {
                        this.SeedlStress = green.stress_index;  // def = 0
                        this.EstabIndex = green.estab_index;    // def = 1.0
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double MaxNutrientConc(int part, int DMD, TPlantElement elem)
        {
            double result;
            if ((part == ptLEAF) || (part == ptSTEM))
            {
                result = this.Params.NutrConcK[1, (int)elem, part] * this.CO2_NutrConc(part, elem) * this.DMDToRelConc(this.Params, part, DMD, elem);
            }
            else
            {
                result = this.Params.NutrConcK[1, (int)elem, part] * this.CO2_NutrConc(part, elem);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double MinNutrientConc(int part, int DMD, TPlantElement elem)
        {
            double result;

            if ((part == ptLEAF) || (part == ptSTEM))
            {
                result = this.Params.NutrConcK[2, (int)elem, part] * this.CO2_NutrConc(part, elem) * this.DMDToRelConc(this.Params, part, DMD, elem);
            }
            else
            {
                result = this.Params.NutrConcK[2, (int)elem, part] * this.CO2_NutrConc(part, elem);
            }

            return result;
        }

        /// <summary>
        ///  Quadratic function of relative nutrient content vs relative DMD
        /// NutrConc[2,] is minimum nutrient content at maximum DMD
        /// NutrConc[3,] is minimum nutrient content at midpoint DMD
        /// NutrConc[4,] is minimum nutrient content at minimum DMD
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double DMDToRelConc(TPastureParamSet Params, int part, int DMD, TPlantElement elem)
        {
            double result;
            double relDMD;
            double mid_RelConc;
            double min_RelConc;
            double _A, _B, _C;

            relDMD = Math.Max(0.0, Math.Min((PastureUtil.HerbageDMD[DMD] - this.Params.MatureK[3, part]) / (Params.MatureK[1, part] - this.Params.MatureK[3, part]), 1.0));
            mid_RelConc = this.Params.NutrConcK[3, (int)elem, part] / this.Params.NutrConcK[2, (int)elem, part];
            min_RelConc = this.Params.NutrConcK[4, (int)elem, part] / this.Params.NutrConcK[2, (int)elem, part];

            _A = 2.0 * min_RelConc - 4.0 * mid_RelConc + 2.0;
            _B = -3.0 * min_RelConc + 4.0 * mid_RelConc - 1.0;
            _C = 1.0 * min_RelConc;
            result = _A * Math.Pow(relDMD, 2) + _B * relDMD + _C;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double DefaultNutrConc(int part, int DMD, TPlantElement elem)
        {
            if (!this.Owner.FElements.Contains(elem))
            {
                return 0.0;
            }
            else
            {
                return this.MaxNutrientConc(part, DMD, elem);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <param name="meanConc"></param>
        /// <param name="massWeights"></param>
        /// <returns></returns>
        public double DefaultDMD_Nutr(int part, int DMD, TPlantElement elem, double meanConc, double[] massWeights)
        {
            double result;
            double scale;
            double denom;
            double maxConc;
            double minConc;
            int idx;

            if (!this.Owner.FElements.Contains(elem))
            {
                result = 0.0;
            }
            else
            {
                scale = 0.0;
                denom = 0.0;
                for (idx = 1; idx <= HerbClassNo; idx++)
                {
                    maxConc = this.MaxNutrientConc(part, idx, elem);
                    minConc = this.MinNutrientConc(part, idx, elem);
                    scale += massWeights[idx] * (meanConc - minConc);
                    denom += massWeights[idx] * (maxConc - minConc);
                }

                if (denom > 0.0)
                {
                    scale = Math.Max(0.0, Math.Min(scale / denom, 1.0));
                }
                else
                {
                    scale = 0.0;
                }

                maxConc = this.MaxNutrientConc(part, DMD, elem);
                minConc = this.MinNutrientConc(part, DMD, elem);
                result = minConc + scale * (maxConc - minConc);
            }

            return result;
        }

        /// <summary>
        /// Default value for the proportion of roots that are effective. Used in
        /// initialisation.
        /// * This equation is arrived at by assuming that the root system is in
        /// equilibrium, with(a) NPP going to roots, (b) transfer between
        /// effective and old roots and(c) death of roots all equal.
        /// </summary>
        /// <returns></returns>
        public double DefaultPropnEffRoots()
        {
            if (this.Params != null)
            {
                return this.Params.RootLossK[2] / (this.Params.RootLossK[1] + this.Params.RootLossK[2]);
            }
            else
            {
                return 1.0;
            }
        }

        /// <summary>
        /// Set the root mass profile within a cohort and root age class to a default
        /// value. Used in initialisation.
        /// </summary>
        /// <param name="age">TOTAL, EFFR or OLDR</param>
        /// <param name="rootDM"></param>
        /// <param name="depth"></param>
        public void SetDefaultRoots(int age, double rootDM, double depth)
        {
            double[] wet_ASW = new double[MaxSoilLayers + 1];
            double[] rootDist = new double[MaxSoilLayers + 1]; // 0..
            double effFract;
            DM_Pool rootPool;
            int layer;

            for (layer = 1; layer <= MaxSoilLayers; layer++)
            {
                wet_ASW[layer] = 1.0;
            }

            this.ComputeRootDistn(depth, wet_ASW, ref rootDist);

            rootPool = this.Owner.MakeNewPool(ptROOT, rootDM);

            if (age != TOTAL)
            {
                for (layer = 1; layer <= MaxSoilLayers; layer++)
                {
                    this.Roots[age, layer] = PoolFraction(rootPool, rootDist[layer]);
                }
            }
            else
            {
                effFract = this.DefaultPropnEffRoots();
                for (layer = 1; layer <= MaxSoilLayers; layer++)
                {
                    this.Roots[EFFR, layer] = PoolFraction(rootPool, rootDist[layer] * effFract);
                    this.Roots[OLDR, layer] = PoolFraction(rootPool, rootDist[layer] * (1.0 - effFract));
                }
            }

            this.ComputeTotals();
        }

        /// <summary>
        /// Area index of a herbage cohort.
        /// *fCO2_SpecLeafArea is a method - see past_CO2.inc
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public double AreaIndex(int part = TOTAL)
        {
            double result;

            if (part == TOTAL)
            {
                result = this.AreaIndex(ptLEAF) + this.AreaIndex(ptSTEM);
            }
            else
            {
                if ((this.SpecificArea[part, TOTAL] <= 0.0)
                        && (this.Herbage[part, TOTAL].DM > 0.0)
                        && (this.Owner.Inputs.Radiation > 0.0))
                {
                    this.InitialiseSpecificAreas(part, this.Owner.Inputs.MeanTemp, this.Owner.Inputs.Radiation);
                }

                result = this.SpecificArea[part, TOTAL] * this.Herbage[part, TOTAL].DM;
            }

            return result;
        }

        /// <summary>
        /// Projected area of shoots -takes extinction coefficient into account
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public double ProjArea(int part = TOTAL)
        {
            return this.Owner.FExtinctionK[this.Status] * this.AreaIndex(part);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="meanTemp"></param>
        /// <param name="radn"></param>
        /// <returns></returns>
        public double ComputeNewSpecificArea(int part, double meanTemp, double radn)
        {
            double result;
            if (part != TOTAL)
            {
                this.FNewSpecificArea[part] = this.Params.LightK[part]                              // Reference specific area
                                               * this.CO2_SpecLeafArea()
                                               * (PastureUtil.REF_RADN + this.Params.LightK[3]) / (radn + this.Params.LightK[3])
                                               * (this.Params.LightK[5] + (1.0 - this.Params.LightK[5]) * PastureUtil.RAMP(meanTemp, 0.0, this.Params.LightK[4]));
                result = this.FNewSpecificArea[part];
            }
            else
            {
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    this.ComputeNewSpecificArea(part, meanTemp, radn);
                }

                result = 0.0;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="meanTemp"></param>
        /// <param name="radn"></param>
        public void InitialiseSpecificAreas(int part, double meanTemp, double radn)
        {
            double specArea = this.ComputeNewSpecificArea(part, meanTemp, radn);

            for (int iDMD = this.FHighestDMDClass[part]; iDMD <= this.FLowestDMDClass[part]; iDMD++)
            {
                this.SpecificArea[part, iDMD] = specArea;
            }

            this.SpecificArea[part, TOTAL] = specArea;
        }

        /// <summary>
        /// Set potential assimilation
        /// </summary>
        /// <param name="value">New value</param>
        /// <param name="limits">Growth limiting factor values</param>
        public void SetPotAssimilation(double value, double[] limits)
        {
            this.FPotAssimilation = value;
            limits.CopyTo(this.LimitFactors, 0);
        }

        /// <summary>
        ///
        /// </summary>
        public void SetStemReserve()
        {
            if (this.Status == stSEEDL || this.Status == stESTAB)
            {
                this.StemReserve = this.Params.TranslocK[3] * this.Herbage[ptSTEM, TOTAL].DM;
            }
            else
            {
                this.StemReserve = 0.0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="DMRemoval"></param>
        public void RemoveStemReserve(double[] DMRemoval)
        {
            double removedDDM;
            double totalDDM;

            removedDDM = 0.0;
            totalDDM = 0.0;
            for (int iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                removedDDM += PastureUtil.HerbageDMD[iDMD] * DMRemoval[iDMD];
                totalDDM += PastureUtil.HerbageDMD[iDMD] * this.Herbage[ptSTEM, iDMD].DM;
            }

            if (totalDDM > 0.0)
            {
                this.StemReserve *= Math.Max(0.0, 1.0 - removedDDM / totalDDM);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="DMRemoval"></param>
        public void RemoveStemReserve(double DMRemoval)
        {
            double[] fClassRemoval = new double[HerbClassNo + 1];

            for (int iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                fClassRemoval[iDMD] = DMRemoval * PastureUtil.Div0(this.Herbage[ptSTEM, iDMD].DM, this.Herbage[ptSTEM, TOTAL].DM);
            }

            this.RemoveStemReserve(fClassRemoval);
        }

        /// <summary>
        /// Compute potential translocation
        /// </summary>
        /// <param name="meanTemp">Mean temperature</param>
        public void ComputePotTranslocation(double meanTemp)
        {
            double limitFactor;
            double translocRate;
            double endDegDays;
            double DDToday;
            double[] digestibleDM = new double[HerbClassNo + 1];
            double DDM_Sum;
            int age, layer;
            int DMD;

            limitFactor = Math.Min(this.LimitFactors[(int)PastureUtil.TGrowthLimit.glLowT], Math.Min(this.LimitFactors[(int)PastureUtil.TGrowthLimit.glSM], this.LimitFactors[(int)PastureUtil.TGrowthLimit.glWLog]));

            if (!this.Params.bAnnual
               && (this.Status == stESTAB)
               && (this.Owner.Phenology == PastureUtil.TDevelopType.Vernalizing || this.Owner.Phenology == PastureUtil.TDevelopType.Vegetative || this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive)
               && ((this.Root2Shoot() > this.fR2S_Target) || (this.Herbage[TOTAL, TOTAL].DM == 0.0))
               && (limitFactor > this.Params.TranslocK[1]))
            {
                // Relocation from belowground reserves
                translocRate = this.Params.TranslocK[2];
                if (this.Herbage[TOTAL, TOTAL].DM > 0.0)
                {
                    translocRate = Math.Min(translocRate, (1.0 - this.fR2S_Target / this.Root2Shoot()) / (1.0 + this.fR2S_Target));
                }
            }
            else
            {
                translocRate = 0.0;
            }

            this.FPotRootTransloc = new double[GrazType.OLDR + 1, GrazType.MaxSoilLayers + 1];
            this.FPotRootTranslocSum = 0.0;
            if (translocRate > 0.0)
            {
                for (age = EFFR; age <= OLDR; age++)
                {
                    for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                    {
                        this.FPotRootTransloc[age, layer] = translocRate * this.Roots[age, layer].DM;
                        this.FPotRootTranslocSum += this.FPotRootTransloc[age, layer];
                    }
                }
            }

            if (this.Params.bHasSeeds
               && ((this.Status == stESTAB) || (this.Status == stSENC))
               && (this.Params.TranslocK[4] > 0.0)
               && (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive || this.Owner.Phenology == PastureUtil.TDevelopType.SprayTopped || this.Owner.Phenology == PastureUtil.TDevelopType.Senescent)
               && (this.Owner.DegDays >= this.Params.DevelopK[6]))
            {
                // Translocation from stem to seed
                endDegDays = this.Params.DevelopK[6] + this.Params.TranslocK[4];
                DDToday = Math.Max(0.0, meanTemp - this.Params.DevelopK[3]);                 // Degree days (base Kv3)
                if (this.Owner.DegDays + DDToday > endDegDays)
                {
                    DDToday = Math.Max(0.0, endDegDays - this.Owner.DegDays);
                }

                translocRate = this.Owner.SeedSetPropn() * (DDToday / this.Params.TranslocK[4]) * this.StemReserve;

                if (this.Owner.DegDays >= endDegDays)                                      // End of translocation period
                {
                    this.StemReserve = 0.0;
                }
            }
            else
            {
                translocRate = 0.0;
            }

            PastureUtil.FillArray(this.FPotStemTransloc, 0.0);
            if (translocRate > 0.0)
            {
                DDM_Sum = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    digestibleDM[DMD] = this.Herbage[ptSTEM, DMD].DM * PastureUtil.HerbageDMD[DMD];
                    DDM_Sum += digestibleDM[DMD];
                }

                this.FPotStemTranslocSum = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    this.FPotStemTransloc[DMD] = Math.Min(digestibleDM[DMD],
                                                   PastureUtil.Div0(digestibleDM[DMD], DDM_Sum) * translocRate);
                    this.FPotStemTranslocSum += this.FPotStemTransloc[DMD];
                }
            }
        }

        /// <summary>
        /// Computes the specific and absolute rates of maintenance respiration and the
        /// growth respiration rate(g/g).
        /// </summary>
        /// <param name="meanTemp"></param>
        /// <param name="dormant"></param>
        public void ComputeRespiration(double meanTemp, bool dormant)
        {
            double TFactor;
            int part, DMD,
            age, layer;

            TFactor = PastureUtil.Q10Func(meanTemp, 1.0, this.Params.RespireK[2]);                    // Temperature effect

            // Maintenance respiration rates (g/g/d)
            this.FMaintRespRate[ptLEAF] = this.Params.RespireK[1] * TFactor * this.GetHerbageConc(ptLEAF, TOTAL, TPlantElement.N);
            this.FMaintRespRate[ptSTEM] = this.Params.RespireK[1] * TFactor * this.GetHerbageConc(ptSTEM, TOTAL, TPlantElement.N);
            this.FMaintRespRate[ptROOT] = this.Params.RespireK[1] * TFactor * Math.Min(this.GetRootConc(TOTAL, TOTAL, TPlantElement.N),
                                                                           this.Params.NutrConcK[1, (int)TPlantElement.N, ptROOT]);
            this.FMaintRespRate[ptSEED] = 0.0;

            if (dormant)
            {
                // Slowed respiration in dormancy
                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    this.FMaintRespRate[part] = this.Params.RespireK[3] * this.FMaintRespRate[part];
                }
            }

            PastureUtil.FillArray(this.fMaintRespiration, 0);

            // Compute maintenance respiration amounts and totals
            for (part = ptLEAF; part <= ptSTEM; part++)
            {
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    this.FShootMaintResp[part, DMD] = this.FMaintRespRate[part] * this.Herbage[part, DMD].DM;
                    this.fMaintRespiration[part] = this.fMaintRespiration[part] + this.FShootMaintResp[part, DMD];
                }
            }

            for (age = EFFR; age <= OLDR; age++)
            {
                for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    this.FRootMaintResp[age, layer] = this.FMaintRespRate[ptROOT] * this.Roots[age, layer].DM;
                    this.fMaintRespiration[ptROOT] = this.fMaintRespiration[ptROOT] + this.FRootMaintResp[age, layer];
                }
            }

            for (part = ptLEAF; part <= ptROOT; part++)
            {
                this.fMaintRespiration[TOTAL] = this.fMaintRespiration[TOTAL] + this.fMaintRespiration[part];
            }

            this.FGrowthRespRate = this.Owner.GrowthRespirationRate();                                       // Growth respiration rate (g/g)
        }

        /// <summary>
        /// Compute the potential net growth
        /// </summary>
        public void ComputePotNetGrowth()
        {
            for (int part = ptLEAF; part <= ptSEED; part++)
            {
                this.FPotPartNetGrowth[part] = (this.FPotAssimilation + this.FPotRootTranslocSum - this.fMaintRespiration[TOTAL])
                                            * this.Allocation[part];
                if (part == ptSEED)
                {
                    this.FPotPartNetGrowth[part] = this.FPotPartNetGrowth[part] + this.FPotStemTranslocSum;
                }

                if (this.FPotPartNetGrowth[part] > 0.0)
                {
                    this.FPotPartNetGrowth[part] = this.FPotPartNetGrowth[part] * (1.0 - this.FGrowthRespRate);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="required"></param>
        /// <param name="remaining"></param>
        /// <returns></returns>
        private double AssignGrowth(double required, ref double remaining)
        {
            double result = Math.Max(0.0, required);
            if (result < remaining)
            {
                remaining -= result;
            }
            else
            {
                result = remaining;
                remaining = 0.0;
            }

            return result;
        }

        /// <summary>
        /// Final computation of the gross assimilation rate, taking into account
        /// nutrient limitations and meristem(sink)-limits on assimilation rate
        /// Final computation of the translocation rates
        /// Computation of the absolute values of respiration and of the "net growth
        /// including translocation".
        ///
        /// Assumes that the following values have been computed:
        /// </summary>
        /// <param name="dormant"></param>
        public void ComputeNetGrowth(bool dormant)
        {
            PastureUtil.TGrowthLimit[] Elem2Limit = { 0, PastureUtil.TGrowthLimit.gl_N, PastureUtil.TGrowthLimit.gl_P, PastureUtil.TGrowthLimit.gl_S };  // [1..3]

            double sink_ShootNPP;
            double sink_ShootAlloc;
            double sink_Respired;
            double sink_Assim;
            double limitNu;
            double remainingAssim;
            double remainingTrans;
            double required;
            double delta;
            double transAlloc;
            int part, DMD;
            int age, layer;

            if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
            {
                if (dormant)
                {
                    // Meristem-limited growth in dormant plants
                    sink_ShootNPP = 0.0;
                    sink_ShootAlloc = 0.0;
                    sink_Respired = 0.0;
                    for (part = ptLEAF; part <= ptSTEM; part++)
                    {
                        sink_ShootNPP += this.Params.MeristemK[1] * this.Herbage[part, TOTAL].DM;
                        sink_ShootAlloc += this.Allocation[part];
                        sink_Respired += this.fMaintRespiration[part];
                    }

                    if (sink_ShootAlloc > 0.0)
                    {
                        sink_Assim = (sink_ShootNPP / (1.0 - this.FGrowthRespRate) + sink_Respired) / sink_ShootAlloc;
                    }
                    else
                    {
                        sink_Assim = 0.0;
                    }
                }
                else
                {
                    sink_Assim = VERYLARGE;
                }

                limitNu = 1.0;
                var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
                foreach (var Elem in values)
                {
                    this.LimitFactors[(int)Elem2Limit[(int)Elem]] = this.NutrLimit(Elem);
                    limitNu = Math.Min(limitNu, this.LimitFactors[(int)Elem2Limit[(int)Elem]]);
                }

                this.Assimilation = Math.Min(limitNu * this.FPotAssimilation, sink_Assim);

                this.RootTranslocSum = 0.0;                                                   // Compute the final translocation rates for the cohort
                for (age = EFFR; age <= OLDR; age++)
                {
                    for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                    {
                        this.FRootTransloc[age, layer] = limitNu * this.FPotRootTransloc[age, layer];
                        this.RootTranslocSum += this.FRootTransloc[age, layer];
                    }
                }

                limitNu = 1.0;
                foreach (var Elem in values)
                {
                    limitNu = Math.Min(limitNu, this.StemNutrLimit(Elem));
                }

                this.StemTranslocSum = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    this.FStemTransloc[DMD] = limitNu * this.FPotStemTransloc[DMD];
                    this.StemTranslocSum += this.FStemTransloc[DMD];
                }

                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    this.FPartNetGrowth[part] = -this.fMaintRespiration[part];
                }

                this.FPartNetGrowth[ptSEED] = this.FPartNetGrowth[ptSEED] + this.StemTranslocSum;

                remainingAssim = this.Assimilation;
                remainingTrans = this.RootTranslocSum;

                required = Math.Max(0.0, -this.FPartNetGrowth[ptSEED]);             // Maintenance of seed gets highest priority
                delta = this.AssignGrowth(required, ref remainingAssim);
                this.FPartNetGrowth[ptSEED] = this.FPartNetGrowth[ptSEED] + delta;
                required = Math.Max(0.0, -this.FPartNetGrowth[ptSEED]);
                delta = this.AssignGrowth(required, ref remainingTrans);
                this.FPartNetGrowth[ptSEED] = this.FPartNetGrowth[ptSEED] + delta;

                // Then other maintenance respiration
                required = Math.Max(0.0, -(this.FPartNetGrowth[ptLEAF] + this.FPartNetGrowth[ptSTEM] + this.FPartNetGrowth[ptROOT]));
                if (required > 0.0)
                {
                    delta = this.AssignGrowth(required, ref remainingAssim);
                    for (part = ptLEAF; part <= ptROOT; part++)
                    {
                        this.FPartNetGrowth[part] = this.FPartNetGrowth[part] + (delta / required) * (-this.FPartNetGrowth[part]);
                    }
                }

                required = Math.Max(0.0, -(this.FPartNetGrowth[ptLEAF] + this.FPartNetGrowth[ptSTEM]));
                if (required > 0.0)
                {
                    delta = this.AssignGrowth(required, ref remainingTrans);
                    for (part = ptLEAF; part <= ptSTEM; part++)
                    {
                        this.FPartNetGrowth[part] = this.FPartNetGrowth[part] + (delta / required) * (-this.FPartNetGrowth[part]);
                    }
                }

                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    if (remainingAssim > 0.0)
                    {
                        this.FPartNetGrowth[part] = this.FPartNetGrowth[part] + this.Allocation[part] * remainingAssim;
                    }

                    if ((part != ptROOT) && (remainingTrans > 0.0))
                    {
                        transAlloc = PastureUtil.Div0(this.Allocation[part], 1.0 - this.Allocation[ptROOT]);
                        this.FPartNetGrowth[part] = this.FPartNetGrowth[part] + transAlloc * remainingTrans;
                    }

                    this.fGrowthRespiration[part] = this.FGrowthRespRate * Math.Max(0.0, this.FPartNetGrowth[part]);
                    this.FPartNetGrowth[part] = this.FPartNetGrowth[part] - this.fGrowthRespiration[part];
                }

                this.fGrowthRespiration[TOTAL] = 0.0;
                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    this.fGrowthRespiration[TOTAL] = this.fGrowthRespiration[TOTAL] + this.fGrowthRespiration[part];
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        if (this.FPartNetGrowth[part] > 0.0)
                        {
                            this.FShootNetGrowth[part, DMD].DM = this.FPartNetGrowth[part] * this.NewShootFraction(part, DMD);
                        }
                        else
                        {
                            this.FShootNetGrowth[part, DMD].DM = this.FPartNetGrowth[part] * this.PartFraction(part, DMD);
                        }
                    }
                }

                for (age = EFFR; age <= OLDR; age++)
                {
                    for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                    {
                        if (this.FPartNetGrowth[ptROOT] > 0.0)
                        {
                            this.FRootNetGrowth[age, layer].DM = this.FPartNetGrowth[ptROOT] * this.FNewRootDistn[age, layer];
                        }
                        else
                        {
                            this.FRootNetGrowth[age, layer].DM = this.FPartNetGrowth[ptROOT] * this.PartFraction(ptROOT, age, layer);
                        }
                    }
                }

                this.FSeedNetGrowth.DM = this.FPartNetGrowth[ptSEED];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double Root2Shoot()
        {
            if (this.Herbage[TOTAL, TOTAL].DM > 0.0)
            {
                return this.Roots[TOTAL, TOTAL].DM / this.Herbage[TOTAL, TOTAL].DM;
            }
            else
            {
                return this.Params.AllocK[1];
            }
        }

        /// <summary>
        ///  Computes the allocation of assimilate to each plant part, and the
        /// allocation of net root growth to each soil layer.
        /// * Assumes that FRootExtension has been computed
        /// </summary>
        public void ComputeAllocation()
        {
            double seedAlloc;
            double R2S_Ratio;
            double shootAlloc;
            double leafScale;
            double leafToShoot;
            double[] sinkStrength = new double[MaxSoilLayers + 1];
            double sinkStrengthSum;
            double[] nextLayerFract = new double[MaxSoilLayers + 1];
            double fractionOccupied;
            double extendRatio;
            int layer;

            if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
            {
                if (this.Params.bHasSeeds && (this.Status == stESTAB) && (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive || this.Owner.Phenology == PastureUtil.TDevelopType.Senescent))
                {
                    seedAlloc = this.Params.AllocK[3] * this.Owner.SeedSetPropn();
                }
                else
                {
                    seedAlloc = 0.0;
                }

                R2S_Ratio = this.Root2Shoot();

                if ((this.Owner.Phenology == PastureUtil.TDevelopType.Senescent)
                   || ((this.Status == stSENC) && (this.Roots[EFFR, TOTAL].DM == 0.0)))
                {
                    this.fR2S_Target = 0.0;
                }
                else if (this.Owner.Phenology != PastureUtil.TDevelopType.Reproductive)
                {
                    this.fR2S_Target = this.Params.AllocK[1];
                }
                else
                {
                    this.fR2S_Target = this.Params.AllocK[1] + (this.Params.AllocK[2] - this.Params.AllocK[1])
                                                   * PastureUtil.SIG(this.Owner.DegDays, 0.0, this.Params.DevelopK[6]);
                }

                if (this.fR2S_Target > 0.0)
                {
                    shootAlloc = R2S_Ratio / (R2S_Ratio + Math.Pow(this.fR2S_Target, 2));
                }
                else
                {
                    shootAlloc = 1.0;
                }

                if (this.ProjArea() > 0.0)
                {
                    leafScale = Math.Min(1.0, this.LimitFactors[(int)PastureUtil.TGrowthLimit.glGAI] / this.ProjArea());
                }
                else
                {
                    leafScale = 1.0;
                }

                leafToShoot = (R2S_Ratio + Math.Pow(this.fR2S_Target, 2)) / (R2S_Ratio + Math.Pow(this.Params.AllocK[1], 2))
                                * Math.Max(0.0, (leafScale * this.Params.AllocK[4] + (1.0 - leafScale) * this.Params.AllocK[5]));

                this.Allocation[ptLEAF] = (1.0 - seedAlloc) * shootAlloc * leafToShoot;
                this.Allocation[ptSTEM] = (1.0 - seedAlloc) * shootAlloc * (1.0 - leafToShoot);
                this.Allocation[ptROOT] = (1.0 - seedAlloc) * (1.0 - shootAlloc);
                this.Allocation[ptSEED] = seedAlloc;

                this.ComputeRootDistn(this.RootDepth, this.Owner.Inputs.ASW, ref sinkStrength);         // Allocation of new (net) root growth between soil layers
                sinkStrengthSum = 0.0;
                for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    sinkStrengthSum += sinkStrength[layer];
                }

                nextLayerFract[0] = 0.0;
                for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    fractionOccupied = Math.Max(0.0, Math.Min((this.RootDepth - this.Owner.FSoilDepths[layer - 1])
                                                        / this.Owner.FSoilLayers[layer], 1.0));
                    extendRatio = this.FRootExtension[layer] / this.Owner.FSoilLayers[layer];

                    if ((layer < this.FMaxRootLayer) && (fractionOccupied > 0.0) && (extendRatio > 0.0)
                                                && (extendRatio - (1.0 - fractionOccupied) > 1e-8))
                    {
                        nextLayerFract[layer] = 0.5 * (Math.Pow(extendRatio - (1.0 - fractionOccupied), 2)
                                                           - Math.Pow(Math.Max(0.0, extendRatio - 1.0), 2))
                                                        / (fractionOccupied * extendRatio);
                    }
                    else
                    {
                        nextLayerFract[layer] = 0.0;
                    }
                }

                for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    this.FNewRootDistn[EFFR, layer] = PastureUtil.Div0((1.0 - nextLayerFract[layer]) * sinkStrength[layer]
                                                        + nextLayerFract[layer - 1] * sinkStrength[layer - 1],
                                                        sinkStrengthSum);
                    this.FNewRootDistn[OLDR, layer] = 0.0;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="DMD"></param>
        /// <param name="roundHigh"></param>
        /// <returns></returns>
        public int DMDToClass(double DMD, bool roundHigh)
        {
            int Result;
            if (roundHigh)
            {
                Result = 1 + (int)Math.Round((PastureUtil.HerbageDMD[1] - DMD) / PastureUtil.CLASSWIDTH - 1.0E-5);
            }
            else
            {
                Result = 1 + (int)Math.Round((PastureUtil.HerbageDMD[1] - DMD) / PastureUtil.CLASSWIDTH + 1.0E-5);
            }

            Result = Math.Max(1, Math.Min(Result, HerbClassNo));

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public int NewHerbageClass(int part)
        {
            return this.DMDToClass(this.Params.MatureK[1, part], true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double NewShootFraction(int part, int DMD)
        {
            double result;
            int iNewClass = this.NewHerbageClass(part);
            if (DMD == iNewClass)
            {
                result = Math.Min(1.0, 1.0 + (this.Params.MatureK[1, part] - PastureUtil.HerbageDMD[iNewClass]) / PastureUtil.CLASSWIDTH);
            }
            else if (DMD == iNewClass + 1)
            {
                result = Math.Max(0.0, (PastureUtil.HerbageDMD[iNewClass] - this.Params.MatureK[1, part]) / PastureUtil.CLASSWIDTH);
            }
            else
            {
                result = 0.0;
            }

            return result;
        }

        /// <summary>
        /// Computes the death rates in green cohorts and the fall rate in standing cohorts
        /// Computes the rates of digestibility decline (g/g/d) for all cohorts
        /// </summary>
        /// <param name="minTemp"></param>
        /// <param name="meanTemp"></param>
        /// <param name="laggedTemp"></param>
        /// <param name="precip"></param>
        /// <param name="trampling"></param>
        /// <param name="surfaceASW"></param>
        public void ComputeFlowRates(double minTemp, double meanTemp, double laggedTemp, double precip, double trampling, double surfaceASW)
        {
            const double DECAY_REF_TEMP = 20.0;

            double[,] baseDeath = new double[ptSTEM + 1, HerbClassNo + 1]; // [ptLEAF..ptSTEM,1..HerbClassNo]
            double frostTemp;
            double frostDeath;
            double seedlDeath;
            double dayStress;
            double rootsDying;
            double degDays;
            double tempFactor;
            double thatchFactor;
            double moistFactorDead;
            double moistFactorLitter;
            double moistFactor;
            double digDecay;
            double indigDecay;
            double microbeRate;
            double fallRate;
            double commRate;
            double incorpRate;
            int part, DMD;

            if ((this.Status == stSEEDL) || (this.Status == stESTAB) || (this.Status == stSENC))
            {
                degDays = Math.Max(0.0, meanTemp - this.Params.MatureK[6, ptLEAF]);                     // Thermal time for aging of shoots

                for (part = ptLEAF; part <= ptSTEM; part++)                                             // Background death rate
                {
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        if (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive)
                        {
                            baseDeath[part, DMD] = this.Params.BaseDeathRates[2, part, DMD] * degDays;
                        }
                        else
                        {
                            baseDeath[part, DMD] = this.Params.BaseDeathRates[1, part, DMD] * degDays;
                        }

                        if (this.Status == stSENC)
                        {
                            baseDeath[part, DMD] = baseDeath[part, DMD] + this.Params.DeathK[3] * degDays;
                        }
                    }
                }

                frostTemp = minTemp - PastureUtil.FrostThreshold;                                 // Frost deaths
                if (frostTemp <= 0.0)
                {
                    frostDeath = PastureUtil.SIG(frostTemp, this.Params.DeathK[4] - this.FrostFactor, this.Params.DeathK[5] - this.FrostFactor);
                }
                else
                {
                    frostDeath = 0.0;
                }

                if (this.Status == stSEEDL)
                {
                    seedlDeath = PastureUtil.RAMP(this.SeedlStress, this.Params.DeathK[7], this.Params.DeathK[8]);

                    dayStress = 1.0 - this.LimitFactors[(int)PastureUtil.TGrowthLimit.glSM] * PastureUtil.Div0(this.LimitFactors[(int)PastureUtil.TGrowthLimit.glGAI], this.ProjArea());
                    this.FDelta_SI = this.Params.DeathK[9] * (dayStress - this.SeedlStress);
                }
                else
                {
                    seedlDeath = 0.0;
                    this.FDelta_SI = 0.0;
                }

                for (part = ptLEAF; part <= ptSTEM; part++)                                      // Overall shoot death rate
                {
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        if ((DMD >= this.FLowestDMDClass[part]) || (this.Status == stSENC))
                        {
                            this.FShootLossRate[part, DMD] = Math.Min(1.0, baseDeath[part, DMD] + frostDeath + seedlDeath);
                        }
                        else
                        {
                            this.FShootLossRate[part, DMD] = Math.Min(1.0, frostDeath + seedlDeath);
                        }
                    }
                }

                // Root flow rates
                this.FRootAgingRate = PastureUtil.Q10Func(laggedTemp, this.Params.RootLossK[1], this.Params.RootLossK[4]);
                rootsDying = PastureUtil.Q10Func(laggedTemp, this.Params.RootLossK[2], this.Params.RootLossK[4]);

                if (!this.Params.bAnnual || (this.Status != stSENC))
                {
                    if (this.Params.RootLossK[1] > 0.0)                                                  // This indicates "use old the root pool"
                    {
                        this.FRootRelocRate = this.Params.RootLossK[3] * rootsDying;
                        this.FRootLossRate[EFFR] = seedlDeath;
                        this.FRootLossRate[OLDR] = seedlDeath + rootsDying - this.FRootRelocRate;
                    }
                    else
                    {                                                                                   // Don't use the old root pool
                        this.FRootRelocRate = 0.0;
                        this.FRootLossRate[EFFR] = seedlDeath + rootsDying;
                        this.FRootLossRate[OLDR] = 1.0;
                    }
                }
                else
                {
                    this.FRootRelocRate = 0.0;
                    if (this.Root2Shoot() > this.Params.AllocK[2])
                    {
                        this.FRootLossRate[EFFR] = Math.Max(rootsDying, 1.0 - this.Params.AllocK[2] / this.Root2Shoot());
                    }
                    else
                    {
                        this.FRootLossRate[EFFR] = rootsDying;
                    }

                    this.FRootLossRate[OLDR] = this.FRootLossRate[EFFR];
                }

                PastureUtil.FillArray(this.FDMDDecline, 0.0);
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                    {
                        if (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive)
                        {
                            this.FDMDDecline[part, DMD] = this.Params.DMDRates[2, part, DMD] * degDays;
                        }
                        else
                        {
                            this.FDMDDecline[part, DMD] = this.Params.DMDRates[1, part, DMD] * degDays;
                        }

                        this.FDMDDecline[part, DMD] = Math.Min(this.FDMDDecline[part, DMD], PastureUtil.CLASSWIDTH);
                    }
                }
            }

            if ((this.Status == stDEAD) || (this.Status == stLITT1) || (this.Status == stLITT2))        // Digestibility decline of dry herbage
            {
                if (meanTemp > 0.0)
                {
                    tempFactor = Math.Exp(this.Params.DecayK[2] / (meanTemp + this.Params.DecayK[3]) * (meanTemp - DECAY_REF_TEMP));
                }
                else
                {
                    tempFactor = 0.0;
                }

                if (this.Owner.HerbageMassGM2(sgLITTER, TOTAL, TOTAL) <= this.Params.DecayK[9])
                {
                    thatchFactor = 1.0;
                }
                else
                {
                    thatchFactor = this.Params.DecayK[9] / this.Owner.HerbageMassGM2(sgLITTER, TOTAL, TOTAL);
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    moistFactorDead = this.Params.DecayK[4] + (1.0 - this.Params.DecayK[4]) * this.RelMoisture[part];
                    moistFactorLitter = PastureUtil.SIG(surfaceASW, this.Params.DecayK[6], this.Params.DecayK[7]);

                    if (this.Status == stDEAD)                                                // RelMoisture has been calculated previously
                    {
                        moistFactor = moistFactorDead;
                    }
                    else
                    {
                        moistFactor = (1.0 - thatchFactor) * moistFactorDead + thatchFactor * moistFactorLitter;
                    }

                    digDecay = this.Params.DecayK[1] * StdMath.XMin(tempFactor, moistFactor);
                    indigDecay = this.Params.DecayK[8] * digDecay;
                    for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                    {
                        microbeRate = digDecay * PastureUtil.HerbageDMD[DMD] + indigDecay * (1.0 - PastureUtil.HerbageDMD[DMD]);
                        this.FShootNetGrowth[part, DMD].DM = -microbeRate * this.Herbage[part, DMD].DM;
                        if (DMD < this.FLowestDMDClass[part])
                        {
                            this.FDMDDecline[part, DMD] = (digDecay - microbeRate) * PastureUtil.HerbageDMD[DMD];
                        }
                        else
                        {
                            this.FDMDDecline[part, DMD] = 0.0;
                        }
                    }
                }
            }

            if (this.Status == stDEAD)
            {
                // Fall of standing dead
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    fallRate = this.Params.FallK[1, part]
                                 * (1.0 + this.Params.FallK[2, part] * (1.0 - Math.Exp(-precip / this.Params.FallK[3, part]))
                                        + this.Params.FallK[4, part] * trampling);
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        this.FShootLossRate[part, DMD] = fallRate;
                    }
                }
            }
            else if (this.Status == stLITT1)
            {
                // Comminution of litter
                if (this.Owner.HerbageMassGM2(sgLITTER, TOTAL, TOTAL) <= this.Params.BreakdownK[5, ptLEAF])
                {
                    thatchFactor = 1.0;
                }
                else
                {
                    thatchFactor = this.Params.BreakdownK[5, ptLEAF] / this.Owner.HerbageMassGM2(sgLITTER, TOTAL, TOTAL);
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    commRate = this.Params.BreakdownK[1, part] * (thatchFactor + this.Params.BreakdownK[2, ptLEAF] * trampling);
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        this.FShootLossRate[part, DMD] = commRate;
                    }
                }
            }
            else if (this.Status == stLITT2)
            {
                // Incorporation of litter
                if (meanTemp >= 0.0)
                {
                    incorpRate = this.Params.BreakdownK[3, ptLEAF]
                                   + (this.Params.BreakdownK[4, ptLEAF] - this.Params.BreakdownK[3, ptLEAF]) * PastureUtil.RAMP(surfaceASW, 0.0, 1.0);
                }
                else
                {
                    incorpRate = 0.0;
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    for (DMD = 1; DMD <= HerbClassNo; DMD++)
                    {
                        this.FShootLossRate[part, DMD] = incorpRate;
                    }
                }
            }
        }

        /// <summary>
        /// N.B. fDeadMoisture is a state variable
        /// </summary>
        /// <param name="moistureChange">Net rate of moisture input (mm/d)</param>
        public void DeadMoistureBalance(double moistureChange)
        {
            double deadArea;                   // Area index of this plant part (m^2/m^2)
            double maxMoisture;                // Maximum moisture content (mm)
            double moistureRate;               // Rate of change of herbage moisture (mm/d)
            double endMoisture;                // Moisture content at the end of the time step (mm)
            double meanMoisture;
            int iPart;

            if (this.Status == stDEAD)
            {
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    maxMoisture = this.Params.DecayK[5] * (0.001 * this.Herbage[iPart, TOTAL].DM);         // This is in mm water, i.e. kg/m^2
                    this.DeadMoisture[iPart] = StdMath.XMin(this.DeadMoisture[iPart], maxMoisture);        // Trap case where dead mass has reduced

                    deadArea = this.AreaIndex(iPart);
                    moistureRate = moistureChange * StdMath.XDiv(deadArea, this.Owner.PastureAreaIndex);
                    endMoisture = StdMath.XMax(0.0, StdMath.XMin(this.DeadMoisture[iPart] + moistureRate, maxMoisture));
                    meanMoisture = endMoisture - StdMath.XDiv(StdMath.Sqr(endMoisture - this.DeadMoisture[iPart]), 2.0 * moistureRate);
                    this.DeadMoisture[iPart] = endMoisture;
                    this.RelMoisture[iPart] = StdMath.XDiv(meanMoisture, maxMoisture);
                }
            }
        }

        /// <summary>
        /// Compute the frost hardening
        /// </summary>
        /// <param name="minTemp"></param>
        public void ComputeFrostHardening(double minTemp)
        {
            double fNewGrowth;
            double fFrostTemp;

            fNewGrowth = this.FPartNetGrowth[ptLEAF] + this.FPartNetGrowth[ptSTEM];
            this.FDeltaFrost = -this.FrostFactor * PastureUtil.Div0(fNewGrowth, this.Herbage[TOTAL, TOTAL].DM + fNewGrowth);

            fFrostTemp = minTemp - PastureUtil.FrostThreshold;
            if (fFrostTemp <= 0.0)
            {
                this.FDeltaFrost += this.Params.DeathK[6];
            }
        }

        /// <summary>
        /// Is establishment today
        /// </summary>
        /// <returns></returns>
        public bool EstablishesToday()
        {
            return (this.Status == stSEEDL) && (this.EstabIndex >= this.Params.SeedlK[1]);
        }

        /// <summary>
        /// Compute the root extension
        /// </summary>
        /// <param name="meanTemp"></param>
        /// <param name="ASW"></param>
        public void ComputeRootExtension(double meanTemp, double[] ASW)
        {
            GrazType.InitLayerArray(ref this.FRootExtension, 0.0);

            if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
            {
                this.FMaxRootLayer = 1;                                                        // Deepest layer containing roots
                while ((this.FMaxRootLayer < this.Owner.FSoilLayerCount)
                      && (this.RootDepth > this.Owner.FSoilDepths[this.FMaxRootLayer]))
                {
                    this.FMaxRootLayer++;
                }

                for (int iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                {
                    this.FRootExtension[iLayer] = this.Params.RootK[2] * Math.Max(0.0, meanTemp - this.Params.RootK[3])
                                                              * this.RelRootExtension(iLayer, ASW[iLayer]);
                    if (iLayer == this.FMaxRootLayer)
                    {
                        this.FRootExtension[iLayer] = Math.Min(this.FRootExtension[iLayer], this.Owner.FMaxRootDepth - this.RootDepth);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the relative root extension
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="ASW"></param>
        /// <returns></returns>
        public double RelRootExtension(int layer, double ASW)
        {
            return this.Owner.FRootRestriction[layer] * PastureUtil.RAMP(ASW, 0.0, this.Params.RootK[4]);
        }

        /// <summary>
        /// Compute the root distribution
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="ASW"></param>
        /// <param name="distn"></param>
        public void ComputeRootDistn(double depth, double[] ASW, ref double[] distn)
        {
            double relDepth;
            double sum;
            int layer;

            distn = new double[MaxSoilLayers + 1];
            if (depth <= 0.0)
            {
                distn[1] = 1.0;
            }
            else
            {
                for (layer = 0; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    relDepth = Math.Min(1.0, this.Owner.FSoilDepths[layer] / depth);
                    distn[layer] = Math.Pow(0.01, relDepth);
                }

                sum = 0.0;
                for (layer = this.Owner.FSoilLayerCount; layer >= 1; layer--)
                {
                    distn[layer] = (distn[layer - 1] - distn[layer]) * this.RelRootExtension(layer, ASW[layer]);
                    sum += distn[layer];
                }

                distn[0] = 0.0;

                if (sum > 0.0)
                {
                    for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                    {
                        distn[layer] = distn[layer] / sum;
                    }
                }
                else
                {
                    distn[1] = 1.0;
                }
            }
        }

        /// <summary>
        /// Effective root length density in m/m^3
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private double EffRootLengthD(int layer)
        {
            return PastureUtil.Div0(this.Params.RootK[9] * this.Roots[EFFR, layer].DM,
                            0.001 * this.Owner.FSoilLayers[layer]);
        }

        /// <summary>
        /// Add root masses and flows from one cohort into this cohort
        /// </summary>
        /// <param name="srcCohort"></param>
        public void AddRoots(TPastureCohort srcCohort)
        {
            int age, layer;

            this.RootDepth = Math.Max(this.RootDepth, srcCohort.RootDepth);
            this.FMaxRootLayer = Math.Max(this.FMaxRootLayer, srcCohort.FMaxRootLayer);
            for (age = EFFR; age <= OLDR; age++)
            {
                for (layer = 1; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    this.FRootTransloc[age, layer] = this.FRootTransloc[age, layer] + srcCohort.FRootTransloc[age, layer];
                    this.Owner.AddPool(srcCohort.FRootNetGrowth[age, layer], ref this.FRootNetGrowth[age, layer]);
                }
            }

            this.FRootAgingRate = WeightAverage(this.FRootAgingRate, this.Roots[EFFR, TOTAL].DM,
                                              srcCohort.FRootAgingRate, srcCohort.Roots[EFFR, TOTAL].DM);
            this.FRootRelocRate = WeightAverage(this.FRootRelocRate, this.Roots[OLDR, TOTAL].DM,
                                              srcCohort.FRootRelocRate, srcCohort.Roots[OLDR, TOTAL].DM);
            for (age = EFFR; age <= OLDR; age++)
            {
                this.FRootLossRate[age] = WeightAverage(this.FRootLossRate[age], this.Roots[age, TOTAL].DM,
                                                       srcCohort.FRootLossRate[age], srcCohort.Roots[age, TOTAL].DM);
            }

            for (age = TOTAL; age <= OLDR; age++)
            {
                for (layer = TOTAL; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    this.Owner.AddPool(srcCohort.Roots[age, layer], ref this.Roots[age, layer]);
                }
            }
        }

        /// <summary>
        /// Zero all root masses and pools
        /// </summary>
        public void ClearRoots()
        {
            int age, layer;

            for (age = TOTAL; age <= OLDR; age++)
            {
                for (layer = TOTAL; layer <= this.Owner.FSoilLayerCount; layer++)
                {
                    PastureUtil.ZeroPool(ref this.Roots[age, layer]);
                }
            }

            this.RootDepth = 0.0;
            this.FMaxRootLayer = 0;
            PastureUtil.FillArray(this.FRootTransloc, 0.0);
            this.Zero2D_DMPool(ref this.FRootNetGrowth);
            this.FRootAgingRate = 0.0;
            this.FRootRelocRate = 0.0;
            for (age = EFFR; age <= OLDR; age++)
            {
                this.FRootLossRate[age] = 0.0;
            }
        }

        /// <summary>
        /// Compute the maximum and critical nutrient demand for this element
        /// </summary>
        /// <param name="elem">Nutrient. N, P, S</param>
        public void ComputeNutrientDemand(TPlantElement elem)
        {
            int stage;
            double[] currDM = new double[ptSEED + 1];   // ptLEAF..ptSEED
            double[] currNu = new double[ptSEED + 1];   // ptLEAF..ptSEED
            double imbalance;
            double growthMax;
            double growthCrit;
            int part, DMD;

            if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
            {
                if (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive)
                {
                    stage = 2;
                }
                else
                {
                    stage = 1;
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    currDM[part] = 0.0;
                    currNu[part] = 0.0;
                    for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                    {
                        currDM[part] = currDM[part] + this.Params.NewTissuePropns[stage, part, DMD] * this.Herbage[part, DMD].DM;
                        currNu[part] = currNu[part] + this.Params.NewTissuePropns[stage, part, DMD] * this.Herbage[part, DMD].Nu[(int)elem];
                    }
                }

                currDM[ptROOT] = this.Roots[EFFR, TOTAL].DM;
                currNu[ptROOT] = this.Roots[EFFR, TOTAL].Nu[(int)elem];
                currDM[ptSEED] = 0.0;
                currNu[ptSEED] = 0.0;

                // Compute the maximum & critical demand
                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    imbalance = currNu[part] - this.Params.NutrConcK[1, (int)elem, part] * currDM[part];
                    growthMax = Math.Max(0.0, this.Params.NutrConcK[1, (int)elem, part] * this.FPotPartNetGrowth[part]);
                    growthCrit = Math.Max(0.0, this.Params.NutrConcK[2, (int)elem, part] * this.FPotPartNetGrowth[part]);

                    this.FNutrientInfo[(int)elem].fMaxDemand[part] = Math.Max(0.0, growthMax - imbalance);
                    this.FNutrientInfo[(int)elem].fCritDemand[part] = Math.Max(0.0, growthCrit - Math.Max(0.0, imbalance));
                    this.FNutrientInfo[(int)elem].fCritDemand[part] = Math.Min(this.FNutrientInfo[(int)elem].fMaxDemand[part], this.FNutrientInfo[(int)elem].fCritDemand[part]);
                }

                this.FNutrientInfo[(int)elem].fMaxDemand[TOTAL] = 0.0;                                   // Compute the total demands for this cohort
                this.FNutrientInfo[(int)elem].fCritDemand[TOTAL] = 0.0;
                for (part = ptLEAF; part <= ptSEED; part++)
                {
                    this.FNutrientInfo[(int)elem].fMaxDemand[TOTAL] = this.FNutrientInfo[(int)elem].fMaxDemand[TOTAL] + this.FNutrientInfo[(int)elem].fMaxDemand[part];
                    this.FNutrientInfo[(int)elem].fCritDemand[TOTAL] = this.FNutrientInfo[(int)elem].fCritDemand[TOTAL] + this.FNutrientInfo[(int)elem].fCritDemand[part];
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Elem"></param>
        public void ResetNutrientSupply(TPlantElement Elem)
        {
            this.FNutrientInfo[(int)Elem].fSupplied = 0.0;
            PastureUtil.FillArray(this.FNutrientInfo[(int)Elem].fRecycled, 0.0);
            this.FNutrientInfo[(int)Elem].fRecycledSum = 0.0;
            this.FNutrientInfo[(int)Elem].fFixed = 0.0;
            PastureUtil.Fill3DArray(this.FNutrientInfo[(int)Elem].fUptake, 0.0);
            this.FNutrientInfo[(int)Elem].fUptakeSum = 0.0;
            PastureUtil.FillArray(this.FNutrientInfo[(int)Elem].fRelocated, 0.0);
            PastureUtil.FillArray(this.FNutrientInfo[(int)Elem].fRelocatedRoot, 0.0);
            PastureUtil.FillArray(this.FNutrientInfo[(int)Elem].fLeached, 0.0);
            this.FNutrientInfo[(int)Elem].fGaseousLoss = 0.0;
        }

        /// <summary>
        /// Add nutrients moving in translocated DM to today's nutrient supply
        /// </summary>
        /// <param name="Elem"></param>
        public void TranslocateNutrients(TPlantElement Elem)
        {
            int age, layer;
            int DMD;

            this.FNutrientInfo[(int)Elem].fRootTranslocSupply = 0.0;
            for (age = EFFR; age <= OLDR; age++)
            {
                for (layer = 1; layer <= this.FMaxRootLayer; layer++)
                {
                    PastureUtil.XInc(ref this.FNutrientInfo[(int)Elem].fRootTranslocSupply, this.FPotRootTransloc[age, layer]
                                           * PastureUtil.Div0(this.Roots[age, layer].Nu[(int)Elem], this.Roots[age, layer].DM));
                }
            }

            this.FNutrientInfo[(int)Elem].fStemTranslocSupply = 0.0;
            for (DMD = this.FHighestDMDClass[ptSTEM]; DMD <= this.FLowestDMDClass[ptSTEM]; DMD++)
            {
                PastureUtil.XInc(ref this.FNutrientInfo[(int)Elem].fStemTranslocSupply, this.FPotStemTransloc[DMD]
                                            * PastureUtil.Div0(this.Herbage[ptSTEM, DMD].Nu[(int)Elem], this.Herbage[ptSTEM, DMD].DM));
            }

            this.FNutrientInfo[(int)Elem].fSupplied += this.FNutrientInfo[(int)Elem].fRootTranslocSupply + this.FNutrientInfo[(int)Elem].fStemTranslocSupply;
        }

        /// <summary>
        /// As green biomass declines in digestibility, it is assumed to release
        /// nutrients which then become available to meet the day's demand
        /// </summary>
        /// <param name="elem"></param>
        public void RecycleNutrients(TPlantElement elem)
        {
            double demandLeft;
            double[] conc = new double[HerbClassNo + 1];
            double[] relConc = new double[HerbClassNo + 1];
            double flux;                                                   // Flux of element out of each herbage pool due to DMD decline
            double lowerConc;
            int part, DMD;

            TNutrientInfo nutrInfo = this.FNutrientInfo[(int)elem];

            demandLeft = Math.Max(0.0, nutrInfo.fMaxDemand[TOTAL] - nutrInfo.fSupplied);               // Compute unsatisfied demand

            nutrInfo.fRecycledSum = 0.0;
            for (part = ptLEAF; part <= ptSTEM; part++)
            {
                for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                {
                    // Concentration in each herbage pool, relative to max & min concentrations
                    conc[DMD] = Math.Max(PastureUtil.Div0(this.Herbage[part, DMD].Nu[(int)elem], this.Herbage[part, DMD].DM),
                                        nutrInfo.fMinShootConc[part, DMD]);
                    relConc[DMD] = PastureUtil.RAMP(conc[DMD], nutrInfo.fMinShootConc[part, DMD], nutrInfo.fMaxShootConc[part, DMD]);
                }

                for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part] - 1; DMD++)
                {
                    flux = Math.Min(1.0, this.FDMDDecline[part, DMD] / PastureUtil.CLASSWIDTH) * this.Herbage[part, DMD].Nu[(int)elem];
                    if (flux > 0.0)
                    {
                        lowerConc = nutrInfo.fMinShootConc[part, DMD + 1] + relConc[DMD] * (nutrInfo.fMaxShootConc[part, DMD + 1] - nutrInfo.fMinShootConc[part, DMD + 1]);
                        nutrInfo.fRecycled[part, DMD] = flux * (1.0 - lowerConc / conc[DMD]);
                        nutrInfo.fRecycledSum += nutrInfo.fRecycled[part, DMD];
                    }
                    else
                    {
                        nutrInfo.fRecycled[part, DMD] = 0.0;
                    }
                }

                for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                {
                    // Any nutrients above the maximum concentration are assumed to becompletely mobile
                    if (conc[DMD] > nutrInfo.fMaxShootConc[part, DMD])
                    {
                        flux = this.Herbage[part, DMD].Nu[(int)elem] - nutrInfo.fMaxShootConc[part, DMD] * this.Herbage[part, DMD].DM;
                        nutrInfo.fRecycled[part, DMD] = nutrInfo.fRecycled[part, DMD] + flux;
                        nutrInfo.fRecycledSum += flux;
                    }
                }
            }

            if (demandLeft < nutrInfo.fRecycledSum)
            {
                // Case where recycling meets all remaining demand
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    for (DMD = this.FHighestDMDClass[part]; DMD <= this.FLowestDMDClass[part]; DMD++)
                    {
                        nutrInfo.fRecycled[part, DMD] = nutrInfo.fRecycled[part, DMD] * demandLeft / nutrInfo.fRecycledSum;
                    }
                }

                nutrInfo.fRecycledSum = demandLeft;
            }

            nutrInfo.fSupplied += nutrInfo.fRecycledSum;                                                // Cumulate supply of this element
        }

        /// <summary>
        /// The fixation model is based on the one in EPIC, and requires the demand
        /// as an input.  EPIC assumes that rhizobia are not limiting when plants
        /// are in the appropriate stage of development.
        /// Note the following changes:
        /// 1. Fixation is computed in a depth-distributed manner, rather than being
        ///    kept to 300 mm depth for water and the entire root zone for NO3 as
        ///    the original.
        /// 2. Fixation activity per unit root mass declines linearly from the
        ///    surface to a proportion (NutrK[5]) of the current rooting depth. At
        ///    lower depths, activity per unit root mass remains constant at
        ///    NutrK[2] of the level at the surface.  Integration of the product of
        ///    root mass density and relative nodule activity gives the complex-
        ///    looking function for NodulePropn
        /// 3. The NO3-per-unit-depth concept used in EPIC for the NO3 inhibition
        ///    effect has been changed to use soil solution NO3 concentration, in
        ///    parts per million.
        /// </summary>
        public void FixNitrogen()
        {
            double fDemandLeft;
            double fNoduleDepth;
            double fDepth;
            double fNoduleDensity;
            double[] fNodulePropn = new double[MaxSoilLayers + 1];
            double fNoduleSum;
            double fPropnFixing;
            double fLimitFactor;
            double fWaterLimit;
            double fNO3Limit;
            int iLayer, iArea;

            TNutrientInfo nutrInfo = this.FNutrientInfo[(int)TPlantElement.N];

            fDemandLeft = nutrInfo.fMaxDemand[TOTAL] - nutrInfo.fSupplied;                          // Compute unsatisfied demand

            fNoduleDepth = this.Params.NFixK[1] * this.RootDepth;
            fNoduleSum = 0.0;                                                                       // Relative nodule density distribution
            for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
            {
                fDepth = 0.5 * (this.Owner.FSoilDepths[iLayer - 1] + this.Owner.FSoilDepths[iLayer]);
                if (fDepth < fNoduleDepth)
                {
                    fNoduleDensity = 1.0 - (1.0 - this.Params.NFixK[2]) * fDepth / fNoduleDepth;
                }
                else
                {
                    fNoduleDensity = 0.0;
                }

                fNodulePropn[iLayer] = fNoduleDensity * this.Roots[EFFR, iLayer].DM;
                fNoduleSum += fNodulePropn[iLayer];
            }

            if (fNoduleSum > 0.0)
            {
                for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                {
                    fNodulePropn[iLayer] = fNodulePropn[iLayer] / fNoduleSum;
                }
            }

            switch (this.Status)
            {
                // Age effect on nodulation
                case stSEEDL: fPropnFixing = (this.EstabIndex - 1.0) / (this.Params.SeedlK[1] - 1.0);
                    break;
                case stESTAB: fPropnFixing = 1.0;
                    break;
                default: fPropnFixing = 0.0;
                    break;
            }

            fLimitFactor = 0.0;

            // Depth-distributed computation, weighted by root activity
            for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
            {
                fWaterLimit = PastureUtil.RAMP(this.Owner.Inputs.ASW[iLayer], 0.0, this.Params.NFixK[3]);
                fNO3Limit = 0.0;

                TSoilNutrientDistn nutrDist = this.Owner.Inputs.Nutrients[(int)TPlantNutrient.pnNO3];
                for (iArea = 0; iArea <= nutrDist.NoAreas - 1; iArea++)
                {
                    fNO3Limit += nutrDist.RelAreas[iArea] * PastureUtil.RAMP(nutrDist.SolnPPM[iArea][iLayer], this.Params.NFixK[5], this.Params.NFixK[4]);
                }

                fLimitFactor += fNodulePropn[iLayer] * StdMath.XMin(fWaterLimit, fNO3Limit);
            }

            nutrInfo.fFixed = fDemandLeft * fPropnFixing * fLimitFactor;                            // Finally, compute the amount fixed
            nutrInfo.fSupplied += nutrInfo.fFixed;
        }

        /// <summary>
        /// Uptake nutrients
        /// </summary>
        /// <param name="elem">N, P, S</param>
        /// <param name="supply"></param>
        public void UptakeNutrients(TPlantElement elem, double[][][] supply)
        {
            TPlantNutrient FirstNutr;
            double[] RLD = new double[MaxSoilLayers + 1];
            double demandLeft;
            double demandArea;
            double uptakeAreaSum;
            int area;
            int layer;
            // TPlantNutrient Nutr;

            TNutrientInfo nutrInfo = this.FNutrientInfo[(int)elem];

            FirstNutr = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray()[0];
            while (PastureUtil.Nutr2Elem[(int)FirstNutr] != elem)
            {
                FirstNutr++;
            }

            for (layer = 1; layer <= this.FMaxRootLayer; layer++)
            {
                RLD[layer] = this.EffRootLengthD(layer);
            }

            demandLeft = Math.Max(0.0, nutrInfo.fMaxDemand[TOTAL] - nutrInfo.fSupplied);

            for (area = 0; area <= this.Owner.Inputs.Nutrients[(int)FirstNutr].NoAreas - 1; area++)
            {
                demandArea = this.Owner.Inputs.Nutrients[(int)FirstNutr].RelAreas[area] * demandLeft;
                uptakeAreaSum = 0.0;
                var values = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray();
                foreach (var Nutr in values)
                {
                    if (PastureUtil.Nutr2Elem[(int)Nutr] == elem)
                    {
                        for (layer = 1; layer <= this.FMaxRootLayer; layer++)
                        {
                            nutrInfo.fUptake[(int)Nutr][area][layer] = supply[(int)Nutr][area][layer]; //kg/ha to g/m2
                            uptakeAreaSum += nutrInfo.fUptake[(int)Nutr][area][layer];
                        }
                    }
                }

                if (uptakeAreaSum > demandArea)
                {
                    foreach (var Nutr in values)
                    {
                        if (PastureUtil.Nutr2Elem[(int)Nutr] == elem)
                        {
                            for (layer = 1; layer <= this.FMaxRootLayer; layer++)
                            {
                                nutrInfo.fUptake[(int)Nutr][area][layer] = nutrInfo.fUptake[(int)Nutr][area][layer] * demandArea / uptakeAreaSum;
                            }
                        }
                    }

                    uptakeAreaSum = demandArea;
                }

                nutrInfo.fUptakeSum += uptakeAreaSum;
            } // loop over areas

            nutrInfo.fSupplied += nutrInfo.fUptakeSum;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        public void RelocateNutrients(TPlantElement elem)
        {
            int iStage;
            double fDemandLeft;
            double fAvail;
            int iPart, iDMD;
            int iLayer;

            if (this.Owner.Phenology == PastureUtil.TDevelopType.Reproductive)
            {
                iStage = 2;
            }
            else
            {
                iStage = 1;
            }

            TNutrientInfo nutrInfo = this.FNutrientInfo[(int)elem];

            fDemandLeft = Math.Max(0.0, nutrInfo.fCritDemand[TOTAL] - nutrInfo.fSupplied);          // Compute unsatisfied *critical* demand

            nutrInfo.fRelocatedSum = 0.0;
            for (iDMD = HerbClassNo; iDMD >= 1; iDMD--)
            {
                // Lower DMD before higher, leaf before stem
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    if ((iDMD >= this.FHighestDMDClass[iPart]) && (iDMD <= this.FLowestDMDClass[iPart]))
                    {
                        fAvail = Math.Max(0.0, this.Herbage[iPart, iDMD].Nu[(int)elem] - nutrInfo.fMinShootConc[iPart, iDMD] * this.Herbage[iPart, iDMD].DM)
                                  * (1.0 - this.Params.NewTissuePropns[iStage, iPart, iDMD]);
                        nutrInfo.fRelocated[iPart, iDMD] = Math.Min(fDemandLeft, this.Params.NutrRelocateK[(int)elem] * fAvail);
                        nutrInfo.fRelocatedSum += nutrInfo.fRelocated[iPart, iDMD];
                        fDemandLeft -= nutrInfo.fRelocated[iPart, iDMD];
                    }
                }
            }

            nutrInfo.fSupplied += nutrInfo.fRelocatedSum;

            if ((nutrInfo.fCritDemand[ptSEED] > 0.0) && (fDemandLeft > 0.0))
            {
                // Demand for seed nutrient can be met by relocating out of roots
                nutrInfo.fRelocatedRoot[TOTAL] = 0.0;
                for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                {
                    fAvail = Math.Max(0.0, this.Roots[EFFR, iLayer].Nu[(int)elem] - this.MinNutrientConc(ptROOT, TOTAL, elem) * this.Roots[EFFR, iLayer].DM);
                    nutrInfo.fRelocatedRoot[iLayer] = this.Params.NutrRelocateK[(int)elem] * fAvail;
                    nutrInfo.fRelocatedRoot[TOTAL] = nutrInfo.fRelocatedRoot[TOTAL] + nutrInfo.fRelocatedRoot[iLayer];
                }

                if (nutrInfo.fRelocatedRoot[TOTAL] > fDemandLeft)
                {
                    for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                    {
                        nutrInfo.fRelocatedRoot[iLayer] = nutrInfo.fRelocatedRoot[iLayer] * fDemandLeft / nutrInfo.fRelocatedRoot[TOTAL];
                    }

                    nutrInfo.fRelocatedRoot[TOTAL] = fDemandLeft;
                }

                nutrInfo.fSupplied += nutrInfo.fRelocatedRoot[TOTAL];
            }
        }

        /// <summary>
        /// Moves mobile nutrient(taken to be all above MinConc) out of
        /// litter biomass in response to rainfall
        /// </summary>
        /// <param name="elem"></param>
        public void LeachNutrients(TPlantElement elem)
        {
            int iPart, iDMD;

            if (this.Owner.Inputs.Precipitation > 0.0)
            {
                TNutrientInfo nutrInfo = this.FNutrientInfo[(int)elem];
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FHighestDMDClass[iPart]; iDMD++)
                    {
                        nutrInfo.fLeached[iPart, iDMD] = (1.0 - Math.Exp(-0.05 * this.Owner.Inputs.Precipitation))
                                                * Math.Max(0.0, this.Herbage[iPart, iDMD].Nu[(int)elem]
                                                - nutrInfo.fMinShootConc[iPart, iDMD] * this.Herbage[iPart, iDMD].DM);
                    }
                }
            }
        }

        /// <summary>
        /// Reduce nutrient fluxes to match the growth rate determined by the most-
        /// limiting nutrient
        /// </summary>
        /// <param name="elem"></param>
        public void RescaleNutrientRates(TPlantElement elem)
        {
            const double EPSILON = 1.0E-7;

            double fDelta;
            double fScale;
            int iPart;
            int iDMD;
            int iLayer;
            int iArea;

            TNutrientInfo nutrInfo = this.FNutrientInfo[(int)elem];

            for (iPart = ptLEAF; iPart <= ptSEED; iPart++)
            {
                nutrInfo.fMaxDemand[iPart] = Math.Max(0.0, nutrInfo.fMaxDemand[iPart] - this.Params.NutrConcK[1, (int)elem, iPart] * (this.FPotPartNetGrowth[iPart] - this.FPartNetGrowth[iPart]));
                nutrInfo.fCritDemand[iPart] = Math.Max(0.0, nutrInfo.fCritDemand[iPart] - this.Params.NutrConcK[2, (int)elem, iPart] * (this.FPotPartNetGrowth[iPart] - this.FPartNetGrowth[iPart]));
            }

            nutrInfo.fMaxDemand[TOTAL] = 0.0;
            nutrInfo.fCritDemand[TOTAL] = 0.0;
            for (iPart = ptLEAF; iPart <= ptSEED; iPart++)
            {
                nutrInfo.fMaxDemand[TOTAL] += nutrInfo.fMaxDemand[iPart];
                nutrInfo.fCritDemand[TOTAL] += nutrInfo.fCritDemand[iPart];
            }

            if ((nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL] > EPSILON)
               && (nutrInfo.fRelocatedRoot[TOTAL] > 0.0))
            {
                // Work backward through the sources of nutrients
                fDelta = Math.Min(nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL], nutrInfo.fRelocatedRoot[TOTAL]);
                fScale = 1.0 - fDelta / nutrInfo.fRelocatedRoot[TOTAL];
                for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                {
                    nutrInfo.fRelocatedRoot[iLayer] = fScale * nutrInfo.fRelocatedRoot[iLayer];
                }

                nutrInfo.fRelocatedRoot[TOTAL] = nutrInfo.fRelocatedRoot[TOTAL] - fDelta;
                nutrInfo.fSupplied -= fDelta;
            }

            if ((nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL] > EPSILON) && (nutrInfo.fRelocatedSum > 0.0))
            {
                fDelta = Math.Min(nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL], nutrInfo.fRelocatedSum);
                fScale = 1.0 - fDelta / nutrInfo.fRelocatedSum;
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        nutrInfo.fRelocated[iPart, iDMD] = fScale * nutrInfo.fRelocated[iPart, iDMD];
                    }
                }

                nutrInfo.fRelocatedSum -= fDelta;
                nutrInfo.fSupplied -= fDelta;
            }

            if ((nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL] > EPSILON) && (nutrInfo.fUptakeSum > 0.0))
            {
                fDelta = Math.Min(nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL], nutrInfo.fUptakeSum);
                fScale = 1.0 - fDelta / nutrInfo.fUptakeSum;
                var values = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray();
                foreach (var Nutr in values)
                {
                    if (PastureUtil.Nutr2Elem[(int)Nutr] == elem)
                    {
                        for (iArea = 0; iArea <= MAXNUTRAREAS - 1; iArea++)
                        {
                            for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                            {
                                nutrInfo.fUptake[(int)Nutr][iArea][iLayer] = fScale * nutrInfo.fUptake[(int)Nutr][iArea][iLayer];
                            }
                        }
                    }
                }

                nutrInfo.fUptakeSum -= fDelta;
                nutrInfo.fSupplied -= fDelta;
            }

            if ((nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL] > EPSILON) && (nutrInfo.fFixed > 0.0))
            {
                fDelta = Math.Min(nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL], nutrInfo.fFixed);
                nutrInfo.fFixed -= fDelta;
                nutrInfo.fSupplied -= fDelta;
            }

            if ((nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL] > EPSILON) && (nutrInfo.fRecycledSum > 0.0))
            {
                fDelta = Math.Min(nutrInfo.fSupplied - nutrInfo.fMaxDemand[TOTAL], nutrInfo.fRecycledSum);
                fScale = 1.0 - fDelta / nutrInfo.fRecycledSum;
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        nutrInfo.fRecycled[iPart, iDMD] = fScale * nutrInfo.fRecycled[iPart, iDMD];
                    }
                }

                nutrInfo.fRecycledSum -= fDelta;
                nutrInfo.fSupplied -= fDelta;
            }
        }

        /// <summary>
        /// 1. Collects recycled, relocated, fixed and taken-up nutrients into the
        ///    nutrSupply variable for later distribution amongst net growth
        /// 2. In litter cohorts, moves leached nutrients into the Leachate holding
        ///    variable
        /// </summary>
        /// <param name="nutrSupply"></param>
        public void UpdateNutrientFlows(ref DM_Pool nutrSupply)
        {
            int iPart, iDMD;
            int iLayer;

            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
            foreach (var Elem in values)
            {
                if (this.Owner.FElements.Contains(Elem))
                {
                    TNutrientInfo nutrInfo = this.FNutrientInfo[(int)Elem];
                    if ((this.Status >= stSEEDL) && (this.Status <= stSENC))
                    {
                        nutrSupply.Nu[(int)Elem] = nutrInfo.fFixed + nutrInfo.fUptakeSum + nutrInfo.fRootTranslocSupply + nutrInfo.fStemTranslocSupply;
                        for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                        {
                            for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FLowestDMDClass[iPart]; iDMD++)
                            {
                                PastureUtil.MoveNutrient(ref this.Herbage[iPart, iDMD], ref nutrSupply, Elem, nutrInfo.fRecycled[iPart, iDMD] + nutrInfo.fRelocated[iPart, iDMD]);
                            }
                        }

                        for (iLayer = 1; iLayer <= this.FMaxRootLayer; iLayer++)
                        {
                            PastureUtil.MoveNutrient(ref this.Roots[EFFR, iLayer], ref nutrSupply, Elem, nutrInfo.fRelocatedRoot[iLayer]);
                        }
                    }
                    else
                    {
                        nutrSupply.Nu[(int)Elem] = 0.0;
                    }

                    if ((this.Status >= stLITT1) && (this.Status <= stLITT2))
                    {
                        for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                        {
                            for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FLowestDMDClass[iPart]; iDMD++)
                            {
                                PastureUtil.MoveNutrient(ref this.Herbage[iPart, iDMD], ref this.Owner.FLeachate, Elem, nutrInfo.fLeached[iPart, iDMD]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Distributes the supply of nutrients between the net growth of the various
        /// plant parts, i.e. FShootNetGrowth, FRootNetGrowth and FSeedNetGrowth
        /// * Critical demand for seed gets first priority.
        /// * Critical demand for other plant parts gets next priority.
        /// * Any excess over critical demand is allocated in proportion to the
        ///   amounts of (maximum demand - critical demand).
        /// </summary>
        /// <param name="nutrSupply"></param>
        public void AllocateNutrientFlows(DM_Pool nutrSupply)
        {
            double fSupply;
            double[] fPartSupply = new double[ptSEED + 1]; // [ptLEAF..ptSEED]
            double fRelSupply;
            double fPropn;
            int iPart, iDMD;
            int iAge, iLayer;

            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
            foreach (var Elem in values)
            {
                if (this.Owner.FElements.Contains(Elem))
                {
                    TNutrientInfo nutrInfo = this.FNutrientInfo[(int)Elem];
                    fSupply = nutrSupply.Nu[(int)Elem];

                    if (fSupply == 0.0)
                    {
                        for (iPart = ptLEAF; iPart <= ptSEED; iPart++)
                        {
                            fPartSupply[iPart] = 0.0;
                        }
                    }
                    else if (nutrInfo.fMaxDemand[TOTAL] <= 0.0)
                    {
                        // This can happen when translocation is occurring but net growth is negative
                        fPartSupply[ptLEAF] = 0.0;
                        fPartSupply[ptSTEM] = Math.Min(fSupply, nutrInfo.fStemTranslocSupply);
                        fPartSupply[ptROOT] = fSupply - fPartSupply[ptSTEM];
                        fPartSupply[ptSEED] = 0.0;
                    }
                    else if (fSupply < nutrInfo.fCritDemand[TOTAL])
                    {
                        // Insufficient to meet critical demands
                        fPartSupply[ptSEED] = Math.Min(fSupply, nutrInfo.fCritDemand[ptSEED]);      // Seed gets absolute priority
                        PastureUtil.XDec(ref fSupply, fPartSupply[ptSEED]);

                        fRelSupply = PastureUtil.Div0(fSupply, nutrInfo.fCritDemand[TOTAL] - nutrInfo.fCritDemand[ptSEED]);
                        for (iPart = ptLEAF; iPart <= ptROOT; iPart++)
                        {
                            fPartSupply[iPart] = fRelSupply * nutrInfo.fCritDemand[iPart];
                        }
                    }
                    else
                    {
                        // Sufficient to meet critical demands
                        for (iPart = ptLEAF; iPart <= ptSEED; iPart++)
                        {
                            if (nutrInfo.fMaxDemand[TOTAL] > nutrInfo.fCritDemand[TOTAL])
                            {
                                fPropn = (nutrInfo.fMaxDemand[iPart] - nutrInfo.fCritDemand[iPart]) / (nutrInfo.fMaxDemand[TOTAL] - nutrInfo.fCritDemand[TOTAL]);
                            }
                            else
                            {
                                fPropn = nutrInfo.fCritDemand[iPart] / nutrInfo.fCritDemand[TOTAL];
                            }

                            fPartSupply[iPart] = nutrInfo.fCritDemand[iPart] + fPropn * (fSupply - nutrInfo.fCritDemand[TOTAL]);
                        }
                    }

                    for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                    {
                        // Subdivide the nutrient supply to each plant part
                        for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FLowestDMDClass[iPart]; iDMD++)
                        {
                            if (this.FPartNetGrowth[iPart] > 0.0)
                            {
                                fPropn = this.NewShootFraction(iPart, iDMD);
                            }
                            else
                            {
                                fPropn = this.PartFraction(iPart, iDMD);
                            }

                            this.FShootNetGrowth[iPart, iDMD].Nu[(int)Elem] = fPropn * fPartSupply[iPart];
                        }
                    }

                    for (iAge = EFFR; iAge <= OLDR; iAge++)
                    {
                        for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
                        {
                            if (this.FPartNetGrowth[ptROOT] > 0.0)
                            {
                                fPropn = this.FNewRootDistn[iAge, iLayer];
                            }
                            else
                            {
                                fPropn = this.PartFraction(ptROOT, iAge, iLayer);
                            }

                            this.FRootNetGrowth[iAge, iLayer].Nu[(int)Elem] = fPropn * fPartSupply[ptROOT];
                        }
                    }

                    this.FSeedNetGrowth.Nu[(int)Elem] = fPartSupply[ptSEED];
                }
            }

            if (this.Owner.FElements.Contains(TPlantElement.N))
            {
                // Ash alkalinity increments
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = this.FHighestDMDClass[iPart]; iDMD <= this.FLowestDMDClass[iPart]; iDMD++)
                    {
                        this.FShootNetGrowth[iPart, iDMD].AshAlk = this.Params.AshAlkK[iPart] * Math.Max(0.0, this.FShootNetGrowth[iPart, iDMD].DM);
                    }
                }

                for (iAge = EFFR; iAge <= OLDR; iAge++)
                {
                    for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
                    {
                        this.FRootNetGrowth[iAge, iLayer].AshAlk = this.Params.AshAlkK[ptROOT] * Math.Max(0.0, this.FRootNetGrowth[iAge, iLayer].DM);
                    }
                }

                this.FSeedNetGrowth.AshAlk = this.Params.AshAlkK[ptSEED] * Math.Max(0.0, this.FSeedNetGrowth.DM);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        public void LoseExcessNutrients(TPlantElement elem)
        {
            double fLoss;
            int iPart, iDMD;

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                if (this.FPartNetGrowth[iPart] <= 0.0)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if (this.Herbage[iPart, iDMD].Nu[(int)elem] > 0.0)
                        {
                            fLoss = Math.Max(0.0, this.Herbage[iPart, iDMD].Nu[(int)elem] - this.FNutrientInfo[(int)elem].fMaxShootConc[iPart, iDMD] * this.Herbage[iPart, iDMD].DM);
                            PastureUtil.XDec(ref this.Herbage[iPart, iDMD].Nu[(int)elem], fLoss);
                            if (elem == TPlantElement.N)
                            {
                                PastureUtil.XInc(ref this.FNutrientInfo[(int)elem].fGaseousLoss, fLoss);
                            }
                            else
                            {
                                PastureUtil.XInc(ref this.FNutrientInfo[(int)elem].fLeached[iPart, iDMD], fLoss);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Traps nutrient uptake allocated to roots by senescing plants on the day of
        /// senescence and moves it to the established cohort (where the root DM has
        /// already been shifted)
        /// </summary>
        /// <param name="elem"></param>
        public void TransferSenescedNutrients(TPlantElement elem)
        {
            int iEstab;
            TPastureCohort estabCohort;
            int iAge;
            int iLayer;

            if (!this.Params.bAnnual && (this.Status == stSENC))
            {
                iEstab = this.Owner.FindCohort(stESTAB);
                if (iEstab >= 0)
                {
                    estabCohort = this.Owner.FCohorts[iEstab];
                    for (iAge = EFFR; iAge <= OLDR; iAge++)
                    {
                        for (iLayer = 1; iLayer <= MaxSoilLayers; iLayer++)
                        {
                            if ((this.Roots[iAge, iLayer].Nu[(int)elem] > 0.0) && (this.Roots[iAge, iLayer].DM == 0.0))
                            {
                                if ((this.Roots[iAge, iLayer].Nu[(int)elem] > 1.0E-5) || (estabCohort.Roots[iAge, iLayer].DM > 0.0))
                                {
                                    estabCohort.Roots[iAge, iLayer].Nu[(int)elem] = estabCohort.Roots[iAge, iLayer].Nu[(int)elem] + this.Roots[iAge, iLayer].Nu[(int)elem];
                                }

                                this.Roots[iAge, iLayer].Nu[(int)elem] = 0.0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="age"></param>
        /// <param name="layer"></param>
        /// <param name="deadPool"></param>
        public void DoDeadRootNutrients(TPlantElement elem, int age, int layer, ref DM_Pool deadPool)
        {
            double fPropn;
            double fExcess;

            if ((!this.Owner.FElements.Contains(elem)) || (this.Roots[age, layer].DM <= 0.0) || (deadPool.DM <= 0.0))
            {
                deadPool.Nu[(int)elem] = 0.0;
            }
            else
            {
                fPropn = this.Roots[age, layer].Nu[(int)elem] * Math.Min(1.0, deadPool.DM / this.Roots[age, layer].DM);
                fExcess = Math.Max(0.0, this.Roots[age, layer].Nu[(int)elem] - this.Params.NutrConcK[1, (int)elem, ptROOT] * this.Roots[age, layer].DM);
                deadPool.Nu[(int)elem] = Math.Max(fPropn, fExcess);
            }

            if ((elem == TPlantElement.N) && (this.Owner.FElements.Contains(elem)))
            {
                if (this.Roots[age, layer].Nu[(int)TPlantElement.N] > 0.0)
                {
                    deadPool.AshAlk = this.Roots[age, layer].AshAlk * (deadPool.Nu[(int)TPlantElement.N] / this.Roots[age, layer].Nu[(int)TPlantElement.N]);
                }
                else
                {
                    deadPool.AshAlk = 0.0;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double NutrLimit(TPlantElement elem)
        {
            double result;
            double fNPPScale;
            double fGrossPP;

            if (!this.Owner.FElements.Contains(TPlantElement.N) && (elem == TPlantElement.N))
            {
                fNPPScale = this.Owner.FFertScalar;
            }
            else if ((this.Owner.FElements.Length == 1 && this.Owner.FElements.Contains(TPlantElement.N)) && (elem == TPlantElement.P))
            {
                fNPPScale = this.Owner.FFertScalar;
            }
            else if ((this.Owner.FElements.Length == 2 && this.Owner.FElements.Contains(TPlantElement.P) && this.Owner.FElements.Contains(TPlantElement.N)) && (elem == TPlantElement.S))
            {
                fNPPScale = this.Owner.FFertScalar;
            }
            else if (this.Owner.FElements.Contains(elem) && (this.FNutrientInfo[(int)elem].fCritDemand[TOTAL] > 0.0))
            {
                fNPPScale = Math.Max(0.0, Math.Min(this.FNutrientInfo[(int)elem].fSupplied / this.FNutrientInfo[(int)elem].fCritDemand[TOTAL], 1.0));
            }
            else
            {
                fNPPScale = 1.0;
            }

            fGrossPP = this.FPotAssimilation + this.FPotRootTranslocSum + this.FPotStemTranslocSum;
            if ((fNPPScale < 1.0) && (fGrossPP > this.fMaintRespiration[TOTAL]))
            {
                result = fNPPScale + (1.0 - fNPPScale) * (this.fMaintRespiration[TOTAL] / fGrossPP);
            }
            else
            {
                result = 1.0;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double StemNutrLimit(TPlantElement elem)
        {
            double stemTranslocSum_;
            int iDMD;
            double result;

            if (this.Owner.FElements.Length == 0)
            {
                result = 1.0;
            }
            else
            {
                stemTranslocSum_ = 0.0;
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    stemTranslocSum_ += this.FPotStemTransloc[iDMD];
                }

                if ((stemTranslocSum_ > 0.0) && (this.FNutrientInfo[(int)elem].fCritDemand[ptSEED] > 0))
                {
                    result = Math.Min(1.0, this.FNutrientInfo[(int)elem].fSupplied / this.FNutrientInfo[(int)elem].fCritDemand[ptSEED]);
                }
                else
                {
                    result = 1.0;
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="nutr"></param>
        /// <returns></returns>
        public double[][] NutrUptake(TPlantNutrient nutr)
        {
            return (double[][]) this.FNutrientInfo[(int)PastureUtil.Nutr2Elem[(int)nutr]].fUptake[(int)nutr].Clone();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double[] CationUptake()
        {
            double[] Result = new double[MaxSoilLayers + 1];
            double fAAProduced;
            double[] fRootFract = new double[MaxSoilLayers + 1];
            double[] fLayerScale = new double[MaxSoilLayers + 1];
            double fScaleSum;
            int iLayer;

            fAAProduced = this.Params.AshAlkK[ptLEAF] * this.FPartNetGrowth[ptLEAF]
                         + this.Params.AshAlkK[ptSTEM] * (this.FPartNetGrowth[ptSTEM] - this.StemTranslocSum)
                         + this.Params.AshAlkK[ptROOT] * (this.FPartNetGrowth[ptROOT] - this.RootTranslocSum)
                         + this.Params.AshAlkK[ptSEED] * this.FPartNetGrowth[ptSEED];

            fScaleSum = 0.0;
            for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
            {
                fRootFract[iLayer] = PastureUtil.Div0(this.Roots[EFFR, iLayer].DM, this.Roots[EFFR, TOTAL].DM);
                fLayerScale[iLayer] = fRootFract[iLayer] * PastureUtil.RAMP(this.Owner.Inputs.pH[iLayer], this.Params.AshAlkK[5], this.Params.AshAlkK[6]);
                fScaleSum += fLayerScale[iLayer];
            }

            for (iLayer = 1; iLayer <= MaxSoilLayers; iLayer++)
            {
                if (fScaleSum > 0.0)
                {
                    fLayerScale[iLayer] = fLayerScale[iLayer] / fScaleSum;
                }
                else
                {
                    fLayerScale[iLayer] = fRootFract[iLayer];
                }
            }

            for (iLayer = 1; iLayer <= MaxSoilLayers; iLayer++)
            {
                Result[iLayer] = fAAProduced * fLayerScale[iLayer];
            }

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GaseousLoss(TPlantElement elem)
        {
            return this.FNutrientInfo[(int)elem].fGaseousLoss;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double CO2_SpecLeafArea()
        {
            double result;
            if (this.Owner.Inputs.CO2_PPM > 0)
            {
                result = 1.0 - this.Params.LightK[6] * (this.Owner.Inputs.CO2_PPM / GrazEnv.REFERENCE_CO2 - 1.0);
            }
            else
            {
                result = 1.0;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double CO2_NutrConc(int part, TPlantElement elem)
        {
            double result;
            if (this.Owner.Inputs.CO2_PPM <= 0)
            {
                result = 1.0;
            }
            else if (part == ptLEAF)
            {
                result = (1.0 - this.Params.NutrCO2K[(int)elem, part] * (this.Owner.Inputs.CO2_PPM / GrazEnv.REFERENCE_CO2 - 1.0)) * this.CO2_SpecLeafArea();
            }
            else
            {
                result = (1.0 - this.Params.NutrCO2K[(int)elem, part] * (this.Owner.Inputs.CO2_PPM / GrazEnv.REFERENCE_CO2 - 1.0));
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="flux"></param>
        /// <param name="priorFluxes"></param>
        /// <returns></returns>
        public double LimitedFlux(DM_Pool source, double flux, ref double priorFluxes)
        {
            double result = Math.Max(0.0, Math.Min(flux, source.DM - priorFluxes));
            priorFluxes += result;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propn"></param>
        /// <param name="priorFluxes"></param>
        /// <returns></returns>
        public double LimitedPropn(DM_Pool source, double propn, ref double priorFluxes)
        {
            double result = Math.Max(0.0, Math.Min(propn * source.DM, source.DM - priorFluxes));
            priorFluxes += result;

            return result;
        }

        /// <summary>
        /// This method carries out the following flows, in order:
        /// - translocation out of roots and stems               fTranslocated
        /// - grazing removal                                  fRemoved
        /// - plant and tissue death, litterfall and degradation   fFluxOver
        /// - DMD decline                                      fFluxDown
        /// - root aging                                       fRootFlux
        /// - root loss and recycling                          fRootFlux
        /// - addition of new growth (incl. new seeds)         fNewGrowth
        /// This method does *not* carry out the following flows:
        /// - addition of newly germinating seedlings
        /// - removal through conservation or killing events
        /// - establishment, senescence or resprouting at the commencement of the
        ///   phenological cycle
        /// </summary>
        /// <param name="newSeeds"></param>
        public void UpdateState(ref DM_Pool newSeeds)
        {
            bool bGreenCohort;
            DM_Pool nutrSupply = new DM_Pool();
            double[] digDMFlux = new double[HerbClassNo + 1];
            double respireLoss;
            double digDMLoss;
            double DMDFlux;
            double[][] grazed = new double[ptSTEM + 1][];                      // [ptLEAF..ptSTEM] of HerbageArray;
            double[,] fluxOver = new double[ptSTEM + 1, HerbClassNo + 1];      // [ptLEAF..ptSTEM,1..HerbClassNo]
            double[,] fluxDown = new double[ptSTEM + 1, HerbClassNo + 1];      // [ptLEAF..ptSTEM,1..HerbClassNo]
            double[,] fluxCO2 = new double[ptSTEM + 1, HerbClassNo + 1];       // [ptLEAF..ptSTEM,1..HerbClassNo]
            double[,] rootsDying = new double[OLDR + 1, MaxSoilLayers + 1];    // [EFFR..OLDR,1..MaxSoilLayers]
            double[] rootsAging = new double[MaxSoilLayers + 1];               // [1..
            double[] rootsRenewed = new double[MaxSoilLayers + 1];             // [1..
            int iOver;

            double area;
            double priorFlux = 0;
            DM_Pool ExitDM = new DM_Pool();
            int iPart, iDMD;
            int iAge, iLayer;

            for (int i = 0; i <= ptSTEM; i++)
            {
                grazed[i] = new double[HerbClassNo + 1];
            }

            bGreenCohort = (this.Status == stSEEDL) || (this.Status == stESTAB) || (this.Status == stSENC);

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                // Amount moving to next lower DMD class when a transfer of digestible DM is made
                digDMFlux[iDMD] = (1.0 - PastureUtil.HerbageDMD[iDMD]) / PastureUtil.CLASSWIDTH;
            }

            if ((this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC) && (this.FPartNetGrowth[ptROOT] > 0.0))
            {
                this.RootDepth += this.FRootExtension[this.FMaxRootLayer];
            }

            if (this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC)
            {
                this.FrostFactor += this.FDeltaFrost;                                                                 // Also updated in addSeedlings()
            }

            if (this.Status == stSEEDL)
            {
                this.SeedlStress += this.FDelta_SI;                                                                   // Also updated in addSeedlings()
            }
            else
            {
                this.SeedlStress = 0.0;
            }

            if ((this.Status == stSEEDL) && (this.Herbage[TOTAL, TOTAL].DM > 0.0))                                    // Also updated in addSeedlings()
            {
                this.EstabIndex *= (1.0 + Math.Max(0.0, this.FPartNetGrowth[ptLEAF] + this.FPartNetGrowth[ptSTEM])
                                                     / this.Herbage[TOTAL, TOTAL].DM);
            }
            else if (this.Status == stSEEDL)
            {
                this.EstabIndex = 1.0;
            }
            else
            {
                this.EstabIndex = 0.0;
            }

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    DMDFlux = this.FDMDDecline[iPart, iDMD] / PastureUtil.CLASSWIDTH;                      // Flux due to tissue aging (g/g/d)
                    respireLoss = Math.Max(0.0, -this.FShootNetGrowth[iPart, iDMD].DM);                         // Respiratory losses (g/m^2/d) including losses in dry herbage

                    if (iPart == ptSTEM)
                    {
                        // Rate at which digestible DM leaves this pool (g/m^2/d)
                        digDMLoss = respireLoss + this.FStemTransloc[iDMD];
                    }
                    else
                    {
                        digDMLoss = respireLoss;
                    }

                    if (!bGreenCohort)
                    {
                        priorFlux = 0.0;
                        fluxCO2[iPart, iDMD] = this.LimitedFlux(this.Herbage[iPart, iDMD], respireLoss, ref priorFlux);
                    }
                    else
                    {
                        priorFlux = respireLoss;
                        fluxCO2[iPart, iDMD] = 0.0;
                    }

                    if (iPart == ptSTEM)
                    {
                        this.FStemTransloc[iDMD] = this.LimitedFlux(this.Herbage[iPart, iDMD], this.FStemTransloc[iDMD], ref priorFlux);
                    }

                    grazed[iPart][iDMD] = this.LimitedPropn(this.Herbage[iPart, iDMD], this.Owner.FGrazedPropn[this.Status, iPart, iDMD], ref priorFlux);
                    fluxOver[iPart, iDMD] = this.LimitedPropn(this.Herbage[iPart, iDMD], this.FShootLossRate[iPart, iDMD], ref priorFlux);
                    fluxDown[iPart, iDMD] = this.LimitedPropn(this.Herbage[iPart, iDMD], DMDFlux, ref priorFlux);
                    if ((this.Status == stSEEDL || this.Status == stESTAB || this.Status == stSENC) && (iDMD < this.FLowestDMDClass[iPart]))
                    {
                        fluxDown[iPart, iDMD] = fluxDown[iPart, iDMD] + this.LimitedFlux(this.Herbage[iPart, iDMD], digDMLoss * digDMFlux[iDMD], ref priorFlux);
                    }

                    if (((this.Status == stLITT1) || (this.Status == stLITT2)) && (iDMD >= this.FLowestDMDClass[iPart]))
                    {
                        fluxOver[iPart, iDMD] = fluxOver[iPart, iDMD] + fluxDown[iPart, iDMD];
                        fluxDown[iPart, iDMD] = 0.0;
                    }
                }
            }

            for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
            {
                for (iAge = EFFR; iAge <= OLDR; iAge++)
                {
                    respireLoss = Math.Max(0.0, -this.FRootNetGrowth[iAge, iLayer].DM);

                    priorFlux = respireLoss;
                    this.FRootTransloc[iAge, iLayer] = this.LimitedFlux(this.Roots[iAge, iLayer], this.FRootTransloc[iAge, iLayer], ref priorFlux);
                    rootsDying[iAge, iLayer] = this.LimitedPropn(this.Roots[iAge, iLayer], this.FRootLossRate[iAge], ref priorFlux);
                }

                rootsAging[iLayer] = this.LimitedPropn(this.Roots[EFFR, iLayer], this.FRootAgingRate, ref priorFlux);
                rootsRenewed[iLayer] = this.LimitedPropn(this.Roots[OLDR, iLayer], this.FRootRelocRate, ref priorFlux);
            }

            if (bGreenCohort)
            {
                iOver = this.Owner.FindCohort(stDEAD);
            }
            else if (this.Status == stDEAD)
            {
                iOver = this.Owner.FindCohort(stLITT1);
            }
            else if (this.Status == stLITT1)
            {
                iOver = this.Owner.FindCohort(stLITT2);
            }
            else
            {
                iOver = -1;
            }

            this.RemoveStemReserve(grazed[ptSTEM]);

            if (this.Owner.FElements.Length > 0)
            {
                this.UpdateNutrientFlows(ref nutrSupply);
                this.AllocateNutrientFlows(nutrSupply);
            }

            PastureUtil.ZeroPool(ref ExitDM);

            // Now execute the mass flows
            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = HerbClassNo; iDMD >= 1; iDMD--)
                {
                    if (!bGreenCohort)                                                              // Respiratory losses come first in dry herbage
                    {
                        area = this.SpecificArea[iPart, iDMD] * this.Herbage[iPart, iDMD].DM;
                        this.Herbage[iPart, iDMD].DM = this.Herbage[iPart, iDMD].DM - fluxCO2[iPart, iDMD];
                        if (this.Herbage[iPart, iDMD].DM > 0.0)                                          // Respiratory losses don't change the herbage area
                        {
                            this.SpecificArea[iPart, iDMD] = area / this.Herbage[iPart, iDMD].DM;
                        }
                    }

                    if (iPart == ptSTEM)                                                            // Translocated DM is already incorporated into F*NetGrowth;
                    {
                        this.Herbage[iPart, iDMD].DM = this.Herbage[iPart, iDMD].DM - this.FStemTransloc[iDMD];
                    }

                    // nutrients were moved in AllocateNutrientFlows()
                    this.Owner.MovePool(grazed[iPart][iDMD], ref this.Herbage[iPart, iDMD], ref ExitDM);

                    TPastureCohort pastureCohort = null;
                    if (iDMD < HerbClassNo)
                    {
                        this.MoveHerbage(iPart, iDMD, fluxDown[iPart, iDMD], ref pastureCohort, iDMD + 1);
                    }

                    if (this.Status != stLITT2)
                    {
                        this.MoveHerbage(iPart, iDMD, fluxOver[iPart, iDMD], ref this.Owner.FCohorts[iOver], iDMD);
                    }
                    else
                    {
                        this.Owner.MoveToResidue(fluxOver[iPart, iDMD],
                                             ref this.Herbage[iPart, iDMD],
                                             this.Status, iPart, iDMD);
                    }

                    if (bGreenCohort)
                    {
                        this.AddHerbage(iPart, iDMD, ref this.FShootNetGrowth[iPart, iDMD], this.FNewSpecificArea[iPart]);
                    }

                    this.FBiomassExitGM2[iPart, iDMD] = fluxOver[iPart, iDMD];
                    if (!bGreenCohort)
                    {
                        this.FBiomassRespireGM2[iPart, iDMD] = fluxCO2[iPart, iDMD];
                    }
                    else
                    {
                        this.FBiomassRespireGM2[iPart, iDMD] = 0.0; // respiration of green is accounted for in FShootNetGrowth
                    }
                }
            }

            for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
            {
                for (iAge = EFFR; iAge <= OLDR; iAge++)
                {
                    if (this.FRootNetGrowth[iAge, iLayer].DM < 0.0)
                    {
                        this.Owner.AddPool(this.FRootNetGrowth[iAge, iLayer], ref this.Roots[iAge, iLayer], true);
                    }

                    this.Roots[iAge, iLayer].DM = this.Roots[iAge, iLayer].DM - this.FRootTransloc[iAge, iLayer];

                    ExitDM.DM = rootsDying[iAge, iLayer];

                    var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
                    foreach (var Elem in values)
                    {
                        this.DoDeadRootNutrients(Elem, iAge, iLayer, ref ExitDM);
                    }

                    AddDMPool(MultiplyDMPool(ExitDM, -1.0), this.Roots[iAge, iLayer]);            // Subtract ExitDM from Roots[]
                    this.Owner.MoveToResidue(ExitDM.DM, ref ExitDM, this.Status, ptROOT, TOTAL, iLayer);
                }

                this.Owner.MovePool(rootsAging[iLayer], ref this.Roots[EFFR, iLayer], ref this.Roots[OLDR, iLayer]);
                this.Owner.MovePool(rootsRenewed[iLayer], ref this.Roots[OLDR, iLayer], ref this.Roots[EFFR, iLayer]);

                for (iAge = EFFR; iAge <= OLDR; iAge++)
                {
                    if (this.FRootNetGrowth[iAge, iLayer].DM >= 0.0)
                    {
                        this.Owner.AddPool(this.FRootNetGrowth[iAge, iLayer], ref this.Roots[iAge, iLayer], true);
                    }
                }
            }

            this.Owner.AddPool(this.FSeedNetGrowth, ref newSeeds, true);

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    PastureUtil.ZeroRoundOff(ref this.Herbage[iPart, iDMD]);
                }
            }

            for (iAge = EFFR; iAge <= OLDR; iAge++)
            {
                for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
                {
                    PastureUtil.ZeroRoundOff(ref this.Roots[iAge, iLayer]);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void ComputeTotals()
        {
            int iPart;
            int iDMD;
            int iAge;
            int iLayer;

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                PastureUtil.ZeroPool(ref this.Herbage[TOTAL, iDMD]);
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    this.Owner.AddPool(this.Herbage[iPart, iDMD], ref this.Herbage[TOTAL, iDMD]);
                }
            }

            for (iPart = TOTAL; iPart <= ptSTEM; iPart++)
            {
                PastureUtil.ZeroPool(ref this.Herbage[iPart, TOTAL]);
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    this.Owner.AddPool(this.Herbage[iPart, iDMD], ref this.Herbage[iPart, TOTAL]);
                }
            }

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                this.SpecificArea[iPart, TOTAL] = 0.0;
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    this.SpecificArea[iPart, TOTAL] = this.SpecificArea[iPart, TOTAL] + (this.SpecificArea[iPart, iDMD] * this.Herbage[iPart, iDMD].DM);
                }

                this.SpecificArea[iPart, TOTAL] = PastureUtil.Div0(this.SpecificArea[iPart, TOTAL], this.Herbage[iPart, TOTAL].DM);
            }

            for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
            {
                PastureUtil.ZeroPool(ref this.Roots[TOTAL, iLayer]);
                for (iAge = EFFR; iAge <= OLDR; iAge++)
                {
                    this.Owner.AddPool(this.Roots[iAge, iLayer], ref this.Roots[TOTAL, iLayer]);
                }
            }

            for (iAge = TOTAL; iAge <= OLDR; iAge++)
            {
                PastureUtil.ZeroPool(ref this.Roots[iAge, TOTAL]);
                for (iLayer = 1; iLayer <= this.Owner.FSoilLayerCount; iLayer++)
                {
                    this.Owner.AddPool(this.Roots[iAge, iLayer], ref this.Roots[iAge, TOTAL]);
                }
            }
        }

        /// <summary>
        /// Remove part of a single herbage pool, ensuring that the nutrient contents
        /// and ash alkalinity are populated
        /// * The fHerbageNutrConc[] property and fAshAlkalinity() function contain
        ///   logic for building default quality values
        /// </summary>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <param name="cutPropn"></param>
        /// <returns></returns>
        public DM_Pool CutHerbage(int part, int DMD, double cutPropn)
        {
            DM_Pool result = new DM_Pool();

            PastureUtil.ZeroPool(ref result);
            this.Owner.MovePoolPropn(cutPropn, ref this.Herbage[part, DMD], ref result);

            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
            foreach (var Elem in values)
            {
                if (!this.Owner.ElementSet.Contains(Elem))
                {
                    result.Nu[(int)Elem] = result.DM * this.Owner.GetHerbageConc(this.Status, part, DMD, Elem);
                }
            }

            if (!this.Owner.ElementSet.Contains(TPlantElement.N))
            {
                result.AshAlk = result.DM * this.Owner.HerbageAshAlkalinity(this.Status, part, DMD);
            }

            return result;
        }

    }
}
