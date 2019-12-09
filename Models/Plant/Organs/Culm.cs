using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Data passed to sorghumLeaf to initialise Culms.
    /// </summary>
    public class CulmParameters
    {
        /// <summary>The numeric rank of the cohort appearing</summary>
        public int CulmNumber { get; set; }

        /// <summary>The Leaf Number when the Tiller was added</summary>
        public double LeafNoAtAppearance { get; set; }

        /// <summary>The proportion of a whole tiller</summary>
        public double InitialProportion { get; set; } = 1.0;

        /// <summary>The area calcs for subsequent tillers are the same shape but not as tall</summary>
        public double VerticalAdjustment { get; set; }

        /// <summary>The planting density for area calculations</summary>
        public double Density { get; set; }

        /// <summary>The Initial Appearance rate for phyllocron</summary>
        public double InitialAppearanceRate { get; set; }
        /// <summary>The Final Appearance rate for phyllocron</summary>
        public double FinalAppearanceRate { get; set; }
        /// <summary>The Final Appearance rate for phyllocron</summary>
        public double RemainingLeavesForFinalAppearanceRate { get; set; }

        /// <summary>/// The aX0 for this Culm </summary>
        public double AX0 { get; set; }

        /// <summary> The aMaxSlope for this Culm </summary>
        public double AMaxSlope { get; set; }

        /// <summary>The aMaxIntercept for this Culm</summary>
        public double AMaxIntercept { get; set; }

    }

    ///<summary>
    /// A Culm represents a collection of leaves
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Culm : Model, ICustomDocumentation
    {
        private const double smm2sm = 0.000001;
        private CulmParameters culmParameters { get; set; }
        /// <summary> The proportion of a whole tiller</summary>
        public double Proportion { get; set; }

        /// <summary> The area calcs for subsequent tillers are the same shape but not as tall</summary>
        public double CurrentLeafNumber { get; set; }

        /// <summary> The amount of new leaf that appeared</summary>
        public double DltNewLeafAppeared { get; set; }

        /// <summary>
        /// The TotalLAI for this Culm
        /// </summary>
        public double TotalLAI { get; set; }

        /// <summary>
        /// Delta leaf number.
        /// </summary>
        private double dltLeafNo;

        /// <summary>
        /// Final Leaf Number used to calculate area
        /// </summary>
        public double FinalLeafNumber { get; set; }

        /// <summary>
        /// Default constructor - this should not be used.
        /// </summary>
        public Culm()
        {
            // Default constructor is needed for instantiating via reflection,
            // which is needed for unit tests.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parameters"></param>
        public Culm(CulmParameters parameters)
        {
            culmParameters = parameters;
            Proportion = culmParameters.InitialProportion;
        }

        /// <summary>
        /// Calculates, updates, and returns dltLeafNo.
        /// </summary>
        public double calcLeafAppearance(double dltTT)
        {
            dltLeafNo = 0;

            // nLeaves is used in partitionDM, so need to retain it in Leaf
            double remainingLeaves = FinalLeafNumber - culmParameters.LeafNoAtAppearance - CurrentLeafNumber;

            if (MathUtilities.IsLessThanOrEqual(remainingLeaves, 0))
                return 0;

            // Peter's 2 stage version used here, modified to apply to last few leaves before flag
            // i.e. c_leaf_no_rate_change is leaf number from the top down (e.g. 4)

            double leafAppRate = culmParameters.InitialAppearanceRate;
            if (MathUtilities.IsLessThanOrEqual(remainingLeaves, culmParameters.RemainingLeavesForFinalAppearanceRate))
                leafAppRate = culmParameters.FinalAppearanceRate;

            // If leaves are still growing, the cumulative number of phyllochrons or fully expanded
            // leaves is calculated from thermal time for the day.
            dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(dltTT, leafAppRate, 0), 0, remainingLeaves);

            CurrentLeafNumber += dltLeafNo;

            return dltLeafNo;
        }

        /// <summary>Add number of new leaf appeared</summary>
        public double calcPotentialArea()
        {
            var leafNoCorrection = 1.52;
            //once leaf no is calculated leaf area of largest expanding leaf is determined
            double leafNoEffective = Math.Min(CurrentLeafNumber + leafNoCorrection, FinalLeafNumber - culmParameters.LeafNoAtAppearance);
            var leafsize = calcIndividualLeafSize(leafNoEffective);

            double leafArea = leafsize * smm2sm * culmParameters.Density * dltLeafNo; // in dltLai
            TotalLAI += leafArea;
            return (leafArea * Proportion);
        }

        private double calcIndividualLeafSize(double leafNo)
        {
            //double aX0 = 0.687;
            //double aMaxSlope = 22.25;
            //double aMaxIntercept = 92.45;

            double largestLeafPlateau = 0.0;
            // use finalLeafNo to calculate the size of the individual leafs
            // Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
            // (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
            //
            double correctedFinalLeafNo = FinalLeafNumber;// - leafNoAtAppearance;
            double largestLeafPos = culmParameters.AX0 * correctedFinalLeafNo; //aX0 = position of the final leaf
                                                                //double leafPlateauStart = 24;
                                                                //adding new code to handle varieties that grow very high number of leaves
            if (largestLeafPlateau > 1)
            {
                if (correctedFinalLeafNo > largestLeafPlateau)
                {
                    largestLeafPos = culmParameters.AX0 * largestLeafPlateau;

                    if (leafNo > largestLeafPos)
                    {
                        double tailCount = largestLeafPlateau - largestLeafPos;
                        if (leafNo < correctedFinalLeafNo - tailCount)
                        {
                            leafNo = largestLeafPos;
                        }
                        else
                        {
                            leafNo = largestLeafPlateau - (correctedFinalLeafNo - leafNo);
                        }
                    }
                }
            }
            double a0 = -0.009, a1 = -0.2;
            double b0 = 0.0006, b1 = -0.43;

            double a = a0 - Math.Exp(a1 * correctedFinalLeafNo); //breadth
            double b = b0 - Math.Exp(b1 * correctedFinalLeafNo); //skewness

            //Relationship for calculating maximum individual leaf area from Total Leaf No
            //Source: Modelling genotypic and environmental control of leaf area dynamics in grain sorghum. II. Individual leaf level 
            //Carberry, Muchow, Hammer,1992
            //written as Y = Y0*exp(a*pow(X-X0,2)+b*(pow(X-X0,3))) 
            //pg314 -Leaf area production model

            //Largest Leaf calculation
            //originally from "Improved methods for predicting individual leaf area and leaf senescence in maize" - Birch, Hammer, Rickert 1998
            //double aMaxB = 4.629148, aMaxC = 6.6261562; 
            //double aMax = aMaxA * (1 - exp(-aMaxB * (finalLeafNo - aMaxC)));  // maximum individual leaf area
            //Calculation then changed to use the relationship as described in the Carberry paper in Table 2
            //The actual intercept and slope will be determined by the cultivar, and read from the config file (sorghum.xml)
            //aMaxS = 19.5; //not 100% sure what this number should be - tried a range and this provided the best fit forthe test data
            double largestLeafSize = culmParameters.AMaxSlope * FinalLeafNumber + culmParameters.AMaxIntercept; //aMaxI is the intercept

            //a vertical adjustment is applied to each tiller - this was discussed in a meeting on 22/08/12 and derived 
            //from a set of graphs that I cant find that compared the curves of each tiller
            //the effect is to decrease the size of the largest leaf by 10% 
            largestLeafSize *= (1 - culmParameters.VerticalAdjustment);
            double leafSize = largestLeafSize * Math.Exp(a * Math.Pow((leafNo - largestLeafPos), 2) + b * Math.Pow((leafNo - largestLeafPos), 3)) * 100;
            return leafSize;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //tags.Add(new AutoDocumentation.Paragraph("Area = " + Area, indent));
            }
        }
    }
}
