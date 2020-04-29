using Models.Core;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Models.GrazPlan
{
    /// <summary>Encapsulates a parameter set for an animal.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class AnimalParameterSet : Model
    {
        /// <summary>The breed ancestry of the parameter set.</summary>
        private Ancestry[] FParentage = new Ancestry[0];

        /// <summary>Condition score system to use</summary>
        public enum Cond_System { 
            /// <summary></summary>
            csSYSTEM1_5, 
            /// <summary></summary>
            csSYSTEM1_8, 
            /// <summary></summary>
            csSYSTEM1_9 
        };

        /// <summary>Construct an animal parameter set.</summary>
        public AnimalParameterSet()
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            DeriveParams();
        }

        /// <summary>Construct an animal parameter set from a source parameters set.</summary>
        /// <param name="srcSet">The source parameter set.</param>
        public AnimalParameterSet(AnimalParameterSet srcSet)
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            CopyParams(srcSet);

            if (srcSet != null)
                Initialise();
        }

        /// <summary>
        /// Editor of the parameters
        /// </summary>
        [Description("Editor")]
        public string sEditor { get; set; } = string.Empty;
        /// <summary>
        /// Date edited
        /// </summary>
        [Description("Date edited")]
        public string sEditDate { get; set; } = string.Empty;
        /// <summary>
        /// Animal type
        /// </summary>
        [Description("Animal type")]
        public GrazType.AnimalType Animal { get; set; }

        /// <summary>Dairy intake peak (c-idy-0)</summary>
        [Description("Dairy intake peak (c-idy-0)")]
        public double FDairyIntakePeak { get; set; }

        /// <summary>
        /// Maximum young
        /// </summary>
        public int MaxYoung { get; set; }
        /// <summary>
        /// Standard reference weights
        /// </summary>
        [Description("SRW Scalars c-srs-")]
        public double[] SRWScalars { get; set; } = new double[2];

        /// <summary>
        /// Potential greasy fleece weight:SRW
        /// </summary>
        [Description("Fleece ration c-pfw-")]
        public double FleeceRatio { get; set; }
        /// <summary>
        /// In microns
        /// </summary>
        [Description("Maximum fleece diameter c-mu-")]
        public double MaxFleeceDiam { get; set; }
        /// <summary>
        /// Fixed attribute (read in)
        /// </summary>
        [Description("Dairy breed?")]
        public bool bDairyBreed { get; set; }

        /// <summary>
        /// Background death rate, per day  [1..2]      
        /// </summary>
        [Description("Background death rate, per day  [1..2] c-d-")]
        public double[] MortRate { get; set; } = new double[3];
        /// <summary>
        /// 
        /// </summary>
        [Description("Mortality age c-d-")]
        public double[] MortAge { get; set; } = new double[3];            //[1..2]
        /// <summary>
        /// Rate of mortality increase for underweight animals
        /// </summary>
        [Description("Rate of mortality increase for underweight animals c-d-")]
        public double MortIntensity { get; set; }
        /// <summary>
        /// Fraction of normal body weight in animals of Size=1 at which mortality starts to increase
        /// </summary>
        [Description("Fraction of normal body weight in animals of Size=1 at which mortality starts to increase c-d-")]
        public double MortCondConst { get; set; }
        /// <summary>
        /// Weight differential in dying animals  
        /// </summary>
        [Description("Weight differential in dying animals c-d-")]
        public double MortWtDiff { get; set; }
        /// <summary>
        /// C(N)
        /// </summary>
        [Description("Growth C c-n-")]
        public double[] GrowthC { get; set; } = new double[5];
        /// <summary>
        /// C(I)
        /// </summary>
        [Description("Intake C c-i-")]
        public double[] IntakeC { get; set; } = new double[22];                                             
        /// <summary>
        /// C(I,15)
        /// </summary>
        [Description("Intake Lact C c-imx-")]
        public double[] IntakeLactC { get; set; } = new double[4];
        /// <summary>
        /// C(R)
        /// </summary>
        [Description("Graze C c-r-")]
        public double[] GrazeC { get; set; } = new double[21];
        /// <summary>
        /// C(K)
        /// </summary>
        [Description("Effic C c-k-")]
        public double[] EfficC { get; set; } = new double[17];
        /// <summary>
        /// C(M)
        /// </summary>
        [Description("Maintenance C c-m-")]
        public double[] MaintC { get; set; } = new double[18];
        /// <summary>
        /// C(RDP)
        /// </summary>
        [Description("DgProtC c-rd-")]
        public double[] DgProtC { get; set; } = new double[9];
        /// <summary>
        /// C(A)
        /// </summary>
        [Description("Prot C c-a-")]
        public double[] ProtC { get; set; } = new double[10];
        /// <summary>
        /// C(P)
        /// </summary>
        [Description("Preg C c-p-")]
        public double[] PregC { get; set; } = new double[14];
        /// <summary>
        /// C(P,14,Y)
        /// </summary>
        [Description("Preg scale c-p14-")]
        public double[] PregScale { get; set; } = new double[4];
        /// <summary>
        /// C(P,15,Y)
        /// </summary>
        [Description("Birth weight scale c-p15-")]
        public double[] BirthWtScale { get; set; } = new double[4];
        /// <summary>
        /// C(L,0,Y)
        /// </summary>
        [Description("Peak Lact C c-l0-")]
        public double[] PeakLactC { get; set; } = new double[4];
        /// <summary>
        /// C(L)
        /// </summary>
        [Description("Lact C c-l-")]
        public double[] LactC { get; set; } = new double[26];
        /// <summary>
        /// C(W)
        /// </summary>
        [Description("Wool C c-w-")]
        public double[] WoolC { get; set; } = new double[15];
        /// <summary>
        /// C(C)
        /// </summary>
        [Description("Chill C c-c-")]
        public double[] ChillC { get; set; } = new double[17];
        /// <summary>
        /// C(G)
        /// </summary>
        [Description("Gain C c-g-")]
        public double[] GainC { get; set; } = new double[19];
        /// <summary>
        /// 
        /// </summary>
        [Description("Phos C c-ph-")]
        public double[] PhosC { get; set; } = new double[16];
        /// <summary>
        /// 
        /// </summary>
        [Description("Sulf C c-su-")]
        public double[] SulfC { get; set; } = new double[5];
        /// <summary>
        /// 
        /// </summary>
        [Description("Meth C c-h-")]
        public double[] MethC { get; set; } = new double[8];
        /// <summary>
        /// Ash alkalinity values
        /// </summary>
        [Description("Ash alkalinity C c-aa-")]
        public double[] AshAlkC { get; set; } = new double[4];
        /// <summary>
        /// 
        /// </summary>
        [Description("Ovulation period c-f4")]
        public int OvulationPeriod { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("Puberty c-pbt-")]
        public int[] Puberty { get; set; } = new int[2];                  //array[Boolean]
        /// <summary>
        /// 
        /// </summary>
        [Description("Day length constant c-f1-")]
        public double[] DayLengthConst { get; set; } = new double[4];     //array[1..3]


        /// <summary>Breed standard reference weight (kg)</summary>
        public double FBreedSRW { get; set; }

        /// <summary>Potential fleece weigth (kg)</summary>
        public double FPotFleeceWt { get; set; }

        /// <summary>
        /// WM(peak)
        /// </summary>
        public double PeakMilk { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double[][] ConceiveSigs { get; set; } = new double[4][];   //[0..3][0..1]

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public double FertWtDiff { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("ToxaemiaSigss c-d")]
        public double[] ToxaemiaSigs { get; set; } = new double[2];       //array[0..1]
        /// <summary>
        /// 
        /// </summary>
        [Description("DystokiaSigs c-d")]
        public double[] DystokiaSigs { get; set; } = new double[2];       //array[0..1]
        /// <summary>
        /// 
        /// </summary>
        [Description("Exposure constants c-d")]
        public double[] ExposureConsts { get; set; } = new double[4];     //array[0..3]
        /// <summary>
        /// 
        /// </summary>
        [Description("Self wean proportion c-swn")]
        public double SelfWeanPropn { get; set; }


        /// <summary>
        /// Initialise the component.
        /// </summary>
        public void Initialise()
        {
            if (Animal == GrazType.AnimalType.Sheep)
                setPotGFW(PotentialGFW);

            if (bDairyBreed)
                setPeakMilk(PotMilkYield);

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
        /// Blend two animal parameter sets, creating a new parameter set.
        /// </summary>
        /// <param name="nameOfNewGenotype">The name given to the new parameter set.</param>
        /// <param name="damBreed">The dam breed.</param>
        /// <param name="sireBreed">The sire breed.</param>
        /// <param name="damProportion">The dam proportion.</param>
        /// <param name="sireProportion">The sire proportion.</param>
        /// <returns></returns>
        public static AnimalParameterSet BlendParameterSets(string nameOfNewGenotype, AnimalParameterSet damBreed, AnimalParameterSet sireBreed, double damProportion, double sireProportion)
        {
            var newGenotype = new AnimalParameterSet();
            newGenotype.sEditor = damBreed.sEditor;
            newGenotype.sEditDate = damBreed.sEditDate;
            newGenotype.Animal = damBreed.Animal;
            newGenotype.bDairyBreed = damBreed.bDairyBreed;
            newGenotype.MaxYoung = damBreed.MaxYoung;
            newGenotype.OvulationPeriod = damBreed.OvulationPeriod;
            newGenotype.Puberty = damBreed.Puberty;

            newGenotype.FBreedSRW = damProportion * damBreed.FBreedSRW + sireProportion * sireBreed.FBreedSRW;
            newGenotype.FPotFleeceWt = damProportion * damBreed.FPotFleeceWt + sireProportion * sireBreed.FPotFleeceWt;
            newGenotype.FDairyIntakePeak = damProportion * damBreed.FDairyIntakePeak + sireProportion * sireBreed.FDairyIntakePeak;
            newGenotype.FleeceRatio = damProportion * damBreed.FleeceRatio + sireProportion * sireBreed.FleeceRatio;
            newGenotype.MaxFleeceDiam = damProportion * damBreed.MaxFleeceDiam + sireProportion * sireBreed.MaxFleeceDiam;
            newGenotype.PeakMilk = damProportion * damBreed.PeakMilk + sireProportion * sireBreed.PeakMilk;
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortRate[idx] = damProportion * damBreed.MortRate[idx] + sireProportion * sireBreed.MortRate[idx];
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortAge[idx] = damProportion * damBreed.MortAge[idx] + sireProportion * sireBreed.MortAge[idx];
            newGenotype.MortIntensity = damProportion * damBreed.MortIntensity + sireProportion * sireBreed.MortIntensity;
            newGenotype.MortCondConst = damProportion * damBreed.MortCondConst + sireProportion * sireBreed.MortCondConst;
            newGenotype.MortWtDiff = damProportion * damBreed.MortWtDiff + sireProportion * sireBreed.MortWtDiff;

            for (int idx = 0; idx < newGenotype.SRWScalars.Length; idx++) newGenotype.SRWScalars[idx] = damProportion * damBreed.SRWScalars[idx] + sireProportion * sireBreed.SRWScalars[idx];
            for (int idx = 1; idx < newGenotype.GrowthC.Length; idx++) newGenotype.GrowthC[idx] = damProportion * damBreed.GrowthC[idx] + sireProportion * sireBreed.GrowthC[idx];
            for (int idx = 1; idx < newGenotype.IntakeC.Length; idx++) newGenotype.IntakeC[idx] = damProportion * damBreed.IntakeC[idx] + sireProportion * sireBreed.IntakeC[idx];
            for (int idx = 0; idx < newGenotype.IntakeLactC.Length; idx++) newGenotype.IntakeLactC[idx] = damProportion * damBreed.IntakeLactC[idx] + sireProportion * sireBreed.IntakeLactC[idx];
            for (int idx = 1; idx < newGenotype.GrazeC.Length; idx++) newGenotype.GrazeC[idx] = damProportion * damBreed.GrazeC[idx] + sireProportion * sireBreed.GrazeC[idx];
            for (int idx = 1; idx < newGenotype.EfficC.Length; idx++) newGenotype.EfficC[idx] = damProportion * damBreed.EfficC[idx] + sireProportion * sireBreed.EfficC[idx];
            for (int idx = 1; idx < newGenotype.MaintC.Length; idx++) newGenotype.MaintC[idx] = damProportion * damBreed.MaintC[idx] + sireProportion * sireBreed.MaintC[idx];
            for (int idx = 1; idx < newGenotype.DgProtC.Length; idx++) newGenotype.DgProtC[idx] = damProportion * damBreed.DgProtC[idx] + sireProportion * sireBreed.DgProtC[idx];
            for (int idx = 1; idx < newGenotype.ProtC.Length; idx++) newGenotype.ProtC[idx] = damProportion * damBreed.ProtC[idx] + sireProportion * sireBreed.ProtC[idx];
            for (int idx = 1; idx < newGenotype.PregC.Length; idx++) newGenotype.PregC[idx] = damProportion * damBreed.PregC[idx] + sireProportion * sireBreed.PregC[idx];
            for (int idx = 1; idx < newGenotype.PregScale.Length; idx++) newGenotype.PregScale[idx] = damProportion * damBreed.PregScale[idx] + sireProportion * sireBreed.PregScale[idx];
            for (int idx = 1; idx < newGenotype.BirthWtScale.Length; idx++) newGenotype.BirthWtScale[idx] = damProportion * damBreed.BirthWtScale[idx] + sireProportion * sireBreed.BirthWtScale[idx];
            for (int idx = 1; idx < newGenotype.PeakLactC.Length; idx++) newGenotype.PeakLactC[idx] = damProportion * damBreed.PeakLactC[idx] + sireProportion * sireBreed.PeakLactC[idx];
            for (int idx = 1; idx < newGenotype.LactC.Length; idx++) newGenotype.LactC[idx] = damProportion * damBreed.LactC[idx] + sireProportion * sireBreed.LactC[idx];
            for (int idx = 1; idx < newGenotype.WoolC.Length; idx++) newGenotype.WoolC[idx] = damProportion * damBreed.WoolC[idx] + sireProportion * sireBreed.WoolC[idx];
            for (int idx = 1; idx < newGenotype.ChillC.Length; idx++) newGenotype.ChillC[idx] = damProportion * damBreed.ChillC[idx] + sireProportion * sireBreed.ChillC[idx];
            for (int idx = 1; idx < newGenotype.GainC.Length; idx++) newGenotype.GainC[idx] = damProportion * damBreed.GainC[idx] + sireProportion * sireBreed.GainC[idx];
            for (int idx = 1; idx < newGenotype.PhosC.Length; idx++) newGenotype.PhosC[idx] = damProportion * damBreed.PhosC[idx] + sireProportion * sireBreed.PhosC[idx];
            for (int idx = 1; idx < newGenotype.SulfC.Length; idx++) newGenotype.SulfC[idx] = damProportion * damBreed.SulfC[idx] + sireProportion * sireBreed.SulfC[idx];
            for (int idx = 1; idx < newGenotype.MethC.Length; idx++) newGenotype.MethC[idx] = damProportion * damBreed.MethC[idx] + sireProportion * sireBreed.MethC[idx];
            for (int idx = 1; idx < newGenotype.AshAlkC.Length; idx++) newGenotype.AshAlkC[idx] = damProportion * damBreed.AshAlkC[idx] + sireProportion * sireBreed.AshAlkC[idx];
            for (int idx = 1; idx < newGenotype.DayLengthConst.Length; idx++) newGenotype.DayLengthConst[idx] = damProportion * damBreed.DayLengthConst[idx] + sireProportion * sireBreed.DayLengthConst[idx];
            for (int idx = 0; idx < newGenotype.ToxaemiaSigs.Length; idx++) newGenotype.ToxaemiaSigs[idx] = damProportion * damBreed.ToxaemiaSigs[idx] + sireProportion * sireBreed.ToxaemiaSigs[idx];
            for (int idx = 0; idx < newGenotype.DystokiaSigs.Length; idx++) newGenotype.DystokiaSigs[idx] = damProportion * damBreed.DystokiaSigs[idx] + sireProportion * sireBreed.DystokiaSigs[idx];
            for (int idx = 0; idx < newGenotype.ExposureConsts.Length; idx++) newGenotype.ExposureConsts[idx] = damProportion * damBreed.ExposureConsts[idx] + sireProportion * sireBreed.ExposureConsts[idx];

            newGenotype.FertWtDiff = damProportion * damBreed.FertWtDiff + sireProportion * sireBreed.FertWtDiff;
            newGenotype.SelfWeanPropn = damProportion * damBreed.SelfWeanPropn + sireProportion * sireBreed.SelfWeanPropn;
            for (int idx = 1; idx < newGenotype.ConceiveSigs.Length; idx++)
                for (int Jdx = 0; Jdx < newGenotype.ConceiveSigs[idx].Length; Jdx++)
                    newGenotype.ConceiveSigs[idx][Jdx] = damProportion * damBreed.ConceiveSigs[idx][Jdx] + sireProportion * sireBreed.ConceiveSigs[idx][Jdx];

            for (int idx = 0; idx <= newGenotype.FParentage.Length - 1; idx++)
                newGenotype.FParentage[idx].fPropn = 0.0;

            SetParentage(damBreed, damProportion, newGenotype);
            SetParentage(sireBreed, sireProportion, newGenotype);

            if (!string.IsNullOrEmpty(nameOfNewGenotype))
                newGenotype.Name = nameOfNewGenotype;
            else if (newGenotype.FParentage.Length == 1)
                newGenotype.Name = newGenotype.FParentage[0].sBaseBreed;
            else if (newGenotype.FParentage.Length > 1)
            {
                int iDecPlaces = 0;
                for (int Idx = 0; Idx <= newGenotype.FParentage.Length - 1; Idx++)
                {
                    if ((newGenotype.FParentage[Idx].fPropn > 0.0005) && (newGenotype.FParentage[Idx].fPropn <= 0.05))
                        iDecPlaces = 1;
                }

                newGenotype.Name = "";
                for (int Idx = 0; Idx <= newGenotype.FParentage.Length - 1; Idx++)
                {
                    if (newGenotype.FParentage[Idx].fPropn > 0.0005)
                    {
                        if (newGenotype.Name != "")
                            newGenotype.Name = newGenotype.Name + ", ";
                        newGenotype.Name = newGenotype.Name + newGenotype.FParentage[Idx].sBaseBreed + " "
                                         + String.Format("{0:0." + new String('0', iDecPlaces) + "}", 100.0 * newGenotype.FParentage[Idx].fPropn) + "%";
                    }
                }
            }
            return newGenotype;
        }

        /// <summary>
        /// Set parentage of a genotype.
        /// </summary>
        /// <param name="parentBreed">The parent breed.</param>
        /// <param name="proportion">The proportion of the parent.</param>
        /// <param name="newGenotype">The genotype to set the parentage in.</param>
        private static void SetParentage(AnimalParameterSet parentBreed, double proportion, AnimalParameterSet newGenotype)
        {
            for (int k = 0; k <= parentBreed.FParentage.Length - 1; k++)
            {
                int i = 0;
                while ((i < newGenotype.FParentage.Length) && (parentBreed.FParentage[k].sBaseBreed != newGenotype.FParentage[i].sBaseBreed))
                    i++;
                if (i == newGenotype.FParentage.Length)
                {
                    Array.Resize(ref newGenotype.FParentage, i + 1);
                    newGenotype.FParentage[i].sBaseBreed = parentBreed.FParentage[k].sBaseBreed;
                    newGenotype.FParentage[i].fPropn = 0.0;
                }
                newGenotype.FParentage[i].fPropn = newGenotype.FParentage[i].fPropn
                                            + proportion * parentBreed.FParentage[k].fPropn;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void DeriveParams()
        {
            MaxYoung = 1;
            while ((MaxYoung < 3) && (BirthWtScale[MaxYoung + 1] > 0.0))
                MaxYoung++;

            FPotFleeceWt = FBreedSRW * FleeceRatio;
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
        /// Breed standard reference weight
        /// </summary>
        public double BreedSRW
        {
            get { return FBreedSRW; }
            set { setSRW(value); }
        }
        /// <summary>
        /// Potential fleece weight
        /// </summary>
        public double PotentialGFW
        {
            get { return FPotFleeceWt; }
            set { setPotGFW(value); }
        }
        /// <summary>
        /// Maximum fleece microns
        /// </summary>
        public double MaxMicrons
        {
            get { return MaxFleeceDiam; }
            set { MaxFleeceDiam = value; }
        }
        /// <summary>
        /// Fleece yield
        /// </summary>
        public double FleeceYield
        {
            get { return WoolC[3]; }
            set { WoolC[3] = value; }
        }
        /// <summary>
        /// Potential milk yield
        /// </summary>
        public double PotMilkYield
        {
            get { return PeakMilk; }
            set { setPeakMilk(value); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bWeaners"></param>
        /// <returns></returns>
        public double AnnualDeaths(bool bWeaners)
        {
            return getDeaths(bWeaners);
        }

        /// <summary>
        /// Conception values
        /// </summary>
        public double[] Conceptions
        {
            get { return getConceptions(); }
            set { setConceptions(value); }
        }
        /// <summary>
        /// Get gestation
        /// </summary>
        public int Gestation
        {
            get { return getGestation(); }
        }
        /// <summary>
        /// Standard reference weight
        /// </summary>
        /// <param name="Repro"></param>
        /// <returns></returns>
        public double fSexStdRefWt(GrazType.ReproType Repro)
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
        
        private const double SIGVAL = 5.88878;                              // 2*ln(0.95/0.05)                         
        private const double DAYSPERYR = 365.25;
        private const double NC = 2.5;                                      // 2.5 cycles joining is assumed            

        // Convert between condition scores and relative condition values   
        /// <summary>
        /// Condition score for condition = 1.0
        /// </summary>
        static public double[] BASESCORE = { 3.0, 4.0, 4.5 };                 
        /// <summary>
        /// Change in condition for unit CS change   
        /// </summary>
        static public double[] SCOREUNIT = { 0.15, 0.09, 0.08 };              
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CondScore"></param>
        /// <param name="System"></param>
        /// <returns></returns>
        static public double CondScore2Condition(double CondScore, Cond_System System = Cond_System.csSYSTEM1_5)
        {
            return 1.0 + (CondScore - BASESCORE[(int)System]) * SCOREUNIT[(int)System];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="System"></param>
        /// <returns></returns>
        static public double Condition2CondScore(double Condition, Cond_System System = Cond_System.csSYSTEM1_5)
        {
            return BASESCORE[(int)System] + (Condition - 1.0) / SCOREUNIT[(int)System];
        }
        /// <summary>
        /// Default fleece weight as a function of age, sex and time since shearing     
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="iFleeceDays"></param>
        /// <returns></returns>
        static public double fDefaultFleece(AnimalParameterSet Params,
                                     int iAgeDays,
                                     GrazType.ReproType Repr,
                                     int iFleeceDays)
        {
            double Result;
            double fMeanAgeFactor;

            iFleeceDays = Math.Min(iFleeceDays, iAgeDays);

            if ((Params.Animal == GrazType.AnimalType.Sheep) && (iFleeceDays > 0))
            {
                fMeanAgeFactor = 1.0 - (1.0 - Params.WoolC[5])
                                        * (Math.Exp(-Params.WoolC[12] * (iAgeDays - iFleeceDays)) - Math.Exp(-Params.WoolC[12] * iAgeDays))
                                        / (Params.WoolC[12] * iFleeceDays);
                Result = Params.FleeceRatio * Params.fSexStdRefWt(Repr) * fMeanAgeFactor * iFleeceDays / 365.0;
            }
            else
                Result = 0.0;
            return Result;
        }

        /// <summary>
        /// Default fibre diameter as a function of age, sex, time since shearing and fleece weight                                                             
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="iFleeceDays"></param>
        /// <param name="fGFW"></param>
        /// <returns></returns>
        static public double fDefaultMicron(AnimalParameterSet Params, int iAgeDays, GrazType.ReproType Repr, int iFleeceDays, double fGFW)
        {
            double fPotFleece;

            if ((iFleeceDays > 0) && (fGFW > 0.0))
            {
                fPotFleece = fDefaultFleece(Params, iAgeDays, Repr, iFleeceDays);
                return Params.MaxMicrons * Math.Pow(fGFW / fPotFleece, Params.WoolC[13]);
            }
            else
                return Params.MaxMicrons;
        }


        private void setSRW(double fValue)
        {
            FBreedSRW = fValue;
            FPotFleeceWt = FleeceRatio * fValue;
            setPeakMilk(IntakeC[11] * fValue);
        }
        private void setPotGFW(double fValue)
        {
            FPotFleeceWt = fValue;
            FleeceRatio = fValue / BreedSRW;
        }
        private void setPeakMilk(double fValue)
        {
            double fRelPeakMilk;

            if (this.bDairyBreed)
            {
                PeakMilk = fValue;
                fRelPeakMilk = PeakMilk / (IntakeC[11] * BreedSRW);

                IntakeLactC[0] = FDairyIntakePeak * ((1.0 - IntakeC[10]) + IntakeC[10] * fRelPeakMilk);
            }
            else
                PeakMilk = fValue;
        }

        /// <summary></summary>
        /// <param name="bIsWeaner"></param>
        private double getDeaths(bool bIsWeaner)
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

        /// <summary></summary>
        /// <param name="bIsWeaner"></param>
        /// <param name="AnnDeaths"></param>
        private void setDeaths(bool bIsWeaner, double AnnDeaths)
        {
            if (1.0 - AnnDeaths < 0)
                throw new Exception("Power of negative number attempted in setDeaths():1.0-AnnDeaths");

            if (bIsWeaner)
                MortRate[2] = 1.0 - Math.Pow(1.0 - AnnDeaths, 1.0 / DAYSPERYR);
            else
                MortRate[1] = 1.0 - Math.Pow(1.0 - AnnDeaths, 1.0 / DAYSPERYR);
        }

        /// <summary></summary>
        private double[] getConceptions()
        {
            double[] result = new double[4];
            double fCR1 = 0.0;
            int N;

            for (N = 1; N <= MaxYoung; N++)
            {
                result[N] = computeConception(ConceiveSigs[N], N, ref fCR1);
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

        /// <summary></summary>
        /// <param name="Sigs"></param>
        /// <param name="N"></param>
        /// <param name="fCR1"></param>
        private double computeConception(double[] Sigs, int N, ref double fCR1)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Rates">Rates array[1..  3]</param>
        private void setConceptions(double[] Rates)
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
                        computeConception(ConceiveSigs[1], 1, ref fCR1);
                    PR = computeConception(Sigs, N, ref fCR1);                          // Search algorithm begins.  Only a little  
                    if (PR > SeekPR)                                                    //   search, so coded for size not speed    
                        Scale = Math.Abs(InitScale[Idx]);
                    else
                        Scale = -InitScale[Idx];

                    do
                    {
                        PrevPR = PR;
                        Sigs[Idx] = Sigs[Idx] + Scale;                                  // Move the parameter up or down...         
                        PR = computeConception(Sigs, N, ref fCR1);                      // Compute the corresponding pregnancy rate 
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

        private int getGestation()
        {
            return Convert.ToInt32(Math.Round(PregC[1]), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Copies all the animal parameters
        /// </summary>
        /// <param name="srcSet">Parameter set to copy from.</param>
        private void CopyParams(AnimalParameterSet srcSet)
        {
            int Idx;

            Name = srcSet.Name;
            DeriveParams();

            AnimalParameterSet prmSet = (AnimalParameterSet)srcSet;

            if (prmSet != null)
            {
                FBreedSRW = prmSet.FBreedSRW;
                FPotFleeceWt = prmSet.FPotFleeceWt;
                FDairyIntakePeak = prmSet.FDairyIntakePeak;
                Array.Resize(ref FParentage, prmSet.FParentage.Length);
                for (Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                    FParentage[Idx] = prmSet.FParentage[Idx];

                sEditor = prmSet.sEditor;
                sEditDate = prmSet.sEditDate;
                Animal = prmSet.Animal;
                MaxYoung = prmSet.MaxYoung;
                Array.Copy(prmSet.SRWScalars, SRWScalars, prmSet.SRWScalars.Length);
                FleeceRatio = prmSet.FleeceRatio;
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