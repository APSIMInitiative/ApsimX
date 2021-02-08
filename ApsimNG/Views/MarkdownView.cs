using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Gdk;
using Gtk;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UserInterface.Extensions;
using Utility;
using Pango;
using UserInterface.Classes;
using Table = Markdig.Extensions.Tables.Table;
#if NETCOREAPP
using StateType = Gtk.StateFlags;
#endif

namespace UserInterface.Views
{
    /// <summary>An interface for a rich text widget.</summary>
    public interface IMarkdownView
    {
        /// <summary>Gets or sets the base path that images should be relative to.</summary>
        string ImagePath { get; set; }

        /// <summary>Gets or sets the markdown text</summary>
        string Text { get; set; }

        /// <summary>Gets or sets the visibility of the widget.</summary>
        bool Visible { get; set; }
    }

    /// <summary>A rich text view capable of rendering markdown-formatted text.</summary>
    public class MarkdownView : ViewBase, IMarkdownView
    {
        /// <summary>
        /// Padding between table columns, in pixels.
        /// </summary>
        /// <remarks>
        /// If pixels turns out to be a bad idea, this could be
        /// refactored to be in pango units.
        /// </remarks>
        private const int tableColumnPadding = 25;

        /// <summary>
        /// Indent size (for quotes/code blocks/etc) in pixels.
        /// </summary>
        private const int indentSize = 30;

        private TextView textView;
        private VBox container;
        private Cursor handCursor;
		private Cursor regularCursor;
        private MarkdownFindView findView;

        /// <summary>Constructor</summary>
        public MarkdownView() { }

        /// <summary>Constructor</summary>
        public MarkdownView(ViewBase owner) : base(owner)
        {
            VBox box = new VBox();
            TextView child = new TextView();
            box.PackStart(child, true, true, 0);
            Initialise(owner, box);
        }

        /// <summary>Constructor</summary>
        public MarkdownView(ViewBase owner, TextView e) : base(owner)
        {
            Initialise(owner, e);
        }

        public void SetMainWidget(Widget widget)
        {
            mainWidget = widget;
        }

        /// <summary>Initialise widget.</summary>
        /// <param name="ownerView">The owner of the widget.</param>
        /// <param name="gtkControl">The raw gtk control.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            base.Initialise(ownerView, gtkControl);
            container = (VBox)gtkControl;
            textView = (TextView)container.Children[0];
            textView.PopulatePopup += OnPopulatePopupMenu;
            findView = new MarkdownFindView();

            textView.Editable = false;
            textView.WrapMode = Gtk.WrapMode.Word;
            textView.VisibilityNotifyEvent += OnVisibilityNotify;
            textView.MotionNotifyEvent += OnMotionNotify;
            textView.WidgetEventAfter += OnWidgetEventAfter;
            CreateStyles(textView);
            mainWidget = container;
            container.ShowAll();
            mainWidget.Destroyed += OnDestroyed;

            handCursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
            regularCursor = new Gdk.Cursor(Gdk.CursorType.Xterm);
            
            textView.KeyPressEvent += OnTextViewKeyPress;
        }

        /// <summary>
        /// Trap keypress events - show the text search dialog on ctrl + f.
        /// </summary>
        /// <param name="sender">Sender widget.</param>
        /// <param name="args">Event arguments.</param>
        private void OnTextViewKeyPress(object sender, KeyPressEventArgs args)
        {
            try
            {
                if ( (args.Event.Key == Gdk.Key.F || args.Event.Key == Gdk.Key.f) && args.Event.State == ModifierType.ControlMask)
                    findView.ShowFor(this);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        [GLib.ConnectBefore]
        private void OnPopulatePopupMenu(object o, PopulatePopupArgs args)
        {
            try
            {
                MenuItem option = new MenuItem("Find Text");
                option.ShowAll();
                option.Activated += OnFindText;
#if NETFRAMEWORK
                args.Menu.Append(option);
#else
                if (args.Popup is Menu menu)
                    menu.Append(option);
#endif
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnFindText(object sender, EventArgs e)
        {
            try
            {
                findView.ShowFor(this);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Gets or sets the visibility of the widget.</summary>
        public bool Visible
        {
            get { return textView.Visible; }
            set { textView.Visible = value; }
        }

        /// <summary>Gets or sets the markdown text.</summary>
        public string Text
        {
            get
            {
                return textView.Buffer.Text;
            }
            set
            {
                // Only keep first child.
                while (container.Children.Length > 1)
                    container.Remove(container.Children[1]);
                if (container.Children.Length > 0)
                    textView = (TextView)container.Children[0];

                if (value != null)
                {
                    MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().Build();
                    MarkdownDocument document = Markdown.Parse(value, pipeline);
                    textView.Buffer.Text = string.Empty;
                    TextIter insertPos = textView.Buffer.GetIterAtOffset(0);
                    insertPos = ProcessMarkdownBlocks(document, ref insertPos, textView, 0);
                    container.ShowAll();
                }
            }
        }

        /// <summary>Gets or sets the base path that images should be relative to.</summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Process a collection of markdown blocks.
        /// </summary>
        /// <param name="blocks">The blocks to process.</param>
        /// <param name="insertPos">The insert position.</param>
        /// <param name="textView">The textview into which the markdown blocks' content will be added.</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="autoNewline">Should newline characters be automatically inserted after each block?</param>
        /// <param name="tags">Any additional tags to be applied to the content when it is added to the textview.</param>
        private TextIter ProcessMarkdownBlocks(IEnumerable<Block> blocks, ref TextIter insertPos, TextView textView, int indent, bool autoNewline = true, params TextTag[] tags)
        {
            // The markdown parser will strip out all of the whitespace (linefeeds) in the
            // text. Therefore, we need to insert newlines between each block - but not
            // every block. Some blocks (such as quote blocks) can be made up of multiple
            // smaller blocks. Therefore, if we just insert newlines at the bottom of this
            // method (ie every time we insert a block), we will end up with scenarios where
            // we have way too many empty lines.
            foreach (var block in blocks)
            {
                if (block is HeadingBlock header)
                {
                    ProcessMarkdownInlines(header.Inline, ref insertPos, textView, indent, GetTags($"Heading{header.Level}", indent).Union(tags).ToArray());
                }
                else if (block is ParagraphBlock paragraph)
                {
                    ProcessMarkdownInlines(paragraph.Inline, ref insertPos, textView, indent, tags);
                }
                else if (block is QuoteBlock quote)
                {
                    ProcessMarkdownBlocks(quote, ref insertPos, textView, indent + 1, false, tags);
                }
                else if (block is ListBlock list)
                {
                    int itemNumber = 1;
                    foreach (Block listBlock in list)
                    {
                        if (list.IsOrdered)
                        {
                            textView.Buffer.InsertWithTags(ref insertPos, $"{itemNumber}. ", GetTags("Normal", indent + 1));
                        }
                        else
                        {
                            textView.Buffer.InsertWithTags(ref insertPos, "• ", GetTags("Normal", indent + 1));
                        }
                        ProcessMarkdownBlocks(new[] { listBlock }, ref insertPos, textView, indent + 1, false, tags);
                        if (itemNumber != list.Count)
                            textView.Buffer.Insert(ref insertPos, "\n");
                        itemNumber++;
                    }
                }
                else if (block is ListItemBlock listItem)
                {
                    ProcessMarkdownBlocks(listItem, ref insertPos, textView, indent, false, tags);
                }
                else if (block is CodeBlock code)
                {
                    string text = code.Lines.ToString().TrimEnd(Environment.NewLine.ToCharArray());
                    textView.Buffer.InsertWithTags(ref insertPos, text, GetTags("Code", indent + 1).Union(tags).ToArray());
                }
                else if (block is Table table)
                {
                    DisplayTable(ref insertPos, table, indent);
                }
                else if (block is ThematicBreakBlock)
                {
                    // Horizontal rule - tbi.
                }
                else
                {
                }
                if (autoNewline)
                    textView.Buffer.Insert(ref insertPos, "\n\n");
            }

            return insertPos;
        }

        /// <summary>
        /// Process a collection of markdown inlines.
        /// </summary>
        /// <param name="inlines">The inlines to process.</param>
        /// <param name="insertPos">The insert position.</param>
        /// <param name="textView">The textview into which the markdown blocks' content will be added.</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="tags">Tags to use for all child inlines.</param>
        private void ProcessMarkdownInlines(IEnumerable<Inline> inlines, ref TextIter insertPos, TextView textView, int indent, params TextTag[] tags)
        {
            foreach (var inline in inlines)
            {
                if (inline is LiteralInline textInline)
                    textView.Buffer.InsertWithTags(ref insertPos, textInline.Content.ToString(), tags.Union(GetTags(null, indent)).ToArray());
                else if (inline is EmphasisInline italicInline)
                {
                    string style;
                    switch (italicInline.DelimiterChar)
                    {
                        case '^':
                            style = "Superscript";
                            break;
                        case '~':
                            style = italicInline.DelimiterCount == 1 ? "Subscript" : "Strikethrough";
                            break;
                        default:
                            style = italicInline.DelimiterCount == 1 ? "Italic" : "Bold";
                            break;
                    }
                    ProcessMarkdownInlines(italicInline, ref insertPos, textView, indent, tags.Union(GetTags(style, indent)).ToArray());
                }
                else if (inline is LinkInline link)
                {
                    // todo: difference between link.Label and link.Title?
                    if (link.IsImage)
                        DisplayImage(link.Url, link.Label, ref insertPos);
                    else
                        ProcessMarkdownInlines(link, ref insertPos, textView, indent, tags.Union(GetTags("Link", indent, link.Url)).ToArray());
                }
                //else if (inline is MarkdownLinkInline markdownLinkInline)
                //    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                    textView.Buffer.InsertWithTags(ref insertPos, codeInline.Content, tags.Union(GetTags(null, indent + 1)).ToArray());
                else if (inline is LineBreakInline br)
                    textView.Buffer.InsertWithTags(ref insertPos, br.IsHard ? "\n" : " ", tags);
                else
                {
                }
            }
        }

        /// <summary>
        /// Display a table.
        /// </summary>
        /// <param name="insertPos"></param>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        private void DisplayTable(ref TextIter insertPos, Table table, int indent)
        {
            int spaceWidth = MeasureText(" ");
            // Setup tab stops for the table.
            TabArray tabs = new TabArray(table.ColumnDefinitions.Count(), true);
            int cumWidth = 0;
            Dictionary<int, int> columnWidths = new Dictionary<int, int>();
            for (int i = 0; i < table.ColumnDefinitions.Count(); i++)
            {
                // The i-th tab stop will be set to the cumulative column width
                // of the first i columns (including padding).
                columnWidths[i] = GetColumnWidth(table, i);
                cumWidth += columnWidths[i];
                tabs.SetTab(i, TabAlign.Left, cumWidth);
            }

            // Create a TextTag containing the custom tab stops.
            // This TextTag will be applied to all text in the table.
            TextTag tableTag = new TextTag(Guid.NewGuid().ToString());
            tableTag.Tabs = tabs;
            tableTag.WrapMode = Gtk.WrapMode.None;
            textView.Buffer.TagTable.Add(tableTag);

            for (int i = 0; i < table.Count; i++)
            {
                TableRow row = (TableRow)table[i];
                for (int j = 0; j < Math.Min(table.ColumnDefinitions.Count(), row.Count); j++)
                {
                    if (row[j] is TableCell cell)
                    {
                        // Figure out which tags to use for this cell. We always
                        // need to use the tableTag (which includes the tab stops)
                        // but if it's the top row, we also include the bold tag.
                        TextTag[] tags;
                        if (row.IsHeader)
                            tags = new TextTag[2] { tableTag, textView.Buffer.TagTable.Lookup("Bold") };
                        else
                            tags = new TextTag[1] { tableTag };

                        TableColumnAlign? alignment = table.ColumnDefinitions[j].Alignment;
                        // If the column is center- or right-aligned, we will insert
                        // some whitespace characters to pad out the text.
                        if (alignment == TableColumnAlign.Center || alignment == TableColumnAlign.Right)
                        {
                            // Calculate the width of the cell contents.
                            int cellWidth = MeasureText(GetCellRawText(cell));

                            // Number of whitespace characters we can fit will be the
                            // number of spare pixels in the cell (width of cell - width
                            // of cell text) divided by the width of a single space char.
                            int spareSpace = (columnWidths[j] - cellWidth) / spaceWidth;
                            if (spareSpace >= 2)
                            {
                                // The number of spaces to insert will be either the total
                                // amount we can fit if right-aligning, or half that amount
                                // if center-aligning.
                                int numSpaces = alignment == TableColumnAlign.Center ? spareSpace / 2 : spareSpace;
                                string whitespace = new string(' ', spareSpace / 2);
                                textView.Buffer.Insert(ref insertPos, whitespace);
                            }
                        }

                        // Recursively process all markdown blocks inside this cell. In
                        // theory, this supports both blocks and inline content. In practice
                        // I wouldn't recommend using blocks inside a table cell.
                        ProcessMarkdownBlocks(cell, ref insertPos, textView, indent, false, tags);
                        if (j != row.Count - 1)
                            textView.Buffer.InsertWithTags(ref insertPos, "\t", tableTag);
                    }
                    else
                        // This shouldn't happen.
                        throw new Exception($"Unknown cell type {row[(int)j].GetType()}.");
                }
                // Insert a newline after each row - except the last row.
                if (i + 1 < table.Count)
                    textView.Buffer.Insert(ref insertPos, "\n");
            }
        }

        /// <summary>
        /// Calculate the width (in pixels) of a column in a table.
        /// This is the width of the widest cell in the column.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="columnIndex">Index of a column in the table.</param>
        private int GetColumnWidth(Table table, int columnIndex)
        {
            int width = 0;
            for (int i = 0; i < table.Count; i++)
            {
                TableRow row = (TableRow)table[i];
                if (columnIndex < row.Count)
                {
                    if (row[columnIndex] is TableCell cell)
                    {
                        string cellText = GetCellRawText(cell);
                        int cellWidth = MeasureText(cellText);
                        width = Math.Max(width, cellWidth + tableColumnPadding);
                    }
                    else
                        throw new NotImplementedException($"Unknown cell type {row[columnIndex].GetType().Name}.");
                }
            }
            return width;
        }

        /// <summary>
        /// Get the raw text in a cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        private string GetCellRawText(TableCell cell)
        {
            TextView tmpView = new TextView();
            CreateStyles(tmpView);
            TextIter iter = tmpView.Buffer.StartIter;
            ProcessMarkdownBlocks(cell, ref iter, tmpView, 0, false);
            string result = tmpView.Buffer.Text;
            tmpView.Cleanup();
            return result;
        }

        /// <summary>
        /// Measure the width of a string in pixels.
        /// </summary>
        /// <param name="text">The text to be measured.</param>
        private int MeasureText(string text)
        {
            Label label = new Label();
#if NETFRAMEWORK
            label.Layout.FontDescription = owner.MainWidget.Style.FontDescription;
#else
            label.Layout.FontDescription = FontDescription.FromString(Utility.Configuration.Settings.FontName);
#endif
            label.Layout.SetText(text);
            label.Layout.GetPixelSize(out int width, out _);
            return width;
        }

        /// <summary>
        /// Display an image
        /// </summary>
        /// <param name="url">The url of the image.</param>
        /// <param name="tooltip">Tooltip to be displayed on the image.</param>
        /// <param name="insertPos">The text iterator insert position.</param>
        private void DisplayImage(string url, string tooltip, ref TextIter insertPos)
        {
            // Convert relative paths in url to absolute.
            string absolutePath = PathUtilities.GetAbsolutePath(url, ImagePath);

            Gtk.Image image = null;
            if (File.Exists(absolutePath))
                // Apparently the native gtk deps we ship with windows releases don't include
                // gtk_image_new_from_file(). Therefore we avoid that particular constructor.
                image = new Gtk.Image(new Pixbuf(absolutePath));
            else
            {
                string imagePath = "ApsimNG.Resources." + url;
                foreach (string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                    if (string.Equals(imagePath, resourceName, StringComparison.InvariantCultureIgnoreCase))
                        image = new Gtk.Image(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
            }

            if (image != null)
            { 
                image.SetAlignment(0, 0);
                if (!string.IsNullOrWhiteSpace(tooltip))
                    image.TooltipText = tooltip;

                image.ShowAll();
                TextChildAnchor anchor = textView.Buffer.CreateChildAnchor(ref insertPos);
                textView.AddChildAtAnchor(image, anchor);
            }
        }

        /// <summary>
        /// Get markdown 'tags' for a given style.
        /// </summary>
        /// <param name="styleName">The name of the style.</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="url">The link url.</param>
        /// <returns></returns>
        private TextTag[] GetTags(string styleName, int indent, string url = null)
        {
            var tags = new List<TextTag>();
            if (!string.IsNullOrEmpty(styleName))
            {
                var styleTag = textView.Buffer.TagTable.Lookup(styleName);
                if (styleTag != null)
                    tags.Add(styleTag);
            }
            if (indent > 0)
            {
                var indentTag = textView.Buffer.TagTable.Lookup($"Indent{indent}");
                if (indentTag != null)
                    tags.Add(indentTag);
            }
            if (url != null)
            {
                // Only add the tag to the tag table if it doesn't already exist.
                // This does actually occur from time to time if a description
                // contains multiple links to the same target.
                var linkTag = textView.Buffer.TagTable.Lookup(url) as LinkTag;
                if (linkTag == null)
                {
                    linkTag = new LinkTag(url);
                    linkTag.SizePoints = 12;
                    textView.Buffer.TagTable.Add(linkTag);
                }
                tags.Add(linkTag);
            }
            return tags.ToArray();
        }

        /// <summary>Create TextView styles.</summary>
        private void CreateStyles(TextView textView)
        {
            var heading1 = new TextTag("Heading1");
            heading1.SizePoints = 30;
            heading1.Weight = Pango.Weight.Bold;
            textView.Buffer.TagTable.Add(heading1);

            var heading2 = new TextTag("Heading2");
            heading2.SizePoints = 25;
            heading2.Weight = Pango.Weight.Bold;
            textView.Buffer.TagTable.Add(heading2);

            var heading3 = new TextTag("Heading3");
            heading3.SizePoints = 20;
            heading3.Weight = Pango.Weight.Bold;
            textView.Buffer.TagTable.Add(heading3);

            var code = new TextTag("Code");
            code.Font = "monospace";
            textView.Buffer.TagTable.Add(code);

            var bold = new TextTag("Bold");
            bold.Weight = Pango.Weight.Bold;
            textView.Buffer.TagTable.Add(bold);

            var italic = new TextTag("Italic");
            italic.Style = Pango.Style.Italic;
            textView.Buffer.TagTable.Add(italic);

            var indent1 = new TextTag("Indent1");
            indent1.LeftMargin = indentSize;
            textView.Buffer.TagTable.Add(indent1);

            var indent2 = new TextTag("Indent2");
            indent2.LeftMargin = 2 * indentSize;
            textView.Buffer.TagTable.Add(indent2);

            var indent3 = new TextTag("Indent3");
            indent3.LeftMargin = 3 * indentSize;
            textView.Buffer.TagTable.Add(indent3);

            var subscript = new TextTag("Subscript");
            subscript.Rise = 0;
            subscript.Scale = Pango.Scale.XSmall;
            textView.Buffer.TagTable.Add(subscript);

            var superscript = new TextTag("Superscript");
            superscript.Rise = 8192;
            superscript.Scale = Pango.Scale.XSmall;
            textView.Buffer.TagTable.Add(superscript);

            var strikethrough = new TextTag("Strikethrough");
            strikethrough.Strikethrough = true;
            textView.Buffer.TagTable.Add(strikethrough);
        }

        // Looks at all tags covering the position (x, y) in the text view,
        // and if one of them is a link, change the cursor to the "hands" cursor
        // typically used by web browsers.
        private void SetCursorIfAppropriate(TextView textView, int x, int y)
        {
            TextIter iter = textView.GetIterAtLocation(x, y);
            // fixme: When we remove gtk2 deps, we can eliminate this check
            if (iter.Equals(TextIter.Zero))
                return;

            var foundLink = false;
            foreach (TextTag tag in iter.Tags)
            {
                if (tag is LinkTag)
                    foundLink = true;
            }

            Gdk.Window window = textView.GetWindow(Gtk.TextWindowType.Text);
            if (window != null)
            {
                if (foundLink/*hovering != hoveringOverLink*/)
                    window.Cursor = handCursor;
                else
                    window.Cursor = regularCursor;
            }
            else
            {

            }
        }

        /// <summary>
        /// Trap widget events to get a left button mouse click.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        private void OnWidgetEventAfter(object sender, WidgetEventAfterArgs args)
        {
            try
            {
                if (args.Event.Type == Gdk.EventType.ButtonRelease)
                {
                    Gdk.EventButton evt = (Gdk.EventButton)args.Event;

                    if (evt.Button == 1)
                    {
                        // we shouldn't follow a link if the user has selected something
                        textView.Buffer.GetSelectionBounds(out TextIter start, out TextIter end);
                        if (start.Offset == end.Offset)
                        {
                            textView.WindowToBufferCoords(TextWindowType.Widget, (int)evt.X, (int)evt.Y, out int x, out int y);
                            TextIter iter = textView.GetIterAtLocation(x, y);
                            // fixme: When we remove gtk2 deps, we can eliminate this check
                            if (iter.Equals(TextIter.Zero))
                                return;

                            foreach (var tag in iter.Tags)
                            {
                                if (tag is LinkTag linkTag)
                                {
                                    // convert modified absolute paths in links for parser acceptance back to real link
                                    // allows tags in URL to link to absolute c: path
                                    if (linkTag.URL.Contains("[drive]"))
                                    {
                                        linkTag.URL = linkTag.URL.Replace("[drive]", ":");
                                        linkTag.URL = linkTag.URL.Replace("../", "/");
                                    }
                                    ProcessUtilities.ProcessStart(linkTag.URL);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the mouse is moved.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMotionNotify(object sender, MotionNotifyEventArgs args)
        {
            try
            {
                var textViewWithMouseCursor = (TextView)sender;
                textViewWithMouseCursor.WindowToBufferCoords(TextWindowType.Widget,
                                                             (int)args.Event.X, (int)args.Event.Y,
                                                             out int x, out int y);
                SetCursorIfAppropriate(textViewWithMouseCursor, x, y);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when widget becomes visible.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="args">Event arguments.</param>
        private void OnVisibilityNotify(object sender, VisibilityNotifyEventArgs args)
        {
            try
            {
                textView.GetPointer(out int wx, out int wy);
                textView.WindowToBufferCoords(TextWindowType.Widget, wx, wy,
                                              out int bx, out int by);
                SetCursorIfAppropriate(textView, bx, by);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Widget is destroyed.</summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">The event aruments.</param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                textView.KeyPressEvent -= OnTextViewKeyPress;
                textView.VisibilityNotifyEvent -= OnVisibilityNotify;
                textView.MotionNotifyEvent -= OnMotionNotify;
                textView.WidgetEventAfter -= OnWidgetEventAfter;
                mainWidget.Destroyed -= OnDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private class LinkTag : TextTag
        {
            public string URL { get; set; }

            public LinkTag(string url) : base(url)
            {
                URL = url;
                ForegroundGdk = (ViewBase.MasterView as ViewBase).MainWidget.GetBackgroundColour(StateType.Selected);
                Underline = Pango.Underline.Single;
            }
        }
    }
}
