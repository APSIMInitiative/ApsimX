using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Factorial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Models.CLEM
{
    /// <summary>
    /// CLEM Zone to control simulation
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This manages all resources and activities for a farm")]
    [HelpUri(@"Content/Features/CLEMComponent.htm")]
    [Version(1, 0, 4, "Random numbers and iteration property moved from this component to a stand-alone component\r\nChanges will be required to your setup")]
    [Version(1, 0, 3, "Updated filtering logic to improve performance")]
    [Version(1, 0, 2, "New ResourceUnitConverter functionality added that changes some reporting.\r\nThis change will cause errors for all previous custom resource ledger reports created using the APSIM Report component.\r\nTo fix errors add \".Name\" to all LastTransaction.ResourceType and LastTransaction.Activity entries in custom ledgers (i.e. LastTransaction.ResourceType.Name as Resource). The CLEM ReportResourceLedger component has been updated to automatically handle the changes")]
    [Version(1, 0, 1, "")]
    [ModelAssociations( associatedModels: new Type[] { typeof(CLEMEvents), typeof(ResourcesHolder), typeof(ActivitiesHolder) }, AssociationStyles = new ModelAssociationStyle[] { ModelAssociationStyle.InScope, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent })]
    [ScopedModel]
    public class ZoneCLEM : Zone, ICLEMUI, ICLEMDescriptiveSummary
    {
        [Link]
        private readonly Summary summary = null;
        private string wholeSimulationSummaryFile = "";

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        [JsonIgnore]
        public string SelectedTab { get; set; }

        /// <summary>
        /// The type of user set for this simulation
        /// </summary>
        [Description("Type of user")]
        public CLEMUserType UserType { get; set; } = CLEMUserType.General;

        /// <summary>
        /// Multiplier from single farm to regional number of farms for market transactions
        /// </summary>
        [Required, GreaterThanValue(0)]
        [Description("Farm multiplier to supply and receive from market")]
        public double FarmMultiplier { get; set; }

        /// <summary>
        /// Index of the simulation Climate Region
        /// </summary>
        [Description("Region id")]
        [Core.Display(Order = -9)]
        public int ClimateRegion { get; set; }

        /// <summary>
        /// Include in overall Descriptive Summary (HTML)
        /// </summary>
        [Description("Include in simulation descriptive summary (HTML)")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool AutoCreateDescriptiveSummary { get; set; }

        /// <summary>
        /// Build TransactionCategory from tree structure
        /// </summary>
        [Description("Build TransactionCategory from tree structure")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool BuildTransactionCategoryFromTree { get; set; }

        /// <summary>
        /// Use model name as TransactionCategory
        /// </summary>
        [Description("Use component name as TransactionCategory")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool UseModelNameAsTransactionCategory { get; set; }

        // ignore zone base class properties from Models.Zone

        /// <inheritdoc/>
        [JsonIgnore]
        public new double Area { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public new double Slope { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public new double AspectAngle { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public new double Altitude { get; set; } = 50;

        /// <summary>
        /// Constructor
        /// </summary>
        public ZoneCLEM()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Helper;
            CLEMModel.SetPropertyDefaults(this);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // manually check associations with the ZoneCLEM as it is not a CLEMModel
            CLEMModel.CheckModelAssociciations(this);

            // remove the overall summary description file if present
            string[] filebits = (sender as Simulation).FileName.Split('.');
            wholeSimulationSummaryFile = $"{filebits.First()}.html";
            if (File.Exists(wholeSimulationSummaryFile))
                File.Delete(wholeSimulationSummaryFile);
        }

        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            // if auto create summary
            if (AutoCreateDescriptiveSummary && !FindAllAncestors<Experiment>().Any())
            {
                if (!File.Exists(wholeSimulationSummaryFile))
                    File.WriteAllText(wholeSimulationSummaryFile, CLEMModel.CreateDescriptiveSummaryHTML(this, false, false, (sender as Simulation).FileName));
                else
                {
                    string html = File.ReadAllText(wholeSimulationSummaryFile);
                    using StringWriter htmlWriter = new();
                    int index = html.IndexOf("<!-- CLEMZoneBody -->");
                    if (index > 0)
                    {
                        htmlWriter.Write(html.Substring(0, index - 1));
                        htmlWriter.Write(CLEMModel.CreateDescriptiveSummaryHTML(this, false, true));
                        htmlWriter.Write(html.Substring(index));
                        File.WriteAllText(wholeSimulationSummaryFile, htmlWriter.ToString());
                    }
                }
            }
        }

        /// <summary>An event handler to allow us to validate properties and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMValidate")]
        private void OnCLEMValidate(object sender, EventArgs e)
        {
            // validation is performed here
            // this event fires after Activity and Resource initialisation so that resources are available to check in the validation.
            // Commencing event is too early as Summary has not been created for reporting.
            // Some values assigned in commencing will not be checked before processing, but will be caught here
            // Each ZoneCLEM and Market will call this validation for all children
            // CLEM components above ZoneCLEM (e.g. RandomNumberGenerator) needs to validate itself
            // The tests of model associations (Attribute) now fire in Commencing and will break on error before any validation.

            // not all errors will be reported in validation so perform in two steps
            Validate(FindInScope<CLEMEvents>(), "", this, summary, FindInScope<CLEMEvents>());
            Validate(this, "", this, summary, FindInScope<CLEMEvents>());
            ReportInvalidParameters(this);
        }

        /// <summary>
        /// Reports any validation errors to exception
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="ApsimXException"></exception>
        public static void ReportInvalidParameters(IModel model)
        {
            IModel simulation = model.FindAncestor<Simulation>();
            var summary = simulation.FindDescendant<Summary>();

            // get all validations
            ReportErrors(model, summary.GetMessages(simulation.Name)?.Where(a => a.Severity == MessageType.Error && a.Text.StartsWith("Invalid parameter ")));

            // get all other errors
            ReportErrors(model, summary.GetMessages(simulation.Name)?.Where(a => a.Severity == MessageType.Error && !a.Text.StartsWith("Invalid parameter ")));
        }

        /// <summary>
        /// Check and throw error is error messages occur
        /// </summary>
        /// <param name="model">Model performing validation</param>
        /// <param name="messages">List of messages</param>
        /// <exception cref="ApsimXException"></exception>
        public static void ReportErrors(IModel model, IEnumerable<Logging.Message> messages)
        {
            // report error and stop
            if (messages.Any())
            {
                // create combined inner exception
                StringBuilder innerExceptionString = new StringBuilder();
                foreach (var error in messages)
                    innerExceptionString.Append($"{error.Text}{Environment.NewLine}");

                Exception innerException = new Exception(innerExceptionString.ToString());
                throw new ApsimXException(model, $"{messages.Count()} error{(messages.Count() == 1 ? "" : "s")} occurred during start up.{Environment.NewLine}See CLEM component [{model.GetType().Name}] Messages tab for details{Environment.NewLine}", innerException);
            }
        }

        /// <summary>
        /// Internal method to iterate through all children in CLEM and report any parameter setting errors
        /// </summary>
        /// <param name="model">The model being validated</param>
        /// <param name="modelPath">Pass blank string. Used for tracking model path</param>
        /// <param name="parentZone">The name of the containing ZoneCLEM or Market for reporting</param>
        /// <param name="summary">Link to summary for reporting</param>
        /// <param name="events">Reference to the CLEM events clock</param>
        /// <returns>Boolean indicating whether validation was successful</returns>
        public static bool Validate(IModel model, string modelPath, IModel parentZone, ISummary summary, CLEMEvents events)
        {
            string starter = "[=";
            if (typeof(IResourceType).IsAssignableFrom(model.GetType()))
                starter = "[r=";
            if (model.GetType() == typeof(ZoneCLEM))
                starter = "[z=";
            if (model.GetType() == typeof(ResourcesHolder))
                starter = "[rs=";
            if (model.GetType() == typeof(LabourRequirement))
                starter = "[l=";
            if (model.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                starter = "[r=";
            if (model.GetType() == typeof(ActivitiesHolder))
                starter = "[as=";
            if (model.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                starter = "[a=";
            if (model.GetType().Name.Contains("Group"))
                starter = "[g=";
            if (model.GetType().Name.Contains("Timer"))
                starter = "[t=";
            if (model.GetType().Name.Contains("Filter"))
                starter = "[f=";

            modelPath += starter + model.Name + "]";
            bool valid = true;
            var validationContext = new ValidationContext(model, null, null);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            if (model.Name.EndsWith(" "))
                validationResults.Add(new ValidationResult("Component name cannot end with a space character", new string[] { "Name" }));

            if (model is CLEMModel clemModel)
            {
                clemModel.CLEMParentName = parentZone.Name;

                // check that simulation time step is supported by this component
                if(!clemModel.TimeStepOK(events))
                {
                    validationResults.Add(new ValidationResult($"The [{events.TimeStep}] time-step of [CLEMEvents] is not supported by [{clemModel.GetType().Name}] component.", new string[] { "Invalid time-step" }));
                }
            }

            if (validationResults.Any())
            {
                valid = false;
                // report all errors
                foreach (var validateError in validationResults)
                {
                    // get description
                    string text = "";
                    var property = model.GetType().GetProperty(validateError.MemberNames.FirstOrDefault());
                    if (property != null)
                    {
                        text = "";
                        if (property.GetCustomAttributes(typeof(DescriptionAttribute), true).Count() > 0)
                        {
                            var attribute = property.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                            var description = (DescriptionAttribute)attribute;
                            text = description.ToString();
                        }
                    }
                    string error = $"Invalid parameter value in {modelPath}{Environment.NewLine}PARAMETER: {validateError.MemberNames.FirstOrDefault()}";
                    if (text != "")
                        error += $"{Environment.NewLine}DESCRIPTION: {text}";
                    error += $"{Environment.NewLine}PROBLEM: {validateError.ErrorMessage}{Environment.NewLine}";
                    summary.WriteMessage(parentZone, error, MessageType.Error);
                }
            }
            foreach (var child in model.Children)
            {
                bool result = Validate(child, modelPath, parentZone, summary, events);
                if (valid && !result)
                    valid = false;
            }
            return valid;
        }

        #region Descriptive summary

        /// <summary>
        /// Summary style to use for this component
        /// </summary>
        public HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public List<string> CurrentAncestorList { get; set; } = new List<string>();

        /// <inheritdoc/>
        public bool FormatForParentControl { get { return CurrentAncestorList.Count > 0; } }

        /// <inheritdoc/>
        [JsonIgnore]
        public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

        ///<inheritdoc/>
        public string GetFullSummary(IModel model, List<string> parentControls, string htmlString, Func<string, string> markdown2Html = null)
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"holdermain\" style=\"opacity: " + ((!this.Enabled) ? "0.4" : "1") + "\">");

            CurrentAncestorList = parentControls.ToList();
            CurrentAncestorList.Add(model.GetType().Name);

            // get clock
            IModel parentSim = FindAncestor<Simulation>();

            htmlWriter.Write(CLEMModel.AddMemosToSummary(parentSim, markdown2Html));

            // create the summary box with properties of this component
            if (this is ICLEMDescriptiveSummary)
            {
                htmlWriter.Write(ModelSummaryOpeningTags());
                htmlWriter.Write(ModelSummaryInnerOpeningTagsBeforeSummary());
                htmlWriter.Write(ModelSummary());
                // TODO: May need to implement Adding Memos for some Models with reduced display
                htmlWriter.Write(ModelSummaryInnerOpeningTags());
                htmlWriter.Write(ModelSummaryInnerClosingTags());
                htmlWriter.Write(ModelSummaryClosingTags());
            }

            // find random number generator
            RandomNumberGenerator rnd = parentSim.FindDescendant<RandomNumberGenerator>();
            if (rnd != null)
            {
                htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");
                htmlWriter.Write("<div class=\"namediv\">" + rnd.Name + "</div><br />");
                htmlWriter.Write("<div class=\"typediv\">RandomNumberGenerator</div>");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"defaultcontent\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Random numbers are provided for this simultion with ");
                if (rnd.Seed == 0)
                    htmlWriter.Write("every run using a different sequence.");
                else
                    htmlWriter.Write("each run identical by using the seed <span class=\"setvalue\">" + rnd.Seed.ToString() + "</span>");
                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write(CLEMModel.AddMemosToSummary(rnd, markdown2Html));

                htmlWriter.Write("\r\n</div>");
            }

            Clock clk = parentSim.FindChild<Clock>();
            if (clk != null)
            {
                htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");
                htmlWriter.Write("<div class=\"namediv\">" + clk.Name + "</div><br />");
                htmlWriter.Write("<div class=\"typediv\">Clock</div>");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"defaultcontent\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\">This simulation runs from ");
                if (clk.Start == null)
                    htmlWriter.Write("<span class=\"errorlink\">[START DATE NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"setvalue\">" + clk.StartDate.ToShortDateString() + "</span>");
                htmlWriter.Write(" to ");
                if (clk.End == null)
                    htmlWriter.Write("<span class=\"errorlink\">[END DATE NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"setvalue\">" + clk.EndDate.ToShortDateString() + "</span>");
                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write(CLEMModel.AddMemosToSummary(clk, markdown2Html));

                htmlWriter.Write("\r\n</div>");
                htmlWriter.Write("\r\n</div>");
            }

            foreach (CLEMModel cm in this.FindAllChildren<CLEMModel>())
                htmlWriter.Write(cm.GetFullSummary(cm, CurrentAncestorList, "", markdown2Html));

            CurrentAncestorList = null;

            return htmlWriter.ToString();
        }

        ///<inheritdoc/>
        public string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("This farm is identified as region ");
                htmlWriter.Write($"<span class=\"setvalue\">{ClimateRegion}</span></div>");

                ResourcesHolder resources = this.FindChild<ResourcesHolder>();
                if (resources != null)
                {
                    if (resources.FoundMarket != null)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write("This farm represents ");
                        htmlWriter.Write($"<span class=\"setvalue\">{FarmMultiplier}</span></div> farm(s) when trading with the Market</div>");
                    }
                }

                //if ((this.FindDescendant<RuminantActivityGrazeAll>() != null) || (this.FindDescendant<RuminantActivityGrazePasture>() != null) || (this.FindDescendant<RuminantActivityGrazePastureHerd>() != null))
                //{
                //    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                //    htmlWriter.Write("Ecological indicators will be calculated every ");
                //    if (EcologicalIndicatorsCalculationInterval <= 0)
                //        htmlWriter.Write("<span class=\"errorlink\">NOT SET</span> months");
                //    else
                //        htmlWriter.Write($"<span class=\"setvalue\">{EcologicalIndicatorsCalculationInterval}</span> month{((EcologicalIndicatorsCalculationInterval == 1) ? "" : "s")}");
                //    htmlWriter.Write($" starting at the end of {EcologicalIndicatorsCalculationMonth}</div>");
                //}

                if (AutoCreateDescriptiveSummary)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"This component will be included in the overall simulation summary decription html file</div>");
                }
                return htmlWriter.ToString();
            }
        }

        ///<inheritdoc/>
        public string ModelSummaryClosingTags()
        {
            return "\r\n</div>\r\n</div>";
        }

        ///<inheritdoc/>
        public string ModelSummaryOpeningTags()
        {
            string overall = "default";
            string extra = "";

            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + ((!this.Enabled) ? 0.4 : 1.0).ToString() + ";\">");
            htmlWriter.Write("\r\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + ModelSummaryNameTypeHeader() + "</div>");
            htmlWriter.Write("\r\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">");

            return htmlWriter.ToString();
        }

        ///<inheritdoc/>
        public string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        ///<inheritdoc/>
        public string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        ///<inheritdoc/>
        public string ModelSummaryInnerOpeningTagsBeforeSummary()
        {
            return "";
        }

        ///<inheritdoc/>
        public string ModelSummaryNameTypeHeader()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"<div class=\"namediv\">{this.Name}{((!this.Enabled) ? " - DISABLED!" : "")}</div>");
            if (GetType().IsSubclassOf(typeof(CLEMActivityBase)))
            {
                htmlWriter.Write("<div class=\"partialdiv\"></div>");
            }
            htmlWriter.Write($"<br /><div class=\"typediv\">{GetType().Name}</div>");
            return htmlWriter.ToString();
        }

        ///<inheritdoc/>
        public string ModelSummaryNameTypeHeaderText()
        {
            return this.Name;
        }
        #endregion

    }
}
