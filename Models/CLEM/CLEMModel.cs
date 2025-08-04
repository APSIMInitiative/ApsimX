using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Models.CLEM
{
    ///<summary>
    /// CLEM base model
    ///</summary>
    [Serializable]
    [Description("This is the Base CLEM model and should not be used directly.")]
    public abstract class CLEMModel : Model, ICLEMUI, ICLEMDescriptiveSummary, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { protected get; set; }

        /// <summary>
        /// Link to summary
        /// </summary>
        [Link]
        [NonSerialized]
        public ISummary Summary = null;

        [NonSerialized]
        private IEnumerable<IActivityTimer> activityTimers = null;

        /// <summary>
        /// Model settings notes
        /// </summary>
        [Description("Notes")]
        [Category("*", "*")]
        [Core.Display(Order = 9999)]
        public string Notes { get; set; }

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Warning log for this CLEM model
        /// </summary>
        [JsonIgnore]
        public WarningLog Warnings = WarningLog.GetInstance(50);

        /// <summary>
        /// Model identifier
        /// </summary>
        [JsonIgnore]
        public Guid UniqueID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Model identifier as string for reporting as UniqueID.ToString() throws ambiguous property error
        /// </summary>
        [JsonIgnore]
        public string UniqueIDString { get { return UniqueID.ToString(); } }

        /// <summary>
        /// Parent CLEM Zone
        /// Stored here so rapidly retrieved
        /// </summary>
        [JsonIgnore]
        public string CLEMParentName { get; set; }

        /// <summary>
        /// return combo name of ParentName.ModelName
        /// </summary>
        [JsonIgnore]
        public string NameWithParent => $"{this.Parent.Name}.{this.Name}";

        /// <inheritdoc/>
        [JsonIgnore]
        public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

        /// <summary>
        /// Method to set defaults from Attribute for this model
        /// </summary>
        protected private void SetDefaults()
        {
            SetPropertyDefaults(this);
        }

        /// <summary>
        /// Public means of setting default values for a selected model
        /// </summary>
        /// <param name="model"></param>
        public static void SetPropertyDefaults(IModel model)
        {
            //Iterate through properties
            foreach (var property in model.GetType().GetProperties())
                //Iterate through attributes of this property
                foreach (Attribute attr in property.GetCustomAttributes(true))
                    //So lets try to load default value to the property
                    //does this property have [DefaultValueAttribute]?
                    if (attr is System.ComponentModel.DefaultValueAttribute dv)
                    {
                        if (dv != null)
                        {
                            if (property.PropertyType.IsEnum)
                                property.SetValue(model, Enum.Parse(property.PropertyType, dv.Value.ToString()));
                            else
                                property.SetValue(model, dv.Value, null);
                        }
                    }

        }

        /// <summary>
        /// A list of activity timers for this activity
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IActivityTimer> ActivityTimers
        {
            get
            {
                if (activityTimers is null)
                    activityTimers = Structure.FindChildren<IActivityTimer>();
                return activityTimers;
            }
        }

        /// <summary>
        /// Is timing ok for the current model
        /// </summary>
        public bool TimingOK
        {
            get
            {
                var result = Structure.FindChildren<IActivityTimer>().Sum(a => a.ActivityDue ? 0 : 1);
                return (result == 0);
            }
        }

        /// <summary>
        /// return a list of components available given the specified types
        /// </summary>
        /// <param name="typesToFind">the list of types to locate</param>
        /// <returns>A list of names of components including any string item in list provided</returns>
        public IEnumerable<string> GetResourcesAvailableByName(object[] typesToFind)
        {
            List<string> results = new List<string>();
            Zone zone = FindAncestor<Zone>();
            if (!(zone is null))
            {
                ResourcesHolder resources = Structure.FindChild<ResourcesHolder>(relativeTo: zone);
                if (!(resources is null))
                {
                    foreach (object type in typesToFind)
                    {
                        if (type is string)
                            results.Add(type as string);
                        else if (type is Type)
                        {
                            var res = resources.FindResource(type as Type);
                            IEnumerable<string> list = null;
                            if (res != null)
                                list = Structure.FindChildren<IResourceType>(relativeTo: res).Select(a => (a as CLEMModel).NameWithParent) ?? null;
                            if (list != null)
                                results.AddRange(Structure.FindChildren<IResourceType>(relativeTo: res)
                                       .Select(a => (a as CLEMModel).NameWithParent));
                        }
                    }
                }
            }
            return results.AsEnumerable();
        }

        /// <summary>
        /// Get a list of model names given specified types as array
        /// </summary>
        /// <param name="typesToFind">the list of types to include</param>
        /// <returns>A list of model names</returns>
        public IEnumerable<string> GetNameOfModelsByType(Type[] typesToFind)
        {
            Simulation simulation = FindAncestor<Simulation>();
            if (simulation is null)
                return new List<string>().AsEnumerable();
            else
            {
                List<Type> types = new List<Type>();
                return simulation.FindAllDescendants().Where(a => typesToFind.ToList().Contains(a.GetType())).Select(a => a.Name);
            }
        }

        /// <summary>
        /// Determines if this component has a valid parent based on parent attributes
        /// </summary>
        /// <returns></returns>
        public bool IsParentValid()
        {
            var parents = ReflectionUtilities.GetAttributes(this.GetType(), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList();
            return (parents.Where(a => a.ParentType.Name == this.Parent.GetType().Name).Any());
        }

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (this is IActivityCompanionModel && Parent != null && Parent is IHandlesActivityCompanionModels)
                return (Parent as IHandlesActivityCompanionModels).DefineCompanionModelLabels(GetType().Name).Identifiers;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to detemrine whether any identifiers have been provided by the parent
        /// Used to hide unnecessary property display in UI
        /// </summary>
        /// <returns></returns>
        public bool ParentSuppliedIdentifiersPresent()
        {
            var psi = ParentSuppliedIdentifiers();
            return (psi != null && psi.Any());
        }

        /// <summary>
        /// A method to return the list of units relavent to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedMeasures()
        {
            if (this is IActivityCompanionModel && Parent != null && Parent is IHandlesActivityCompanionModels)
                return (Parent as IHandlesActivityCompanionModels).DefineCompanionModelLabels(GetType().Name).Measures;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to detemrine whether any measures have been provided by the parent
        /// Used to hide unnecessary property display in UI
        /// </summary>
        /// <returns></returns>
        public bool ParentSuppliedMeasuresPresent()
        {
            var psm = ParentSuppliedMeasures();
            return (psm != null && psm.Any());
        }

        #region descriptive summary

        /// <summary>
        /// Returns the opacity value for this component in the summary display
        /// </summary>
        public double SummaryOpacity(bool formatForParent) => ((!this.Enabled & (!formatForParent | (formatForParent & this.Parent.Enabled))) ? 0.4 : 1.0);

        /// <summary>
        /// Create a html snippet
        /// </summary>
        /// <param name="value">The value to report</param>
        /// <param name="errorString">Error text when missing</param>
        /// <param name="entryStyle">Style of snippet</param>
        /// <param name="htmlTags">Include html tags</param>
        /// <param name="warnZero">Allow zero entries</param>
        /// <returns>HTML span snippet</returns>
        public static string DisplaySummaryValueSnippet<T>(T value, string errorString = "Not set", HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default, bool htmlTags = true, bool warnZero = false)
        {
            string spanClass = "setvalue";
            string errorClass = "errorlink";
            switch (entryStyle)
            {
                case HTMLSummaryStyle.Default:
                    break;
                case HTMLSummaryStyle.Resource:
                    spanClass = "resourcelink";
                    break;
                case HTMLSummaryStyle.SubResource:
                    break;
                case HTMLSummaryStyle.SubResourceLevel2:
                    break;
                case HTMLSummaryStyle.Activity:
                    spanClass = "activitylink";
                    break;
                case HTMLSummaryStyle.SubActivity:
                    break;
                case HTMLSummaryStyle.SubActivityLevel2:
                    break;
                case HTMLSummaryStyle.Helper:
                    break;
                case HTMLSummaryStyle.FileReader:
                    spanClass = "filelink";
                    break;
                case HTMLSummaryStyle.Filter:
                    spanClass = "filterset";
                    errorClass = "filtererror";
                    break;
                default:
                    break;
            }

            bool zeroFound = false;
            double zeroTest = 0;
            if (value != null && warnZero && double.TryParse(value.ToString(), out zeroTest))
            {
                if (zeroTest == 0)
                {
                    zeroFound = true;
                    errorClass = "warninglink";
                    errorString = "? 0";
                }
            }

            string htmlEnd = (htmlTags) ? "</span>" : string.Empty;
            string valueString;
            string htmlStart = String.Empty;
            if (!zeroFound && value != null && value.ToString() != "" && value.ToString() != "NaN")
            {
                valueString = value.ToString();
                if (htmlTags)
                    htmlStart = $"<span class=\"{spanClass}\">";
            }
            else
            {
                valueString = errorString;
                if (htmlTags)
                    htmlStart = $"<span class=\"{errorClass}\">";
            }

            return $"{htmlStart}{valueString}{htmlEnd}";
        }

        /// <summary>
        /// Create a html snippet
        /// </summary>
        /// <param name="value">The value to report</param>
        /// <param name="errorString">Error text when missing</param>
        /// <param name="entryStyle">Style of snippet</param>
        /// <param name="htmlTags">Include html tags</param>
        /// <param name="warnZero">Display warning if value is zero</param>
        /// <returns>HTML span snippet</returns>
        public static string DisplaySummaryValueSnippet<T>(IList<T> value, string errorString = "Not set", HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default, bool htmlTags = true, bool warnZero = false)
        {
            string result = string.Empty;
            if (value.Any())
            {
                foreach (T item in value)
                    result += DisplaySummaryValueSnippet<T>(item, errorString, entryStyle, htmlTags, warnZero) + " ";
            }
            else
                result = DisplaySummaryValueSnippet<T>(null, errorString, entryStyle, htmlTags, warnZero);
            return result.TrimEnd();
        }

        /// <summary>
        /// Create a html snippet
        /// </summary>
        /// <param name="value">The value to report</param>
        /// <param name="errorString">Error text when missing</param>
        /// <param name="htmlTags">Include html tags</param>
        /// <param name="nullGeneralYards">replace empty with general yards</param>
        /// <returns>HTML span snippet</returns>
        public static string DisplaySummaryResourceTypeSnippet(string value, string errorString = "Not set", bool htmlTags = true, bool nullGeneralYards = false)
        {
            string spanClass = "resourcelink";
            string errorClass = "errorlink";
            string htmlEnd = (htmlTags) ? "</span>" : string.Empty;
            string valueString = string.Empty;
            string htmlStart = (htmlTags) ? $"<span class=\"{spanClass}\">" : string.Empty;

            if (value == null || value == "")
            {
                if (!nullGeneralYards)
                {
                    valueString = errorString;
                    htmlStart = (htmlTags) ? $"<span class=\"{errorClass}\">" : string.Empty;
                }
                else
                {
                    valueString = "Not specified - General yards";
                }
            }
            else
                valueString = value;

            return $"{htmlStart}{valueString}{htmlEnd}";
        }

        /// <summary>
        /// Provide a list of child types to include or ignore from summary for the given model
        /// </summary>
        /// <returns>List of (type, include, borderClass, introtext)</returns>
        public virtual List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
            };
        }

        /// <summary>
        /// Provide a list of child types to include or ignore from summary for the given model
        /// </summary>
        /// <returns>List of (type, include, borderClass, introtext)</returns>
        public virtual IEnumerable<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> HandleChildrenInSummary()
        {
            var modelsToSummarise = GetChildrenInSummary();

            // add all remaining models not specified above
            IEnumerable<IModel> unique = new List<IModel>();
            foreach (var selectFilter in modelsToSummarise.Select(a => a.models))
                unique = unique.Union(selectFilter);
            modelsToSummarise.Add((Structure.FindChildren<IModel>().Where(a => !unique.Contains(a)), true, "", "", ""));

            return modelsToSummarise;
        }

        /// <inheritdoc/>
        public virtual string ModelSummary()
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual string GetFullSummary(IModel model, List<string> parentControls, string htmlString, Func<string, string> markdown2Html = null)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (model.GetType().IsSubclassOf(typeof(CLEMModel)))
                {
                    CLEMModel cm = model as CLEMModel;
                    cm.CurrentAncestorList = parentControls.ToList();
                    cm.CurrentAncestorList.Add(model.GetType().Name);

                    htmlWriter.Write(cm.ModelSummaryOpeningTags());

                    if (cm.Notes != null && cm.Notes != "")
                    {
                        string memoContainerClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-container-simple" : "memo-container";
                        string memoHeadClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-head-simple" : "memo-head";
                        string memoTextClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-text-simple" : "memo-text";
                        htmlWriter.Write($"\r\n<div class='{memoContainerClass}'><div class='{memoHeadClass}'>Notes</div>");
                        htmlWriter.Write($"\r\n<div class='{memoTextClass}'>{cm.Notes}</div></div>");
                    }

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTagsBeforeSummary());

                    if (ReportMemosType == DescriptiveSummaryMemoReportingType.AtTop)
                        htmlWriter.Write(AddMemosToSummary(model, Structure, markdown2Html));

                    if (model is IActivityCompanionModel)
                    {
                        if (!FormatForParentControl && (model as IActivityCompanionModel).Identifier != null)
                            htmlWriter.Write($"\r\n<div class=\"activityentry\">Applies to {CLEMModel.DisplaySummaryValueSnippet((model as IActivityCompanionModel).Identifier)}</div>");
                    }

                    htmlWriter.Write(cm.ModelSummary());

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTags());

                    // TODO: think through the various model types that do not support memos being writen within children
                    // for example all the filters in a filter group and timers and cohorts
                    // basically anyting that does special actions with all the children
                    // if the current model supports memos in place set reportMemosInPlace to true.

                    if (ReportMemosType == DescriptiveSummaryMemoReportingType.AtBottom)
                        htmlWriter.Write(AddMemosToSummary(model, Structure, markdown2Html));

                    var childrenToSummarise = HandleChildrenInSummary();
                    foreach (var item in childrenToSummarise)
                    {
                        if (item.include)
                            htmlWriter.Write(GetChildDescriptiveSummaries(item.models, item.introText, item.missingText, item.borderClass, htmlString, cm, markdown2Html));
                    }

                    htmlWriter.Write(cm.ModelSummaryInnerClosingTags());

                    htmlWriter.Write(cm.ModelSummaryClosingTags());
                }
                return htmlWriter.ToString();
            }
        }

        private string GetChildDescriptiveSummaries(IEnumerable<IModel> models, string introText, string MissingText, string borderClass, string htmlString, CLEMModel cm, Func<string, string> markdown2Html = null)
        {
            bool addBorderIt = introText != "" && this is CLEMRuminantActivityBase && models.Any();
            if (models.Any() || MissingText != "")
            {
                using (StringWriter htmlWriter = new StringWriter())
                {
                    if (borderClass != "")
                        htmlWriter.Write($"\r\n<div class=\"{borderClass}\">");

                    if (introText != "")
                        htmlWriter.Write($"<div class=\"childgrouplabel\">{introText}</div>");

                    foreach (var item in models)
                    {
                        if (item is Memo)
                        {
                            if (ReportMemosType == DescriptiveSummaryMemoReportingType.InPlace)
                            {
                                string markdownMemo = (item as Memo).Text;
                                if (markdown2Html != null)
                                    markdownMemo = markdown2Html(markdownMemo);
                                markdownMemo = markdownMemo.Replace("\n", "<br />").Replace("</p><br />", "</p>");
                                string memoContainerClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-container-simple" : "memo-container";
                                string memoHeadClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-head-simple" : "memo-head";
                                string memoTextClass = (ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-text-simple" : "memo-text";

                                htmlWriter.Write($"<div class='{memoContainerClass}'><div class='{memoHeadClass}'>Memo</div><div class='{memoTextClass}'>{markdownMemo}</div></div>");
                            }
                        }
                        else
                        {
                            if (item is CLEMModel)
                                htmlWriter.Write((item as CLEMModel).GetFullSummary(item, cm.CurrentAncestorList.ToList(), htmlString));
                        }
                    }
                    if (!models.Any() && MissingText != "")
                        // write models not found error
                        htmlWriter.Write($"<div class=\"errorbanner clearfix><div class=\"filtererror\">{MissingText}</div></div>");

                    if (borderClass != "")
                        htmlWriter.Write("</div>");

                    return htmlWriter.ToString();
                }
            }
            return string.Empty;
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public virtual HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public List<string> CurrentAncestorList { get; set; } = new List<string>();

        /// <inheritdoc/>
        public bool FormatForParentControl { get { return CurrentAncestorList.Count > 1; } }

        /// <inheritdoc/>
        public virtual string ModelSummaryClosingTags()
        {
            return "\r\n</div>\r\n</div>";
        }

        /// <summary>
        /// Create memos included for summary description
        /// </summary>
        /// <param name="model">Model to report child memos for</param>
        /// <param name="structure">Structure instance</param>
        /// <param name="markdown2Html">markdown to html converter</param>
        /// <returns></returns>
        public static string AddMemosToSummary(IModel model, IStructure structure, Func<string, string> markdown2Html = null)
        {
            string html = "";
            string memoContainerClass = ((model as CLEMModel)?.ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-container-simple" : "memo-container";
            string memoHeadClass = ((model as CLEMModel)?.ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-head-simple" : "memo-head";
            string memoTextClass = ((model as CLEMModel)?.ModelSummaryStyle == HTMLSummaryStyle.Filter) ? "memo-text-simple" : "memo-text";

            foreach (var memo in structure.FindChildren<Memo>(relativeTo: model as INodeModel))
            {
                html += $"<div class='{memoContainerClass}'><div class='{memoHeadClass}'>Memo</div>";

                string memoText = memo.Text;
                if (markdown2Html != null)
                    memoText = markdown2Html(memoText);
                memoText = memoText.Replace("\n\n", "\n").Replace("\n", "<br />").Replace("</p><br />", "</p>");

                html += $"<div class='{memoTextClass}'>{memoText}</div></div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public virtual string ModelSummaryOpeningTags()
        {
            string overall = "activity";
            string extra = "";

            if (this.ModelSummaryStyle == HTMLSummaryStyle.Default)
            {
                if (this is Relationship || this.GetType().IsSubclassOf(typeof(Relationship)))
                    this.ModelSummaryStyle = HTMLSummaryStyle.Default;
                else if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                    this.ModelSummaryStyle = HTMLSummaryStyle.Resource;
                else if (typeof(IResourceType).IsAssignableFrom(this.GetType()))
                    this.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
                else if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                    this.ModelSummaryStyle = HTMLSummaryStyle.Activity;
            }

            switch (ModelSummaryStyle)
            {
                case HTMLSummaryStyle.Default:
                    overall = "default";
                    break;
                case HTMLSummaryStyle.Resource:
                    overall = "resource";
                    break;
                case HTMLSummaryStyle.SubResource:
                    overall = "resource";
                    extra = "light";
                    break;
                case HTMLSummaryStyle.SubResourceLevel2:
                    overall = "resource";
                    extra = "dark";
                    break;
                case HTMLSummaryStyle.Activity:
                    break;
                case HTMLSummaryStyle.SubActivity:
                    extra = "light";
                    break;
                case HTMLSummaryStyle.Helper:
                    break;
                case HTMLSummaryStyle.SubActivityLevel2:
                    extra = "dark";
                    break;
                case HTMLSummaryStyle.FileReader:
                    overall = "file";
                    break;
                case HTMLSummaryStyle.Filter:
                    overall = "filter";
                    break;
                default:
                    break;
            }

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"holder{((extra == "") ? "main" : "sub")} {overall}\" style=\"opacity: {SummaryOpacity(FormatForParentControl)};\">");
                htmlWriter.Write($"\r\n<div class=\"clearfix {overall}banner{extra}\">{ModelSummaryNameTypeHeader()}</div>");
                htmlWriter.Write($"\r\n<div class=\"{overall}content{((extra != "") ? extra : "")}\">");

                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public virtual string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual string ModelSummaryInnerOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (this.GetType().IsSubclassOf(typeof(CLEMResourceTypeBase)))
                {
                    // add units when completed
                    string units = (this as IResourceType).Units;
                    if (units != "NA")
                        htmlWriter.Write($"\r\n<div class=\"activityentry\">This resource is measured in {CLEMModel.DisplaySummaryValueSnippet(units)}</div>");
                }
                if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                    if (this.Children.Count() == 0)
                        htmlWriter.Write("\r\n<div class=\"activityentry\">Empty</div>");

                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public virtual string ModelSummaryInnerOpeningTagsBeforeSummary()
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual string ModelSummaryNameTypeHeaderText()
        {
            return this.Name;
        }

        /// <inheritdoc/>
        public string ModelSummaryNameTypeHeader()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"<div class=\"namediv\">{ModelSummaryNameTypeHeaderText()} {((!this.Enabled) ? " - DISABLED!" : "")}<br ><div class=\"typediv\">{this.GetType().Name}</div></div>");
                if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                {
                    htmlWriter.Write("<div class=\"partialdiv\"");
                    switch ((this as CLEMActivityBase).OnPartialResourcesAvailableAction)
                    {
                        case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                            htmlWriter.Write(" tooltip = \"Error and Stop on insufficient resources\">Stop");
                            break;
                        case OnPartialResourcesAvailableActionTypes.SkipActivity:
                            htmlWriter.Write(">Skip");
                            break;
                        case OnPartialResourcesAvailableActionTypes.UseAvailableResources:
                            htmlWriter.Write(">Partial");
                            break;
                        case OnPartialResourcesAvailableActionTypes.UseAvailableWithImplications:
                            htmlWriter.Write(">Impact");
                            break;
                        default:
                            break;
                    }
                    htmlWriter.Write("</div>");

                    if (this is CLEMActivityBase)
                    {
                        string transCat = CLEMActivityBase.UpdateTransactionCategory(this as CLEMActivityBase);
                        if (transCat != "")
                            htmlWriter.Write($"<div class=\"partialdiv\">tag: {transCat}</div>");
                    }
                }
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Create the HTML for the descriptive summary display of a supplied component
        /// </summary>
        /// <param name="modelToSummarise">Model to create summary fpr</param>
        /// <param name="darkTheme">Boolean representing if in dark mode</param>
        /// <param name="markdown2Html">Method to convert markdown to html</param>
        /// <param name="bodyOnly">Only produve the body html</param>
        /// <param name="apsimFilename">Create master simulation summary header</param>
        /// <returns></returns>
        public static string CreateDescriptiveSummaryHTML(Model modelToSummarise, bool darkTheme = false, bool bodyOnly = false, string apsimFilename = "", Func<string, string> markdown2Html = null)
        {
            // currently includes autoupdate script for display of summary information in browser
            // give APSIM Next Gen no longer has access to WebKit HTMLView in GTK for .Net core
            // includes <!-- graphscript --> to add graphing js details if needed
            // includes <!-- CLEMZoneBody --> to add multiple components for overall summary

            string htmlString = "<!DOCTYPE html>\r\n" +
                "<html>\r\n<head>\r\n<script type=\"text / javascript\" src=\"https://livejs.com/live.js\"></script>\r\n" +
                "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\r\n" +
                "<meta http-equiv = \"Pragma\" content = \"no-cache\" />\r\n" +
                "<meta http-equiv = \"Expires\" content = \"0\" />\r\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\r\n" +
                "<style>\r\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
                "table {border-collapse: collapse; font-size:0.8em; }" +
                ".resource table,th,td {border: 1px solid #996633; }" +
                "table th {padding:8px; color:[HeaderFontColor];}" +
                "table td {padding:8px; }" +
                " td:nth-child(n+2) {text-align:center;}" +
                " th:nth-child(1) {text-align:left;}" +
                ".resource th {background-color: #996633 !important; }" +
                ".resource tr:nth-child(2n+3) {background:[ResRowBack] !important;}" +
                ".resource tr:nth-child(2n+2) {background:[ResRowBack2] !important;}" +
                ".resource td.fill {background-color: #c1946c !important;}" +
                ".resource td.disabled {opacity: 0.5 !important;}" +
                ".resourceborder {border-color:#996633; border-width:1px; border-style:solid; padding:0px; background-color:Cornsilk !important; }" +
                ".resource h1,h2,h3 {color:#996633; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".resourcebanner {background-color:#996633 !important; color:[ResFontBanner]; padding:5px; font-weight:bold; border-radius:5px 5px 0px 0px; }" +
                ".resourcebannerlight {background-color:#c1946c !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcebannerdark {background-color:#996633 !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcecontent {background-color:[ResContBack] !important; margin-bottom:40px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcebanneralone {background-color:[ResContBack] !important; margin:10px 0px 5px 0px; border-radius:5px 5px 5px 5px; border-color:#996633; border-width:1px; border-style:solid solid solid solid; padding:5px;}" +
                ".resourcecontentlight {background-color:[ResContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#c1946c; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcecontentdark {background-color:[ResContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcelink {color:#996633; font-weight:bold; background-color:Cornsilk !important; border-color:#996633; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".activity th,td {padding:5px; }" +
                ".activity table,th,td {border: 1px solid #996633; }" +
                ".activity th {background-color: #996633 !important; }" +
                ".activity td.fill {background-color: #996633 !important; }" +
                ".activity table {border-collapse: collapse; font-size:0.8em; }" +
                ".activity h1 {color:#009999; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".activityborder {border-color:#009999; border-width:2px; border-style:none none none solid; padding:0px 0px 0px 10px; margin-bottom:15px; }" +
                ".activityborderfull {border-color:#009999; border-radius:5px; background-color:#f0f0f0 !important; border-width:1px; border-style:solid; margin-bottom:40px; }" +
                ".activitybanner {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:#f0f0f0; padding:5px; font-weight:bold }" +
                ".activitybannerlight {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannerdark {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannercontent {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:5px; }" +
                ".activitycontent {background-color:[ActContBack] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#009999; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitycontentlight {background-color:[ActContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:solid; padding:10px;}" +
                ".activitycontentdark {background-color:[ActContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitypadding {padding:10px; }" +
                ".activityentry {padding:5px 0px 5px 0px; }" +
                ".activityarea {padding:10px; }" +
                ".activitygroupsborder {border-color:#86b2b1; background-color:[ActContBackGroups] !important; border-width:1px; border-style:solid; padding:5px 10px; margin-bottom:5px; margin-top:15px;}" +
                ".activitylink {color:#009999; font-weight:bold; background-color:[ActContBack] !important; border-color:#009999; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".topspacing { margin-top:10px; }" +
                ".disabled { color:#CCC; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:left; vertical-align:middle; font-size:0.6em; }" +
                ".highlightdiv { float:left; vertical-align:middle; color:black; border-color:black; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-top: 5px; margin-right:10px; border-radius:3px; background-color:DarkOrange}" +
                ".partialdiv { font-size:0.8em; float:right; text-transform: uppercase; color:white; font-weight:bold; vertical-align:middle; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px;  border-radius:3px; }" +
                ".partialinvertdiv { font-size:0.8em; float:right; text-transform: uppercase; color:black; font-weight:bold; vertical-align:middle; background-color:white; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px; border-radius:6px; }" +
                ".filelink {color:green; font-weight:bold; background-color:mintcream !important; border-color:green; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".errorlink {color:white; font-weight:bold; background-color:red !important; border-color:darkred; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".warninglink {color:white; font-weight:bold; background-color:orange !important; border-color:darkorange; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".setvalue {font-weight:bold; background-color: [ValueSetBack] !important; Color: [ValueSetFont]; border-color:#697c7c; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".folder {color:#666666; font-style: italic; font-size:1.1em; }" +
                ".childgrouplabel {color:#666666; font-style: italic; font-size:0.9em; }" +
                ".childgrouprotationborder {border-color:#86b2b1; background-color:[CropRotationBack] !important; border-width:1px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px;margin-top:10px; border-radius:5px; }" +
                ".childgroupactivityborder {border-color:#009999; background-color:[ActContBackLight] !important; border-width:1px; border-style:solid; padding:5px; margin-bottom:5px; margin-top:5px; border-radius:5px;}" +
                ".childgroupfilterborder {border-color:#cc33cc; background-color:[LabourGroupBack] !important; border-width:1px; border-style:solid; padding:5px; margin-bottom:5px; margin-top:5px; border-radius:5px;}" +
                ".labournote {font-style: italic; color:#666666; padding-top:7px;}" +
                ".warningbanner {background-color:Orange !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".errorbanner {background-color:Red !important; border-radius:5px 5px 5px 5px; color:White; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".memobanner {background-color:white !important; border-radius:5px 5px 5px 5px;border-color:Blue;border-width:1px;border-style:solid;color:Navy; padding:10px; margin-bottom:10px;margin-top:10px; font-size:0.8em;}" +
                ".memobanner {background-color:white !important; border-radius:5px 5px 5px 5px;border-color:Blue;border-width:1px;border-style:solid;color:Navy; padding:10px; margin-bottom:10px;margin-top:10px; font-size:0.8em;}" +
                ".memo-container {display:grid; grid-template-columns: 70px auto;border-radius:7px; border-color:DeepSkyBlue; border-width:2px; border-style:solid; margin-bottom:10px; margin-top:10px;}" +
                ".memo-head {background-color:DeepSkyBlue;padding:10px;color:white;font-weight:bold;}" +
                ".memo-text {margin:auto;margin-left:15px;padding:5px;color:Black;}" +
                ".memo-container-simple {float:left; display:grid; grid-template-columns: 55px auto;border-radius:3px; border-color:DeepSkyBlue; border-width:2px; border-style:solid; margin:5px 5px 0px 5px;}" +
                ".memo-head-simple {background-color:DeepSkyBlue;padding:0px;color:white;font-weight:bold;}" +
                ".memo-text-simple {margin:auto 5px; color:Black; font-size:0.8em;}" +
                ".memo-container h1 {color:#000000; } .activity h1,h2,h3 { color:#000000; margin-bottom:5px; }" +
                ".filterlink {font-weight:bold; color:#cc33cc; background-color:[FiltContBack] !important; border-color:#cc33cc; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".filtername {margin:5px 0px 5px 0px; font-size:0.9em; color:#cc33cc;font-weight:bold;}" +
                ".filterborder {display: block; width: 100% - 40px; border-color:#cc33cc; background-color:[FiltContBack] !important; border-width:1px; border-style:solid; padding:0px 5px 5px 5px; margin:5px 0px 5px 0px; border-radius:5px; }" +
                ".filterset {font-size:0.85em; font-weight:bold; color:#cc33cc; background-color:[FiltContBack] !important; border-width:0px; border-style:none; padding: 1px 3px; margin: 2px 3px 0px 0px; border-radius:3px; }" +
                ".filteractivityborder {background-color:[FiltContActivityBack] !important; color:#fff; }" +
                ".filter {float: left; border-color:#cc33cc; background-color:#cc33cc !important; color:white; border-width:1px; border-style:solid; padding: 1px 5px 1px 5px; margin: 5px 5px 0px 5px; border-radius:3px;}" +
                ".filtererror {font-size:0.85em; font-weight:bold; border-color:red; background-color:[FiltContBack] !important; color:red; border-width:1px; border-style:solid; padding: 1px 3px; font-weight:bold; margin: 2px 3px 0px 0px; border-radius:3px;}" +
                ".filebanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:mintcream; padding:5px; font-weight:bold }" +
                ".filecontent {background-color:[ContFileBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".defaultbanner {background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:[ContDefaultBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".holdersub {margin: 5px 0px 5px}" +
                ".otherlink {font-weight:bold; color:black; background-color:[ContDefaultBack] !important; border-color:black; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                "\r\n</style>\r\n<!-- graphscript --></ head>\r\n<body>\r\n<!-- CLEMZoneBody -->";

            // apply theme based settings
            if (!darkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "black");
                htmlString = htmlString.Replace("[HeaderFontColor]", "white");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResRowBack2]", "white");
                htmlString = htmlString.Replace("[ResContBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResContBackLight]", "white");
                htmlString = htmlString.Replace("[ResContBackDark]", "floralwhite");
                htmlString = htmlString.Replace("[ResFontBanner]", "white");
                htmlString = htmlString.Replace("[ResFontContent]", "black");

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#efffff");
                htmlString = htmlString.Replace("[ActContBackLight]", "#ffffff");
                htmlString = htmlString.Replace("[ActContBackDark]", "#efffff");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#ffffff");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#e6e6e6");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#000");

                htmlString = htmlString.Replace("[ContFileBack]", "#deffde");

                htmlString = htmlString.Replace("[CropRotationBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#996633");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#fbe8fc");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#e8fbfc");
                htmlString = htmlString.Replace("[ValueSetFont]", "#000000");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");
                htmlString = htmlString.Replace("[HeaderFontColor]", "black");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResRowBack2]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResContBackLight]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBackDark]", "#281A0E");
                htmlString = htmlString.Replace("[ResFontBanner]", "#ffffff");
                htmlString = htmlString.Replace("[ResFontContent]", "#ffffff");

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#003F3D");
                htmlString = htmlString.Replace("[ActContBackLight]", "#005954");
                htmlString = htmlString.Replace("[ActContBackDark]", "#f003F3D");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#f003F3D");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#282828");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#686868");

                htmlString = htmlString.Replace("[ContFileBack]", "#0C440C");

                htmlString = htmlString.Replace("[CropRotationBack]", "#97B2B1");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#c1946c");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#c1946c");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#5c195e");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#49adc4");
                htmlString = htmlString.Replace("[ValueSetFont]", "#0e2023");
            }

            using (StringWriter htmlWriter = new StringWriter())
            {
                if (!bodyOnly)
                {
                    htmlWriter.Write(htmlString);

                    if (apsimFilename == "")
                        htmlWriter.Write("\r\n<span style=\"font-size:0.8em; font-weight:bold\">You will need to keep refreshing this page to see changes relating to the last component selected</span><br /><br />");
                }
                htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");

                string fullname = modelToSummarise.Name;
                if (modelToSummarise is CLEMModel)
                    fullname = (modelToSummarise as CLEMModel).NameWithParent;

                if (apsimFilename != "")
                    htmlWriter.Write($"<div class=\"namediv\">Full simulation settings</div>");
                else
                    htmlWriter.Write($"<div class=\"namediv\">Component {modelToSummarise.GetType().Name} named {fullname}</div>");

                htmlWriter.Write($"<br /><div class=\"typediv\">Details</div>");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"defaultcontent\">");

                if (apsimFilename != "")
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Filename: {apsimFilename}</div>");
                    Model sim = (modelToSummarise as Model).FindAncestor<Simulation>();
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Simulation: {sim.Name}</div>");
                }

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}</div>");
                htmlWriter.Write("\r\n</div>");

                if (modelToSummarise is ZoneCLEM)
                    htmlWriter.Write((modelToSummarise as ZoneCLEM).GetFullSummary(modelToSummarise, new List<string>(), htmlWriter.ToString(), markdown2Html));
                else if (modelToSummarise is Market)
                    htmlWriter.Write((modelToSummarise as Market).GetFullSummary(modelToSummarise, new List<string>(), markdown2Html));
                else if (modelToSummarise is CLEMModel)
                    htmlWriter.Write((modelToSummarise as CLEMModel).GetFullSummary(modelToSummarise, new List<string>(), htmlWriter.ToString(), markdown2Html));
                else if (modelToSummarise is ICLEMDescriptiveSummary)
                    htmlWriter.Write((modelToSummarise as ICLEMDescriptiveSummary).GetFullSummary(modelToSummarise, new List<string>(), htmlWriter.ToString(), markdown2Html));
                else
                    htmlWriter.Write("<b>This component has no descriptive summary</b>");

                if (!bodyOnly)
                    htmlWriter.WriteLine("\r\n</body>\r\n</html>");

                if (htmlWriter.ToString().Contains("<canvas"))
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    StreamReader textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Models.Resources.CLEM.Chart.min.js"));
                    string graphString = textStreamReader.ReadToEnd();
                    if (!darkTheme)
                    {
                        graphString = graphString.Replace("[GraphGridLineColour]", "#eee");
                        graphString = graphString.Replace("[GraphGridZeroLineColour]", "#999");
                        graphString = graphString.Replace("[GraphPointColour]", "#00bcd6");
                        graphString = graphString.Replace("[GraphLineColour]", "#fda50f");
                        graphString = graphString.Replace("[GraphLabelColour]", "#888");
                    }
                    else
                    {
                        // dark theme
                        graphString = graphString.Replace("[GraphGridLineColour]", "#555");
                        graphString = graphString.Replace("[GraphGridZeroLineColour]", "#888");
                        graphString = graphString.Replace("[GraphPointColour]", "#00bcd6");
                        graphString = graphString.Replace("[GraphLineColour]", "#ff0");
                        graphString = graphString.Replace("[GraphLabelColour]", "#888");
                    }

                    return htmlWriter.ToString().Replace("<!-- graphscript -->", $"<script>{graphString}</script>");
                }
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
