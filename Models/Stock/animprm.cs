using Models.Core;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Models.GrazPlan
{
    /// <summary>
    /// The animal parameters object
    /// </summary>
    public class GlobalAnimalParams
    {
        private ParameterSet _GAnimalParams = null;
        /// <summary>
        /// The object that contains the animal parameters
        /// </summary>
        /// <returns></returns>
        public AnimalParamSet AnimalParamsGlb()
        {
            if (_GAnimalParams == null)
            {
                _GAnimalParams = new AnimalParamSet();
                GlobalParameterFactory.ParamXMLFactory().ReadDefaults("Models.Resources.ruminant.prm", ref _GAnimalParams);
            }
            return (AnimalParamSet)_GAnimalParams;
        }
    }

    /// <summary>
    /// Contains a blended genotype
    /// </summary>
    public struct AnimalParamBlend
    {
        /// <summary>
        /// Breed parameters
        /// </summary>
        public AnimalParamSet Breed;
        /// <summary>
        /// Proportion of the breed
        /// </summary>
        public double fPropn;
    }

    /// <summary>
    /// Animal parameter set
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class AnimalParamSet : ParameterSet
    {
        /*
        public float[] TPregArray       = array[0..299] of Float;
        TLactArray       = array[0..365] of Float;
        TConceptionArray = array[1..  3] of Float;
        */
        /// <summary>
        /// Return a copy of this object
        /// </summary>
        /// <returns></returns>
        public AnimalParamSet Copy()
        {
            return ObjectCopier.Clone(this);
        }
        /// <summary>
        /// Condition score system to use
        /// </summary>
        public enum Cond_System { 
            /// <summary>
            /// 
            /// </summary>
            csSYSTEM1_5, 
            /// <summary>
            /// 
            /// </summary>
            csSYSTEM1_8, 
            /// <summary>
            /// 
            /// </summary>
            csSYSTEM1_9 };

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public struct Ancestry
        {
            /// <summary></summary>
            public string sBaseBreed;
            /// <summary></summary>
            public double fPropn;
        }

        /// <summary>
        /// 
        /// </summary>
        public double FBreedSRW { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double FPotFleeceWt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Dairy intake peak (c-idy-0)")]
        public double FDairyIntakePeak { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Ancestry[] FParentage = new Ancestry[0];

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

        /// <summary>
        /// TODO: Test this
        /// </summary>
        /// <param name="bIsWeaner"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Sigs"></param>
        /// <param name="N"></param>
        /// <param name="fCR1"></param>
        /// <returns></returns>
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
        /// Overrides the base function and copies all the animal parameters
        /// </summary>
        /// <param name="srcSet"></param>
        /// <param name="bCopyData"></param>
        override protected void CopyParams(ParameterSet srcSet, bool bCopyData)
        {
            int Idx;

            base.CopyParams(srcSet, false);

            AnimalParamSet prmSet = (AnimalParamSet)srcSet;

            if (bCopyData && (prmSet != null))
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
                Array.Copy(prmSet.SRWScalars,SRWScalars, prmSet.SRWScalars.Length);
                FleeceRatio = prmSet.FleeceRatio;
                MaxFleeceDiam = prmSet.MaxFleeceDiam;
                bDairyBreed = prmSet.bDairyBreed;
                PeakMilk = prmSet.PeakMilk;
                Array.Copy(prmSet.MortRate, MortRate, prmSet.MortRate.Length);
                Array.Copy(prmSet.MortAge, MortAge, prmSet.MortAge.Length);
                MortIntensity = prmSet.MortIntensity;
                MortCondConst = prmSet.MortCondConst;
                MortWtDiff = prmSet.MortWtDiff;
                Array.Copy(prmSet.GrowthC,GrowthC, prmSet.GrowthC.Length);
                Array.Copy(prmSet.IntakeC, IntakeC,prmSet.IntakeC.Length);
                Array.Copy(prmSet.IntakeLactC, IntakeLactC, prmSet.IntakeLactC.Length);
                Array.Copy(prmSet.GrazeC, GrazeC, prmSet.GrazeC.Length);
                Array.Copy(prmSet.EfficC, EfficC, prmSet.EfficC.Length);
                Array.Copy(prmSet.MaintC,MaintC, prmSet.MaintC.Length);
                Array.Copy(prmSet.DgProtC,DgProtC, prmSet.DgProtC.Length);
                Array.Copy(prmSet.ProtC, ProtC, prmSet.ProtC.Length);
                Array.Copy(prmSet.PregC, PregC, prmSet.PregC.Length);
                Array.Copy(prmSet.PregScale, PregScale, prmSet.PregScale.Length);
                Array.Copy(prmSet.BirthWtScale, BirthWtScale,prmSet.BirthWtScale.Length);
                Array.Copy(prmSet.PeakLactC, PeakLactC, prmSet.PeakLactC.Length);
                Array.Copy(prmSet.LactC,LactC, prmSet.LactC.Length);
                Array.Copy(prmSet.WoolC, WoolC, prmSet.WoolC.Length);
                Array.Copy(prmSet.ChillC, ChillC,prmSet.ChillC.Length);
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
                Array.Copy(prmSet.ExposureConsts,ExposureConsts, prmSet.ExposureConsts.Length);
                SelfWeanPropn = prmSet.SelfWeanPropn;

                for (Idx = 0; Idx <= DefinitionCount() - 1; Idx++)
                    GetDefinition(Idx).SetDefined(prmSet.GetDefinition(Idx));
            }
        }
        /// <summary>
        /// Make a new animal parameter set that is a child of this one
        /// </summary>
        /// <returns></returns>
        override protected ParameterSet MakeChild()
        {
            return new AnimalParamSet(this);
        }

        /// <summary>
        /// 
        /// </summary>
        override protected void DefineEntries()
        {
            DefineParameters("editor", TYPETEXT);
            DefineParameters("edited", TYPETEXT);

            DefineParameters("animal", TYPETEXT);
            DefineParameters("srw", TYPEREAL);
            DefineParameters("dairy", TYPEBOOL);
            DefineParameters("c-pfw", TYPEREAL);
            DefineParameters("c-mu", TYPEREAL);
            DefineParameters("c-srs-castr;male", TYPEREAL);
            DefineParameters("c-n-1:4", TYPEREAL);
            DefineParameters("c-i-1:21", TYPEREAL);
            DefineParameters("c-imx-0:3", TYPEREAL);
            DefineParameters("c-r-1:20", TYPEREAL);
            DefineParameters("c-k-1:16", TYPEREAL);
            DefineParameters("c-m-1:17", TYPEREAL);
            DefineParameters("c-rd-1:8", TYPEREAL);
            DefineParameters("c-a-1:9", TYPEREAL);
            DefineParameters("c-p-1:13", TYPEREAL);
            DefineParameters("c-p14-1:3", TYPEREAL);
            DefineParameters("c-p15-1:3", TYPEREAL);
            DefineParameters("c-l0-1:3", TYPEREAL);
            DefineParameters("c-l-1:25", TYPEREAL);
            DefineParameters("c-w-1:14", TYPEREAL);
            DefineParameters("c-c-1:16", TYPEREAL);
            DefineParameters("c-g-1:18", TYPEREAL);
            DefineParameters("c-ph-1:15", TYPEREAL);
            DefineParameters("c-su-1:4", TYPEREAL);
            DefineParameters("c-h-1:7", TYPEREAL);
            DefineParameters("c-aa-1:3", TYPEREAL);
            DefineParameters("c-f1-1:3", TYPEREAL);
            DefineParameters("c-f2-1:3", TYPEREAL);
            DefineParameters("c-f3-1:3", TYPEREAL);
            DefineParameters("c-f4", TYPEINT);
            DefineParameters("c-pbt-female;male", TYPEINT);
            DefineParameters("c-d-1:15", TYPEREAL);
            DefineParameters("c-swn", TYPEREAL);
        }
        /// <summary>
        /// Get the floating point value
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected double GetRealParam(string[] sTagList)
        {
            int Idx;

            double result = 0.0;

            if (sTagList[0] == "srw")
                result = BreedSRW;
            else if (sTagList[0] == "c")
            {
                if (sTagList[1] == "pfw")
                    result = FleeceRatio;
                else if (sTagList[1] == "mu")
                    result = MaxFleeceDiam;
                else if (sTagList[1] == "srs")
                {
                    if (sTagList[2] == "castr")
                        result = SRWScalars[(int)GrazType.ReproType.Castrated];
                    else if (sTagList[2] == "male")
                        result = SRWScalars[(int)GrazType.ReproType.Male];
                }
                else if (sTagList[1] == "swn")
                    result = SelfWeanPropn;
                else
                {
                    Idx = Convert.ToInt32(sTagList[2], CultureInfo.InvariantCulture);

                    if (sTagList[1] == "n")
                        result = GrowthC[Idx];
                    else if (sTagList[1] == "i")
                        result = IntakeC[Idx];
                    else if (sTagList[1] == "imx")
                        result = IntakeLactC[Idx];  
                    else if (sTagList[1] == "r")
                        result = GrazeC[Idx];
                    else if (sTagList[1] == "k")
                        result = EfficC[Idx];
                    else if (sTagList[1] == "m")
                        result = MaintC[Idx];
                    else if (sTagList[1] == "rd")
                        result = DgProtC[Idx];
                    else if (sTagList[1] == "a")
                        result = ProtC[Idx];
                    else if (sTagList[1] == "p")
                        result = PregC[Idx];
                    else if (sTagList[1] == "p14")
                        result = PregScale[Idx];
                    else if (sTagList[1] == "p15")
                        result = BirthWtScale[Idx];
                    else if (sTagList[1] == "l0")
                        result = PeakLactC[Idx];
                    else if (sTagList[1] == "l")
                        result = LactC[Idx];
                    else if (sTagList[1] == "w")
                        result = WoolC[Idx];
                    else if (sTagList[1] == "c")
                        result = ChillC[Idx];
                    else if (sTagList[1] == "g")
                        result = GainC[Idx];
                    else if (sTagList[1] == "ph")
                        result = PhosC[Idx];
                    else if (sTagList[1] == "su")
                        result = SulfC[Idx];
                    else if (sTagList[1] == "h")
                        result = MethC[Idx];
                    else if (sTagList[1] == "aa")
                        result = AshAlkC[Idx];
                    else if (sTagList[1] == "f1")
                        result = DayLengthConst[Idx];
                    else if (sTagList[1] == "f2")
                        result = ConceiveSigs[Idx][0];
                    else if (sTagList[1] == "f3")
                        result = ConceiveSigs[Idx][1];
                    else if (sTagList[1] == "d")
                    {
                        switch (Idx)
                        {
                            case 1: result = MortRate[1];
                                break;
                            case 2: result = MortIntensity;
                                break;
                            case 3: result = MortCondConst;
                                break;
                            case 4:
                            case 5: result = ToxaemiaSigs[Idx - 4];
                                break;
                            case 6:
                            case 7: result = DystokiaSigs[Idx - 6];
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11: result = ExposureConsts[Idx - 8];
                                break;
                            case 12: result = MortWtDiff;
                                break;
                            case 13: result = MortRate[2];
                                break;
                            case 14: result = MortAge[1];
                                break;
                            case 15: result = MortAge[2];
                                break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected int GetIntParam(string[] sTagList)
        {
            int result = 0;

            if (sTagList[0] == "c")
            {
                if (sTagList[1] == "f4")
                    result = OvulationPeriod;
                else if (sTagList[1] == "pbt")
                {
                    if (sTagList[2] == "female")
                        result = Puberty[0];    //[false]
                    else if (sTagList[2] == "male")
                        result = Puberty[1]; //[true]
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected string GetTextParam(string[] sTagList)
        {
            string result = "";
            if (sTagList[0] == "editor")
                result = sEditor;
            else if (sTagList[0] == "edited")
                result = sEditDate;
            else if (sTagList[0] == "animal")
                result = GrazType.AnimalText[(int)Animal].ToLower();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected bool GetBoolParam(string[] sTagList)
        {
            bool result = false;
            if (sTagList[0] == "dairy")
                result = bDairyBreed;

            return result;
        }
        /// <summary>
        /// Set the floating point value
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="fValue"></param>
        protected override void SetRealParam(string[] sTagList, double fValue)
        {
            int Idx;

            if (sTagList[0] == "srw")
                FBreedSRW = fValue;
            else if (sTagList[0] == "c")
            {
                if (sTagList[1] == "pfw")
                    FleeceRatio = fValue;
                else if (sTagList[1] == "mu")
                    MaxFleeceDiam = fValue;
                else if (sTagList[1] == "srs")
                {
                    if (sTagList[2] == "castr")
                        SRWScalars[(int)GrazType.ReproType.Castrated] = fValue;
                    else if (sTagList[2] == "male")
                        SRWScalars[(int)GrazType.ReproType.Male] = fValue;
                }
                else if (sTagList[1] == "swn")
                    SelfWeanPropn = fValue;
                else
                {
                    Idx = Convert.ToInt32(sTagList[2], CultureInfo.InvariantCulture);

                    if (sTagList[1] == "n")
                        GrowthC[Idx] = fValue;
                    else if (sTagList[1] == "i")
                        IntakeC[Idx] = fValue;
                    else if (sTagList[1] == "imx")
                    {
                        IntakeLactC[Idx] = fValue;
                        if (Idx == 0)
                          FDairyIntakePeak = fValue;
                    }
                    else if (sTagList[1] == "r")
                        GrazeC[Idx] = fValue;
                    else if (sTagList[1] == "k")
                        EfficC[Idx] = fValue;
                    else if (sTagList[1] == "m")
                        MaintC[Idx] = fValue;
                    else if (sTagList[1] == "rd")
                        DgProtC[Idx] = fValue;
                    else if (sTagList[1] == "a")
                        ProtC[Idx] = fValue;
                    else if (sTagList[1] == "p")
                        PregC[Idx] = fValue;
                    else if (sTagList[1] == "p14")
                        PregScale[Idx] = fValue;
                    else if (sTagList[1] == "p15")
                        BirthWtScale[Idx] = fValue;
                    else if (sTagList[1] == "l0")
                        PeakLactC[Idx] = fValue;
                    else if (sTagList[1] == "l")
                        LactC[Idx] = fValue;
                    else if (sTagList[1] == "w")
                        WoolC[Idx] = fValue;
                    else if (sTagList[1] == "c")
                        ChillC[Idx] = fValue;
                    else if (sTagList[1] == "g")
                        GainC[Idx] = fValue;
                    else if (sTagList[1] == "ph")
                        PhosC[Idx] = fValue;
                    else if (sTagList[1] == "su")
                        SulfC[Idx] = fValue;
                    else if (sTagList[1] == "h")
                        MethC[Idx] = fValue;
                    else if (sTagList[1] == "aa")
                        AshAlkC[Idx] = fValue;
                    else if (sTagList[1] == "f1")
                        DayLengthConst[Idx] = fValue;
                    else if (sTagList[1] == "f2")
                        ConceiveSigs[Idx][0] = fValue;
                    else if (sTagList[1] == "f3")
                        ConceiveSigs[Idx][1] = fValue;
                    else if (sTagList[1] == "d")
                    {
                        switch (Idx)
                        {
                            case 1:
                                MortRate[1] = fValue;
                                break;
                            case 2:
                                MortIntensity = fValue;
                                break;
                            case 3:
                                MortCondConst = fValue;
                                break;
                            case 4:
                            case 5:
                                ToxaemiaSigs[Idx - 4] = fValue;
                                break;
                            case 6:
                            case 7:
                                DystokiaSigs[Idx - 6] = fValue;
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                                ExposureConsts[Idx - 8] = fValue;
                                break;
                            case 12:
                                MortWtDiff = fValue;
                                break;
                            case 13:
                                MortRate[2] = fValue;
                                break;
                            case 14:
                                MortAge[1] = fValue;
                                break;
                            case 15:
                                MortAge[2] = fValue;
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="iValue"></param>
        override protected void SetIntParam(string[] sTagList, int iValue)
        {
            if (sTagList[0] == "c")
            {
                if (sTagList[1] == "f4")
                    OvulationPeriod = iValue;
                else if (sTagList[1] == "pbt")
                {
                    if (sTagList[2] == "female")
                        Puberty[0] = iValue;    //[false]
                    else if (sTagList[2] == "male")
                        Puberty[1] = iValue; //[true]
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="sValue"></param>
        override protected void SetTextParam(string[] sTagList, string sValue)
        {
            if (sTagList[0] == "editor")
                sEditor = sValue;
            else if (sTagList[0] == "edited")
                sEditDate = sValue;
            else if (sTagList[0] == "animal")
            {
                if (sValue.ToLower().Trim() == GrazType.AnimalText[(int)GrazType.AnimalType.Cattle].ToLower())
                    Animal = GrazType.AnimalType.Cattle;
                else
                    Animal = GrazType.AnimalType.Sheep;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="bValue"></param>
        override protected void SetBoolParam(string[] sTagList, bool bValue)
        {
            if (sTagList[0] == "dairy")
                bDairyBreed = bValue;
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
        /// WM(peak)
        /// </summary>
        public double PeakMilk { get; set; }

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
        /// Construct and animal parameter set
        /// </summary>
        public AnimalParamSet()
            : base()
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            ConstructCopy(null);
        }

        /// <summary>
        /// Construct an animal parameter set from a source one
        /// </summary>
        public AnimalParamSet(AnimalParamSet src)
            : base(src)
        {
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            ConstructCopy(src);
        }

        /// <summary>
        /// Alternative copy constructor
        /// </summary>
        /// <param name="aParent"></param>
        /// <param name="srcSet"></param>
        public AnimalParamSet(ParameterSet aParent, AnimalParamSet srcSet)
            : base(aParent/*, srcSet*/)
        {
            //create a new array
            for (int i = 0; i < ConceiveSigs.Length; i++)
                ConceiveSigs[i] = new double[2];
            ConstructCopy(srcSet);

            if (srcSet != null)
                Initialise();
        }

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
        /// Copies a parameter set from AnimalParamsGlb
        /// </summary>
        /// <param name="sBreedName"></param>
        public static AnimalParamSet CreateFactory(string sBreedName)
        {
            AnimalParamSet newObj = null;

            AnimalParamSet baseParams;
            GlobalAnimalParams animalParams = new GlobalAnimalParams();
            baseParams = (AnimalParamSet)animalParams.AnimalParamsGlb().GetNode(sBreedName);
            if (baseParams != null)
                newObj = new AnimalParamSet(null, baseParams);
            else
            {
                newObj = new AnimalParamSet();
                throw new Exception("Breed name \"" + sBreedName + "\" not recognised");
            }
            
            return newObj;
        }

        /// <summary>
        /// Creates an object based on the parameters passed
        /// </summary>
        /// <param name="sBreedName"></param>
        /// <param name="Blend"></param>
        /// <returns></returns>
        public static AnimalParamSet CreateFactory(string sBreedName, AnimalParamBlend[] Blend)
        {
            AnimalParamSet newObj = null;
            if (Blend.Length == 0)                                                   // No mixture of breeds provided, so     
                newObj = CreateFactory(sBreedName);                                     //   copy a breed from AnimalParamsGlb   
            else if (Blend.Length == 2)                                             // Special case: optimized for speed     
            {
                newObj = new AnimalParamSet((ParameterSet)null, (AnimalParamSet)null);
            }
            else
            {
                newObj = new AnimalParamSet(null, Blend[0].Breed);                            // Sets the integer, string and Boolean  
            }
            newObj.InitParameterSet(sBreedName, Blend);

            return newObj;
        }

        /// <summary>
        /// Called by CreateFactory() and creates a mixture of several genotypes                                       
        /// </summary>
        /// <param name="sBreedName"></param>
        /// <param name="Blend"></param>
        virtual public void InitParameterSet(string sBreedName, AnimalParamBlend[] Blend)
        {
            ParameterDefinition prmDefn;
            AnimalParamSet Breed0;
            AnimalParamSet Breed1;
            double fPropn0;
            double fPropn1;
            double fParamSum;
            double fPropnSum;
            int iDecPlaces;
            int Idx, Jdx, Kdx;
            //TGrazType.ReproType Repro;

            if (Blend.Length == 2)                                             // Special case: optimized for speed     
            {                                                                  //   (used in producing offspring)       
                Breed0 = Blend[0].Breed;
                Breed1 = Blend[1].Breed;

                fPropn0 = Blend[0].fPropn;
                fPropn1 = Blend[1].fPropn;
                if (fPropn1 > 0.0)
                    fPropn0 = fPropn0 / (fPropn0 + fPropn1);
                else
                    fPropn0 = 1.0;
                fPropn1 = 1.0 - fPropn0;

                sEditor = Breed0.sEditor;                                       // String and integer parameters         
                sEditDate = Breed0.sEditDate;                                     //   (consistent with the general case)  
                Animal = Breed0.Animal;
                bDairyBreed = Breed0.bDairyBreed;
                MaxYoung = Breed0.MaxYoung;
                OvulationPeriod = Breed0.OvulationPeriod;
                Puberty = Breed0.Puberty;

                FBreedSRW = fPropn0 * Breed0.FBreedSRW + fPropn1 * Breed1.FBreedSRW;
                FPotFleeceWt = fPropn0 * Breed0.FPotFleeceWt + fPropn1 * Breed1.FPotFleeceWt;
                FDairyIntakePeak = fPropn0 * Breed0.FDairyIntakePeak + fPropn1 * Breed1.FDairyIntakePeak;
                FleeceRatio = fPropn0 * Breed0.FleeceRatio + fPropn1 * Breed1.FleeceRatio;
                MaxFleeceDiam = fPropn0 * Breed0.MaxFleeceDiam + fPropn1 * Breed1.MaxFleeceDiam;
                PeakMilk = fPropn0 * Breed0.PeakMilk + fPropn1 * Breed1.PeakMilk;
                for (Idx = 1; Idx <= 2; Idx++)
                    MortRate[Idx] = fPropn0 * Breed0.MortRate[Idx] + fPropn1 * Breed1.MortRate[Idx];
                for (Idx = 1; Idx <= 2; Idx++)
                    MortAge[Idx] = fPropn0 * Breed0.MortAge[Idx] + fPropn1 * Breed1.MortAge[Idx];
                MortIntensity = fPropn0 * Breed0.MortIntensity + fPropn1 * Breed1.MortIntensity;
                MortCondConst = fPropn0 * Breed0.MortCondConst + fPropn1 * Breed1.MortCondConst;
                MortWtDiff = fPropn0 * Breed0.MortWtDiff + fPropn1 * Breed1.MortWtDiff;

                for (Idx = 0; Idx < SRWScalars.Length; Idx++) SRWScalars[Idx] = fPropn0 * Breed0.SRWScalars[Idx] + fPropn1 * Breed1.SRWScalars[Idx];
                for (Idx = 1; Idx < GrowthC.Length; Idx++) GrowthC[Idx] = fPropn0 * Breed0.GrowthC[Idx] + fPropn1 * Breed1.GrowthC[Idx];
                for (Idx = 1; Idx < IntakeC.Length; Idx++) IntakeC[Idx] = fPropn0 * Breed0.IntakeC[Idx] + fPropn1 * Breed1.IntakeC[Idx];
                for (Idx = 0; Idx < IntakeLactC.Length; Idx++) IntakeLactC[Idx] = fPropn0 * Breed0.IntakeLactC[Idx] + fPropn1 * Breed1.IntakeLactC[Idx];
                for (Idx = 1; Idx < GrazeC.Length; Idx++) GrazeC[Idx] = fPropn0 * Breed0.GrazeC[Idx] + fPropn1 * Breed1.GrazeC[Idx];
                for (Idx = 1; Idx < EfficC.Length; Idx++) EfficC[Idx] = fPropn0 * Breed0.EfficC[Idx] + fPropn1 * Breed1.EfficC[Idx];
                for (Idx = 1; Idx < MaintC.Length; Idx++) MaintC[Idx] = fPropn0 * Breed0.MaintC[Idx] + fPropn1 * Breed1.MaintC[Idx];
                for (Idx = 1; Idx < DgProtC.Length; Idx++) DgProtC[Idx] = fPropn0 * Breed0.DgProtC[Idx] + fPropn1 * Breed1.DgProtC[Idx];
                for (Idx = 1; Idx < ProtC.Length; Idx++) ProtC[Idx] = fPropn0 * Breed0.ProtC[Idx] + fPropn1 * Breed1.ProtC[Idx];
                for (Idx = 1; Idx < PregC.Length; Idx++) PregC[Idx] = fPropn0 * Breed0.PregC[Idx] + fPropn1 * Breed1.PregC[Idx];
                for (Idx = 1; Idx < PregScale.Length; Idx++) PregScale[Idx] = fPropn0 * Breed0.PregScale[Idx] + fPropn1 * Breed1.PregScale[Idx];
                for (Idx = 1; Idx < BirthWtScale.Length; Idx++) BirthWtScale[Idx] = fPropn0 * Breed0.BirthWtScale[Idx] + fPropn1 * Breed1.BirthWtScale[Idx];
                for (Idx = 1; Idx < PeakLactC.Length; Idx++) PeakLactC[Idx] = fPropn0 * Breed0.PeakLactC[Idx] + fPropn1 * Breed1.PeakLactC[Idx];
                for (Idx = 1; Idx < LactC.Length; Idx++) LactC[Idx] = fPropn0 * Breed0.LactC[Idx] + fPropn1 * Breed1.LactC[Idx];
                for (Idx = 1; Idx < WoolC.Length; Idx++) WoolC[Idx] = fPropn0 * Breed0.WoolC[Idx] + fPropn1 * Breed1.WoolC[Idx];
                for (Idx = 1; Idx < ChillC.Length; Idx++) ChillC[Idx] = fPropn0 * Breed0.ChillC[Idx] + fPropn1 * Breed1.ChillC[Idx];
                for (Idx = 1; Idx < GainC.Length; Idx++) GainC[Idx] = fPropn0 * Breed0.GainC[Idx] + fPropn1 * Breed1.GainC[Idx];
                for (Idx = 1; Idx < PhosC.Length; Idx++) PhosC[Idx] = fPropn0 * Breed0.PhosC[Idx] + fPropn1 * Breed1.PhosC[Idx];
                for (Idx = 1; Idx < SulfC.Length; Idx++) SulfC[Idx] = fPropn0 * Breed0.SulfC[Idx] + fPropn1 * Breed1.SulfC[Idx];
                for (Idx = 1; Idx < MethC.Length; Idx++) MethC[Idx] = fPropn0 * Breed0.MethC[Idx] + fPropn1 * Breed1.MethC[Idx];
                for (Idx = 1; Idx < AshAlkC.Length; Idx++) AshAlkC[Idx] = fPropn0 * Breed0.AshAlkC[Idx] + fPropn1 * Breed1.AshAlkC[Idx];
                for (Idx = 1; Idx < DayLengthConst.Length; Idx++) DayLengthConst[Idx] = fPropn0 * Breed0.DayLengthConst[Idx] + fPropn1 * Breed1.DayLengthConst[Idx];
                for (Idx = 0; Idx < ToxaemiaSigs.Length; Idx++) ToxaemiaSigs[Idx] = fPropn0 * Breed0.ToxaemiaSigs[Idx] + fPropn1 * Breed1.ToxaemiaSigs[Idx];
                for (Idx = 0; Idx < DystokiaSigs.Length; Idx++) DystokiaSigs[Idx] = fPropn0 * Breed0.DystokiaSigs[Idx] + fPropn1 * Breed1.DystokiaSigs[Idx];
                for (Idx = 0; Idx < ExposureConsts.Length; Idx++) ExposureConsts[Idx] = fPropn0 * Breed0.ExposureConsts[Idx] + fPropn1 * Breed1.ExposureConsts[Idx];

                FertWtDiff = fPropn0 * Breed0.FertWtDiff + fPropn1 * Breed1.FertWtDiff;
                SelfWeanPropn = fPropn0 * Breed0.SelfWeanPropn + fPropn1 * Breed1.SelfWeanPropn;
                for (Idx = 1; Idx < ConceiveSigs.Length; Idx++)
                    for (Jdx = 0; Jdx < ConceiveSigs[Idx].Length; Jdx++)
                        ConceiveSigs[Idx][Jdx] = fPropn0 * Breed0.ConceiveSigs[Idx][Jdx] + fPropn1 * Breed1.ConceiveSigs[Idx][Jdx];

                for (Idx = 0; Idx <= DefinitionCount() - 1; Idx++)
                    GetDefinition(Idx).SetDefined(Blend[0].Breed.GetDefinition(Idx));
            }
            else                                                                         // Mixture of breeds provided            
            {
                if (Blend.Length > 1)                                                 // Blend the numeric parameter values    
                {
                    for (Idx = 0; Idx <= ParamCount() - 1; Idx++)
                    {
                        prmDefn = GetParam(Idx);
                        if (prmDefn.ParamType == TYPEREAL)
                        {
                            fParamSum = 0.0;
                            fPropnSum = 0.0;
                            for (Jdx = 0; Jdx <= Blend.Length - 1; Jdx++)
                            {
                                if (Blend[Jdx].Breed.IsDefined(prmDefn.FullName))
                                {
                                    fParamSum = fParamSum + Blend[Jdx].fPropn * Blend[Jdx].Breed.ParamReal(prmDefn.FullName);
                                    fPropnSum = fPropnSum + Blend[Jdx].fPropn;
                                }
                            }
                            if (fPropnSum > 0.0)
                                SetParam(prmDefn.FullName, fParamSum / fPropnSum);
                        }
                    }
                }
            }

            if (Blend.Length > 0)
            {
                fPropnSum = 0.0;
                for (Jdx = 0; Jdx <= Blend.Length - 1; Jdx++)
                    fPropnSum = fPropnSum + Blend[Jdx].fPropn;

                if (fPropnSum > 0.0)
                {
                    for (Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                        FParentage[Idx].fPropn = 0.0;
                    for (Jdx = 0; Jdx <= Blend.Length - 1; Jdx++)
                    {
                        for (Kdx = 0; Kdx <= Blend[Jdx].Breed.FParentage.Length - 1; Kdx++)
                        {
                            Idx = 0;
                            while ((Idx < FParentage.Length) && (Blend[Jdx].Breed.FParentage[Kdx].sBaseBreed != FParentage[Idx].sBaseBreed))
                                Idx++;
                            if (Idx == FParentage.Length)
                            {
                                Array.Resize(ref FParentage, Idx + 1);
                                FParentage[Idx].sBaseBreed = Blend[Jdx].Breed.FParentage[Kdx].sBaseBreed;
                                FParentage[Idx].fPropn = 0.0;
                            }
                            FParentage[Idx].fPropn = FParentage[Idx].fPropn
                                                      + (Blend[Jdx].fPropn / fPropnSum) * Blend[Jdx].Breed.FParentage[Kdx].fPropn;
                        }
                    }
                }
            }

            if (sBreedName != "")                                                    // Construct a name for the new genotype 
                Name = sBreedName;
            else if (FParentage.Length == 1)
                Name = FParentage[0].sBaseBreed;
            else if (FParentage.Length > 1)
            {
                iDecPlaces = 0;
                for (Idx = 0; Idx <= FParentage.Length - 1; Idx++)
                {
                    if ((FParentage[Idx].fPropn > 0.0005) && (FParentage[Idx].fPropn <= 0.05))
                        iDecPlaces = 1;
                }

                Name = "";
                for (Idx = 0; Idx <= FParentage.Length - 1; Idx++)
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
        /// Mix of two genotypes (as at mating)
        /// </summary>
        /// <param name="sBreedName"></param>
        /// <param name="damBreed"></param>
        /// <param name="sireBreed"></param>
        /// <param name="iGeneration"></param>
        /// <returns>The new object</returns>
        public static AnimalParamSet CreateFactory(string sBreedName, AnimalParamSet damBreed, AnimalParamSet sireBreed, int iGeneration = 1)
        {
            AnimalParamBlend[] aBlend = new AnimalParamBlend[2];

            aBlend[0].Breed = damBreed;
            aBlend[0].fPropn = Math.Pow(0.5, iGeneration);
            aBlend[1].Breed = sireBreed;
            aBlend[1].fPropn = 1.0 - aBlend[0].fPropn;
            return CreateFactory(sBreedName, aBlend);
        }

        /// <summary>
        /// 
        /// </summary>
        override public void DeriveParams()
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
        /// Returns TRUE i.f.f. all parameters other than the breed name are identical
        /// </summary>
        /// <param name="otherSet"></param>
        /// <returns></returns>
        public bool bFunctionallySame(AnimalParamSet otherSet)
        {
            int iCount;
            ParameterDefinition Defn;
            string sTag;
            int iPrm;

            bool result = true;
            iCount = this.ParamCount();

            iPrm = 0;
            while ((iPrm < iCount) && result)
            {
                Defn = this.GetParam(iPrm);
                if (Defn != null)
                {
                    sTag = Defn.FullName;
                    if (this.IsDefined(sTag))
                    {
                        if (Defn.ParamType == TYPETEXT)
                            result = (result && (this.ParamStr(sTag) == otherSet.ParamStr(sTag)));
                        else if (Defn.ParamType == TYPEREAL)
                            result = (result && (this.ParamReal(sTag) == otherSet.ParamReal(sTag)));
                        else if (Defn.ParamType == TYPEINT)
                            result = (result && (this.ParamInt(sTag) == otherSet.ParamInt(sTag)));
                        else if (Defn.ParamType == TYPEBOOL)
                            result = (result && (this.ParamBool(sTag) == otherSet.ParamBool(sTag)));
                        else
                            result = false;
                    }
                    else
                        result = (result && !otherSet.IsDefined(Defn.FullName));
                }
                else
                    result = false;

                iPrm++;
            }
            return result;
        }

        /// <summary>
        /// Returns the parameter set corresponding to a given name.                 
        /// * sBreedName may actually be the name of a "breed group", i.e. a comma-   
        ///   separated list of functionally identical breeds. In this case the       
        ///   parameter set for the first member of the group is returned.            
        /// </summary>
        /// <param name="breedName">The breed name</param>
        /// <returns></returns>
        public AnimalParamSet Match(string breedName)
        {
            if (breedName.IndexOf(',') >= 0)
                breedName = breedName.Remove(breedName.IndexOf(','), breedName.Length - breedName.IndexOf(','));

            return (AnimalParamSet)GetNode(breedName);
        }

        /// <summary>
        /// Returns the number of breeds of a given animal type
        /// </summary>
        /// <param name="animalType">The animal type</param>
        /// <returns></returns>
        public int BreedCount(GrazType.AnimalType animalType)
        {
            AnimalParamSet breedSet;
            
            int result = 0;
            for (int idx = 0; idx <= LeafCount(true) - 1; idx++)                                     // Current locale only                      
            {
                breedSet = (AnimalParamSet)GetLeaf(idx, true);
                if (breedSet.Animal == animalType)
                    result++;
            }

            return result;
        }

        /// <summary>
        /// Iterates through breeds of a given animal type and returns the breed name
        /// </summary>
        /// <param name="animalType">The animal type</param>
        /// <param name="breedIdx">The breed index 0-n</param>
        /// <returns></returns>
        public string BreedName(GrazType.AnimalType animalType, int breedIdx)
        {
            AnimalParamSet breedSet;
            int count;
            int found;
            int idx;

            count = LeafCount(true);                                             // Current locale only                      
            found = -1;
            idx = 0;
            breedSet = null;
            while ((idx < count) && (found < breedIdx))
            {
                breedSet = (AnimalParamSet)GetLeaf(idx, true);
                if (breedSet.Animal == animalType)
                    found++;
                idx++;
            }

            if (found == breedIdx)
                return breedSet.Name;
            else
                return "";
        }

        /// <summary>
        /// Populates a string list with the names of "breed groups", i.e. sets of    
        /// parameter sets that are identical in all respects save their names.       
        /// </summary>
        /// <param name="animalType">The animal type</param>
        /// <param name="breedList">The list of breeds found that are the same</param>
        public void GetBreedGroups(GrazType.AnimalType animalType, List<string> breedList)
        {
            bool sameFound;
            int idx, jdx;

            breedList.Clear();                                                              // Start by forming a list of all breeds.   
            for (idx = 0; idx <= BreedCount(animalType) - 1; idx++)
                breedList.Add(BreedName(animalType, idx));

            for (idx = breedList.Count - 1; idx >= 1; idx--)
            {
                sameFound = false;
                for (jdx = idx - 1; jdx >= 0; jdx--)
                    if (!sameFound)
                    {
                        sameFound = Match(breedList[idx]).bFunctionallySame(Match(breedList[jdx]));
                        if (sameFound)
                        {
                            breedList[jdx] = breedList[jdx] + ", " + breedList[idx];
                            breedList.RemoveAt(idx);
                        }
                    }
            }
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
        /// 
        /// </summary>
        /// <param name="bWeaners"></param>
        /// <param name="AnnDeaths"></param>
        public void SetAnnualDeaths(bool bWeaners, double AnnDeaths)
        {
            setDeaths(bWeaners, AnnDeaths);
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
        /// 
        /// </summary>
        /// <param name="fSRW"></param>
        /// <returns></returns>
        public double fSRWToPFW(double fSRW)
        {
            return FleeceRatio * fSRW;
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
        static public double fDefaultFleece(AnimalParamSet Params,
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
        static public double fDefaultMicron(AnimalParamSet Params, int iAgeDays, GrazType.ReproType Repr, int iFleeceDays, double fGFW)
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
    }
}