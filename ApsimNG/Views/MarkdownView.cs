using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;
using Cairo;
using ClosedXML.Excel;
using Gdk;
using Gtk;
using Markdig;
using Markdig.Extensions;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Models.Core;
using Utility;
using Table = Markdig.Extensions.Tables.Table;

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

    public class SubSuperScriptExtensions : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<SubSuperScriptParser>())
                pipeline.InlineParsers.Insert(0, new SubSuperScriptParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            throw new NotImplementedException();
        }
    }

    public class SubSuperScriptParser : InlineParser
    {
        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var startPosition = processor.GetSourcePosition(slice.Start, out int line, out int column);

            // Slightly faster to perform our own search for opening characters
            var nextStart = processor.Parsers.IndexOfOpeningCharacter(slice.Text, slice.Start + 1, slice.End);
            //var nextStart = str.IndexOfAny(processor.SpecialCharacters, slice.Start + 1, slice.Length - 1);
            string text = slice.Text.Substring(slice.Start, slice.End - slice.Start + 1);

            if (slice.PeekCharExtra(-1) != '>')
                return false;

            string openSuperscript = "<sup>";
            string closeSuperscript = "</sup>";
            string openSubscript = "<sub>";
            string closeSubscript = "</sub>";
            
            if (TryMatch(slice, openSuperscript))
            {
                int indexCloseSuper = text.IndexOf(closeSuperscript);
                if (indexCloseSuper > 0)
                {
                    slice = new StringSlice(text.Substring(0, indexCloseSuper));
                    processor.Inline = new EmphasisInline()
                    {
                        DelimiterChar = '^',
                        DelimiterCount = 1,
                    };
                    return true;
                }
            }
            else if (TryMatch(slice, openSubscript))
            {
                int indexCloseSub = text.IndexOf(closeSubscript);
                if (indexCloseSub > 0)
                {
                    slice = new StringSlice(text.Substring(0, indexCloseSub));
                    processor.Inline = new EmphasisInline()
                    {
                        DelimiterChar = '~',
                        DelimiterCount = 1,
                    };
                    return true;
                }
            }
            return false;
        }

        private bool TryMatch(StringSlice slice, string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            for (int i = 0; i < text.Length; i++)
                if (slice.PeekCharExtra(-1 * text.Length + i) != text[i])
                    return false;
            return true;
        }
    }

    /// <summary>A rich text view.</summary>
    public class MarkdownView : ViewBase, IMarkdownView
    {
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
            box.PackStart(child);
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
            container = (VBox)gtkControl;
            textView = (TextView)container.Children[0];
            textView.PopulatePopup += OnPopulatePopupMenu;
            findView = new MarkdownFindView();

            textView.Editable = false;
            textView.WrapMode = WrapMode.Word;
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
                args.Menu.Append(option);
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
                    MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<SubSuperScriptExtensions>().Build();
                    MarkdownDocument document = Markdown.Parse(value, pipeline);
                    textView.Buffer.Text = string.Empty;
                    TextIter insertPos = textView.Buffer.GetIterAtOffset(0);
                    insertPos = ProcessMarkdownBlocks(document, ref insertPos, 0);
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
        /// <param name="indent">The indent level.</param>
        /// <returns></returns>
        private TextIter ProcessMarkdownBlocks(IEnumerable<Block> blocks, ref TextIter insertPos, int indent)
        {
            foreach (var block in blocks)
            {
                if (block is HeadingBlock header)
                {
                    ProcessMarkdownInlines(header.Inline, ref insertPos, indent, GetTags($"Heading{header.Level}", indent));
                    textView.Buffer.Insert(ref insertPos, "\n\n");
                }
                else if (block is ParagraphBlock paragraph)
                {
                    ProcessMarkdownInlines(paragraph.Inline, ref insertPos, indent);
                    textView.Buffer.Insert(ref insertPos, "\n\n");
                }
                else if (block is QuoteBlock quote)
                {
                    ProcessMarkdownBlocks(quote, ref insertPos, indent + 1);
                }
                else if (block is ListBlock list)
                {
                    int itemNumber = 1;
                    foreach (Block listBlock in list)
                    {
                        if (list.IsOrdered)
                        {
                            textView.Buffer.InsertWithTags(ref insertPos, $"{itemNumber}. ", GetTags("Normal", indent + 1));
                            itemNumber++;
                        }
                        else
                        {
                            textView.Buffer.InsertWithTags(ref insertPos, "• ", GetTags("Normal", indent + 1));
                        }
                        ProcessMarkdownBlocks(new[] { listBlock }, ref insertPos, indent + 1);
                        // textView.Buffer.Insert(ref insertPos, "\n\n");
                    }
                }
                else if (block is CodeBlock code)
                {
                    textView.Buffer.InsertWithTags(ref insertPos, code.Lines.ToString(), GetTags("Code", indent + 1));
                    textView.Buffer.InsertWithTags(ref insertPos, Environment.NewLine);
                }
                else if (block is Table table)
                    DisplayTable(ref insertPos, table);
                else if (block is ListItemBlock listItem)
                {
                    ProcessMarkdownBlocks(listItem, ref insertPos, indent);
                }
                else if (block is ThematicBreakBlock)
                {
                    if (string.IsNullOrWhiteSpace(textView.Buffer.Text))
                    {
                        container.Remove(textView);
                        container.Add(new HSeparator());
                        textView = new TextView();
                        textView.PopulatePopup += OnPopulatePopupMenu;
                        container.Add(textView);
                        insertPos = textView.Buffer.GetIterAtOffset(0);
                    }
                    else
                        textView.Buffer.Insert(ref insertPos, Environment.NewLine + Environment.NewLine);
                }
                else
                {
                }
            }

            return insertPos;
        }

        /// <summary>
        /// Process a collection of markdown inlines.
        /// </summary>
        /// <param name="inlines">The inlines to process.</param>
        /// <param name="insertPos">The insert position.</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="tags">Tags to use for all child inlines.</param>
        private void ProcessMarkdownInlines(IEnumerable<Inline> inlines, ref TextIter insertPos, int indent, params TextTag[] tags)
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
                    ProcessMarkdownInlines(italicInline, ref insertPos, indent, GetTags(style, indent));
                }
                else if (inline is LinkInline link)
                {
                    // todo: difference between link.Label and link.Title?
                    if (link.IsImage)
                        DisplayImage(link.Url, link.Label, ref insertPos);
                    else
                        ProcessMarkdownInlines(link, ref insertPos, indent, GetTags("Link", indent, link.Url));
                }
                //else if (inline is MarkdownLinkInline markdownLinkInline)
                //    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                    textView.Buffer.InsertWithTags(ref insertPos, codeInline.Content, GetTags(null, indent + 1));
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
        private void DisplayTable(ref TextIter insertPos, Table table)
        {
            Gtk.Table tableWidget = new Gtk.Table((uint)table.Count, (uint)table.ColumnDefinitions.Count(), false);
            for (uint i = 0; i < table.Count; i++)
                for (uint j = 0; j < table.ColumnDefinitions.Count(); j++)
                {
                    string text = "";
                    TableRow row = (TableRow)table[(int)i];
                    // fixme - need to add more robust parsing here
                    // Really, we should be using the above parsing methods.
                    if (row.Count > j && row[(int)j] is TableCell cell)
                        foreach (ParagraphBlock paragraph in cell.OfType<ParagraphBlock>())
                            foreach (LiteralInline inline in paragraph.Inline.OfType<LiteralInline>())
                                text += inline.Content.ToString();
                    if (row.IsHeader)
                        text = $"<b>{text}</b>";
                    tableWidget.Attach(new Label() { Markup = text, Xalign = 0, Selectable = true }, j, j + 1, i, i + 1, AttachOptions.Fill, AttachOptions.Fill, 5, 5);
                }
            TextChildAnchor anchor = textView.Buffer.CreateChildAnchor(ref insertPos);
            textView.AddChildAtAnchor(tableWidget, anchor);
            textView.Buffer.Insert(ref insertPos, "\n\n");
        }

        /// <summary>
        /// Display an image
        /// </summary>
        /// <param name="url">The url of the image.</param>
        /// <param name="insertPos">The text iterator insert position.</param>
        private void DisplayImage(string url, string tooltip, ref TextIter insertPos)
        {
            // Convert relative paths in url to absolute.
            string absolutePath = PathUtilities.GetAbsolutePath(url, ImagePath);

            Gtk.Image image = null;
            if (File.Exists(absolutePath))
                image = new Gtk.Image(absolutePath);
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

                var eventBox = new EventBox();
                eventBox.Visible = true;
                eventBox.ModifyBg(StateType.Normal, mainWidget.Style.Base(StateType.Normal));
                eventBox.Add(image);

                container.Add(eventBox);
                image.Visible = true;

                textView = new TextView();
                textView.PopulatePopup += OnPopulatePopupMenu;
                container.Add(textView);
                textView.Visible = true;
                insertPos = textView.Buffer.GetIterAtOffset(0);
                CreateStyles(textView);
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
            var styleTag = textView.Buffer.TagTable.Lookup(styleName);
            if (styleTag != null)
                tags.Add(styleTag);

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
            indent1.LeftMargin = 30;
            textView.Buffer.TagTable.Add(indent1);

            var indent2 = new TextTag("Indent2");
            indent2.LeftMargin = 60;
            textView.Buffer.TagTable.Add(indent2);

            var indent3 = new TextTag("Indent3");
            indent3.LeftMargin = 90;
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
                                    Process.Start(linkTag.URL);
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
                ForegroundGdk = (ViewBase.MasterView as ViewBase).MainWidget.Style.Background(StateType.Selected);
                Underline = Pango.Underline.Single;
            }
        }
    }
}
