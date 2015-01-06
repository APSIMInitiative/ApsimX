using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.Soils;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A simple root organ
    /// </summary>
    [Serializable]
    public class SimpleRoot : BaseOrgan // FIXME HEB This was inheriting from organ but changed to base organ to fix bug. Need to check collatoral impacts
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>The dm demand function</summary>
        [Link]
        Function DMDemandFunction = null;

        /// <summary>The uptake</summary>
        private double Uptake = 0;
        /// <summary>The current paddock name</summary>
        private string CurrentPaddockName;
        /// <summary>Our name</summary>
        private string OurName;
        /// <summary>The talk directly to root</summary>
        private bool TalkDirectlyToRoot;

        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
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

        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override BiomassSupplyType DMSupply { get { return new BiomassSupplyType { Fixation = 0, Retranslocation = 0 }; } }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                Live.StructuralWt += value.Structural;
            }
        }

        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        public override BiomassPoolType NDemand { get { return new BiomassPoolType(); } }
        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        public override BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } }
        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        public override double WaterDemand { get { return 0; } }


        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public override double WaterUptake
        {
            get
            {
                return Uptake;
            }
        }
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        /// <exception cref="System.Exception">Cannot set water allocation for roots</exception>
        public override double WaterAllocation
        {
            get { return 0; }
            set
            {
                throw new Exception("Cannot set water allocation for roots");
            }
        }

        /// <summary>
        /// Gets a value indicating whether [root model exists].
        /// </summary>
        /// <value><c>true</c> if [root model exists]; otherwise, <c>false</c>.</value>
        private bool RootModelExists
        {
            get
            {
                if (Apsim.Find(this, Plant.Name + "Root") != null)
                    return true;
                else
                    return false;
            }
        }


        /// <summary>Gets or sets the water supply.</summary>
        /// <value>The water supply.</value>
        public override double WaterSupply
        {
            get
            {
                CurrentPaddockName = Apsim.FullPath(this);
                OurName = CurrentPaddockName;
                if (OurName.Length > 0)
                    OurName += ".";
                OurName += Plant.Name;

                TalkDirectlyToRoot = RootModelExists;

                double[] SWSupply;
                if (TalkDirectlyToRoot)
                {
                    SWSupply = (double[])Apsim.Get(this, OurName + "Root.SWSupply");
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




        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
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
