namespace UserInterface.Classes
{
    using APSIM.Shared.Utilities;
    using HtmlAgilityPack;
    using MigraDoc.DocumentObjectModel;
    using MigraDoc.DocumentObjectModel.Tables;
    using PdfSharp.Drawing;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;

    /// <summary>
    /// 
    /// </summary>
    public class HtmlToMigraDoc
    {
        private static bool foundCode = false;

        /// <summary>
        /// A list of table column names - used when parsing HTML tables.
        /// </summary>
        private static List<string> tableColumnNames = null;

        /// <summary>
        /// Convert the specified HTML into a MigraDoc section.
        /// </summary>
        /// <param name="html">The HTML to parse.</param>
        /// <param name="section">To section to store the elements in.</param>
        /// <param name="imagePath">Path for images.</param>
        /// <param name="relativePath">If images are provided as a relative path name, the full path name will be resolved relative to this path.</param>
        public static void Convert(string html, DocumentObject section, string imagePath, string relativePath)
        {
            if (!string.IsNullOrEmpty(html) && section != null)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                ConvertNodes(doc.DocumentNode.ChildNodes, section, imagePath, relativePath);
                AddStylesToDoc(section.Document);
            }
        }

        /// <summary>
        /// Convert the specified HTML nodes into MigraDoc elements.
        /// </summary>
        /// <param name="nodes">The HTML nodes.</param>
        /// <param name="section">The section to store the elements in.</param>
        /// <param name="imagePath">Path for images.</param>
        /// <param name="relativePath">If images are provided as a relative path name, the full path name will be resolved relative to this path.</param>
        private static void ConvertNodes(HtmlNodeCollection nodes, DocumentObject section, string imagePath, string relativePath)
        {
            foreach (var node in nodes)
            {
                DocumentObject result = ParseNode(node, section, imagePath, relativePath);
                if (node.HasChildNodes)
                    ConvertNodes(node.ChildNodes, result ?? section, imagePath, relativePath);
                if (node.Name == "table" && result is Table table)
                    FixTableSize(table);
            }
        }

        private static void FixTableSize(Table table)
        {
            XGraphics graphics = XGraphics.CreateMeasureContext(new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards);
            var fontSize = table.Document.Styles["Table"].Font.Size.Value;
            var gdiFont = new PdfSharp.Drawing.XFont("Arial", fontSize);

            for (int j = 0; j < table.Columns.Count; j++)
            {
                double columnWidth = 0;
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    foreach (Paragraph paragraph in table[i, j].Elements.OfType<Paragraph>())
                    {
                        string contents = string.Empty;
                        foreach (DocumentObject paragraphElement in paragraph.Elements)
                            if (paragraphElement is MigraDoc.DocumentObjectModel.Text)
                                contents += (paragraphElement as MigraDoc.DocumentObjectModel.Text).Content;
                            else if (paragraphElement is MigraDoc.DocumentObjectModel.Hyperlink)
                                contents += (paragraphElement as MigraDoc.DocumentObjectModel.Hyperlink).Name;
                        XSize size = graphics.MeasureString(contents, gdiFont);
                        columnWidth = Math.Max(columnWidth, size.Width);
                    }
                }
                table.Columns[j].Width = Unit.FromPoint(columnWidth) + 5;
            }
        }

        /// <summary>
        /// Look at the specified HTML node and create the necessary elements under section.
        /// </summary>
        /// <param name="node">The HTML node to examine.</param>
        /// <param name="section">The section to store the elements in.</param>
        /// <param name="imagePath">Path for images.</param>
        /// <param name="relativePath">If images are provided as a relative path name, the full path name will be resolved relative to this path.</param>
        /// <returns>The newly created MigraDoc section. Can be null.</returns>
        private static DocumentObject ParseNode(HtmlNode node, DocumentObject section, string imagePath, string relativePath)
        {
            switch (node.Name)
            {
                case "h1": return AddHeading(section, 1);
                case "h2": return AddHeading(section, 2);
                case "h3": return AddHeading(section, 3);
                case "h4": return AddHeading(section, 4);
                case "h5": return AddHeading(section, 5);
                case "h6": return AddHeading(section, 6);
                case "#text": return AddText(section, node.InnerText);
                case "p": return AddParagraph(section, node);
                case "strong": FormattedText text = AddFormattedText(section); text.Bold = true; return text;
                case "em": text = AddFormattedText(section); text.Italic = true; return text;
                case "u": text = AddFormattedText(section); text.Underline = Underline.Single; return text;
                case "i": text = AddFormattedText(section); text.Italic = true; return text;
                case "sup": text = AddFormattedText(section); text.Superscript = true; return text;
                case "sub": text = AddFormattedText(section); text.Subscript = true; return text;
                case "a": return AddHyperlink(section, node);
                case "hr": Paragraph paragraph = GetParagraph(section); paragraph.Style = "HorizontalRule"; return paragraph;
                case "br": return AddLineBreak(section);
                case "li": return AddListItem(section, node);
                case "table": return AddTable(section);
                case "th": return AddTableHeading(node, section);
                case "tr": return AddTableRow(node, section);
                case "td": return AddTableColumn(node, section);
                case "img": return AddImage(node, section, imagePath, relativePath);
                case "pre": foundCode = true; return null;
                case "code": if (!foundCode) { FormattedText txt = AddFormattedText(section); txt.FontName = "Courier New"; return txt; } else return null;
            }

            return null;
        }

        private static DocumentObject AddHyperlink(DocumentObject section, HtmlNode node)
        {
            string href = node.GetAttributeValue("href", "");
            Hyperlink link;
            href = href.Replace("&amp;", "&");
            if (href.StartsWith("#"))
                link = GetParagraph(section).AddHyperlink(href.Substring(1), HyperlinkType.Bookmark);
            else
                link = GetParagraph(section).AddHyperlink(href, HyperlinkType.Web);
            return link;
        }

        /// <summary>
        /// Add an image to the section.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="section"></param>
        /// <param name="imagePath">Path for images.</param>
        /// <param name="relativePath">If images are provided as a relative path name, the full path name will be resolved relative to this path.</param>
        /// <returns></returns>
        private static DocumentObject AddImage(HtmlNode node, DocumentObject section, string imagePath, string relativePath)
        {
            HtmlAttribute srcAttribute = node.Attributes["src"];
            if (srcAttribute != null)
            {
                string absolutePath = PathUtilities.GetAbsolutePath(srcAttribute.Value, relativePath);
                string fullPath;
                if (String.IsNullOrEmpty(imagePath))
                    fullPath = srcAttribute.Value;
                else if (File.Exists(absolutePath))
                    fullPath = absolutePath;
                else if (File.Exists(srcAttribute.Value))
                    fullPath = srcAttribute.Value;
                else
                    fullPath = GetImagePath(srcAttribute.Value, imagePath);

                if (File.Exists(fullPath))
                {
                    Paragraph para = section.Section.AddParagraph();
                    para.AddImage(fullPath);
                }
            }
            return section;
        }

        /// <summary>
        /// Copies an image stored as an embdedded resource to a given directory and returns the path to the image.
        /// </summary>
        /// <param name="imageName">Name of the image (without the path).</param>
        /// <param name="imageDirectory">Directory which the image should be copied to.</param>
        /// <returns>Full path to the image.</returns>
        public static string GetImagePath(string imageName, string imageDirectory)
        {
            string path = Path.Combine(imageDirectory, imageName);
            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                GetImageResource(imageName).CopyTo(file);
            }

            return path;
        }

        #region Table methods.
        /// <summary>
        /// Add a table to the specified section.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        private static DocumentObject AddTable(DocumentObject section)
        {
            tableColumnNames = new List<string>();
            Table table = null;
            if (section is Section)
            {
                table = (section as Section).AddTable();
                var tableEnd = (section as Section).AddParagraph();
                tableEnd.Style = "ListEnd";
            }
            else if (section is Paragraph)
                table = (section as Paragraph).Section.AddTable();

            if (table != null)
            {
                table.Borders.Visible = true;
                table.Rows.LeftIndent = "0cm";
            }
            return table;
        }

        /// <summary>
        /// Add a table heading to the specified section.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private static DocumentObject AddTableHeading(HtmlNode node, DocumentObject section)
        {
            Table table = (section as Table);

            // Add all columns.
            string innerText = node.InnerText.Replace("\r", string.Empty);
            innerText = innerText.Replace("\n", string.Empty);
            innerText = innerText.Replace("\t", string.Empty);

            tableColumnNames.Add(innerText);
            table.AddColumn();

            return table;
        }

        /// <summary>
        /// Add a table row to the specified section.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private static DocumentObject AddTableRow(HtmlNode node, DocumentObject section)
        {
            Table table = null;
            if (section is Table)
                table = (section as Table);
            else if (section is Row)
                table = (section as Row).Table;

            if (tableColumnNames.Count > 0)
            {
                Row row = table.AddRow();
                row.HeadingFormat = true;
                row.Shading.Color = Colors.LightBlue;
                row.Format.Alignment = ParagraphAlignment.Left;
                row.VerticalAlignment = VerticalAlignment.Center;
                row.Format.Font.Bold = true;
                for (int i = 0; i < tableColumnNames.Count; i++)
                {
                    Paragraph tableText = row.Cells[i].AddParagraph(tableColumnNames[i]);
                    tableText.Style = "TableText";
                }
                tableColumnNames.Clear();
            }
            else if (node.ParentNode.Name == "thead")
                return section;

            Row newRow = table.AddRow();
            newRow.VerticalAlignment = VerticalAlignment.Center;
            return newRow;
        }

        /// <summary>
        /// Add a table column to the specified section.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private static DocumentObject AddTableColumn(HtmlNode node, DocumentObject section)
        {
            Row row = (section as Row);
            int index = 0;
            foreach (HtmlNode sibling in node.ParentNode.ChildNodes)
            {
                if (sibling == node)
                {
                    // If there is whitespace at the end of the row, sometimes the HTML parser
                    // will consider that whitespace to be an empty cell. This would normally
                    // cause an argument out of range exception due to the row having more cells
                    // than the table has columns. In such a scenario, we simply ignore the extra
                    // cell.
                    if (row.Table.Columns.Count > index)
                    {
                        string text = node.InnerText == "&nbsp;" ? "" : node.InnerText;
                        Paragraph tableText = row.Cells[index].AddParagraph(text);
                        if (node.Attributes.Contains("align"))
                        {
                            string alignment = node.Attributes["align"].Value;
                            if (String.Compare(alignment, "right", true) == 0)
                                tableText.Format.Alignment = ParagraphAlignment.Right;
                            else if (String.Compare(alignment, "center", true) == 0)
                                tableText.Format.Alignment = ParagraphAlignment.Center;
                        }
                        tableText.Style = "TableText";
                    }
                    return section;
                }
                else if (sibling.Name == "td")
                    index++;
            }
            return section;
        }

        #endregion

        /// <summary>
        /// Fetches an image from a resource, using a case-insensitive search.
        /// </summary>
        /// <param name="imageName">Name of the iamge.</param>
        /// <returns></returns>
        private static Stream GetImageResource(string imageName)
        {
            string imagePath = "ApsimNG.Resources." + imageName;
            foreach (string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                if (string.Equals(imagePath, resourceName, StringComparison.InvariantCultureIgnoreCase))
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            throw new Exception("Unable to locate resource: " + imageName);
        }

        /// <summary>
        /// Add a paragraph.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private static DocumentObject AddParagraph(DocumentObject section, HtmlNode node)
        {
            Paragraph newParagraph = null;
            int quoteLevel = 0;
            bool isListItem = false;
            HtmlNode testNode = node.ParentNode;
            string listStyle = "";
            while (testNode != null)
            {
                if (testNode.Name == "blockquote")
                    quoteLevel++;
                if (testNode.Name == "li")
                {
                    isListItem = true;
                    listStyle = testNode.ParentNode.Name == "ul" ? "UnorderedList" : "OrderedList";
                }
                testNode = testNode.ParentNode;
            }

            if (isListItem)
            {
                Paragraph paragraph = GetParagraph(section);
                bool isFirst = node.ParentNode.Elements("p").First() == node;
                if (!isFirst)
                    paragraph.AddLineBreak();
                return paragraph;
            }

            if (section is Section)
                newParagraph = (section as Section).AddParagraph();
            else if (section is Paragraph)
                newParagraph = (section as Paragraph).Section.AddParagraph();
            else if (section is Cell)
                newParagraph = (section as Cell).AddParagraph();

            if (quoteLevel > 0)
            {
                newParagraph.Format.LeftIndent = Unit.FromCentimeter(1.0 * quoteLevel);
                newParagraph.Format.RightIndent = Unit.FromCentimeter(1.0 * quoteLevel);
            }
            return newParagraph;
        }

        /// <summary>
        /// Add a list item.
        /// </summary>
        /// <param name="parentObject">The MigraDoc object to add it to.</param>
        /// <param name="node">The HTML node.</param>
        /// <returns>The newly created list item.</returns>
        private static DocumentObject AddListItem(DocumentObject parentObject, HtmlNode node)
        {
            string listStyle = node.ParentNode.Name == "ul" ? "UnorderedList" : "OrderedList";

            Section section;
            if (parentObject is Section)
                section = (Section)parentObject;
            else
                section = (Section)parentObject.Section;

            bool isFirst = node.ParentNode.Elements("li").First() == node;
            bool isLast = node.ParentNode.Elements("li").Last() == node;

            Paragraph listItem = section.AddParagraph();
            listItem.Style = listStyle;

            // disable continuation if this is the first list item
            listItem.Format.ListInfo.ContinuePreviousList = !isFirst;

            if (isLast)
            {
                var listEnd = section.AddParagraph();
                listEnd.Style = "ListEnd";
            }
            return listItem;
        }

        /// <summary>
        /// Add a line break to MigraDoc object.
        /// </summary>
        /// <param name="parentObject">MigraDoc object</param>
        /// <returns>The paragraph or FormattedText.</returns>
        private static DocumentObject AddLineBreak(DocumentObject parentObject)
        {
            if (parentObject is FormattedText)
            {
                (parentObject as FormattedText).AddLineBreak();
                return parentObject as FormattedText;
            }

            Paragraph paragraph = GetParagraph(parentObject);
            paragraph.AddLineBreak();
            return paragraph;
        }

        /// <summary>
        /// Add a heading
        /// </summary>
        /// <param name="parentObject">The MigraDoc object to store the heading in.</param>
        /// <param name="headingNumber">The heading number.</param>
        /// <returns>The newly created paragraph.</returns>
        private static DocumentObject AddHeading(DocumentObject parentObject, int headingNumber)
        {
            Paragraph paragraph = null;
            if (parentObject is Section)
                paragraph = (parentObject as Section).AddParagraph();
            else if (parentObject is Paragraph)
                paragraph = (parentObject as Paragraph).Section.AddParagraph();
            if (paragraph != null)
            {
                paragraph.Style = "Heading" + headingNumber;
            }
            return paragraph;
        }

        /// <summary>
        /// Add text to the specified section.
        /// </summary>
        /// <param name="parentObject">The MigraDoc object to store the text in.</param>
        /// <param name="text">The text to add.</param>
        /// <returns>The </returns>
        private static DocumentObject AddText(DocumentObject parentObject, string text)
        {
            if (foundCode)
            {
                if (parentObject is Section)
                {
                    AddCodeBlock(parentObject as Section, text);
                    var tableEnd = (parentObject as Section).AddParagraph();
                    tableEnd.Style = "ListEnd";
                }
                else if (parentObject is Paragraph)
                {
                    AddCodeBlock((parentObject as Paragraph).Section, text);
                }
                foundCode = false;
                return null;
            }

            // remove line breaks
            var innerText = text.Replace("\r", " ").Replace("\n", " ");

            if (string.IsNullOrWhiteSpace(innerText))
                return parentObject;

            // decode escaped HTML
            innerText = WebUtility.HtmlDecode(innerText);

            // text elements must be wrapped in a paragraph but this could also be FormattedText or a Hyperlink
            if (parentObject is FormattedText)
                return (parentObject as FormattedText).AddText(innerText);
            else if (parentObject is Hyperlink)
            {
                (parentObject as Hyperlink).AddText(innerText);
                return parentObject.Section.LastParagraph;
            }
            else if (parentObject is Paragraph)
                return (parentObject as Paragraph).AddText(innerText);
            else if (parentObject is Section)
                return (parentObject as Section).AddParagraph(innerText);
            else if (parentObject is Cell)
                return (parentObject as Cell).AddParagraph(innerText);
            else
                return null;
        }

        /// <summary>
        /// Add formatted text to the MigraDoc object.
        /// </summary>
        /// <param name="parentObject">The MigraDoc object to store the elements in.</param>
        /// <returns>The formatted text object.</returns>
        private static FormattedText AddFormattedText(DocumentObject parentObject)
        {
            FormattedText formattedText = parentObject as FormattedText;
            if (formattedText == null)
                return GetParagraph(parentObject).AddFormattedText();
            else
                return formattedText.AddFormattedText();
        }

        /// <summary>
        /// If parentObject is a paragraph, return it, otherwise create a new paragraph and return it.
        /// </summary>
        /// <param name="parentObject">The MigraDoc object.</param>
        /// <returns>Always a paragraph. Never null.</returns>
        private static Paragraph GetParagraph(DocumentObject parentObject)
        {
            if (parentObject is Paragraph)
                return parentObject as Paragraph;
            else if (parentObject is Section)
                return (parentObject as Section).AddParagraph();
            else if (parentObject is Cell)
                return (parentObject as Cell).AddParagraph();
            else
                return parentObject.Section.AddParagraph();
        }

        /// <summary>
        /// Add MigraDoc styles.
        /// </summary>
        /// <param name="doc">The document to add the styles to.</param>
        private static void AddStylesToDoc(MigraDoc.DocumentObjectModel.Document doc)
        {
            var body = doc.Styles["Normal"];
            body.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(10);

            if (doc.Styles["TableText"] == null)
            {
                var tableTextStyle = doc.AddStyle("TableText", "Normal");
                // tableTextStyle.ParagraphFormat.LineSpacing = 0;
                tableTextStyle.ParagraphFormat.SpaceAfter = 5;
            }

            body.ParagraphFormat.LineSpacingRule = MigraDoc.DocumentObjectModel.LineSpacingRule.Multiple;
            body.ParagraphFormat.LineSpacing = 1.0;
            body.ParagraphFormat.SpaceAfter = 10;

            var footer = doc.Styles["Footer"];
            footer.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(9);

            var h1 = doc.Styles["Heading1"];
            h1.Font.Bold = true;
            h1.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(15);
            h1.ParagraphFormat.SpaceBefore = 0;

            var h2 = doc.Styles["Heading2"];
            h2.Font.Bold = true;
            h2.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(13);
            h2.ParagraphFormat.SpaceBefore = 0;

            var h3 = doc.Styles["Heading3"];
            h3.Font.Bold = true;
            h3.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(11);
            h3.ParagraphFormat.SpaceBefore = 0;

            var h4 = doc.Styles["Heading4"];
            h4.Font.Bold = true;
            h4.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(10);
            h4.ParagraphFormat.SpaceBefore = 0;

            var h5 = doc.Styles["Heading5"];
            h5.Font.Bold = true;
            h5.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(9);
            h5.ParagraphFormat.SpaceBefore = 0;

            var h6 = doc.Styles["Heading6"];
            h6.Font.Bold = true;
            h6.Font.Size = MigraDoc.DocumentObjectModel.Unit.FromPoint(9);
            h6.ParagraphFormat.SpaceBefore = 0;


            var links = doc.Styles["Hyperlink"];
            links.Font.Color = MigraDoc.DocumentObjectModel.Colors.Blue;

            if (doc.Styles["ListEnd"] == null)
            {
                var unorderedlist = doc.AddStyle("ListEnd", "Normal");
                unorderedlist.ParagraphFormat.SpaceAfter = 0;
            }

            if (doc.Styles["UnorderedList"] == null)
            {
                var unorderedlist = doc.AddStyle("UnorderedList", "Normal");
                var listInfo = new MigraDoc.DocumentObjectModel.ListInfo();
                listInfo.ListType = MigraDoc.DocumentObjectModel.ListType.BulletList1;
                unorderedlist.ParagraphFormat.ListInfo = listInfo;
                unorderedlist.ParagraphFormat.LeftIndent = "1cm";
                unorderedlist.ParagraphFormat.FirstLineIndent = "-0.5cm";
                unorderedlist.ParagraphFormat.SpaceAfter = 0;
            }

            if (doc.Styles["OrderedList"] == null)
            {
                var orderedlist = doc.AddStyle("OrderedList", "UnorderedList");
                orderedlist.ParagraphFormat.ListInfo.ListType = MigraDoc.DocumentObjectModel.ListType.NumberList1;
                orderedlist.ParagraphFormat.LeftIndent = "1cm";
                orderedlist.ParagraphFormat.FirstLineIndent = "-0.5cm";
                orderedlist.ParagraphFormat.SpaceAfter = 0;
            }

            if (doc.Styles["ListEnd"] == null)
            {
                var unorderedlist = doc.AddStyle("ListEnd", "Normal");
                unorderedlist.ParagraphFormat.SpaceAfter = 0;
            }

            if (doc.Styles["HorizontalRule"] == null)
            { 
                var hr = doc.AddStyle("HorizontalRule", "Normal");
                var border = new MigraDoc.DocumentObjectModel.Border();
                border.Width = "1pt";
                border.Color = MigraDoc.DocumentObjectModel.Colors.DarkGray;
                hr.ParagraphFormat.Borders.Bottom = border;
                hr.ParagraphFormat.LineSpacing = 0;
                hr.ParagraphFormat.SpaceBefore = 15;
            }
            if (doc.Styles["TableParagraph"] == null)
            {
                var style = doc.Styles.AddStyle("TableParagraph", "Normal");
                style.Font.Size = 8;
                style.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0);
                style.Font = new MigraDoc.DocumentObjectModel.Font("Courier New");
            }
        }

        /// <summary>
        /// Add a text frame.
        /// </summary>
        /// <param name="section">Section to which the text will be added.</param>
        /// <param name="text">Text to be added.</param>
        private static void AddCodeBlock(Section section, string text)
        {
            Table table = section.AddTable();
            table.Borders.Width = "0.5pt";
            table.Borders.Color = MigraDoc.DocumentObjectModel.Colors.DarkGray;
            table.LeftPadding = "5mm";
            table.Rows.LeftIndent = "0cm";
            
            var column = table.AddColumn();
            column.Width = Unit.FromMillimeter(180);

            Row row = table.AddRow();
            Paragraph p = row[0].AddParagraph();
            p.Style = "TableParagraph";

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string spacedLine = line.Replace(' ', (char)0xa0); // Turn spaces into non-breaking spaces
                
                p = row[0].AddParagraph();
                p.AddText(WebUtility.HtmlDecode(spacedLine));
                p.Style = "TableParagraph";
            }
        }
    }
}