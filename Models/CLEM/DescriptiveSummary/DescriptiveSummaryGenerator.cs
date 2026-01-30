using DocumentFormat.OpenXml.InkML;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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

        // For each entry in 'colours', if the 'light' string references a key in CLEMcolours,
        // replace it with the actual colour string from CLEMcolours.
        foreach (var key in colours.Keys.ToList())
        {
            var (light, dark) = colours[key];
            if (CLEMcolours.TryGetValue(light, out var clemValue))
            {
                // replace light with the mapped value
                light = clemValue.light;
                // assign the updated tuple back into the dictionary
                colours[key] = (light, dark);
            }
            if (CLEMcolours.TryGetValue(dark, out var clemValueDark))
            {
                // replace dark with the mapped value
                dark = clemValueDark.dark;
                // assign the updated tuple back into the dictionary
                colours[key] = (light, dark);
            }
        }
    }

    readonly string htmlStartString = "<!DOCTYPE html>\r\n" +
        "<html lang=\"en\">\r\n<head>\r\n<script type=\"text/javascript\" src=\"https://livejs.com/live.js\"></script>\r\n" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" + 
        "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\r\n" +
        "<meta http-equiv=\"Pragma\" content=\"no-cache\" />\r\n" +
        "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />";

    readonly string htmlEndString = "@media print { body { -webkit - print - color - adjust: exact; }}" +
        "\r\n</style>\r\n<!-- graphscript -->\r\n</head>\r\n";

    readonly string cssString = "<style>body {color: [BodyFont]; background-color: [BodyBackground]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
        "table {border-collapse:collapse; font-size:0.8em; }" +
        "table th {padding:8px; color:[HeaderBodyFont];}" +
        "table td {padding:8px; }" +
        "td:nth-child(n+2) {text-align:center;}" +
        "th:nth-child(1) {text-align:left;}" +
        "td.disabled {opacity: 0.5 !important;}" +
        ".resource table,th,td {border: 1px solid [TableBorder-Res]; }" +
        ".resource th {background-color: [TableHeaderBackground-Res] !important; }" +
        ".resource tr:nth-child(2n+3) {background:[TableRowBackground1-Res] !important;}" +
        ".resource tr:nth-child(2n+2) {background:[TableRowBackground2-Res] !important;}" +
        ".resource td.fill {background-color:[TableCellBackground1-Res] !important;}" +
        ".activity table,th,td {border: 1px solid Resource; }" +
        ".activity th {background-color: Resource !important; }" +
        ".activity td.fill {background-color: Resource !important; }" +
        ".resource h1,h2,h3 {color:[Headings-Res]; } " +
        ".activity h1,h2,h3 { color:[Headings-Act]; }" +
        ".disabled { color:#CCC; }" +
        ".disabledcomponent {opacity: 0.3}" +

        ".clearfix { overflow: auto; }" +
        ".partial { font-size:0.8em; float:right; text-transform: uppercase; color:white; font-weight:bold; vertical-align:middle; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px; border-radius:3px; }" +

        ".topspacing { margin-top:10px; }" +
        ".holdermain {margin: 0px 0px 40px 0px}" +
        ".holdersub {margin: 20px 0px 5px}" +

        ".folder {color:#666666; font-style: italic; font-size:1.1em; }" +

        ".componentBanner {font-weight:bold; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:0px; color:[BannerFont-Other]; background-color:[BannerBackground-Other];}" +
        ".componentContent {margin-bottom:20px; border-radius:0px 0px 5px 5px; border-width:1px; border-style:none solid solid solid; padding:10px; background-color:[ContentBackground-Other]; border-color:[BannerBackground-Other];}" +
        ".componentContentNoBanner {margin-bottom:20px; border-radius:5px; border-width:1px; border-style:solid; padding:5px; background-color:[ContentBackground-Other]; border-color:[BannerBackground-Other];}" +

        ".resource .componentContentAlone {background-color:[BannerContentBackground-Res]; border-color:Resource}" +

        ".resource .componentBanner {color:[BannerFont-Res]; background-color:[BannerBackground-Res];}" +
        ".resource .light.componentBanner {background-color:[BannerBackgroundLight-Res]; }" +
        ".resource .dark.componentBanner {background-color:[BannerBackgroundDark-Res]; }" +
        ".resource .light.componentContent {background-color:[ContentBackgroundLight-Res]; border-color:[BannerBackgroundLight-Res]; }" +
        ".resource .dark.componentContent {background-color:[ContentBackgroundDark-Res]; border-color:[BannerBackgroundDark-Res]; }" +

        ".activity .componentBanner {color:[BannerFont-Act]; background-color:[BannerBackground-Act]}" +
        ".activity .light.componentBanner {background-color:[BannerBackgroundLight-Act]; }" +
        ".activity .dark.componentBanner {background-color:[BannerBackgroundDark-Act]; }" +
        ".activity .light.componentContent {background-color:[ContentBackgroundLight-Act]; border-color:[BannerBackgroundLight-Act]; }" +
        ".activity .dark.componentContent {background-color:[ContentBackgroundDark-Act]; border-color:[BannerBackgroundDark-Act]; }" +

        ".file .componentBanner {color:[BannerFont-File]; background-color:[BannerBackground-File]}" +
        ".file .componentContent {background-color:[ContentBackground-File] !important; border-color:[BannerBackground-File]; }" +
        ".other .componentBanner {color:[BannerFont-Other] !important; background-color:[BannerBackground-Other] !important}" +
        ".other .componentContent {background-color:[ContentBackground-Other] !important; border-color:[BannerBackground-Other] !important; }" +

        ".memo .componentBanner {background-color:[MemoBorder]; padding:10px; color:[MemoTitle]; }" +

        ".memobanner {background-color:white !important; border-radius:5px 5px 5px 5px;border-color:Blue;border-width:1px;border-style:solid;color:Navy; padding:10px; margin-bottom:10px;margin-top:10px; font-size:0.8em;}" +
        ".memo-container {display:grid; grid-template-columns: 70px auto;border-radius:7px; border-color:[MemoBorder]; border-width:2px; border-style:solid; background-color:[MemoBackground]; margin-bottom:10px; margin-top:10px;}" +
        ".memo-head {background-color:[MemoBorder]; padding:10px; color:[MemoTitle]; font-weight:bold;}" +
        ".memo-text {margin:auto;margin-left:15px;padding:5px;color:[MemoText];}" +
        ".memo-container-simple {float:left; display:grid; grid-template-columns: 55px auto;border-radius:3px; border-color:[MemoBorder]; border-width:2px; border-style:solid; margin:5px 5px 0px 5px;}" +
        ".memo-head-simple {background-color:[MemoBorder]; padding:0px; color:[MemoTitle]; font-weight:bold;}" +
        ".memo-text-simple {margin:auto 5px; color:[MemoText]; font-size:0.8em;}" +
        ".memo-container h1 {color:[MemoText]; } " +

        ".entryHolder {padding:5px 0px 5px 0px;}" +
        ".entryHolder.indent {margin-left:15px;}" +

        ".entryValue {font-weight:bold; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; color:[ValueFont]; background-color:[ValueBackground]; border-color:[ValueBorder]; }" +
        ".resourceValue.entryValue {color:[ValueFont-Res]; background-color:[ValueBackground-Res]; border-color:[ValueBorder-Res];}" +
        ".activityValue.entryValue {color:[ValueFont-Act]; background-color:[ValueBackground-Act]; border-color:[ValueBorder-Act];}" +
        ".fileValue.entryValue {color:[ValueFont-File]; background-color:[ValueBackground-File]; border-color:[ValueBorder-File];}" +
        ".errorValue.entryValue {color:[ValueFont-Error]; background-color:[ValueBackground-Error]; border-color:[ValueBorder-Error];}" +
        ".warningValue.entryValue {color:[ValueFont-Warn]; background-color:[ValueBackground-Warn]; border-color:[ValueBorder-Warn];}" +
        ".filterValue.entryValue {color:[ValueFont-Filter]; background-color:[ValueBackground-Filter]; border-color:[ValueBorder-Filter];}" +
        ".filterError.entryValue {color:[ValueFont-Filter]; background-color:[ValueBackground-Filter]; border-color:[ValueBorder-Filter];}" +
        ".otherValue.entryValue {color:[ValueFont-Other]; background-color:[ValueBackground-Other]; border-color:[ValueBorder-Other];}" +
        ".labelValue.entryValue {background-color:transparent; border-color:transparent; border-width:0px; border-style:none; padding:0px; border-radius:0px;}" +
        ".filterItem.entryValue {color:[Font-Filter]; background-color:[Filter]; border-color:[Filter]; padding: 2px 5px 2px 5px; margin: 5px 5px 0px 5px;}" +

        ".entryValue.floatLeft {float:left; vertical-align:middle;}" +
        ".entryValue.smaller {font-size:0.6em;}" +
        ".filterItem .entryValue {font-size:0.9em;}" +
        ".error.filteritem {border-color:[Font-FilterError]; background-color:[Background-ErrorFilter]; color:[Font-FilterError]; font-weight:bold; }" +
        ".filteritemstitle {float:left; margin-right:5px;}" +
        ".childTitle {margin:10px 0px 5px 0px; font-weight:bold;}" + //  font-size:0.9em;
        ".filter.childTitle {color:[Font-Filter];}" +
        ".resource.childTitle {color:[Headings-Res];}" +

        ".childgrouplabel {padding:5px 0px 5px 0px; margin-bottom: 10px;}" + //color:#666666; font-style: italic; font-size:0.9em;
        ".childgroupborder {border-width:0px; border-style:solid; padding:10px; margin:10px 0px 5px 0px; border-radius:5px; }" +
        ".rotationgroup.childgroupborder {border-color:[GroupBorder-Rotation]; background-color:[GroupBackground-Rotation]; border-width:1px; }" +
        ".activitygroup.childgroupborder {border-color:[GroupBorder-Act]; background-color:[GroupBackground-Act]; border-width:1px; }" +
        ".filtergroup.childgroupborder {border-color:[GroupBorder-Filter]; background-color:[GroupBackground-Filter]; border-width:1px; }" +
        ".resourcegroup.childgroupborder {border-color:[GroupBorder-Res]; background-color:[BannerBackgroundDark-Res]; border-width:1px; }" +
        ".labourgroup.childgroupborder {border-color:[GroupBorder-Labour]; background-color:[GroupBackground-Labour]; border-width:1px; }" +
        ".filteritems.childgroupborder {display: block; width: 100% - 40px; border-color:[Filter]; background-color:[Content-Filter]; border-width:1px; padding:0px 5px 5px 5px; margin:5px 0px 5px 0px;}" +
        ".othergroup.childgroupborder {border-color:[BannerBackground-Other]; background-color:transparent; border-width:1px; }" +

        ".parametername {float: left; background-color:Resource !important; color:[BannerFont-Res]; border-width:0px; border-style:none; padding: 1px 5px 1px 5px; margin: 7px 0px 0px 0px; border-radius:3px; font-weight:bold; font-size:0.6em}" +
        ".parameterdetails {float: left; padding: 1px 5px 1px 5px; margin: 0px 5px 0px 5px;}" +

        ".infoBanner {background-color:[BannerBackground]; border-radius:5px 5px 5px 5px; color:[BannerFont]; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
        ".infoBanner.warning {background-color:[BannerBackground-Warning]; color:[BannerFont-Warning];}" +
        ".infoBanner.error {background-color:[BannerBackground-Error]; color:[BannerFont-Error];}" +

        ".detailsnote {font-weight:bold; margin:5px 0px 5px 0px; padding:5px; }" + //font - style:italic; font-size:0.8em; 
        ".resource .childgroupborder .detailsnote {border-color:[GroupBorder-Res]; background-color:[GroupBackground-Res]; border-width:1px; }" +
        
        ".activityborder {border-color:[GroupBackground-Act]; border-width:2px; border-style:none none none solid; padding:0px 0px 0px 10px; margin-bottom:15px; }" +
        "";

    readonly static Dictionary<string, (string light, string dark)> CLEMcolours = new()
    {
        { "Resource", ("#996Resource633", "#996633") },
        { "ResourceLight", ("#c1946c","#c1946c") },
        { "Activity", ("#009999", "#009999") },
        { "Filter", ("#cc33cc","#952295") },
        { "FontLight", ("floralwhite","#281A0E") },
    };

    private Dictionary<string, (string light, string dark)> colours = new()
    {
        { "BodyBackground", ("white", "#101010") },
        { "BodyFont", ("black", "#d0d0d0") },
        { "Headings-Res", ("Resource","Resource") },
        { "Headings-Act", ("Activity","Activity") },
        { "MemoBorder", ("deepskyblue", "#49adc4") },
        { "MemoBackground", ("white", "#006064") },
        { "MemoTitle", ("white", "#0e2023") },
        { "MemoText", ("black", "#CFD8DC") },
        { "TableBorder-Res", ("Resource", "Resource") },
        { "TableHeaderBackground-Res", ("Resource", "Resource") },
        { "TableRowBackground1-Res", ("FontLight","FontLight") },
        { "TableRowBackground2-Res", ("white","#3F2817") },
        { "TableCellBackground1-Res", ("ResourceLight","ResourceLight") },
        { "Border-Res", ("ResourceLight","ResourceLight") },
        { "BorderBackground-Res", ("Cornsilk","Cornsilk") },
        { "BannerBackground-Res", ("Resource","Resource") },
        { "BannerBackgroundLight-Res", ("ResourceLight","ResourceLight") },
        { "BannerBackgroundDark-Res", ("Resource","Resource") },
        { "BannerFont-Res", ("FontLight","FontLight") },
        { "ContentBackground-Res", ("FontLight","FontLight") },
        { "ContentBackgroundLight-Res", ("white","#3F2817") },
        { "ContentBackgroundDark-Res", ("FontLight","FontLight") },
        { "ContentFont-Res", ("black","#e5e5e5") },
        { "ContentBorderLight-Res", ("ResourceLight","ResourceLight") },
        { "ContentBorderDark-Res", ("Resource","Resource") },
        { "Border-Act", ("ResourceLight","ResourceLight") },
        { "BorderBackground-Act", ("Cornsilk","Cornsilk") },
        { "BannerBackground-Act", ("Activity","Activity") },
        { "BannerBackgroundLight-Act", ("white","Activity") },
        { "BannerBackgroundDark-Act", ("Activity","Activity") },
        { "BannerFont-Act", ("FontLight","FontLight") },
        { "ContentBackground-Act", ("#efffff","#003F3D") },
        { "ContentBackgroundLight-Act", ("white","#005954") },
        { "ContentBackgroundDark-Act", ("#efffff","#003F3D") },
        { "ContentFont-Act", ("black","#e5e5e5") },
        { "ContentBorderLight-Act", ("ResourceLight","ResourceLight") },
        { "ContentBorderDark-Act", ("Resource","Resource") },
        { "BannerBackground-File", ("Green","Green") },
        { "BannerFont-File", ("MintGreen","MintGreen") },
        { "ContentBackground-File", ("#deffde","#0C440C") },
        { "BannerBackground-Other", ("black","#686868") },
        { "BannerFont-Other", ("white","#E0E0E0") },
        { "ContentBackground-Other", ("#e6e6e6","#282828") },
        { "ValueFont", ("black","#0e2023") },
        { "ValueBackground", ("#e8fbfc","#49adc4") },
        { "ValueBorder", ("#e8fbfc","#49adc4") },
        { "ValueFont-Res", ("Resource","Resource") },
        { "ValueBackground-Res", ("Cornsilk","Cornsilk") },
        { "ValueBorder-Res", ("Resource","Resource") },
        { "ValueFont-Act", ("Activity","Activity") },
        { "ValueBackground-Act", ("#efffff", "#efffff") },
        { "ValueBorder-Act", ("Activity","Activity") },
        { "ValueFont-File", ("Green","Green") },
        { "ValueBackground-File", ("mintcream", "mintcream") },
        { "ValueBorder-File", ("Green","Green") },
        { "ValueFont-Filter", ("Filter","#de91de") },
        { "ValueBackground-Filter", ("#fbe8fc","#1a011b") },
        { "ValueBorder-Filter", ("Filter","#1a011b") },
        { "ValueFont-FilterError", ("Red","Red") },
        { "ValueBackground-FilterError", ("#fbe8fc","#5c195e") },
        { "ValueBorder-FilterError", ("Red","Red") },
        { "ValueFont-Error", ("White","Pink") },
        { "ValueBackground-Error", ("Red", "DarkRed") },
        { "ValueBorder-Error", ("DarkRed","DarkRed") },
        { "ValueFont-Warn", ("White","White") },
        { "ValueBackground-Warn", ("Orange","Orange") },
        { "ValueBorder-Warn", ("DarkOrange", "DarkOrange") },
        { "ValueFont-Other", ("Black","Black") },
        { "ValueBackground-Other", ("#e6e6e6","#e6e6e6") },
        { "ValueBorder-Other", ("Black", "Black") },
        { "GroupBorder-Res", ("Resource", "Resource") },
        { "GroupBorder-Act", ("Activity", "Activity") },
        { "GroupBorder-Filter", ("Filter","Filter") },
        { "GroupBorder-Rotation", ("#86b2b1", "#86b2b1") },
        { "GroupBorder-Labour", ("Resource","ResourceLight") },
        { "GroupBackground-Res", ("white","#3F2817") },
        { "GroupBackground-Act", ("white","#f003F3D") },
        { "GroupBackground-Filter", ("#fbe8fc","#1a011b") },
        { "GroupBackground-Rotation", ("white","#97B2B1") },
        { "GroupBackground-Labour", ("white","ResourceLight") },
        { "Filter", ("Filter","Filter") },
        { "Content-Filter", ("#fbe8fc","#5c195e") },
        { "Font-Filter", ("white","#f0caf0") },
        { "Background-FilterError", ("#ffcccc", "#ffcccc") },
        { "Font-FilterError", ("red","red") },
        { "HeaderFontColor", ("white", "black") },
        { "BannerFont", ("blue", "navy") },
        { "BannerBackground", ("lightblue", "steelblue") },
        { "BannerBorder", ("blue", "navy") },
        { "BannerFont-Warning", ("white", "#eadecf") },
        { "BannerBackground-Warning", ("orange", "darkorange") },
        { "BannerBorder-Warning", ("darkorange", "black") },
        { "BannerFont-Error", ("white", "#eadecf") },
        { "BannerBackground-Error", ("red", "darkred") },
        { "BannerBorder-Error", ("darkred", "black") },
    };

    readonly static Dictionary<string, (string, string)> graphColours = new()
    {
        { "GraphGridLineColour", ("#eee", "#777") },
        { "GraphGridZeroLineColour", ("#999", "#999") },
        { "GraphPointColour", ("#00bcd6", "white") },
        { "GraphLineColour", ("#fda50f", "#49adc4") },
        { "GraphLabelColour", ("#888", "white") },
        { "GraphAxisLabelColour", ("#888", "#49adc4") }
        
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

        string textToWrite = sb.ToString();
        if (textToWrite.Contains("<canvas"))
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            StreamReader textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Models.Resources.CLEM.Chart.min.js"));
            textToWrite = textToWrite.Replace("<!-- graphscript -->", $"<script>{textStreamReader.ReadToEnd()}</script>");
            textToWrite = UpdateCSSColours(textToWrite, graphColours);
        }
        File.WriteAllText(filename, textToWrite);
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
            AddNotes(cm.Notes, (provider.SummaryStyle == HTMLSummaryStyle.Filter));

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
                AddBlockWithText($"Applies to {DisplaySummaryValueSnippet(acm.Identifier)}");
            }
        }

        provider.BuildSummary();

        var childrenToSummarise = provider.HandleChildrenInSummary();

        if (provider.WrapChildren)
        {
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
        }
         
        if (provider.ReportMemosType == DescriptiveSummaryMemoReportingType.AtBottom)
        {
            AddMemos(model.Children.OfType<Memo>());
        }

        provider.CreateSummaryClosingBlocks();

        if (provider.WrapChildren == false)
        {
            foreach (var item in childrenToSummarise)
            {
                if (item.Include)
                {
                    GetChildDescriptiveSummaries(provider, item);
                }
            }
        }
    }

    private void GetChildDescriptiveSummaries(IDescriptiveSummaryProvider provider, ChildComponentGroup componentGroup, Func<string, string> markdown2Html = null)
    {
        if (!componentGroup.SelectedModels.Any() && string.IsNullOrEmpty(componentGroup.Missing)) return;

        using (OpenBlock(componentGroup.BorderCssClass, allowIgnore: true))
        {
            if (componentGroup.Introduction != "")
            {
                AddBlockWithText(componentGroup.Introduction, "childgrouplabel");
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
                using (OpenBlock("infoBanner error clearfix"))
                {
                    AddBlockWithText(componentGroup.Missing);
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
                AddBlockWithText("Notes", memoHeadClass);
                AddBlockWithText(text, memoTextClass);
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
        string htmlTag = "";
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
        {
            htmlTag = "<br>";
        }
        return $"{htmlTag}{Environment.NewLine}{GetIndentTabs}";
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

    /// <summary>
    /// Provide a bold formatted text snippet
    /// </summary>
    /// <param name="text">Text to bold</param>
    /// <returns>Formatted snippet</returns>
    public string DisplayBold(string text)
    {
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
            return $"<b>{text}</b>";
        else
            return $"**{text}**";
    }   

    /// <summary>
    /// Provide a italic formatted text snippet
    /// </summary>
    /// <param name="text">Text to italicize</param>
    /// <returns>Formatted snippet</returns>
    public string DisplayItalic(string text)
    {
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
            return $"<i>{text}</i>";
        else
            return $"*{text}*";
    }   

    /// <summary>
    /// Provide a superscript formatted text snippet
    /// </summary>
    /// <param name="text">Text to format as superscript</param>
    /// <returns>Formatted snippet</returns>
    public string DisplaySuperScript(string text)
    {
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
            return $"<sup>{text}</sup>";
        else
            return $"^{text}^";
    }

    /// <summary>
    /// Provide a subscript formatted text snippet
    /// </summary>
    /// <param name="text">Text to format as subscript</param>
    /// <returns>Formatted snippet</returns>
    public string DisplaySubScript(string text)
    {
        if (OutputFormat == DescriptiveSummaryFormat.HTML)
            return $"<sub>{text}</sub>";
        else
            return $"~{text}~";
    }

    /// <summary>
    /// Add a formatted parameter value entry
    /// </summary>
    /// <param name="name">Name of parameter group</param>
    /// <param name="text">Formatted text to display</param>
    public void AddSummaryParameterSnippet(string name, string text)
    {
        using (OpenBlock("entryHolder clearfix"))
        {
            AddBlockWithText(text, "parameterdetails");
            AddBlockWithText(name, "parametername");
        }
    }
    private string UpdateCSSColours(string css, Dictionary<string, (string light, string dark)> dictionaryOfColours)
    {
        // for each key in colours, replace in cssText
        string updatedCss = css;
        foreach (var key in dictionaryOfColours.Keys)
        {
            var (light, dark) = dictionaryOfColours[key];
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
            openTag = "<table><thead><tr>";
            cellWrapStart = "<th>";
            cellWrapEnd = "</th>";
            endTag = "</tr></thead><tbody>";
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
                Append("</tbody></table>");
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
        sb.AppendLine(UpdateCSSColours(cssString, colours));
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
        AddBlockWithText($"{GetIndentTabs}You will need to keep refreshing this page to see changes relating to the last component selected", "detailsnote");

        using (OpenBlock("clearfix componentBanner"))
        {
            AddBlockWithText($"Component {component.GetType().Name} named {component.Name}", "entryValue labelValue floatLeft");
            AddLineBreak();
            AddBlockWithText($"Details", "entryValue labelValue floatLeft smaller");
        }

        using (OpenBlock("componentContent"))
        {
            //Model sim = (component as Model).Node.FindParent<Simulation>(relativeTo: component as Model, recurse: true);
            //AddBlockWithText(sb, "entryHolder", $"{GetIndentTabs}Simulation: {sim.Name}");
            AddBlockWithText($"{GetIndentTabs}Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}");
        }
    }

    /// <summary>
    /// Add a block with specified text
    /// </summary>
    /// <param name="text"></param>
    /// <param name="classString"></param>
    /// <param name="styleString"></param>
    /// <param name="disabled"></param>
    /// <param name="tag">HTML block type</param>
    /// <param name="addTopBottomMargin"></param>
    /// <param name="ignore"></param>
    public void AddBlockWithText(string text, string classString = "",string styleString = "", bool disabled = false, string tag = "div", bool addTopBottomMargin = true, bool ignore = false)
    {
        using (OpenBlock(classString, styleString, tag: tag, disabled: disabled))
        {
            sb.AppendLine(GetIndentTabs + text);
        }
    }

    /// <summary>
    /// Opens a block and returns an IDisposable that will close it when disposed.
    /// </summary>
    public IDisposable OpenBlock(string classString = "", string styleString = "",
        string id = "", bool disabled = false, string tag = "div", bool addTopBottomMargin = true, bool allowIgnore = false)
    {
        if (tag == "div" && string.IsNullOrWhiteSpace(classString) && string.IsNullOrWhiteSpace(styleString) && addTopBottomMargin)
        {
            if (allowIgnore)
            {
                return new BlockScope(this, ignore: true);
            }
            classString = "entryHolder";
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
            htmlStart = $"<span class=\"entryValue errorValue\">";
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
    public string DisplaySummaryValueSnippet<T>(T value, string errorString = "Not set", HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default, bool htmlTags = true, bool warnZero = false, bool errorNotSet = false, string spanClass = "")
    {
        string errorClass = "errorValue";
        switch (entryStyle)
        {
            case HTMLSummaryStyle.Default:
                break;
            case HTMLSummaryStyle.Resource:
                spanClass = "resourceValue";
                break;
            case HTMLSummaryStyle.SubResource:
                break;
            case HTMLSummaryStyle.SubResourceLevel2:
                break;
            case HTMLSummaryStyle.Activity:
                spanClass = "activityValue";
                break;
            case HTMLSummaryStyle.SubActivity:
                break;
            case HTMLSummaryStyle.SubActivityLevel2:
                break;
            case HTMLSummaryStyle.Helper:
                break;
            case HTMLSummaryStyle.FileReader:
                spanClass = "fileValue";
                break;
            case HTMLSummaryStyle.Filter:
                spanClass = "filterValue";
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
                            errorClass = "warningValue";
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
                htmlStart = $"<span class=\"entryValue {spanClass}\">";
            }
        }
        else
        {
            valueString = errorString;
            if (htmlTags)
            {
                htmlStart = $"<span class=\"entryValue {errorClass}\">";
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
        string spanClass = "resourceValue";
        string errorClass = "errorValue";
        string htmlEnd = (htmlTags) ? "</span>" : string.Empty;
        string htmlStart = (htmlTags) ? $"<span class=\"entryValue {spanClass}\">" : string.Empty;

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

    /// <summary>
    /// Get the file extension to use based on the descriptive summary format provided.
    /// </summary>
    /// <param name="format">Descriptive summary format required</param>
    /// <returns>file extension with full stop</returns>
    public static string FileExtensionToUse(DescriptiveSummaryFormat format)
    {
        switch (format)
        {
            case DescriptiveSummaryFormat.HTML:
                return ".html";
            case DescriptiveSummaryFormat.Markdown:
                return ".md";
            case DescriptiveSummaryFormat.Text:
                return ".txt";
            default:
                return ".txt";
        }
    }

}
