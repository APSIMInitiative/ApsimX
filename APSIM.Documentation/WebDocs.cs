using System.Collections.Generic;
using Models.Core;
using System.IO;
using APSIM.Shared.Documentation;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data;
using System;
using Markdig;
using System.Reflection;
using APSIM.Shared.Utilities;
using APSIM.Documentation.Models;
using APSIM.Documentation.Bibliography;
using Models.Core.ApsimFile;
using APSIM.Shared.Mapping;
using SkiaSharp;
using APSIM.Documentation.Graphing;

namespace APSIM.Documentation
{
    /// <summary>
    /// A pdf file which is built from a model.
    /// </summary>
    public static class WebDocs
    {
        /// <summary>
        /// Gets a html page as a string generated from an apsimx file.
        /// </summary>
        public static string GetPage(string apsimDirectory, string name)
        {
            if(name == "CLEM Documentation site")
                return "";
            
            string[] validations = GetValidationFolderNames(apsimDirectory);
            string[] tutorials = GetTutorialFileNames(apsimDirectory);

            string filename = name;
            if (filename == "AGPRyegrass" || filename == "AGPWhiteClover")
            {
                filename = "AgPasture";
                name = "AgPasture";
            }

            if(filename == "Lifecycle")
            {
                filename = name.ToLower();
            }

            bool isValidation = false;
            bool isTutorial = false;

            if (validations.Contains(filename))
                isValidation = true;

            if (tutorials.Contains(filename))
                isTutorial = true;

            if(filename == "SorghumDCaPST")
            {
                name = "DCaPST/Sorghum";
                isValidation = true;
            }

            filename += ".apsimx";
            
            string path = apsimDirectory;
            if (isValidation)
                path += "/Tests/Validation/" + name + "/" + filename;
            else if (isTutorial && filename == "lifecycle.apsimx")
                path += "/Examples/Tutorials/Lifecycle/" + filename;
            else if (isTutorial && filename =="CLEM_Example_Cropping.apsimx")
                path += "/Examples/CLEM/" + filename;     
            else if (isTutorial && filename =="CLEM_Example_Grazing.apsimx")
                path += "/Examples/CLEM/" + filename;         
            else if (isTutorial)
                path += "/Examples/Tutorials/" + filename;
            else
                throw new Exception($"Provided name \"{name}\", does not match any validation folders or tutorial files.");

            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, e => throw e, false, compileManagerScripts: false).NewModel as Simulations;
            return GenerateWeb(sims);
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetCSS()
        {
            return ReflectionUtilities.GetResourceAsString("APSIM.Documentation.Resources.docs.css");
        }

        /// <summary>
        /// Generate the auto-documentation at the given output path.
        /// </summary>
        /// <param name="model">Path to which the file will be generated.</param>
        public static string Generate(IModel model)
        {
            string html = GenerateWeb(model);
            html = AddBoilerplate(model.Name + " Documentation", GetCSS(), html);
            return html;
        }

        /// <summary>
        /// Generate the auto-documentation at the given output path.
        /// </summary>
        /// <param name="model">Path to which the file will be generated.</param>
        public static string GenerateWeb(IModel model)
        {
            return TagsToHTMLString(AutoDocumentation.Document(model));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tags">Tags to be converted</param>
        public static string TagsToHTMLString(List<ITag> tags)
        {
            tags.Add(new Section("References"));
            string markdown = ConvertToMarkdown(tags, "");
            string headerImg = ConvertToMarkdown(new List<ITag>(){AddHeaderImageTag()},"");
            markdown = headerImg + markdown;
            List<(string, string)> htmlSegments = GetAllHTMLSegments(markdown, out string output1);
            List<ICitation> citations = ProcessCitations(output1, out string output2);
            if (citations.Count > 0)
            {
                output2 += WriteBibliography(citations);
            }
            else
            {
                int lastHash =  output2.LastIndexOf("#");
                output2 = output2.Substring(0, lastHash);
                output2 = output2.Replace("<a href=\"#references\"><div class=\"docs-nav\">References</div></a>\n", "");
            }

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(output2, pipeline);
            html = RestoreHTMLSegments(html, htmlSegments);
            html = AddTableWrappers(html);
            html = AddCSSClasses(html);
            html = AddContentWrapper(GetNavigationHTML(tags), html); 
            

            return html;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Image AddHeaderImageTag() 
        {
            return new Image("AIBanner.png");
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetNavigationHTML(List<ITag> tags)
        {

            string html = "<div class=\"docs-navcontainer\">\n";
            html += "<div class=\"docs-navbar\">\n";
            foreach(ITag tag in tags)
            {
                if (tag is Section section)
                {
                    string id = section.Title.ToLower().Replace(" ", "-");
                    html += $"<a href=\"#{id}\"><div class=\"docs-nav\">{section.Title}</div></a>\n";
                }
            }
            html += "</div>\n";
            html += "</div>\n";
            return html;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceName">Resource file name.</param>
        public static SkiaSharp.SKImage LoadFromResource(string resourceName)
        {
            using (Stream stream = GetStreamFromResource(resourceName))
                return SkiaSharp.SKImage.FromEncodedData(stream);
        }

        /// <summary>
        /// Loads an image stream from the given resource name
        /// Will attempt to locate the resource in various assemblies.
        /// </summary>
        /// <param name="resourceName">Name of the resource</param>
        /// <returns>The resource as a stream</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static Stream GetStreamFromResource(string resourceName)
        {
            foreach (Assembly assembly in GetAssemblies())
            {
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                    return stream;
                string fullName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.Contains(resourceName));
                if (fullName != null)
                    return assembly.GetManifestResourceStream(fullName);
            }
            throw new FileNotFoundException($"Unable to load image from resource name '{resourceName}': resource not found");
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            return new string[]
            {
                "APSIM.Interop",
                "ApsimNG",
                "Models",
                "APSIM.Shared",
            }.Select(GetAssembly)
             .Where(a => a != null);
        }

        private static Assembly GetAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        }

        /// <summary>
        /// 
        /// </summary>
        public static string ConvertToMarkdown(List<ITag> tags, string heading) 
        {
            string output = "";

            string header = "#" + heading;
            int sectionCount = 0;
            string headingPrefix = ".";
            if (header.Length == 1)
                headingPrefix = " ";

            foreach(ITag tag in tags)
            {
                if (tag is Section section)
                {
                    sectionCount += 1;
                    string sectionHeader = $"{header}{headingPrefix}{sectionCount}";
                    output += $"{sectionHeader} {section.Title}\n\n";
                    output += ConvertToMarkdown(section.Children, sectionHeader);
                }
                else if (tag is Paragraph paragraph) 
                {

                    List<string> lines = paragraph.text.Split("\n").ToList();
                    lines = ConvertMarkdownCode(lines);
                    foreach (string line in lines)
                    {
                        string text = line.Trim();
                        if (text.StartsWith('#'))
                        {
                            string hashes = "#";
                            foreach(char c in header)
                                if (c == '#')
                                    hashes += "#";
                            text = hashes + text.Replace("#", "");
                        }
                        text = ReplaceImagePathWithEncodedString(text);
                        output += $"{text}\n";
                    }
                    output += "\n";
                }
                else if (tag is Table table) 
                {
                    List<int> columnWidths = new List<int>();
                    foreach(DataColumn col in table.data.Table.Columns)
                    {
                        int maxLength = col.ColumnName.Length;
                        foreach(DataRow row in table.data.Table.Rows)
                            if (row[col].ToString().Length > maxLength)
                                maxLength = row[col].ToString().Replace("\n", "").Replace("\r", "").Length;
                        columnWidths.Add(maxLength);
                    }

                    int colCount = table.data.Table.Columns.Count;
                    string line1 = "|";
                    string line2 = "|";
                    for(int i = 0; i < colCount; i++)
                    {
                        string content = table.data.Table.Columns[i].ColumnName;
                        line1 += $" {content}";
                        for(int k = 0; k < columnWidths[i]-content.Length; k++)
                            line1 += " ";
                        line1 += " |";

                        line2 += " ";
                        for(int k = 0; k < columnWidths[i]; k++)
                            line2 += "-";
                        line2 += " |";                        
                    }
                    output += line1 + "\n" + line2 + "\n";

                    int rowCount = table.data.Table.Rows.Count;
                    for(int i = 0; i < rowCount; i++)
                    {
                        string line = "|";
                        DataRow row = table.data.Table.Rows[i];
                        for(int j = 0; j < colCount; j++)
                        {
                            DataColumn col = table.data.Table.Columns[j];
                            string content = row[col].ToString().Replace("\n", "").Replace("\r", "");
                            line += $" {content}";
                            for(int k = 0; k < columnWidths[j]-content.Length; k++)
                                line += " ";
                            line += " |";
                        }
                        output += line + "\n";
                    }
                    output += "\n";
                }
                else if (tag is Image img)
                {
                    string imgMarkdown = GetMarkdownImageFromSKImage(img.GetRaster());
                    output += imgMarkdown;
                }
                else if (tag is Map map)
                {
                    SKImage mapImage = map.ToImage(800);
                    string imgMarkdown = GetMarkdownImageFromSKImage(mapImage);
                    output += imgMarkdown;
                }
                else if (tag is Video video)
                {
                    output += $"![Video]({video.Source})\n";
                }
                else if (tag is Graph graph)
                {
                    SKImage graphImage = GetGraphImage(graph);
                    string imgMarkdown = GetMarkdownImageFromSKImage(graphImage);
                    output += imgMarkdown;
                }
            }
            return output;
        }

        /// <summary>
        /// Returns a list of all citations found, and replaces the text of the citation with the reference
        /// </summary>
        public static List<ICitation> ProcessCitations(string input, out string output) 
        {

            Regex regex = new Regex(@"\[\w+\]");
            MatchCollection matches = regex.Matches(input);

            output = input;
            List<ICitation> citations = new List<ICitation>();
            List<string> citesFound = new List<string>();

            foreach(Match match in matches)
            {
                string value = match.Value;
                string cleanedValue = value.Replace("[", "").Replace("]", "");
                if (!citesFound.Contains(cleanedValue))
                {
                    citesFound.Add(cleanedValue);
                    ICitation citation = AutoDocumentation.Bibilography.Lookup(cleanedValue);
                    if (citation != null)
                    {
                        citations.Add(citation);
                        output = output.Replace(value, citation.InTextCite);
                    }
                }
            }
            
            return citations;
        }

        /// <summary>
        /// Add a bibliography to the document.
        /// </summary>
        public static string WriteBibliography(List<ICitation> citations)
        {
            string output = "";

            // Ensure references in bibliography are sorted alphabetically
            // by their full text.
            IEnumerable<ICitation> sorted = citations.OrderBy(c => c.BibliographyText);

            foreach (ICitation citation in sorted)
            {

                // If a URL is provided for this citation, insert the citation
                // as a hyperlink.
                bool isLink = !string.IsNullOrEmpty(citation.URL);
                if (isLink) 
                {
                    output += $"[{citation.BibliographyText}]({citation.URL})\n\n";
                }
                else
                {
                    output += $"{citation.BibliographyText}\n\n";
                }
            }

            return output;
        }

        /// <summary>
        /// Replaces the image path in any md with a base64 encoded image string.
        /// </summary>
        public static string ReplaceImagePathWithEncodedString(string markdown)
        {
            string output = markdown;

            Regex regex = new Regex(@"\!\[(.*)\]\((.*)\)");
            MatchCollection matches = regex.Matches(output);

            Regex linkRegex = new(@"(\(http){+}");
            MatchCollection linkMatches = linkRegex.Matches(output);

            if (linkMatches.Count > 0)
                return output;
            
            foreach(Match match in matches)
            {
                string caption = match.Groups[1].ToString();
                string filename = match.Groups[2].ToString();

                SKImage headerImg = LoadFromResource(filename);
                string replacement = GetMarkdownImageFromSKImage(headerImg);
                output = output.Replace(match.Value, replacement);
            }
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        public static List<(string, string)> GetAllHTMLSegments(string markdown, out string output)
        {
            output = markdown;

            List<(string, string)> htmlSegments = new List<(string, string)>();
            int counter = 0;

            Regex regex = new Regex(@"\<([\w\W][^\<]*)\>");
            MatchCollection matches = regex.Matches(output);

            foreach(Match match in matches)
            {
                counter += 1;
                string value = match.Value;
                string tag = "[HTML_TAG_" + counter.ToString() + "]";
                htmlSegments.Add((tag, value));
                output = output.Replace(value, tag);
            }
            return htmlSegments;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string RestoreHTMLSegments(string markdown, List<(string, string)> segments)
        {
            string output = markdown;
            foreach((string, string) segment in segments)
                output = output.Replace(segment.Item1, segment.Item2);

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string AddCSSClasses(string html)
        {
            string output = html;
            output = output.Replace("<h1 ", "<h1 class=\"docs-h1\" ");
            output = output.Replace("<h2 ", "<h2 class=\"docs-h2\" ");
            output = output.Replace("<img ", "<img class=\"docs-img\" ");
            output = output.Replace("<table>", "<table class=\"docs-table\" ");
            output = output.Replace("<th>", "<th class=\"docs-th\"> ");
            output = output.Replace("<td>", "<td class=\"docs-td\"> ");
            output = output.Replace("<tr>", "<tr class=\"docs-tr\"> ");        
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string AddContentWrapper(string navigation, string content)
        {
            string output = "";
            output += "<div class=\"docs-wrapper\">\n";
            output += navigation;
            output += "<div class=\"docs-content\">\n";
            output += content;
            output += "</div>\n";
            output += "</div>\n";
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string AddTableWrappers(string html)
        {
            string output = html;
            output = output.Replace("<table>", "<div class=\"docs-table-container\">\n<table>");
            output = output.Replace("</table>", "</table>\n</div>");
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string AddBoilerplate(string title, string css, string content)
        {
            string output = "";
            output += "<!DOCTYPE html>\n";
            output += "<html lang=\"en\">\n";
            output += "<head>\n";
            output += "<meta charset=\"UTF-8\">\n";
            output += "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n";
            output += "<title>"+title+"</title>\n";
            output += "<link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">\n";
            output += "<link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>\n";
            output += "<link href=\"https://fonts.googleapis.com/css2?family=Montserrat:ital,wght@0,100..900;1,100..900&display=swap\" rel=\"stylesheet\">\n";
            output += "<style>\n";
            output += "body { background-color: #004d47; }\n";
            output += css;
            output += "</style>\n";
            output += "</head>\n";
            output += "<body>\n";
            output += content;
            output += "</body>\n";
            output += "</html>\n";
            return output;
        }

        /// <summary>
        /// Converts a SKImage to an Markdown image string.
        /// </summary>
        /// <param name="skimage"> A <see cref="SKImage"/> object</param>
        /// <returns></returns>
        public static string GetMarkdownImageFromSKImage(SKImage skimage)
        {
            var bytes = skimage.Encode(SKEncodedImageFormat.Png, 100);
            string base64String = Convert.ToBase64String(bytes.ToArray());
            string replacement = $"![](data:image/png;base64,{base64String})\n\n";
            return replacement;
        }

        /// <summary>
        /// Converts code in Memo(Paragraph ITags) to markdown format.
        /// </summary>
        /// <returns></returns>
        public static List<string> ConvertMarkdownCode(List<string> paraLines)
        {
            // Get consecutive lines that start with triple tabs.
            bool inCodeBlock = false;
            List<string> formattedLines = new();
            string pdfCodeLine = @"(\t{3})(.*)";

            foreach (string line in paraLines)
            {
                if(Regex.IsMatch(line, pdfCodeLine))
                {
                    if(!inCodeBlock)
                    {
                        formattedLines.Add("");
                        formattedLines.Add("```");
                        inCodeBlock = true;
                    }
                    formattedLines.Add(line);
                }
                else
                {
                    if(inCodeBlock)
                    {
                        formattedLines.Add("```");
                        formattedLines.Add("");
                        inCodeBlock = false;
                    }
                    formattedLines.Add(line);
                }
            }
            return formattedLines.ToList();
        }


        /// <summary>
        /// Gets an array of the folder names in the validation folder.
        /// </summary>
        /// <param name="apsimDirectory"></param>
        /// <returns></returns>
        private static string[] GetValidationFolderNames(string apsimDirectory)
        {
            string validationPath = apsimDirectory + "/Tests/Validation/";
            string[] validations = Directory.GetDirectories(apsimDirectory + "/Tests/Validation/");
            string[] dcapstValidations = Directory.GetDirectories(apsimDirectory + "/Tests/Validation/DCaPST");

            validations.Concat(dcapstValidations).ToArray();

            for(int i = 0;i < validations.Length; i++)
                validations[i] = validations[i].Replace(validationPath, "").Replace(".apsimx", "");
            return validations;
        }

        /// <summary>
        /// Get tutorial file names from the tutorials folder.
        /// </summary>
        /// <param name="apsimDirectory"></param>
        /// <returns></returns>
        private static string[] GetTutorialFileNames(string apsimDirectory)
        {
            string examplesPath = apsimDirectory + "/Examples/";
            string tutorialPath = "Tutorials/";
            string lifecyclePath = "Lifecycle/";
            string clemPath = "CLEM/";
            string[] tutorials = Directory.GetFiles(apsimDirectory + "/Examples/Tutorials/", "*.apsimx", SearchOption.AllDirectories);
            string[] clemTutorials = Directory.GetFiles(apsimDirectory + "/Examples/CLEM/", "*.apsimx", SearchOption.AllDirectories);
            
            tutorials = tutorials.Concat(clemTutorials).ToArray();

            for(int i = 0;i < tutorials.Length; i++)
                tutorials[i] = tutorials[i].Replace(examplesPath, "").Replace(".apsimx", "").
                Replace(tutorialPath,"").Replace("\\","/").
                Replace(lifecyclePath,"").Replace(clemPath,"");

            return tutorials;
        }

        /// <summary>
        /// Get an image from a graph tag
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        private static SKImage GetGraphImage(Graph graph)
        {
            GraphExporter exporter = new GraphExporter();

            var plot = exporter.ToPlotModel(graph);

            // Temp hack - set marker size to 5. We need to review
            // appropriate sizing for graphs in autodocs.
            if (plot is OxyPlot.PlotModel model)
                foreach (var series in model.Series.OfType<OxyPlot.Series.LineSeries>())
                    series.MarkerSize = 5;

            return exporter.Export(plot, 800, 600);
        }

    }
}
