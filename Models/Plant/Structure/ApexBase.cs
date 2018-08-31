namespace Models.PMF.Struct
{
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

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

        [ChildLink]
        private IFunction stemSenescenceAge = null;

        /// <summary>The number of apex in each age group.</summary>
        private List<double> apexGroupSize = new List<double>();

        /// <summary>The age of apex in age group.</summary>
        private List<double> apexGroupAge = new List<double>();

        /// <summary>Total apex number in plant.</summary>
        [XmlIgnore]
        [Description("Total apex number in plant")]
        public double Number { get; set; }

        /// <value>Senscenced by age.</value>
        [XmlIgnore]
        public bool SenescenceByAge { get; set; }

        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupSize { get { return apexGroupSize.ToArray(); } }

        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double[] GroupAge { get { return apexGroupAge.ToArray(); } }

        /// <summary>Apex number by age</summary>
        /// <param name="age">Threshold age</param>
        public double NumByAge(double age)
        {
            double num = 0;
            for (int i = 0; i < apexGroupAge.Count; i++)
            {
                if (apexGroupAge[i] <= age)
                {
                    num += apexGroupSize[i++];
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
        /// TODO: This method needs documentation
        /// </summary>
        /// <param name="NewLeaf"></param>
        public void DoCalculations(LeafCohort NewLeaf)
        {
            // update age of cohorts
            for (int i = 0; i < apexGroupAge.Count; i++)
                apexGroupAge[i]++;

            // check for increase in apex size, add new group if needed
            // (ApexNum should be an integer, but need to check in case of flag leaf)
            double deltaApex = apexGroupSize.Sum() - Number;
            if (deltaApex < -1E-12)
            {
                if (apexGroupSize.Count == 0)
                    apexGroupSize.Add(Number);
                else
                    apexGroupSize.Add(Number - apexGroupSize[apexGroupSize.Count - 1]);
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
                    if (remainingRemoveApex <= 0.0)
                        break;
                }

                if (remainingRemoveApex > 0.0)
                    throw new Exception("There are not enough apex to remove from plant.");
            }
        }

        /// <summary>
        /// Reset the apex instance
        /// </summary>
        public void Reset()
        {
            apexGroupAge.Clear();
            apexGroupSize.Clear();
            SenescenceByAge = false;
        }
        
        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">sender of the event.</param>
        /// <param name="Sow">Sowing data to initialise from.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type Sow)
        {
            if (Sow.Plant == plant)
            {
                Reset();
                Number = structure.PrimaryBudNo;
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

                    if (phenology.Stage > 4 & !SenescenceByAge)
                    {
                        double senescenceNum = NumByAge(stemSenescenceAge.Value());
                        Number -= senescenceNum;
                        SenescenceByAge = true;
                        structure.TotalStemPopn -= senescenceNum * plant.Population;
                    }
                }

                // Reduce the apex number by branching mortality
                Number -= structure.branchMortality.Value() * (Number - 1);
            }
        }
    }
}
