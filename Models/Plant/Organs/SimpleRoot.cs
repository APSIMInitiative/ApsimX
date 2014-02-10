using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.Soils;

namespace Models.PMF.Organs
{
    [Serializable]
    public class SimpleRoot : BaseOrgan // FIXME HEB This was inheriting from organ but changed to base organ to fix bug. Need to check collatoral impacts
    {
        [Link]
        Plant Plant = null;

        [Link]
        Function DMDemandFunction = null;

        private double Uptake = 0;
        private string CurrentPaddockName;
        private string OurName;
        private bool TalkDirectlyToRoot;

        public override BiomassPoolType DMDemand
        {
            get
            {
                double Demand = 0;
                if (DMDemandFunction != null)
                    Demand = DMDemandFunction.Value;
                else Demand = 0;
                return new BiomassPoolType { Structural = Demand };
            }
        }

        public override BiomassSupplyType DMSupply { get { return new BiomassSupplyType { Fixation = 0, Retranslocation = 0 }; } }
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                Live.StructuralWt += value.Structural;
            }
        }

        public override BiomassPoolType NDemand { get { return new BiomassPoolType(); } }
        public override BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } }
        public override double WaterDemand { get { return 0; } }

        
        [Units("mm")]
        public override double WaterUptake
        {
            get
            {
                return Uptake;
            }
        }
        public override double WaterAllocation
        {
            get { return 0; }
            set
            {
                throw new Exception("Cannot set water allocation for roots");
            }
        }

        private bool RootModelExists
        {
            get
            {
                if (this.Find(Plant.Name + "Root") != null)
                    return true;
                else
                    return false;
            }
        }

        
        public override double WaterSupply
        {
            get
            {
                CurrentPaddockName = this.FullPath;
                OurName = CurrentPaddockName;
                if (OurName.Length > 0)
                    OurName += ".";
                OurName += Plant.Name;

                TalkDirectlyToRoot = RootModelExists;

                double[] SWSupply;
                if (TalkDirectlyToRoot)
                {
                    SWSupply = (double[]) this.Get(OurName + "Root.SWSupply");
                    return Utility.Math.Sum(SWSupply);
                }

                else
                {
                    double Total = 0;
                    //foreach (Zone SubPaddock in this.Models)
                    //{
                    //    SWSupply = (double[]) this.Get(SubPaddock.FullPath + "." + Plant.Name + "Root.SWSupply");
                    //    Total += Utility.Math.Sum(SWSupply);
                    //}
                    return Total;
                }
            }
        }




        public override void DoWaterUptake(double Amount)
        {
            Uptake = Amount;
            if (TalkDirectlyToRoot)
                //MyPaddock.Set(OurName + "Root.SWUptake", Amount);
                throw new NotImplementedException();
            else
            {
                throw new NotImplementedException();

                //List<string> ModelNames = new List<string>();
                //List<double> Supply = new List<double>();
                //foreach (Zone SubPaddock in this.Models)
                //{
                //    string ModelName = SubPaddock.FullPath + "." + Plant.Name + "Root";
                //    double[] SWSupply = (double[])this.Get(ModelName + ".SWSupply");
                //    Supply.Add(Utility.Math.Sum(SWSupply));
                //    ModelNames.Add(ModelName);
                //}
                //double fraction = Amount / Utility.Math.Sum(Supply);
                //if (fraction > 1)
                //    throw new Exception("Requested SW uptake > Available supplies.");
                ////int i = 0;
                //foreach (string ModelName in ModelNames)
                //{
                //    //MyPaddock.Set(ModelName + ".SWUptake", Supply[i] * fraction);
                //    throw new NotImplementedException();
                //    //i++;
                //}

            }

        }

    }
}
