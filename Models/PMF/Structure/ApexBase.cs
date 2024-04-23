using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Newtonsoft.Json;

namespace Models.PMF.Struct
{

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ApexGroup
    {
        /// <summary>The number of apex in each age group.</summary>
        private List<double> apexGroupSize = new List<double>();

        /// <summary>The age of apex in age group.</summary>
        private List<double> apexGroupAge = new List<double>();

        /// <summary>Total apex number in plant.</summary>
        [JsonIgnore]
        [Description("Total apex number in plant")]
        public double Number { get; set; }

        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupSize
        {
            get
            {
                return apexGroupSize.ToArray();
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    apexGroupSize.Add(value[i]);
            }
        }


        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupAge
        {
            get
            {
                return apexGroupAge.ToArray();
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    apexGroupAge.Add(value[i]);
            }
        }


    }


    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public abstract class ApexBase : Model, IApex
    {
        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private Structure structure = null;

        [Link(Type = LinkType.Child)]
        private IFunction stemSenescenceAge = null;

        /// <summary>The apex group.</summary>
        private List<double> apexGroupSize = new List<double>();

        /// <summary>The age of apex in age group.</summary>
        private List<double> apexGroupAge = new List<double>();

        /// <summary>Total apex number in plant.</summary>
        [JsonIgnore]
        [Description("Total apex number in plant")]
        public double Number { get; set; }

        /// <value>Senscenced by age.</value>
        [JsonIgnore]
        public bool SenescenceByAge { get; set; }

        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupSize
        {
            get
            {
                return apexGroupSize.ToArray();
            }
        }


        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupAge
        {
            get
            {
                return apexGroupAge.ToArray();
            }
        }

        /// <summary>Apex number by age</summary>
        /// <param name="age">Threshold age</param>
        public double NumByAge(double age)
        {
            //Calculation the infertile tiller number and 
            //Scale up to population scale

            //Leaf cohort model is a single plant model. Structure model is a population model.Apex model need to scale up from single plant level to population scale.
            //In the population scale, all tillers in the population were split into two groups(i.e.strong and weak) according to appeared tip number.The age for infertile tiller also indicated the probability of tiller senescence for the non-integer value.

            //Case | Tips | Age of infertile | Infertile tillers | Notes
            //-------- | ----- | ----------------- | -------------------------  | ---------------------------------------------
            //1 | 7 | 3 | N1 + N2 + N3 |
            //2 | 7 | 3.4 | N1 + N2 + N3 + N4 * 0.4 | Non - integer in the age of infertile indicates partial senescence for tillers with age 4(40 %)
            //3 | 7.2 | 3 | (N1 + N2 + N3) * 0.8 + (N1 + N2) * 0.2 | We assume there are 20 % tillers are stronger another 50 % tillers(i.e.one more leaves).Consequently, the tillers can be split into two groups by 20 % (stronger)and 80 % (weaker).For 20 % tillers, the Age of infertile equals to 2.For 80 % tillers, the Age of infertile equals to 3.
            //4 | 7.2 | 3 .4 | (N1 + N2 + N3 + N4\*0.4)\*0.8 + (N1 + N2 + N3\*0.4)\*0.2 | We still split all tillers into two groups(i.e.stronger and weaker) as Case 3.In each group, the method in Case 2 is used to calculate the infertile tillers.

            //In the codes, the total number of infertile tillers is calculated through looping all apex groups. 

            //Case | Tips | Age of infertile | weakTillerRatio | strongTillerRatio | weakAge | strongAge | ageFraction
            //-------- | ----- | --------------------- | -----------------------  | --------------------  | ------------- | --------------  | -------------
            //1 | 7 | 3 | 1 | 0 | 4 | 3 | 0
            //1 | 7 | 3.4 | 1 | 0 | 4 | 3 | 0.4
            //1 | 7.2 | 3 | 0.8 | 0 .2 | 4 | 3 | 0
            //1 | 7.2 | 3.4 | 0.8 | 0 .2 | 4 | 3 | 0.4

            double num = 0;
            double strongTillerRatio = structure.LeafTipsAppeared - Math.Floor(structure.LeafTipsAppeared);
            double weakTillerRatio = 1 - strongTillerRatio;
            double weakAge = Math.Floor(age) + 1;
            double strongAge = weakAge - 1;
            double ageFraction = age - Math.Truncate(age);

            for (int i = apexGroupAge.Count; i > 0; i--)
            {
                if (apexGroupAge[i - 1] < weakAge)
                {
                    num += weakTillerRatio * apexGroupSize[i - 1];
                }
                else if (Math.Abs(apexGroupAge[i - 1] - weakAge) < Double.Epsilon)
                {
                    num += weakTillerRatio * apexGroupSize[i - 1] * ageFraction;
                }
                if (apexGroupAge[i - 1] < strongAge)
                {
                    num += strongTillerRatio * apexGroupSize[i - 1];
                }
                else if (Math.Abs(apexGroupAge[i - 1] - strongAge) < Double.Epsilon)
                {
                    num += strongTillerRatio * apexGroupSize[i - 1] * ageFraction;
                }
            }
            return num;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public abstract double Appearance(double population, double totalStemPopn);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public abstract double LeafTipAppearance(double population, double totalStemPopn);

        /// <summary>
        /// Reset the apex instance
        /// </summary>
        public void Reset()
        {
            apexGroupAge.Clear();
            apexGroupSize.Clear();
            SenescenceByAge = false;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                if (structure.TimeForAnotherLeaf && structure.AllLeavesAppeared == false)
                {
                    // Apex calculation
                    Number += structure.branchingRate.Value() * structure.PrimaryBudNo;


                    // update age of cohorts
                    for (int i = 0; i < apexGroupAge.Count; i++)
                        apexGroupAge[i]++;

                }
                // Reduce the apex number by branching mortality
                Number -= structure.branchMortality.Value() * (Number - 1);


                // check for increase in apex size, add new group if needed
                // (ApexNum should be an integer, but need to check in case of flag leaf)
                double deltaApex = apexGroupSize.Sum() - Number;
                if (deltaApex < -1E-12)
                {
                    if (apexGroupSize.Count == 0)
                        apexGroupSize.Add(Number);
                    else
                        apexGroupSize.Add(-deltaApex);
                    apexGroupAge.Add(1);
                }

                // check for reduction in the apex size
                if ((apexGroupSize.Count > 0) && (deltaApex > 1E-12))
                {
                    double remainingRemoveApex = deltaApex;
                    for (int i = apexGroupSize.Count - 1; i > 0; i--)
                    {
                        double remove = Math.Min(apexGroupSize[i], remainingRemoveApex);
                        apexGroupSize[i] -= remove;
                        remainingRemoveApex -= remove;
                        if (remainingRemoveApex <= 0.00001)
                            break;
                    }

                    if (remainingRemoveApex > 0.00001)
                        throw new Exception("There are not enough apex to remove from plant.");
                }
                if (phenology.Stage > 4 && !SenescenceByAge)
                {
                    double senescenceNum = NumByAge(stemSenescenceAge.Value());
                    Number -= senescenceNum;
                    Number = Math.Max(1, Number);
                    SenescenceByAge = true;
                    structure.TotalStemPopn -= senescenceNum * plant.Population;
                }
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">sender of the event.</param>
        /// <param name="Sow">Sowing data to initialise from.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters Sow)
        {
            if (Sow.Plant == plant)
            {
                Reset();
                Number = structure.PrimaryBudNo;
                apexGroupAge.Add(1);
                apexGroupSize.Add(1);
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            Reset();
        }

    }
}
