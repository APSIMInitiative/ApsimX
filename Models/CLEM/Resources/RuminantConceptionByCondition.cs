using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Advanced ruminant conception for first conception less than 12 months, 12-24 months, 2nd calf and 3+ calf
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Advanced ruminant conception for first pregnancy less than 12 months, 12-24 months, 24 months, 2nd calf and 3+ calf")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/resources/ruminant/ruminantadvancedconception.htm")]
    public class RuminantConceptionByCondition: CLEMModel, IConceptionModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionByCondition()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Condition cutoff for conception
        /// </summary>
        [Description("Condition (wt/normalised wt) below which no conception")]
        [Required, GreaterThanValue(0)]
        public double ConditionCutOff { get; set; }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "Conception is determined by animal condition measured as the ratio of live weight to normalised weight for age.\nNo breeding females will conceove if this ration is below ";
            if(ConditionCutOff==0)
            {
                html += "<span class=\"errorlink\">No set<\\span>";
            }
            else
            {
                html += "<span class=\"setvalue\">"+ConditionCutOff.ToString("0.0##")+"<\\span>";
            }
            html += "</div>";
            return html;
        }

        /// <summary>
        /// Calculate conception rate for a female based on condition score
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            bool isConceptionReady = false;
            if (female.Age >= female.BreedParams.MinimumAge1stMating && female.NumberOfBirths == 0)
            {
                isConceptionReady = true;
            }
            else
            {
                double currentIPI = female.BreedParams.InterParturitionIntervalIntercept * Math.Pow((female.Weight / female.StandardReferenceWeight), female.BreedParams.InterParturitionIntervalCoefficient) * 30.64;
                // calculate inter-parturition interval
                currentIPI = Math.Max(currentIPI, female.BreedParams.GestationLength * 30.4 + female.BreedParams.MinimumDaysBirthToConception); // 2nd param was 61
                double ageNextConception = female.AgeAtLastConception + (currentIPI / 30.4);
                isConceptionReady = (female.Age >= ageNextConception);
            }

            // if first mating and of age or suffcient time since last birth/conception
            if (isConceptionReady)
            {
                return (female.RelativeCondition >= ConditionCutOff) ? 1 : 0; 
            }
            return 0;
        }
    }

}
