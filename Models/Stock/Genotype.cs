namespace Models.GrazPlan
{
    using Models.Core;
    using Models.Core.Run;
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>Encapsulates a parameter set for an animal.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class Genotype : Model
    {
        private const double DAYSPERYR = 365.25;
        private const double NC = 2.5;                                      // 2.5 cycles joining is assumed            

        /// <summary>The breed ancestry of the parameter set.</summary>
        private Ancestry[] FParentage = new Ancestry[0];

        // ----------------- Constructors ----------------------

        /// <summary>Construct an animal parameter set.</summary>
        public Genotype()
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
        }

        /// <summary>Construct an animal parameter set from a source parameters set.</summary>
        /// <param name="srcSet">The source parameter set.</param>
        public Genotype(Genotype srcSet)
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            CopyParams(srcSet);

            if (srcSet != null)
                Initialise();
        }

        /// <summary>
        /// Constructor for a breed cross. Blend two animal parameter sets.
        /// </summary>
        /// <param name="nameOfNewGenotype">The name given to the new parameter set.</param>
        /// <param name="damBreed">The dam breed.</param>
        /// <param name="sireBreed">The sire breed.</param>
        /// <param name="damProportion">The dam proportion.</param>
        /// <param name="sireProportion">The sire proportion.</param>
        /// <returns></returns>
        public Genotype(string nameOfNewGenotype, Genotype damBreed, Genotype sireBreed, double damProportion, double sireProportion)
        {
            sEditor = damBreed.sEditor;
            sEditDate = damBreed.sEditDate;
            Animal = damBreed.Animal;
            bDairyBreed = damBreed.bDairyBreed;
            MaxYoung = damBreed.MaxYoung;
            OvulationPeriod = damBreed.OvulationPeriod;
            Puberty = damBreed.Puberty;

            BreedSRW = damProportion * damBreed.BreedSRW + sireProportion * sireBreed.BreedSRW;
            PotFleeceWt = damProportion * damBreed.PotFleeceWt + sireProportion * sireBreed.PotFleeceWt;
            FDairyIntakePeak = damProportion * damBreed.FDairyIntakePeak + sireProportion * sireBreed.FDairyIntakePeak;
            FleeceRatio = damProportion * damBreed.FleeceRatio + sireProportion * sireBreed.FleeceRatio;
            MaxFleeceDiam = damProportion * damBreed.MaxFleeceDiam + sireProportion * sireBreed.MaxFleeceDiam;
            PeakMilk = damProportion * damBreed.PeakMilk + sireProportion * sireBreed.PeakMilk;
            for (int idx = 1; idx <= 2; idx++)
                MortRate[idx] = damProportion * damBreed.MortRate[idx] + sireProportion * sireBreed.MortRate[idx];
            for (int idx = 1; idx <= 2; idx++)
                MortAge[idx] = damProportion * damBreed.MortAge[idx] + sireProportion * sireBreed.MortAge[idx];
            MortIntensity = damProportion * damBreed.MortIntensity + sireProportion * sireBreed.MortIntensity;
            MortCondConst = damProportion * damBreed.MortCondConst + sireProportion * sireBreed.MortCondConst;
            MortWtDiff = damProportion * damBreed.MortWtDiff + sireProportion * sireBreed.MortWtDiff;

            for (int idx = 0; idx < SRWScalars.Length; idx++) SRWScalars[idx] = damProportion * damBreed.SRWScalars[idx] + sireProportion * sireBreed.SRWScalars[idx];
            for (int idx = 1; idx < GrowthC.Length; idx++) GrowthC[idx] = damProportion * damBreed.GrowthC[idx] + sireProportion * sireBreed.GrowthC[idx];
            for (int idx = 1; idx < IntakeC.Length; idx++) IntakeC[idx] = damProportion * damBreed.IntakeC[idx] + sireProportion * sireBreed.IntakeC[idx];
            for (int idx = 0; idx < IntakeLactC.Length; idx++) IntakeLactC[idx] = damProportion * damBreed.IntakeLactC[idx] + sireProportion * sireBreed.IntakeLactC[idx];
            for (int idx = 1; idx < GrazeC.Length; idx++) GrazeC[idx] = damProportion * damBreed.GrazeC[idx] + sireProportion * sireBreed.GrazeC[idx];
            for (int idx = 1; idx < EfficC.Length; idx++) EfficC[idx] = damProportion * damBreed.EfficC[idx] + sireProportion * sireBreed.EfficC[idx];
            for (int idx = 1; idx < MaintC.Length; idx++) MaintC[idx] = damProportion * damBreed.MaintC[idx] + sireProportion * sireBreed.MaintC[idx];
            for (int idx = 1; idx < DgProtC.Length; idx++) DgProtC[idx] = damProportion * damBreed.DgProtC[idx] + sireProportion * sireBreed.DgProtC[idx];
            for (int idx = 1; idx < ProtC.Length; idx++) ProtC[idx] = damProportion * damBreed.ProtC[idx] + sireProportion * sireBreed.ProtC[idx];
            for (int idx = 1; idx < PregC.Length; idx++) PregC[idx] = damProportion * damBreed.PregC[idx] + sireProportion * sireBreed.PregC[idx];
            for (int idx = 1; idx < PregScale.Length; idx++) PregScale[idx] = damProportion * damBreed.PregScale[idx] + sireProportion * sireBreed.PregScale[idx];
            for (int idx = 1; idx < BirthWtScale.Length; idx++) BirthWtScale[idx] = damProportion * damBreed.BirthWtScale[idx] + sireProportion * sireBreed.BirthWtScale[idx];
            for (int idx = 1; idx < PeakLactC.Length; idx++) PeakLactC[idx] = damProportion * damBreed.PeakLactC[idx] + sireProportion * sireBreed.PeakLactC[idx];
            for (int idx = 1; idx < LactC.Length; idx++) LactC[idx] = damProportion * damBreed.LactC[idx] + sireProportion * sireBreed.LactC[idx];
            for (int idx = 1; idx < WoolC.Length; idx++) WoolC[idx] = damProportion * damBreed.WoolC[idx] + sireProportion * sireBreed.WoolC[idx];
            for (int idx = 1; idx < ChillC.Length; idx++) ChillC[idx] = damProportion * damBreed.ChillC[idx] + sireProportion * sireBreed.ChillC[idx];
            for (int idx = 1; idx < GainC.Length; idx++) GainC[idx] = damProportion * damBreed.GainC[idx] + sireProportion * sireBreed.GainC[idx];
            for (int idx = 1; idx < PhosC.Length; idx++) PhosC[idx] = damProportion * damBreed.PhosC[idx] + sireProportion * sireBreed.PhosC[idx];
            for (int idx = 1; idx < SulfC.Length; idx++) SulfC[idx] = damProportion * damBreed.SulfC[idx] + sireProportion * sireBreed.SulfC[idx];
            for (int idx = 1; idx < MethC.Length; idx++) MethC[idx] = damProportion * damBreed.MethC[idx] + sireProportion * sireBreed.MethC[idx];
            for (int idx = 1; idx < AshAlkC.Length; idx++) AshAlkC[idx] = damProportion * damBreed.AshAlkC[idx] + sireProportion * sireBreed.AshAlkC[idx];
            for (int idx = 1; idx < DayLengthConst.Length; idx++) DayLengthConst[idx] = damProportion * damBreed.DayLengthConst[idx] + sireProportion * sireBreed.DayLengthConst[idx];
            for (int idx = 0; idx < ToxaemiaSigs.Length; idx++) ToxaemiaSigs[idx] = damProportion * damBreed.ToxaemiaSigs[idx] + sireProportion * sireBreed.ToxaemiaSigs[idx];
            for (int idx = 0; idx < DystokiaSigs.Length; idx++) DystokiaSigs[idx] = damProportion * damBreed.DystokiaSigs[idx] + sireProportion * sireBreed.DystokiaSigs[idx];
            for (int idx = 0; idx < ExposureConsts.Length; idx++) ExposureConsts[idx] = damProportion * damBreed.ExposureConsts[idx] + sireProportion * sireBreed.ExposureConsts[idx];

            FertWtDiff = damProportion * damBreed.FertWtDiff + sireProportion * sireBreed.FertWtDiff;
            SelfWeanPropn = damProportion * damBreed.SelfWeanPropn + sireProportion * sireBreed.SelfWeanPropn;
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            for (int idx = 1; idx < ConceiveSigs.Length; idx++)
                for (int Jdx = 0; Jdx < ConceiveSigs[idx].Length; Jdx++)
                    ConceiveSigs[idx][Jdx] = damProportion * damBreed.ConceiveSigs[idx][Jdx] + sireProportion * sireBreed.ConceiveSigs[idx][Jdx];

            for (int idx = 0; idx <= FParentage.Length - 1; idx++)
                FParentage[idx].fPropn = 0.0;

            SetParentage(damBreed, damProportion);
            SetParentage(sireBreed, sireProportion);

            if (!string.IsNullOrEmpty(nameOfNewGenotype))
                Name = nameOfNewGenotype;
            else if (FParentage.Length == 1)
                Name = FParentage[0].sBaseBreed;
            else if (FParentage.Length > 1)
            {
                int iDecPlaces = 0;
                for (int Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                {
                    if ((FParentage[Idx].fPropn > 0.0005) && (FParentage[Idx].fPropn <= 0.05))
                        iDecPlaces = 1;
                }

                Name = "";
                for (int Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                {
                    if (FParentage[Idx].fPropn > 0.0005)
                    {
                        if (Name != "")
                            Name = Name + ", ";
                        Name = Name + FParentage[Idx].sBaseBreed + " "
                                         + String.Format("{0:0." + new String('0', iDecPlaces) + "}", 100.0 * FParentage[Idx].fPropn) + "%";
                    }
                }
            }
        }

        /// <summary>
        /// Construct a parameter set from a set of parameter values.
        /// </summary>
        /// <param name="name">Name of the animal parameter set.</param>
        /// <param name="animalTypeString">The animal type.</param>
        /// <param name="parameters">The parameter values to apply.</param>
        public Genotype(string name, string animalTypeString, List<PropertyReplacement> parameters)
        {
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            Name = name;
            if (animalTypeString == "cattle")
                Animal = GrazType.AnimalType.Cattle;
            else if (animalTypeString == "sheep")
                Animal = GrazType.AnimalType.Sheep;
            parameters.ForEach(o => o.Replace(this));
            PotFleeceWt = FleeceRatio * BreedSRW;
            SetPeakMilk(IntakeC[11] * BreedSRW);
        }

        // ----------------- User parameters ----------------------

        /// <summary>Editor of the parameters</summary>
        [Description("Editor")]
        public string sEditor { get; set; } = string.Empty;

        /// <summary>Date edited</summary>
        [Description("Date edited")]
        public string sEditDate { get; set; } = string.Empty;

        /// <summary>Animal type</summary>
        [Description("Animal type")]
        [Units("-")]
        public GrazType.AnimalType Animal { get; set; }

        /// <summary>Dairy intake peak (c-idy-0)</summary>
        [Description("Dairy intake peak (c-idy-0)")]
        public double FDairyIntakePeak { get; set; }

        /// <summary>Standard reference weights</summary>
        [Description("SRW Scalars c-srs-")]
        public double[] SRWScalars { get; set; } = new double[2];

        /// <summary>Maximum fleece diameter c-mu- (microns)</summary>
        [Description("Maximum fleece diameter c-mu-")]
        public double MaxFleeceDiam { get; set; }

        /// <summary>Fixed attribute (read in)</summary>
        [Description("Dairy breed?")]
        [Units("-")]
        public bool bDairyBreed { get; set; }

        /// <summary>Background death rate, per day  [1..2]</summary>
        [Description("Background death rate, per day  [1..2] c-d-")]
        [Units("0-1")]
        public double[] MortRate { get; set; } = new double[3];

        /// <summary>Mortality age c-d-</summary>
        [Description("Mortality age c-d-")]
        public double[] MortAge { get; set; } = new double[3];            //[1..2]

        /// <summary>Rate of mortality increase for underweight animals.</summary>
        [Description("Rate of mortality increase for underweight animals c-d-")]
        [Units("0-1")]
        public double MortIntensity { get; set; }

        /// <summary>Fraction of normal body weight in animals of Size=1 at which mortality starts to increase</summary>
        [Description("Fraction of normal body weight in animals of Size=1 at which mortality starts to increase c-d-")]
        public double MortCondConst { get; set; }

        /// <summary>Weight differential in dying animals</summary>
        [Description("Weight differential in dying animals c-d-")]
        public double MortWtDiff { get; set; }

        /// <summary>Growth C c-n-</summary>
        [Description("Growth C c-n-")]
        public double[] GrowthC { get; set; } = new double[5];

        /// <summary>Intake C c-i-</summary>
        [Description("Intake C c-i-")]
        public double[] IntakeC { get; set; } = new double[22];

        /// <summary>Intake Lact C c-imx-</summary>
        [Description("Intake Lact C c-imx-")]
        public double[] IntakeLactC { get; set; } = new double[4];

        /// <summary>Graze C c-r-</summary>
        [Description("Graze C c-r-")]
        public double[] GrazeC { get; set; } = new double[21];

        /// <summary>Effic C c-k</summary>
        [Description("Effic C c-k-")]
        public double[] EfficC { get; set; } = new double[17];

        /// <summary>Maintenance C c-m-</summary>
        [Description("Maintenance C c-m-")]
        public double[] MaintC { get; set; } = new double[18];

        /// <summary>DgProtC c-rd-</summary>
        [Description("DgProtC c-rd-")]
        public double[] DgProtC { get; set; } = new double[9];

        /// <summary>Prot C c-a-</summary>
        [Description("Prot C c-a-")]
        public double[] ProtC { get; set; } = new double[10];

        /// <summary>Preg C c-p-</summary>
        [Description("Preg C c-p-")]
        public double[] PregC { get; set; } = new double[14];

        /// <summary>Preg scale c-p14-</summary>
        [Description("Preg scale c-p14-")]
        public double[] PregScale { get; set; } = new double[4];

        /// <summary>Birth weight scale c-p15</summary>
        [Description("Birth weight scale c-p15-")]
        public double[] BirthWtScale { get; set; } = new double[4];

        /// <summary>Peak Lact C c-l0-</summary>
        [Description("Peak Lact C c-l0-")]
        public double[] PeakLactC { get; set; } = new double[4];

        /// <summary>Lact C c-l-</summary>
        [Description("Lact C c-l-")]
        public double[] LactC { get; set; } = new double[26];

        /// <summary>Wool C c-w-</summary>
        [Description("Wool C c-w-")]
        public double[] WoolC { get; set; } = new double[15];

        /// <summary>Chill C c-c</summary>
        [Description("Chill C c-c-")]
        public double[] ChillC { get; set; } = new double[17];

        /// <summary>Gain C c-g</summary>
        [Description("Gain C c-g-")]
        public double[] GainC { get; set; } = new double[19];

        /// <summary>Phos C c-ph</summary>
        [Description("Phos C c-ph-")]
        public double[] PhosC { get; set; } = new double[16];

        /// <summary>Sulf C c-su-</summary>
        [Description("Sulf C c-su-")]
        public double[] SulfC { get; set; } = new double[5];

        /// <summary>Meth C c-h-</summary>
        [Description("Meth C c-h-")]
        public double[] MethC { get; set; } = new double[8];

        /// <summary>Ash alkalinity C c-aa-</summary>
        [Description("Ash alkalinity C c-aa-")]
        public double[] AshAlkC { get; set; } = new double[4];

        /// <summary>Ovulation period c-f</summary>
        [Description("Ovulation period c-f4")]
        public int OvulationPeriod { get; set; }

        /// <summary>Puberty c-pbt</summary>
        [Description("Puberty c-pbt-")]
        public int[] Puberty { get; set; } = new int[2];                  //array[Boolean]

        /// <summary>Day length constant c-f1</summary>
        [Description("Day length constant c-f1-")]
        public double[] DayLengthConst { get; set; } = new double[4];     //array[1..3]

        /// <summary>Conceive sigs c-f2</summary>
        [Description("Conceive sigs c-f2")]
        public double[] F2
        {
            get
            {
                var f2 = new double[4];
                for (int i = 0; i < 4; i++)
                    f2[i] = ConceiveSigs[i][0];
                return f2;
            }
            set
            {
                for (int i = 0; i < 4; i++)
                    ConceiveSigs[i][0] = value[i];
            }
        }

        /// <summary>Conceive sigs c-f3</summary>
        [Description("Conceive sigs c-f3")]
        public double[] F3
        {
            get
            {
                var f2 = new double[4];
                for (int i = 0; i < 4; i++)
                    f2[i] = ConceiveSigs[i][1];
                return f2;
            }
            set
            {
                for (int i = 0; i < 4; i++)
                    ConceiveSigs[i][1] = value[i];
            }
        }

        /// <summary>ToxaemiaSigss c-d</summary>
        [Description("ToxaemiaSigss c-d")]
        public double[] ToxaemiaSigs { get; set; } = new double[2];       //array[0..1]

        /// <summary>DystokiaSigs c-</summary>
        [Description("DystokiaSigs c-d")]
        public double[] DystokiaSigs { get; set; } = new double[2];       //array[0..1]

        /// <summary>Exposure constants c-d</summary>
        [Description("Exposure constants c-d")]
        public double[] ExposureConsts { get; set; } = new double[4];     //array[0..3]

        /// <summary>Self wean proportion c-swn</summary>
        [Description("Self wean proportion c-swn")]
        public double SelfWeanPropn { get; set; }

        // ------------------ Outputs / States ------------------

        /// <summary>Maximum young</summary>
        [Description("Maximum young")]
        [Units("-")]
        public int MaxYoung { get; set; }

        /// <summary>Breed standard reference weight (kg)</summary>
        [Description("Breed standard reference weight (kg)")]
        [Units("kg")]
        public double BreedSRW { get; set; }

        /// <summary>Potential fleece weight (kg)</summary>
        [Description("Potential fleece weight (kg)")]
        [Units("kg")]
        public double PotFleeceWt { get; set; }

        /// <summary>Potential greasy fleece weight:SRW</summary>
        [Units("-")]
        public double FleeceRatio { get; set; }

        /// <summary>Peak milk</summary>
        [Description("Peak milk")]
        [Units("kg")]
        public double PeakMilk { get; set; }

        /// <summary>ConceiveSigs</summary>
        public double[][] ConceiveSigs { get; set; } = new double[4][];   //[0..3][0..1]

        /// <summary>FertWtDiff</summary>
        public double FertWtDiff { get; set; }

        /// <summary>Fleece yield</summary>
        [Units("0-1")]
        public double FleeceYield { get { return WoolC[3]; } }

        /// <summary>Conception values</summary>
        public double[] Conceptions 
        { 
            get 
            {
                double[] result = new double[4];
                double fCR1 = 0.0;
                int N;

                for (N = 1; N <= MaxYoung; N++)
                {
                    result[N] = ComputeConception(ConceiveSigs[N], N, ref fCR1);
                    result[N] = 1.0E-5 * Math.Round(result[N] / 1.0E-5);
                }
                for (N = 1; N <= MaxYoung - 1; N++)
                {
                    result[N] = result[N] - result[N + 1];
                }
                for (N = MaxYoung + 1; N <= 3; N++)
                    result[N] = 0.0;

                return result;
            } 
        }

        /// <summary>Get gestation</summary>
        public int Gestation { get { return Convert.ToInt32(Math.Round(PregC[1]), CultureInfo.InvariantCulture); } }

        /// <summary>
        /// Initialise the component.
        /// </summary>
        public void Initialise()
        {
            if (Animal == GrazType.AnimalType.Sheep)
                FleeceRatio = PotFleeceWt / BreedSRW;

            if (bDairyBreed)
                SetPeakMilk(PeakMilk);

            if (FParentage.Length == 0)
            {
                Array.Resize(ref FParentage, 1);
                FParentage[0].sBaseBreed = Name;
                FParentage[0].fPropn = 1.0;
            }
            else
            {
                Array.Resize(ref FParentage, FParentage.Length);
                for (int Jdx = 0; Jdx <= FParentage.Length - 1; Jdx++)
                {
                    FParentage[Jdx].sBaseBreed = FParentage[Jdx].sBaseBreed;
                    FParentage[Jdx].fPropn = FParentage[Jdx].fPropn;
                }
            }
        }

        /// <summary>
        /// Initialise this instance with starting values for various parameters
        /// </summary>
        /// <param name="srw">Standard reference weight (kg).</param>
        /// <param name="potentialFleeceWeight">Potential fleece weight (kg).</param>
        /// <param name="maxMicrons">Maximum fleece microns.</param>
        /// <param name="fleeceYield">Fleece yield (kg).</param>
        /// <param name="potMilkYield">Potential milk yield.</param>
        /// <param name="conceptions">Conception values.</param>
        /// <param name="matureDeathRate">Mature animal death rate.</param>
        /// <param name="weanerDeathRate">Weaner death rate.</param>
        public void InitialiseWithParams(double srw = double.NaN, 
                                         double potentialFleeceWeight = double.NaN,
                                         double maxMicrons = double.NaN,
                                         double fleeceYield = double.NaN,
                                         double potMilkYield = double.NaN,
                                         double[] conceptions = null,
                                         double matureDeathRate = double.NaN, 
                                         double weanerDeathRate = double.NaN)
        {
            if (!double.IsNaN(srw))
            {
                BreedSRW = srw;
                PotFleeceWt = FleeceRatio * srw;
                SetPeakMilk(IntakeC[11] * srw);
            }
            if (!double.IsNaN(potentialFleeceWeight))
            {
                PotFleeceWt = potentialFleeceWeight;
                FleeceRatio = PotFleeceWt / BreedSRW;
            }
            if (!double.IsNaN(maxMicrons))
                MaxFleeceDiam = maxMicrons;
            if (!double.IsNaN(fleeceYield))
                WoolC[3] = fleeceYield;
            if (!double.IsNaN(potMilkYield))
                SetPeakMilk(potMilkYield);
            if (conceptions != null && conceptions.Length == 4 && !double.IsNaN(conceptions[0]))
                SetConceptions(conceptions);
            if (!double.IsNaN(matureDeathRate))
            {
                if (1.0 - matureDeathRate < 0)
                    throw new Exception("Power of negative number attempted in setting mature death rate.");

                MortRate[1] = 1.0 - Math.Pow(1.0 - matureDeathRate, 1.0 / DAYSPERYR);
            }
            if (!double.IsNaN(weanerDeathRate))
            {
                if (1.0 - weanerDeathRate < 0)
                    throw new Exception("Power of negative number attempted in setting weaner death rate.");

                MortRate[2] = 1.0 - Math.Pow(1.0 - weanerDeathRate, 1.0 / DAYSPERYR);
            }
        }

        /// <summary>Calculate some derived parameters.</summary>
        public void DeriveParams()
        {
            MaxYoung = 1;
            while ((MaxYoung < 3) && (BirthWtScale[MaxYoung + 1] > 0.0))
                MaxYoung++;

            PotFleeceWt = BreedSRW * FleeceRatio;
            if (Animal == GrazType.AnimalType.Cattle)
                PeakMilk = IntakeC[11] * BreedSRW;

            if (GrazeC[20] == 0.0)
                GrazeC[20] = 11.5;
        }

        /// <summary>
        /// Count of parents
        /// </summary>
        /// <returns>The count of parents</returns>
        public int ParentageCount()
        {
            int result;

            if (FParentage != null)
                result = FParentage.Length;
            else
                result = 0;
            if (result == 0)
                result = 1;

            return result;
        }

        /// <summary>
        /// Parent breed at the index
        /// </summary>
        /// <param name="parentIdx">The index of the parent</param>
        /// <returns>The parent breed</returns>
        public string ParentageBreed(int parentIdx)
        {
            if ((FParentage.Length == 0) && (parentIdx == 0))
                return Name;
            else
                return FParentage[parentIdx].sBaseBreed;
        }

        /// <summary>
        /// The proportion of the parent
        /// </summary>
        /// <param name="parentIdx">The index of the parent</param>
        /// <returns>The proportion</returns>
        public double ParentagePropn(int parentIdx)
        {
            if ((FParentage.Length == 0) && (parentIdx == 0))
                return 1.0;
            else
                return FParentage[parentIdx].fPropn;
        }

        /// <summary>
        /// Get animal deaths.
        /// </summary>
        /// <param name="bIsWeaner">Get deaths for weaners?</param>
        /// <returns></returns>
        public double AnnualDeaths(bool bIsWeaner)
        {
            if (bIsWeaner)
            {
                if (1.0 - MortRate[2] < 0)
                    throw new Exception("Power of negative number attempted in getDeaths():1.0-MortRate[2]");
            }
            else
            {
                if (1.0 - MortRate[1] < 0)
                    throw new Exception("Power of negative number attempted in getDeaths():1.0-MortRate[1]");
            }

            if (bIsWeaner)
                return 1.0 - Math.Pow(1.0 - MortRate[2], DAYSPERYR);
            else
                return 1.0 - Math.Pow(1.0 - MortRate[1], DAYSPERYR);
        }

        /// <summary>
        /// Standard reference weight
        /// </summary>
        /// <param name="Repro"></param>
        /// <returns></returns>
        public double SexStdRefWt(GrazType.ReproType Repro)
        {
            if ((Repro == GrazType.ReproType.Castrated) || (Repro == GrazType.ReproType.Male))
                return SRWScalars[(int)Repro] * BreedSRW;
            else
                return BreedSRW;
        }

        /// <summary>
        /// Standard birth weight
        /// </summary>
        /// <param name="iNoYoung"></param>
        /// <returns></returns>
        public double StdBirthWt(int iNoYoung)
        {
            return BreedSRW * BirthWtScale[iNoYoung];
        }

        /// <summary>
        /// Set parentage of a genotype.
        /// </summary>
        /// <param name="parentBreed">The parent breed.</param>
        /// <param name="proportion">The proportion of the parent.</param>
        private void SetParentage(Genotype parentBreed, double proportion)
        {
            for (int k = 0; k <= parentBreed.FParentage.Length - 1; k++)
            {
                int i = 0;
                while ((i < FParentage.Length) && (parentBreed.FParentage[k].sBaseBreed != FParentage[i].sBaseBreed))
                    i++;
                if (i == FParentage.Length)
                {
                    Array.Resize(ref FParentage, i + 1);
                    FParentage[i].sBaseBreed = parentBreed.FParentage[k].sBaseBreed;
                    FParentage[i].fPropn = 0.0;
                }
                FParentage[i].fPropn = FParentage[i].fPropn + proportion * parentBreed.FParentage[k].fPropn;
            }
        }

        /// <summary>
        /// Set the peak milk
        /// </summary>
        /// <param name="peakMilkValue">The peak milk value.</param>
        private void SetPeakMilk(double peakMilkValue)
        {
            double fRelPeakMilk;

            if (this.bDairyBreed)
            {
                PeakMilk = peakMilkValue;
                fRelPeakMilk = PeakMilk / (IntakeC[11] * BreedSRW);

                IntakeLactC[0] = FDairyIntakePeak * ((1.0 - IntakeC[10]) + IntakeC[10] * fRelPeakMilk);
            }
            else
                PeakMilk = peakMilkValue;
        }

        /// <summary>Compute conceptions</summary>
        /// <param name="Sigs"></param>
        /// <param name="N"></param>
        /// <param name="fCR1"></param>
        private double ComputeConception(double[] Sigs, int N, ref double fCR1)
        {
            double fCR_N;

            if (Sigs[0] < 5.0)
                fCR_N = StdMath.SIG(1.0, Sigs);
            else
                fCR_N = 0.0;

            if (N == 1)
                fCR1 = fCR_N;
            if (1.0 - fCR1 < 0)
                throw new Exception("Power of negative number attempted in computeConception():1.0-fCR1");
            return StdMath.XDiv(fCR_N, fCR1) * (1.0 - StdMath.Pow(1.0 - fCR1, NC));

        }

        /// <summary>Set conception rates.</summary>
        /// <param name="Rates">Rates array[1..  3]</param>
        private void SetConceptions(double[] Rates)
        {
            double[] InitScale = new double[2] { 0.08, -0.05 };

            double PR, SeekPR, PrevPR;
            double Scale;
            double fCR1;
            double[] Sigs = new double[2];
            int N, Idx, P;

            for (N = 1; N <= MaxYoung; N++)
            {
                SeekPR = 0.0;                                                           // SeekPR is the proportion of mothers      
                for (P = N; P <= MaxYoung; P++)                                         //   conceiving at least N young            
                    SeekPR = SeekPR + Rates[P];
                SeekPR = Math.Min(SeekPR, 0.9975);                                      // If 1.0, no sensitivity to condition      

                if (SeekPR <= 0.0)                                                      // Force zero conception rate if SeekPR = 0 
                    ConceiveSigs[N][0] = 10.0;
                else
                {
                    if (Animal == GrazType.AnimalType.Sheep)                            // For sheep, use the default value for   
                    {                                                                   //   curvature and fit the 50% point      
                        Sigs = ConceiveSigs[N];
                        Idx = 0;
                    }
                    else if ((Animal == GrazType.AnimalType.Cattle) && (N == 1))        // For single calves, use the default value 
                    {                                                                   //   for the 50% point and fit the curvature
                        Sigs = ConceiveSigs[N];
                        Idx = 1;
                    }
                    else                                                                // For twin calves, use the curvature for   
                    {                                                                   //   single calves and fit the 50% point    
                        Sigs = ConceiveSigs[N - 1];
                        Idx = 0;
                    }

                    fCR1 = 0;
                    if (N > 1)
                        ComputeConception(ConceiveSigs[1], 1, ref fCR1);
                    PR = ComputeConception(Sigs, N, ref fCR1);                          // Search algorithm begins.  Only a little  
                    if (PR > SeekPR)                                                    //   search, so coded for size not speed    
                        Scale = InitScale[Idx];
                    else
                        Scale = -InitScale[Idx];

                    do
                    {
                        PrevPR = PR;
                        Sigs[Idx] = Sigs[Idx] + Scale;                                  // Move the parameter up or down...         
                        PR = ComputeConception(Sigs, N, ref fCR1);                      // Compute the corresponding pregnancy rate 
                                                                                        //   at (BC x size) = 1                     
                        if ((PrevPR > SeekPR) && (PR <= SeekPR))                        // If the difference (current-wanted)       
                            Scale = -0.25 * Scale;                                      //   changes sign, reduce the step size and 
                        else if ((PrevPR < SeekPR) && (PR >= SeekPR))                   //   change direction                       
                            Scale = -0.25 * Scale;
                    } while ((Math.Abs(SeekPR - PR) >= 1.0E-6) && (Math.Abs(Scale) >= 0.00001));     // until (Abs(SeekPR-PR) < 1.0E-6) or (Abs(Scale) < 0.00001);

                    Array.Copy(Sigs, ConceiveSigs[N], ConceiveSigs[N].Length);
                } //{_ SeekPR > 0 _}
            } //{_ FOR N := 1 TO MaxYoung _}
        }

        /// <summary>
        /// Copies all the animal parameters
        /// </summary>
        /// <param name="srcSet">Parameter set to copy from.</param>
        private void CopyParams(Genotype srcSet)
        {
            int Idx;

            Name = srcSet.Name;
            DeriveParams();

            Genotype prmSet = (Genotype)srcSet;

            if (prmSet != null)
            {
                BreedSRW = prmSet.BreedSRW;
                PotFleeceWt = prmSet.PotFleeceWt;
                FDairyIntakePeak = prmSet.FDairyIntakePeak;
                Array.Resize(ref FParentage, prmSet.FParentage.Length);
                for (Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                    FParentage[Idx] = prmSet.FParentage[Idx];

                sEditor = prmSet.sEditor;
                sEditDate = prmSet.sEditDate;
                Animal = prmSet.Animal;
                MaxYoung = prmSet.MaxYoung;
                Array.Copy(prmSet.SRWScalars, SRWScalars, prmSet.SRWScalars.Length);
                MaxFleeceDiam = prmSet.MaxFleeceDiam;
                bDairyBreed = prmSet.bDairyBreed;
                PeakMilk = prmSet.PeakMilk;
                Array.Copy(prmSet.MortRate, MortRate, prmSet.MortRate.Length);
                Array.Copy(prmSet.MortAge, MortAge, prmSet.MortAge.Length);
                MortIntensity = prmSet.MortIntensity;
                MortCondConst = prmSet.MortCondConst;
                MortWtDiff = prmSet.MortWtDiff;
                Array.Copy(prmSet.GrowthC, GrowthC, prmSet.GrowthC.Length);
                Array.Copy(prmSet.IntakeC, IntakeC, prmSet.IntakeC.Length);
                Array.Copy(prmSet.IntakeLactC, IntakeLactC, prmSet.IntakeLactC.Length);
                Array.Copy(prmSet.GrazeC, GrazeC, prmSet.GrazeC.Length);
                Array.Copy(prmSet.EfficC, EfficC, prmSet.EfficC.Length);
                Array.Copy(prmSet.MaintC, MaintC, prmSet.MaintC.Length);
                Array.Copy(prmSet.DgProtC, DgProtC, prmSet.DgProtC.Length);
                Array.Copy(prmSet.ProtC, ProtC, prmSet.ProtC.Length);
                Array.Copy(prmSet.PregC, PregC, prmSet.PregC.Length);
                Array.Copy(prmSet.PregScale, PregScale, prmSet.PregScale.Length);
                Array.Copy(prmSet.BirthWtScale, BirthWtScale, prmSet.BirthWtScale.Length);
                Array.Copy(prmSet.PeakLactC, PeakLactC, prmSet.PeakLactC.Length);
                Array.Copy(prmSet.LactC, LactC, prmSet.LactC.Length);
                Array.Copy(prmSet.WoolC, WoolC, prmSet.WoolC.Length);
                Array.Copy(prmSet.ChillC, ChillC, prmSet.ChillC.Length);
                Array.Copy(prmSet.GainC, GainC, prmSet.GainC.Length);
                Array.Copy(prmSet.PhosC, PhosC, prmSet.PhosC.Length);
                Array.Copy(prmSet.SulfC, SulfC, prmSet.SulfC.Length);
                Array.Copy(prmSet.MethC, MethC, prmSet.MethC.Length);
                Array.Copy(prmSet.AshAlkC, AshAlkC, prmSet.AshAlkC.Length);
                OvulationPeriod = prmSet.OvulationPeriod;
                Array.Copy(prmSet.Puberty, Puberty, prmSet.Puberty.Length);
                Array.Copy(prmSet.DayLengthConst, DayLengthConst, prmSet.DayLengthConst.Length);
                for (int i = 0; i < prmSet.ConceiveSigs.Length; i++)
                    Array.Copy(prmSet.ConceiveSigs[i], ConceiveSigs[i], prmSet.ConceiveSigs[i].Length);
                FertWtDiff = prmSet.FertWtDiff;
                Array.Copy(prmSet.ToxaemiaSigs, ToxaemiaSigs, prmSet.ToxaemiaSigs.Length);
                Array.Copy(prmSet.DystokiaSigs, DystokiaSigs, prmSet.DystokiaSigs.Length);
                Array.Copy(prmSet.ExposureConsts, ExposureConsts, prmSet.ExposureConsts.Length);
                SelfWeanPropn = prmSet.SelfWeanPropn;
            }
        }

        /// <summary></summary>
        [Serializable]
        private struct Ancestry
        {
            /// <summary></summary>
            public string sBaseBreed;
            /// <summary></summary>
            public double fPropn;
        }
    }
}