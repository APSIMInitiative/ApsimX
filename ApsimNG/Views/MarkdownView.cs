using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using APSIM.Shared.Utilities;
using Gdk;
using Gtk;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Pango;
using UserInterface.Extensions;
using Utility;
using Table = Markdig.Extensions.Tables.Table;

namespace UserInterface.Views
{

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
        private Cursor handCursor;
        private Cursor regularCursor;
        private MarkdownFindView findView;
        private AccelGroup accelerators = new AccelGroup();
        private Menu popupMenu = new Menu();

        /// <summary>Table of font sizes for ASCII characters. Set on attach and reset on attach whenver font has changed.</summary>
        private static readonly int[] fontCharSizes = new int[128];

        /// <summary>Table of font sizes for unicode symbols not in ascii. Dynamically filled, cleared on attach when font changed.</summary>
        private static readonly Dictionary<Rune, int> nonASCIICharSizes = new();

        /// <summary>The font name and point size. Used to determine whether or not the font has changed.</summary>
        private static string fontName = "";

        /// <summary>Constructor</summary>
        public MarkdownView() { }

        /// <summary>Constructor</summary>
        public MarkdownView(ViewBase owner) : base(owner)
        {
            Initialise(owner, new ScrolledWindow());
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

            if (fontName != Configuration.Settings.FontName)
            {
                fontName = Configuration.Settings.FontName;
                Label helper = new();
                helper.Layout.FontDescription = FontDescription.FromString(fontName);
                for (int i = 32; i < 127; i++)
                {
                    helper.Layout.SetText(((char)i).ToString());
                    helper.Layout.GetPixelSize(out var width, out _);
                    fontCharSizes[i] = width;
                }
                nonASCIICharSizes.Clear();
            }

            if (gtkControl is ScrolledWindow scroller)
            {
                textView = new TextView();
                scroller.Add(textView);
                mainWidget = scroller;
            }
            else
            {
                textView = (TextView)gtkControl;
                mainWidget = textView;
            }
            mainWidget.Margin = 10;
            textView.PopulatePopup += OnPopulatePopupMenu;
            findView = new MarkdownFindView();

            textView.Editable = false;
            textView.WrapMode = Gtk.WrapMode.Word;
            textView.VisibilityNotifyEvent += OnVisibilityNotify;
            textView.MotionNotifyEvent += OnMotionNotify;
            textView.WidgetEventAfter += OnWidgetEventAfter;
            CreateStyles(textView);
            mainWidget.ShowAll();
            mainWidget.Destroyed += OnDestroyed;
            mainWidget.Realized += OnRealized;
            textView.FocusInEvent += OnGainFocus;
            textView.FocusOutEvent += OnLoseFocus;

            handCursor = new Gdk.Cursor(Gdk.Display.Default, Gdk.CursorType.Hand2);
            regularCursor = new Gdk.Cursor(Gdk.Display.Default, Gdk.CursorType.Xterm);

            textView.KeyPressEvent += OnTextViewKeyPress;
        }

        /// <summary>
        /// Called when the text editor loses keyboard focus.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnLoseFocus(object sender, FocusOutEventArgs args)
        {
            try
            {
                if (mainWidget.Toplevel is Gtk.Window window)
                    window.RemoveAccelGroup(accelerators);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the text editor gains keyboard focus.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnGainFocus(object sender, FocusInEventArgs args)
        {
            try
            {
                if (mainWidget.Toplevel is Gtk.Window window)
                    window.AddAccelGroup(accelerators);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Context menu items aren't actually added to the context menu until the
        /// user requests the context menu (ie via right clicking). Keyboard shortcuts
        /// (accelerators) won't work until this occurs. Therefore, we now manually
        /// fire off a populate-popup signal to cause the context menu to be populated.
        /// (This doesn't actually cause the context menu to be displayed.)
        ///
        /// We wait until the widget is realized so that the owner of the view has a
        /// chance to add context menu items.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnRealized(object sender, EventArgs args)
        {
            try
            {
                GLib.Signal.Emit(textView, "populate-popup", popupMenu);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
                if ((args.Event.Key == Gdk.Key.F || args.Event.Key == Gdk.Key.f) && args.Event.State == ModifierType.ControlMask)
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
                option.AddAccelerator("activate", accelerators, (uint)Gdk.Key.F, ModifierType.ControlMask, AccelFlags.Visible);
                option.ShowAll();
                option.Activated += OnFindText;

                if (args.Popup is Menu menu)
                    menu.Append(option);

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
                textView.Buffer.Clear();

                if (value != null)
                {
                    MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UsePipeTables().UseEmphasisExtras().Build();
                    MarkdownDocument document = Markdown.Parse(value, pipeline);
                    TextIter insertPos = textView.Buffer.GetIterAtOffset(0);
                    insertPos = ProcessMarkdownBlocks(document, ref insertPos, textView, 0);
                    mainWidget.ShowAll();
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
            Block prevBlock = null;
            foreach (var block in blocks)
            {
                if (block is HeadingBlock header)
                {
                    ProcessMarkdownInlines(header.Inline, ref insertPos, textView, indent, GetTags(textView, $"Heading{header.Level}", indent).Union(tags).ToArray());
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
                    if (prevBlock is ParagraphBlock) //add special case newline for sublists
                        textView.Buffer.Insert(ref insertPos, "\n");

                    int itemNumber = 1;
                    foreach (Block listBlock in list)
                    {
                        if (list.IsOrdered)
                            textView.Buffer.InsertWithTags(ref insertPos, $"{itemNumber}. ", GetTags(textView, "Normal", indent + 1));
                        else
                            textView.Buffer.InsertWithTags(ref insertPos, "• ", GetTags(textView, "Normal", indent + 1));

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
                    textView.Buffer.InsertWithTags(ref insertPos, text, GetTags(textView, "Code", indent + 1).Union(tags).ToArray());
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
                // Don't insert auto newlines after the last block in the document.
                if (autoNewline && !(block.Parent is MarkdownDocument && block.Parent.LastChild == block))
                    textView.Buffer.Insert(ref insertPos, "\n\n");

                prevBlock = block;
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
                    textView.Buffer.InsertWithTags(ref insertPos, textInline.Content.ToString(), tags.Union(GetTags(textView, null, indent)).ToArray());
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
                    ProcessMarkdownInlines(italicInline, ref insertPos, textView, indent, tags.Union(GetTags(textView, style, indent)).ToArray());
                }
                else if (inline is LinkInline link)
                {
                    // todo: difference between link.Label and link.Title?
                    if (link.IsImage)
                        DisplayImage(link.Url, link.Label, ref insertPos);
                    else
                        ProcessMarkdownInlines(link, ref insertPos, textView, indent, tags.Union(GetTags(textView, "Link", indent, link.Url)).ToArray());
                }
                //else if (inline is MarkdownLinkInline markdownLinkInline)
                //    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                    textView.Buffer.InsertWithTags(ref insertPos, codeInline.Content, tags.Union(GetTags(textView, null, indent + 1)).ToArray());
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
            int spaceWidth = fontCharSizes[' '];
            // Setup tab stops for the table.
            TabArray tabs = new TabArray(table.ColumnDefinitions.Count, true);
            int cumWidth = 0;
            Dictionary<int, List<int>> cellLengthsByRow = new();
            for (int i = 0; i < table.Count; i++)
            {
                var b = table[i];
                if (b is TableRow tr)
                {
                    cellLengthsByRow[i] = tr
                        .Select(b => GetCellRawText((TableCell)b))
                        .Select(MeasureText)
                        .ToList();
                }
                else
                    throw new Exception($"Error when processing Markdown table: {b.GetType()} is not a table row.");
            }

            int[] columnWidths = new int[table.ColumnDefinitions.Count];
            for (int i = 0; i < table.ColumnDefinitions.Count; i++)
            {
                var columnLength = cellLengthsByRow.Values
                    .Where(l => l.Count > i)
                    .Select(l => l[i] + tableColumnPadding)
                    .DefaultIfEmpty(0)
                    .Max();
                columnWidths[i] = columnLength;
                cumWidth += columnLength;
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
                for (int j = 0; j < Math.Min(table.ColumnDefinitions.Count, row.Count); j++)
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
                            int cellWidth = cellLengthsByRow[i][j];

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
        /// Get the raw text in a cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        private static string GetCellRawText(TableCell cell)
        {
            StringBuilder sb = new();
            foreach (var block in cell)
                ExtractBlockText(block);
            return sb.ToString();

            void ExtractBlockText(Block block)
            {
                if (block is LeafBlock lb)
                    foreach (var inline in lb.Inline)
                        ExtractInlineText(inline);
                else if (block is ContainerBlock cb)
                    foreach (var innerBlock in cb)
                        ExtractBlockText(innerBlock);
                else
                    throw new NotImplementedException($"Unknown block: {block.GetType()}.");
            }

            void ExtractInlineText(Inline inline)
            {
                if (inline is LiteralInline literal)
                    sb.Append(literal.Content.AsSpan());
                else if (inline is ContainerInline ci)
                    foreach (var il in ci)
                        ExtractInlineText(il);
            }
        }

        /// <summary>
        /// Measure the width of a string in pixels.
        /// </summary>
        /// <param name="text">The text to be measured.</param>
        private static int MeasureText(string text)
        {
            return text.EnumerateRunes().Sum(MeasureRuneSize);
        }

        /// <summary>
        /// Measures the size of a unicode rune in pixels.
        /// </summary>
        /// <param name="ch">The rune to measure.</param>
        private static int MeasureRuneSize(Rune ch)
        {
            if (ch.IsAscii)
                return fontCharSizes[ch.Value];
            // Non-printing variation selector. Gets a pixel size for some reason.
            if (0xFE00 <= ch.Value && ch.Value <= 0xFE0F)
                return 0;

            int width;
            if (!nonASCIICharSizes.TryGetValue(ch, out width))
            {
                Label helper = new();
                helper.Layout.FontDescription = FontDescription.FromString(fontName);
                helper.Layout.SetText(ch.ToString());
                helper.Layout.GetPixelSize(out width, out _);
                nonASCIICharSizes[ch] = width;
            }
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

                image.Halign = image.Valign = 0;

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
        /// <param name="textView">The textview whose tag table should be searched.</param>
        /// <param name="styleName">The name of the style.</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="url">The link url.</param>
        /// <returns></returns>
        private TextTag[] GetTags(TextView textView, string styleName, int indent, string url = null)
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

            // Give Gtk time to digest these additions to the tag table
            // Otherwise we can sometimes get nulls when we access the tags
            while (GLib.MainContext.Iteration())
                ;
        }

        // Looks at all tags covering the position (x, y) in the text view,
        // and if one of them is a link, change the cursor to the "hands" cursor
        // typically used by web browsers.
        private void SetCursorIfAppropriate(TextView textView, int x, int y)
        {
            TextIter iter = textView.GetIterAtLocation(x, y);
            var foundLink = false;
            // fixme: When we remove gtk2 deps, we can eliminate this check
            if (!iter.Equals(TextIter.Zero))
            {
                foreach (TextTag tag in iter.Tags)
                {
                    if (tag is LinkTag)
                        foundLink = true;
                }
            }

            Gdk.Window window = textView.GetWindow(TextWindowType.Text);
            if (window != null)
            {
                if (foundLink)
                    window.Cursor = handCursor;
                else
                    window.Cursor = regularCursor;
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

                Gdk.Display.Default.GetPointer(out _, out int wx, out int wy, out _);

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
                popupMenu.Clear();
                popupMenu.Dispose();
                accelerators.Dispose();
                findView.Destroy();
                textView.PopulatePopup -= OnPopulatePopupMenu;
                textView.VisibilityNotifyEvent -= OnVisibilityNotify;
                textView.MotionNotifyEvent -= OnMotionNotify;
                textView.WidgetEventAfter -= OnWidgetEventAfter;
                mainWidget.Destroyed -= OnDestroyed;
                mainWidget.Realized -= OnRealized;
                textView.FocusInEvent -= OnGainFocus;
                textView.FocusOutEvent -= OnLoseFocus;
                textView.KeyPressEvent -= OnTextViewKeyPress;
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

                ForegroundGdk = (ViewBase.MasterView as ViewBase).MainWidget.StyleContext.GetColor(StateFlags.Link).ToColour().ToGdk();

                Underline = Pango.Underline.Single;
            }
        }
    }

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
}
