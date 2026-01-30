using APSIM.Core;
using APSIM.Shared.Documentation.Extensions;
using DocumentFormat.OpenXml.EMMA;
using Models.CLEM.Activities;
using Models.CLEM.DescriptiveSummary;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Attributes;
using Models.Factorial;
using Models.Storage;
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
    public class ZoneCLEM : Zone, ICLEMUI, IScopedModel, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        [JsonIgnore]
        public new IStructure Structure { get; set; }

        [Link]
        private readonly Summary summary = null;
        [Link]
        private readonly Simulation simulation = null;
        [Link]
        private readonly DataStore dataStore = null;

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
        public bool AutoCreateDescriptiveSummary { get; set; }

        /// <summary>
        /// The output format for the descriptive summary
        /// </summary>
        [Description("Descriptive summary format")]
        public DescriptiveSummaryFormat DescriptiveSummaryFormatStyle { get; set; } = DescriptiveSummaryFormat.HTML;

        /// <summary>
        /// Build TransactionCategory from tree structure
        /// </summary>
        [Description("Build TransactionCategory from tree structure")]
        public bool BuildTransactionCategoryFromTree { get; set; }

        /// <summary>
        /// Use model name as TransactionCategory
        /// </summary>
        [Description("Use component name as TransactionCategory")]
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

        ///<summary>What kind of canopy</summary>
        [JsonIgnore]
        public new string CanopyType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // manually check associations with the ZoneCLEM as it is not a CLEMModel
            CLEMModel.CheckModelAssociations(this);

            // remove the overall summary description file if present
            string[] filebits = (sender as Simulation).FileName.Split('.');
            wholeSimulationSummaryFile = filebits.First();
            switch (DescriptiveSummaryFormatStyle)
            {
                case DescriptiveSummaryFormat.HTML:
                    wholeSimulationSummaryFile += ".html";
                    break;
                case DescriptiveSummaryFormat.Markdown:
                    wholeSimulationSummaryFile += ".md";
                    break;
                case DescriptiveSummaryFormat.Text:
                    wholeSimulationSummaryFile += ".txt";
                    break;
                default:
                    break;
            }

            if (File.Exists(wholeSimulationSummaryFile))
            {
                File.Delete(wholeSimulationSummaryFile);
            }
        }

        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            // if auto create summary
            if (AutoCreateDescriptiveSummary && !Structure.FindParents<Experiment>().Any())
            {
                var allZoneCLEM = Structure.FindAll<ZoneCLEM>();
                if (allZoneCLEM.Any() && allZoneCLEM.FirstOrDefault() != this) return;

                DescriptiveSummaryGenerator summaryGenerator = new DescriptiveSummaryGenerator(DescriptiveSummaryFormatStyle, false);
                summaryGenerator.GenerateSummaryForComponentAndChildren(Structure.FindParent<Simulation>(recurse: true), wholeSimulationSummaryFile);

                //
                //
                // ToDo: leave until I confirm that the new descriptive summary with providers works on all APSIM components from Simulation down.
                //
                //

                //if (!File.Exists(wholeSimulationSummaryFile))
                //{
                //    File.WriteAllText(wholeSimulationSummaryFile, CLEMModel.CreateDescriptiveSummaryHTML(this, Structure, false, false, (sender as Simulation).FileName));
                //}
                //else
                //{
                //    string html = File.ReadAllText(wholeSimulationSummaryFile);
                //    using StringWriter htmlWriter = new();
                //    int index = html.IndexOf("<!-- CLEMZoneBody -->");
                //    if (index > 0)
                //    {
                //        htmlWriter.Write(html[..(index - 1)]);
                //        htmlWriter.Write(CLEMModel.CreateDescriptiveSummaryHTML(this, Structure, false, true));
                //        htmlWriter.Write(html[index..]);
                //        File.WriteAllText(wholeSimulationSummaryFile, htmlWriter.ToString());
                //    }
                //}
            }
        }

        /// <summary>An event handler to catch file association errors before moving to initialisation of resources and activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialise(object sender, EventArgs e)
        {
            // Performed in CLEMInitialiseResource to catch any errors thrown in previous early CLEMInitialise and checksof RuminantHerd
            // The tests of model associations (Attribute) now fire in Commencing and this section is designed to fire errors if issues found prior to any resource or activity initialisation.

            ReportInvalidParameters(this, dataStore, summary, simulation.Name);
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
            Validate(Structure.Find<CLEMEvents>(), "", this, summary, Structure.Find<CLEMEvents>());
            Validate(this, "", this, summary, Structure.Find<CLEMEvents>());
            ReportInvalidParameters(this, dataStore, summary, simulation.Name);
        }

        /// <summary>
        /// Reports any validation errors to exception
        /// </summary>
        /// <param name="model">The model calling this method</param>
        /// <param name="dataStore">the datastore where messages are written</param>
        /// <param name="summary"></param>
        /// <param name="simulationId"></param>
        /// <exception cref="ApsimXException"></exception>
        public static void ReportInvalidParameters(IModel model, DataStore dataStore, Summary summary, string simulationId)
        {
            // force summary to write messages
            // if not included the messages table isn't propagated to exit model on errors detected.
            summary.WriteMessagesToDataStore();
            dataStore .Writer.WaitForIdle();

            // get all validations
            ReportErrors(model, summary.GetMessages(simulationId)?.Where(a => a.Severity == MessageType.Error && a.Text.StartsWith("Invalid parameter ")));

            // get all other errors
            ReportErrors(model, summary.GetMessages(simulationId)?.Where(a => a.Severity == MessageType.Error && !a.Text.StartsWith("Invalid parameter ")));
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
                {
                    innerExceptionString.Append($"{error.Text}{Environment.NewLine}");
                }

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
            {
                validationResults.Add(new ValidationResult("Component name cannot end with a space character", new string[] { "Name" }));
            }

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
                        if (property.GetCustomAttributes(typeof(DescriptionAttribute), true).Length > 0)
                        {
                            var attribute = property.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                            var description = (DescriptionAttribute)attribute;
                            text = description.ToString();
                        }
                    }
                    string error = $"Invalid parameter value in {modelPath}{Environment.NewLine}PARAMETER: {validateError.MemberNames.FirstOrDefault()}";
                    if (text != "")
                    {
                        error += $"{Environment.NewLine}DESCRIPTION: {text}";
                    }

                    error += $"{Environment.NewLine}PROBLEM: {validateError.ErrorMessage}{Environment.NewLine}";
                    summary.WriteMessage(parentZone, error, MessageType.Error);
                }
            }
            foreach (var child in model.Children)
            {
                bool result = Validate(child, modelPath, parentZone, summary, events);
                if (valid && !result)
                {
                    valid = false;
                }
            }
            return valid;
        }
    }
}
