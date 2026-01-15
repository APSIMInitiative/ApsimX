using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.ApsimFile;
using Models.PMF.Struct;
using Svg.FilterEffects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Helper to write per-component descriptive summary files to disk.
/// </summary>
public class DescriptiveSummaryGenerator
{
    private List<(string tag, string id, bool disabled)> openBlockIds = [];

    private StringBuilder sb;

    /// <summary>
    /// Determine the current indent index based on open divs
    /// </summary>
    public int IndentIndex { get { return openBlockIds.Count; } }

    /// <summary>
    /// Returns a sequence of tab characters representing the current indent index
    /// </summary>
    public string GetIndentTabs { get { return new string('\t', Math.Max(0, IndentIndex)); } }

    /// <summary>
    /// Dark mode switch
    /// </summary>
    public bool IsDarkMode { get; set; } = false;

    /// <summary>
    /// Identifies that we are in a disabled state
    /// </summary>
    public bool CurrentlyDisabled
    {
        get
        {
            return openBlockIds.Any(a => a.disabled);
        }
    }

    /// <summary>
    /// Format type for output file
    /// </summary>
    public DescriptiveSummaryFormat OutputFormat { get; set; } = DescriptiveSummaryFormat.HTML;

    /// <summary>
    /// Constructor
    /// </summary>
    public DescriptiveSummaryGenerator(DescriptiveSummaryFormat format, bool isDarkMode)
    {
        OutputFormat = format;
        IsDarkMode = isDarkMode;
    }

    readonly string htmlStartString = "<!DOCTYPE html>\r\n" +
        "<html lang=\"en\">\r\n<head>\r\n<script type=\"text/javascript\" src=\"https://livejs.com/live.js\"></script>\r\n" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" + 
        "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\r\n" +
        "<meta http-equiv=\"Pragma\" content=\"no-cache\" />\r\n" +
        "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />";

    readonly string htmlEndString = "@media print { body { -webkit - print - color - adjust: exact; }}" +
        "\r\n</style>\r\n<!-- graphscript -->\r\n</head>\r\n";

    readonly string cssString = "<style>body {color: [FontColor]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
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
        ".filtersettitle {float:left; margin-right:5px;}" +
        ".filteractivityborder {background-color:[FiltContActivityBack] !important; color:#fff; }" +
        ".filter {float: left; border-color:#cc33cc; background-color:#cc33cc !important; color:white; border-width:1px; border-style:solid; padding: 1px 5px 1px 5px; margin: 5px 5px 0px 5px; border-radius:3px;}" +
        ".filtererror {font-size:0.85em; font-weight:bold; border-color:red; background-color:[FiltContBack] !important; color:red; border-width:1px; border-style:solid; padding: 1px 3px; font-weight:bold; margin: 2px 3px 0px 0px; border-radius:3px;}" +
        ".filebanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:mintcream; padding:5px; font-weight:bold }" +
        ".filecontent {background-color:[ContFileBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
        ".defaultbanner {background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
        ".defaultcontent {background-color:[ContDefaultBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
        ".holdermain {margin: 20px 0px 20px 0px}" +
        ".holdersub {margin: 5px 0px 5px}" +
        ".detailsnote {font-size:0.8em; font-style:italic; margin-bottom:10px;}" +
        ".otherlink {font-weight:bold; color:black; background-color:[ContDefaultBack] !important; border-color:black; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" + 
        ".disabledcomponent {opacity: 0.3}";

    readonly static Dictionary<string, (string, string)> colours = new()
    {
        { "FontColor", ("black", "#E5E5E5") },
        { "HeaderFontColor", ("white", "black") },
        { "ResRowBack", ("floralwhite","#281A0E") },
        { "ResRowBack2", ("white","#3F2817") },
        { "ResContBack", ("floralwhite","#281A0E") },
        { "ResContBackLight", ("white","#3F2817") },
        { "ResContBackDark", ("floralwhite","#281A0E") },
        { "ResFontBanner", ("white","white") },
        { "ResFontContent", ("black","white") },
        { "ActContBack", ("#efffff","#003F3D") },
        { "ActContBackLight", ("white","#005954") },
        { "ActContBackDark", ("#efffff","#f003F3D") },
        { "ActContBackGroups", ("white","#f003F3D") },
        { "ContDefaultBack", ("#e6e6e6","#282828") },
        { "ContDefaultBanner", ("black","#686868") },
        { "ContFileBack", ("#deffde","#0C440C") },
        { "CropRotationBack", ("white","#97B2B1") },
        { "LabourGroupBack", ("white","#c1946c") },
        { "LabourGroupBorder", ("#996633","#c1946c") },
        { "FiltContBack", ("#fbe8fc","#5c195e") },
        { "FiltContActivityBack", ("#cc33cc","#cc33cc") },
        { "ValueSetBack", ("#e8fbfc","#49adc4") },
        { "ValueSetFont", ("black","#0e2023") }
    };

    readonly static Dictionary<string, (string, string)> graphColours = new()
    {
        { "FontColor", ("black", "#E5E5E5") },
        { "HeaderFontColor", ("white", "black") },
        { "ResRowBack", ("floralwhite","#281A0E") },
        { "ResRowBack2", ("white","#3F2817") },
        { "ResContBack", ("floralwhite","#281A0E") },
        { "ResContBackLight", ("white","#3F2817") },
        { "ResContBackDark", ("floralwhite","#281A0E") },
        { "ResFontBanner", ("white","white") },
        { "ResFontContent", ("black","white") },
        { "ActContBack", ("#efffff","#003F3D") },
        { "ActContBackLight", ("white","#005954") },
        { "ActContBackDark", ("#efffff","#f003F3D") },
        { "ActContBackGroups", ("white","#f003F3D") },
        { "ContDefaultBack", ("#e6e6e6","#282828") },
        { "ContDefaultBanner", ("black","#686868") },
        { "ContFileBack", ("#deffde","#0C440C") },
        { "CropRotationBack", ("white","#97B2B1") },
        { "LabourGroupBack", ("white","#c1946c") },
        { "LabourGroupBorder", ("#996633","#c1946c") },
        { "FiltContBack", ("#fbe8fc","#5c195e") },
        { "FiltContActivityBack", ("#cc33cc","#cc33cc") },
        { "ValueSetBack", ("#e8fbfc","#49adc4") },
        { "ValueSetFont", ("black","#0e2023") }
    };

    /// <summary>
    /// Method to generate the descriptive summary for a specified component.
    /// </summary>
    public void GenerateSummaryForComponentAndChildren(IModel componentRoot, string filename, bool? dark = null, DescriptiveSummaryFormat? format = null)
    {
        ArgumentNullException.ThrowIfNull(componentRoot);
        if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));
        if (dark is not null)
            IsDarkMode = dark.Value;
        if (format is not null)
            OutputFormat = format.Value;

        sb = new StringBuilder();

        AddHTMLHeader(System.Net.WebUtility.HtmlEncode(componentRoot.Name ?? componentRoot.GetType().Name));
        AddDetails(componentRoot);
        AppendSummariesRecursively(componentRoot, 0);
        AddHTMLFooter();

        //File.WriteAllText(MakeSafeFileName(filename), sb.ToString());
        File.WriteAllText(filename, sb.ToString());
    }

    // Recursively visit model and its children, appending descriptive summary fragments.
    private void AppendSummariesRecursively(IModel model, int level)
    {
        if (model == null) return;

        var provider = DescriptiveSummaryResolver.GetProviderInstance(model, this);
        provider.NestedLevel = level+1;

        // Opening wrapper
        provider.CreateSummaryOpeningBlocks();

        if (!provider.FormatForParentControl && model is CLEMModel cm)
            AddNotes(cm.Notes, (cm.ModelSummaryStyle == HTMLSummaryStyle.Filter));

        // Place any pre-summary inner tags
        provider.CreateSummaryInnerOpeningBlocksBeforeSummary();
        // The concrete provider or model should override ModelSummary() to provide content

        if (provider.ReportMemosType == DescriptiveSummaryMemoReportingType.AtTop)
        {
            AddMemos(model.Children.OfType<Memo>());
        }

        if (model is IActivityCompanionModel acm)
        {
            if (!provider.FormatForParentControl && acm.Identifier != null)
            {
                AddBlockWithText("activityentry", $"Applies to {CLEMModel.DisplaySummaryValueSnippet(acm.Identifier)}");
            }
        }

        provider.BuildSummary();

        var childrenToSummarise = provider.HandleChildrenInSummary();
        foreach (var item in childrenToSummarise)
        {
            if (item.Include)
            {
                // Inner tags around the body (if used)
                provider.CreateSummaryInnerOpeningBlocks(item);

                GetChildDescriptiveSummaries(provider, item);

                provider.CreateSummaryInnerClosingBlocks(item);
            }
        }

        if (provider.ReportMemosType == DescriptiveSummaryMemoReportingType.AtBottom)
        {
            AddMemos(model.Children.OfType<Memo>());
        }

        provider.CreateSummaryClosingBlocks();
    }

    private void GetChildDescriptiveSummaries(IDescriptiveSummaryProvider provider, ChildComponentGroup componentGroup, Func<string, string> markdown2Html = null)
    {
        if (!componentGroup.SelectedModels.Any() && string.IsNullOrEmpty(componentGroup.Missing)) return;

        bool addBorderIt = componentGroup.Introduction != "" && provider.Model is CLEMRuminantActivityBase && componentGroup.SelectedModels.Any();

        using (OpenBlock(componentGroup.BorderCssClass))
        {
            if (componentGroup.Introduction != "")
            {
                AddBlockWithText("childgrouplabel", componentGroup.Introduction);
            }

            foreach (var item in componentGroup.SelectedModels)
            {
                switch (item)
                {
                    case Memo memo:
                        if (provider.ReportMemosType == DescriptiveSummaryMemoReportingType.InPlace)
                        {
                            AddMemos(new List<Memo> { memo });
                        }
                        break;
                    case IModel _:
                        AppendSummariesRecursively(item, provider.NestedLevel);
                        break;
                    default:
                        break;
                }
            }

            if (!componentGroup.SelectedModels.Any() && componentGroup.Missing != "")
            {
                using (OpenBlock("errorbanner clearfix"))
                {
                    AddBlockWithText("activityentry", componentGroup.Missing);
                }
            }
        }
    }

    /// <summary>
    /// Method to determine if notes property need to be displayed
    /// </summary>
    /// <param name="text">Note text</param>
    /// <param name="useSimpleFormat">Switch to use the simple format style</param>
    /// <param name="title">Label for title area</param>
    private void AddNotes(string text, bool useSimpleFormat = false, string title = "Notes")
    {
        if (!string.IsNullOrEmpty(text))
        {
            string memoContainerClass = useSimpleFormat ? "memo-container-simple" : "memo-container";
            string memoHeadClass = useSimpleFormat ? "memo-head-simple" : "memo-head";
            string memoTextClass = useSimpleFormat ? "memo-text-simple" : "memo-text";

            using (OpenBlock(memoContainerClass))
            {
                AddBlockWithText(memoHeadClass, "Notes");
                AddBlockWithText(memoTextClass, text);
            }
        }
    }

    /// <summary>
    /// Method to add memos
    /// </summary>
    /// <param name="memos">The list of memos to be added</param>
    private void AddMemos(IEnumerable<Memo> memos)
    {
        foreach (var memo in memos)
        {
            AddNotes(memo.Text, useSimpleFormat: false, title: memo.Name);
        }
    }

    private string MakeSafeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";
        var invalids = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalids.Contains(c) ? '_' : c)).Trim();
    }

    /// <summary>
    /// Adds specified string to output stream
    /// </summary>
    /// <param name="text">Text to add to output</param>
    public void Append(string text)
    { 
        if (text.Length > 0)
        {
            sb.AppendLine($"{GetIndentTabs}{text}");
            if (OutputFormat == DescriptiveSummaryFormat.HTML & text.Trim().EndsWith('>') == false)
            {
                AddLineBreak();
            }
        }
    }

    /// <summary>
    /// Adds a line break to output stream (e.g. br tag for html)
    /// </summary>
    public void AddLineBreak()
    {
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
        {
            sb.AppendLine($"{GetIndentTabs}<br>");
        }
    }

    /// <summary>
    /// Returns a line break snippet for the current output format including carriage return characters (e.g. br tag for html)
    /// </summary>
    public string DisplayLineBreak()
    {
        string htmltag = "";
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
        {
            htmltag = "<br>";
        }
        return $"{htmltag}{Environment.NewLine}{GetIndentTabs}";
    }

    /// <summary>
    /// Returns the plural suffix for a given count.
    /// </summary>
    /// <param name="count">The count to evaluate. A count not 1 (negative, zero, or > 1) identifies the plural.</param>
    /// <param name="word">The word to use (default empty to only return suffix)</param>
    /// <param name="singularSuffix">Specify the singular suffix (default is empty string)</param>
    /// <param name="pluralSuffix">Specify the plural suffix (default is 's")</param>
    /// <returns>Returns the singular or plural word based on the specified count</returns>
    public string DisplayPlural(int count, string word = "", string singularSuffix = "", string pluralSuffix = "s")
    {
        return $"{word}{((count == 1) ? singularSuffix : pluralSuffix)}";
    }

    private string SetCSS()
    {
        // for each key in colours, replace in cssText
        string updatedCss = cssString;
        foreach (var key in colours.Keys)
        {
            var (light, dark) = colours[key];
            if (IsDarkMode)
            {
                updatedCss = updatedCss.Replace($"[{key}]", dark);
            }
            else
            {
                updatedCss = updatedCss.Replace($"[{key}]", light);
            }
        }
        return updatedCss;
    }

    /// <summary>
    /// Create a table with specified columns
    /// </summary>
    public void CreateTable(IEnumerable<string> columnHeadingLabels)
    {
        string openTag = "|";
        string cellWrapStart = "";
        string cellWrapEnd = "|";
        string endTag = "";


        if (OutputFormat == DescriptiveSummaryFormat.HTML)
        {
            openTag = "<table><tr>";
            cellWrapStart = "<th>";
            cellWrapEnd = "</th>";
            endTag = "</tr>";
        }

        sb.AppendLine(GetIndentTabs+openTag);
        sb.Append(GetIndentTabs);
        foreach (var label in columnHeadingLabels)
        {
            sb.Append($"{cellWrapStart}{label}{cellWrapEnd}");
        }
        sb.AppendLine(endTag);

        if (OutputFormat == DescriptiveSummaryFormat.Markdown)
        {
            sb.Append(GetIndentTabs+openTag);
            foreach (var label in columnHeadingLabels)
            {
                sb.Append($"------{cellWrapEnd}");
            }
            sb.AppendLine(endTag);
        }
    }

    /// <summary>
    /// Adds a new row to an open table passing the column values
    /// </summary>
    /// <param name="columnValues">The string value to place in each cell</param>
    /// <param name="enabled">A switch to determine if the row is enabled</param>
    public void AddTableRow(IEnumerable<(string, bool)> columnValues, bool enabled)
    {
        string openTag = "|";
        string cellWrapStart = "";
        string cellWrapEnd = "|";
        string endTag = "";


        if (OutputFormat == DescriptiveSummaryFormat.HTML)
        {
            openTag = $"<tr{(enabled ? "" : " class=\"disabled\"")}>";
            cellWrapStart = "<td>";
            cellWrapEnd = "</td>";
            endTag = "</tr>";
        }

        sb.Append(GetIndentTabs + openTag);
        foreach (var label in columnValues)
        {
            if (OutputFormat == DescriptiveSummaryFormat.HTML && label.Item2)
                cellWrapStart = "<td class=\"fill\">";
            sb.Append($"{cellWrapStart}{label.Item1}{cellWrapEnd}");
        }
        sb.AppendLine(endTag);
    }

    /// <summary>
    /// Close a table
    /// </summary>
    public void CloseTable()
    {
        switch (OutputFormat)
        {
            case DescriptiveSummaryFormat.HTML:
                Append("</table>");
                break;
            case DescriptiveSummaryFormat.Markdown:
                sb.AppendLine();
                sb.AppendLine();
                break;
            case DescriptiveSummaryFormat.Text:
                break;
            default:
                break;
        }
    }

    private void AddHTMLHeader(string title)
    {
        if (OutputFormat != DescriptiveSummaryFormat.HTML)
            return;

        // Document header
        sb.AppendLine(htmlStartString);
        sb.AppendLine($"<title>Summary - {title}</title>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine(SetCSS());
        sb.AppendLine(htmlEndString);
        sb.AppendLine("<body>\r\n<!-- CLEMZoneBody -->");
    }

    private void AddHTMLFooter()
    {
        if (OutputFormat != DescriptiveSummaryFormat.HTML)
            return;

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }

    private void AddDetails(IModel component)
    {
        AddBlockWithText("detailsnote", $"{GetIndentTabs}You will need to keep refreshing this page to see changes relating to the last component selected");

        using (OpenBlock("clearfix defaultbanner"))
        {
            AddBlockWithText("namediv", $"Component {component.GetType().Name} named {component.Name}");
            AddLineBreak();
            AddBlockWithText("typediv", $"Details");
        }

        using (OpenBlock("defaultcontent"))
        {
            //Model sim = (component as Model).Node.FindParent<Simulation>(relativeTo: component as Model, recurse: true);
            //AddBlockWithText(sb, "activityentry", $"{GetIndentTabs}Simulation: {sim.Name}");
            AddBlockWithText("activityentry", $"{GetIndentTabs}Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}");
        }
    }

    /// <summary>
    /// Add a block with specified text
    /// </summary>
    /// <param name="classString"></param>
    /// <param name="text"></param>
    /// <param name="styleString"></param>
    /// <param name="disabled"></param>
    /// <param name="tag">HTML block type</param>
    public void AddBlockWithText(string classString, string text, string styleString = "", bool disabled = false, string tag = "div")
    {
        using (OpenBlock(classString, styleString, tag: tag, disabled: disabled))
        {
            sb.AppendLine(GetIndentTabs + text);
        }
    }

    /// <summary>
    /// Opens a block and returns an IDisposable that will close it when disposed.
    /// </summary>
    public IDisposable OpenBlock(string classString, string styleString = "",
        string id = "", bool disabled = false, string tag = "div")
    {
        if (tag == "div" && string.IsNullOrWhiteSpace(classString) && string.IsNullOrWhiteSpace(styleString))
        {
            return new BlockScope(this, ignore: true);
        }

        switch (OutputFormat)
        {
            case DescriptiveSummaryFormat.HTML:
                var openTag = new StringBuilder();
                openTag.Append($"{GetIndentTabs}<{tag}");
                if (!string.IsNullOrWhiteSpace(classString))
                    openTag.Append($" class=\"{classString}\"");
                if (!string.IsNullOrEmpty(styleString))
                    openTag.Append($" style=\"{styleString}\"");
                openTag.Append('>');
                sb.AppendLine(openTag.ToString());
                break;

            case DescriptiveSummaryFormat.Markdown:
                // simple mapping: use a bold heading for named divs or custom mapping as needed
                sb.AppendLine("**");
                break;

            case DescriptiveSummaryFormat.Text:
                sb.AppendLine("");
                break;
        }

        // record open div for indent / tracking purposes
        openBlockIds.Add((tag, id, disabled));

        return new BlockScope(this);
    }

    /// <summary>
    /// Close the most recently opened block and check id matches if provided.
    /// </summary>
    /// <param name="id">Label to identify the next block to close</param>
    public void CloseMostRecentBlock(string id = "")
    {
        if (openBlockIds.Count == 0) return;

        if (id != "" && openBlockIds[^1].id != id)
            throw new InvalidOperationException($"Mismatched block close. Expected to close block with id '{openBlockIds[^1].id}', but got '{id}'.");

        string tag = openBlockIds[^1].tag;

        // remove last
        openBlockIds.RemoveAt(openBlockIds.Count - 1);

        switch (OutputFormat)
        {
            case DescriptiveSummaryFormat.HTML:
                sb.AppendLine($"{GetIndentTabs}</{tag}>");
                break;
            case DescriptiveSummaryFormat.Markdown:
                // nothing specific to close for markdown; optionally add spacing
                sb.AppendLine("** ");
                break;
            case DescriptiveSummaryFormat.Text:
                sb.AppendLine();
                break;
        }
    }

    /// <summary>
    /// Small disposable used to close a div when the using scope ends.
    /// </summary>
    private sealed class BlockScope : IDisposable
    {
        private readonly DescriptiveSummaryGenerator parent;
        private readonly bool ignore;
        private bool disposed;

        public BlockScope(DescriptiveSummaryGenerator parent, bool ignore = false)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.ignore = ignore;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!ignore)
            {
                parent.CloseMostRecentBlock();
            }
        }
    }

    /// <summary>
    /// Create an error link snipped with specified text
    /// </summary>
    /// <param name="errorString"></param>
    /// <param name="htmlTags"></param>
    /// <returns></returns>
    public string DisplayErrorSnippet(string errorString = "Error", bool htmlTags = true)
    {
        string htmlStart = String.Empty;
        string htmlEnd = (htmlTags) ? "</span>" : string.Empty;
        if (htmlTags)
        {
            htmlStart = $"<span class=\"errorlink\">";
        }
        return $"{htmlStart}{errorString}{htmlEnd}";
    }

    /// <summary>
    /// Create a html snippet for a given value
    /// </summary>
    /// <param name="value">The value to report</param>
    /// <param name="errorString">Error text when missing</param>
    /// <param name="entryStyle">Style of snippet</param>
    /// <param name="htmlTags">Include html tags</param>
    /// <param name="warnZero">Format as warning ? 0 if zero</param>
    /// <param name="errorNotSet">Format as error if value not set or default</param>
    /// <param name="spanClass">Override the default 'setvalue' class to use.</param>
    /// <returns>HTML span snippet</returns>
    public string DisplaySummaryValueSnippet<T>(T value, string errorString = "Not set", HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default, bool htmlTags = true, bool warnZero = false, bool errorNotSet = false, string spanClass = "setvalue")
    {
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
        if (value != null && warnZero)
        {
            try
            {
                TypeCode tc = Type.GetTypeCode(value.GetType());
                switch (tc)
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        double zeroTest = Convert.ToDouble(value);
                        if (zeroTest == 0.0)
                        {
                            zeroFound = true;
                            errorClass = "warninglink";
                            errorString = "? 0";
                        }
                        break;
                }
            }
            catch
            {
                // ignore conversion errors and leave zeroFound = false
            }
        }
        string valueString;
        string htmlStart = String.Empty;
        string htmlEnd = (htmlTags) ? "</span>" : string.Empty;

        bool isMissingOrEmpty = (value == null) || (value.ToString() == "") || (value.ToString() == "NotSet");

        // detect NaN for float/double
        bool isNaN = false;
        if (!isMissingOrEmpty)
        {
            TypeCode tc2 = Type.GetTypeCode(value.GetType());
            if (tc2 == TypeCode.Double)
            {
                double d = (double)Convert.ChangeType(value, typeof(double));
                isNaN = double.IsNaN(d);
            }
            else if (tc2 == TypeCode.Single)
            {
                float f = (float)Convert.ChangeType(value, typeof(float));
                isNaN = float.IsNaN(f);
            }
        }

        if (!zeroFound && !isMissingOrEmpty && !isNaN && value.ToString() != "NaN")
        {
            // Format numerics using current culture with appropriate grouping and decimal separators.
            string formatted = null;
            try
            {
                TypeCode tc = Type.GetTypeCode(value.GetType());
                switch (tc)
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        // Integers: no decimal places, include group separators.
                        formatted = ((IFormattable)value).ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        // Floating: use "N" to include group separators and decimal separator per culture.
                        formatted = ((IFormattable)value).ToString("N", System.Globalization.CultureInfo.CurrentCulture);
                        break;
                    default:
                        // Non-numeric: if IFormattable respect culture, else fallback to ToString()
                        if (value is IFormattable formattable)
                            formatted = formattable.ToString(null, System.Globalization.CultureInfo.CurrentCulture);
                        else
                            formatted = value.ToString();
                        break;
                }
            }
            catch
            {
                // Fallback to ToString if formatting fails.
                formatted = value.ToString();
            }

            valueString = formatted;
            if (htmlTags)
            {
                htmlStart = $"<span class=\"{spanClass}\">";
            }
        }
        else
        {
            valueString = errorString;
            if (htmlTags)
            {
                htmlStart = $"<span class=\"{errorClass}\">";
            }
        }
        return $"{htmlStart}{valueString}{htmlEnd}";
    }

    /// <summary>
    /// Create a summary html snippet from a list of values
    /// </summary>
    /// <param name="value">A list of value to report</param>
    /// <param name="errorString">Error text when missing</param>
    /// <param name="entryStyle">Style of snippet</param>
    /// <param name="htmlTags">Include html tags</param>
    /// <param name="warnZero">Display warning if value is zero</param>
    /// <returns>HTML span snippet</returns>
    public string DisplaySummaryValueSnippet<T>(IList<T> value, string errorString = "Not set", HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default, bool htmlTags = true, bool warnZero = false)
    {
        string result = string.Empty;
        if (value.Any())
        {
            foreach (T item in value)
            {
                result += DisplaySummaryValueSnippet<T>(item, errorString, entryStyle, htmlTags, warnZero) + " ";
            }
        }
        else
        {
            result = DisplaySummaryValueSnippet<T>(null, errorString, entryStyle, htmlTags, warnZero);
        }

        return result.TrimEnd();
    }

    /// <summary>
    /// Create a summary resource type link as html snippet
    /// </summary>
    /// <param name="value">The value to report</param>
    /// <param name="errorString">Error text when missing</param>
    /// <param name="htmlTags">Include html tags</param>
    /// <param name="nullGeneralYards">replace empty with general yards</param>
    /// <returns>HTML span snippet</returns>
    public string DisplaySummaryResourceTypeSnippet(string value, string errorString = "Not set", bool htmlTags = true, bool nullGeneralYards = false)
    {
        string spanClass = "resourcelink";
        string errorClass = "errorlink";
        string htmlEnd = (htmlTags) ? "</span>" : string.Empty;
        string htmlStart = (htmlTags) ? $"<span class=\"{spanClass}\">" : string.Empty;

        string valueString;
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
        {
            valueString = value;
        }

        return $"{htmlStart}{valueString}{htmlEnd}";
    }

}
