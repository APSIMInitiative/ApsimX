using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Linq.Expressions;
using Models.CLEM.Groupings;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using System.Reflection;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on the number or summarised value of a property of selected ruminants
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMRuminantActivityBase))]
    [Description("This activity timer is is based on whether the size or value of individual property is within a specified range for the herd.")]
    [HelpUri(@"Content/Features/Timers/HerdLevel.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerRuminantLevel : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link] Clock clock = null;

        double amountAtFirstCheck;
        DateTime checkDate = DateTime.Now;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups = new List<RuminantGroup>();
        private CLEMRuminantActivityBase parentRuminantBase = null;
        [NonSerialized]
        private PropertyInfo propertyInfo = null;

        /// <summary>
        /// Style of this ruminant timer
        /// </summary>
        [Description("Style of timer")]
        [Required]
        public ActivityTimerRuminantLevelStyle TimerStyle { get; set; }

        /// <summary>
        /// Name of property to use
        /// </summary>
        [Description("Ruminant property to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = nameof(GetParameters), VisibleCallback = nameof(PropertyNameNeeded))]
        public string RuminantProperty { get; set; }

        /// <summary>
        /// Operator to filter with
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        [Core.Display(Type = DisplayType.DropDown, Values = nameof(GetOperators))]
        public ExpressionType Operator { get; set; }
        private object[] GetOperators() => new object[]
        {
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual
        };

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        public double Amount { get; set; }

        /// <summary>
        /// Determines if a property name is needed from user
        /// </summary>
        public bool PropertyNameNeeded() => TimerStyle != ActivityTimerRuminantLevelStyle.NumberOfIndividuals; 

        private IEnumerable<string> GetParameters() => typeof(Ruminant).GetProperties().Where(a => a.PropertyType == typeof(double) || a.PropertyType == typeof(int)).Select(a => a.Name).OrderBy(k => k);

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerRuminantLevel()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            parentRuminantBase = Parent as CLEMRuminantActivityBase;
            if (parentRuminantBase is null)
            {
                string warn = $"The timer [a={NameWithParent}] must be placed below an activity that supports ruminant handling [CLEMRuminantActivityBase]";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
            }

            // get list of filters
            filterGroups = FindAllChildren<RuminantGroup>();
            if(!filterGroups.Any())
            {
                string warn = $"The timer [a={NameWithParent}] requires at least one [f=RuminantGroup] placed below to identify individuals to consider";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
            }

            // get property method if required
            if(TimerStyle != ActivityTimerRuminantLevelStyle.NumberOfIndividuals)
            {
                if((RuminantProperty??"") != "")
                    propertyInfo = typeof(Ruminant).GetProperty(RuminantProperty);

                if ((RuminantProperty ?? "") == "" || propertyInfo is null)
                {
                    string warn = $"A Ruminant property must be provided in [a={NameWithParent}] for style [{TimerStyle}]";
                    Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                }
            }
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if (clock.Today != checkDate)
                {
                    // filter based on all filtergroups
                    uniqueIndividuals = CLEMRuminantActivityBase.GetUniqueIndividuals<Ruminant>(filterGroups, parentRuminantBase.CurrentHerd());

                    if (!uniqueIndividuals.Any())
                        StatusMessage = "No individuals found";

                    if (TimerStyle == ActivityTimerRuminantLevelStyle.NumberOfIndividuals)
                        amountAtFirstCheck = uniqueIndividuals.Count();
                    else
                    {
                        var individualProperty = uniqueIndividuals.Select(a => Convert.ToDouble(propertyInfo.GetValue(a)));
                        switch (TimerStyle)
                        {
                            case ActivityTimerRuminantLevelStyle.SumOfProperty:
                                amountAtFirstCheck = individualProperty.Sum();
                                break;
                            case ActivityTimerRuminantLevelStyle.MeanOfProperty:
                                amountAtFirstCheck = individualProperty.Mean();
                                break;
                            case ActivityTimerRuminantLevelStyle.MinimumOfProperty:
                                amountAtFirstCheck = individualProperty.Min();
                                break;
                            case ActivityTimerRuminantLevelStyle.MaximumOfProperty:
                                amountAtFirstCheck = individualProperty.Max();
                                break;
                            default:
                                amountAtFirstCheck = 0;
                                break;
                        }
                    }
                    checkDate = clock.Today;
                }

                bool due = false;
                switch (Operator)
                {
                    case ExpressionType.Equal:
                        due = (amountAtFirstCheck == Amount);
                        break;
                    case ExpressionType.NotEqual:
                        due = (amountAtFirstCheck != Amount);
                        break;
                    case ExpressionType.LessThan:
                        due = (amountAtFirstCheck < Amount);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        due = (amountAtFirstCheck <= Amount);
                        break;
                    case ExpressionType.GreaterThan:
                        due = (amountAtFirstCheck > Amount);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        due = (amountAtFirstCheck >= Amount);
                        break;
                    default:
                        break;
                }

                return due;
            }
        }

        ///<inheritdoc/>
        public string StatusMessage { get; set;}

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            return false;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"clearfix\"><div class=\"filter\">");
                htmlWriter.Write("Perform when ");
                if(TimerStyle == ActivityTimerRuminantLevelStyle.NumberOfIndividuals)
                    htmlWriter.Write($"{ DisplaySummaryValueSnippet("the number of individuals", "Not set", HTMLSummaryStyle.Default)}");
                else
                {
                    string stl = "[Unknown]";
                    switch (TimerStyle)
                    {
                        case ActivityTimerRuminantLevelStyle.SumOfProperty:
                            stl = "sum";
                            break;
                        case ActivityTimerRuminantLevelStyle.MeanOfProperty:
                            stl = "mean";
                            break;
                        case ActivityTimerRuminantLevelStyle.MinimumOfProperty:
                            stl = "minimum";
                            break;
                        case ActivityTimerRuminantLevelStyle.MaximumOfProperty:
                            stl = "maximum";
                            break;
                    }
                    htmlWriter.Write($"the {DisplaySummaryValueSnippet(stl, "Not set", HTMLSummaryStyle.Default)} of {DisplaySummaryValueSnippet(RuminantProperty)}", "Not set", HTMLSummaryStyle.Default);
                }
                htmlWriter.Write($" {DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Default)}");
                htmlWriter.Write($" {DisplaySummaryValueSnippet(Amount, "Not set", HTMLSummaryStyle.Default)}</div></div>");
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Convert the operator to a symbol
        /// </summary>
        /// <returns>Operator as symbol</returns>
        protected string OperatorToSymbol()
        {
            switch (Operator)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "!=";
                default:
                    return Operator.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "<div>";
        }


        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write(this.Name);
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<RuminantGroup>(), true, "childgroupfilterborder", "Based on unique individuals selected from:", "")
            };
        }


        #endregion

    }
}
