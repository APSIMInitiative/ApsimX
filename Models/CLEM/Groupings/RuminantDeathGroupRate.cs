using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Interfaces;
using System.IO;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for determining death by a specified rate
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityDeath))]
    [Description("Manages the death of specified ruminants based on annual mortality rate.")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantDeathGroupRate.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantDeathGroupRate : RuminantGroup, IRuminantDeathGroup, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        /// <summary>
        /// Determine whether parameters are specified or taken from other simulation parameters
        /// </summary>
        [Description("Values to use")]
        [Required]
        public ParameterStyle Style { get; set; } = ParameterStyle.GetFromParameters;

        /// <summary>
        /// Annual mortality rate
        /// </summary>
        [Description("Annual mortality rate")]
        [Required, GreaterThanValue(0), Proportion]
        [Core.Display(VisibleCallback = "VisibleCustomProperties")]
        public double Rate { get; set; } = 0.03;

        /// <summary>
        /// Determine if custom properties should be displayed to the user
        /// </summary>
        public bool VisibleCustomProperties() => Style == ParameterStyle.Specify;
    
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantDeathGroupRate()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            Rate /= 365.0;
        }

        /// <inheritdoc/>
        public void DetermineDeaths(IEnumerable<Ruminant> individuals)
        {
            // convert mortality from annual to time-step.
            double mortalityRate = Rate * events.Interval;

            foreach (var ind in individuals)
            {
                if (Style == ParameterStyle.GetFromParameters)
                    mortalityRate = ind.Parameters.FindBaseMortalityRate  * events.Interval; 
                //ToDo: fix so this is calculated to daily once elsewhere.
                //ToDo: check CD1 is daily rate

                if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), mortalityRate))
                {
                    ind.Died = true;
                    ind.SaleFlag = HerdChangeReason.DiedMortality;
                }
            }
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if(Style == ParameterStyle.GetFromParameters)
            {
                // ensure parameters are available for all ruminant types
                foreach (var rumtype in FindAllInScope<RuminantType>())
                {
                    if(rumtype.Parameters.Grow24 is null && rumtype.Parameters.Grow is null)
                    {
                        string[] memberNames = new string[] { "Cannot find mortality parameters" };
                        results.Add(new ValidationResult($"The [GetFromParameters] setting requires a [Parameters.Grow24] or [Parameters.Grow] provided in [r={rumtype.Name}] for [a={Name}].{Environment.NewLine}Provide required breed parameters or use the [Specify] style", memberNames));
                    }
                }
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            switch (Style)
            {
                case ParameterStyle.GetFromParameters:
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">The annual mortality rates for the specified individuals each time-step are provided in the following breed parameter files: ");
                    foreach (var rumtype in FindAllInScope<RuminantType>())
                    {
                        htmlWriter.Write(rumtype.Name);
                        if (rumtype.Parameters.Grow24 is not null)
                            htmlWriter.Write($".Parameters.Grow24.BasalMortalityRate_CD1 = {rumtype.Parameters.Grow24_CD.BasalMortalityRate_CD1}");
                        else if (rumtype.Parameters.Grow is not null)
                            htmlWriter.Write($".Parameters.Grow.MortalityBase = {rumtype.Parameters.Grow.MortalityBase}");
                        else
                            htmlWriter.Write($"<span=\"errorlink\">Missing Grow or Grow24 parameters</span>");
                    }
                    htmlWriter.Write("</div>");
                    break;
                case ParameterStyle.Specify:
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">The annual mortality rate of {DisplaySummaryValueSnippet(Rate, warnZero: true)} will be applied to the specified individuals each time-step to determine if death occurs.</div>");
                    break;
                default:
                    break;
            }
            return htmlWriter.ToString();
        }

        #endregion

    }

    /// <summary>
    /// Method of providing parameters
    /// </summary>
    public enum ParameterStyle
    {
        /// <summary>
        /// Get from current parameters
        /// </summary>
        GetFromParameters,
        /// <summary>
        /// Specify the parameters needed
        /// </summary>
        Specify
    }

}

