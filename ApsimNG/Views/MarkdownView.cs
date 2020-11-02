using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using APSIM.Shared.Utilities;
using Cairo;
using ClosedXML.Excel;
using Gdk;
using Gtk;
using Microsoft.Toolkit.Parsers.Markdown;
using Microsoft.Toolkit.Parsers.Markdown.Blocks;
using Microsoft.Toolkit.Parsers.Markdown.Inlines;
using Models.Core;
using Utility;

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
                    var document = new MarkdownDocument();
                    document.Parse(value.Replace("~~~", "```"));
                    textView.Buffer.Text = string.Empty;
                    TextIter insertPos = textView.Buffer.GetIterAtOffset(0);
                    insertPos = ProcessMarkdownBlocks(document.Blocks, ref insertPos, 0);
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
        private TextIter ProcessMarkdownBlocks(IList<MarkdownBlock> blocks, ref TextIter insertPos, int indent)
        {
            foreach (var block in blocks)
            {
                if (block is HeaderBlock header)
                {
                    textView.Buffer.InsertWithTags(ref insertPos, header.ToString().Trim(), 
                                                   GetTags($"Heading{header.HeaderLevel}", indent));
                    textView.Buffer.Insert(ref insertPos, "\n\n");
                }
                else if (block is ParagraphBlock paragraph)
                {
                    ProcessMarkdownInlines(paragraph.Inlines, ref insertPos, indent);
                    textView.Buffer.Insert(ref insertPos, "\n\n");
                }
                else if (block is QuoteBlock quote)
                {
                    ProcessMarkdownBlocks(quote.Blocks, ref insertPos, indent + 1);
                }
                else if (block is ListBlock list)
                {
                    int itemNumber = 1;
                    foreach (var listItem in list.Items)
                    {
                        foreach (var listBlock in listItem.Blocks)
                        {
                            if (list.Style == ListStyle.Bulleted)
                                textView.Buffer.InsertWithTags(ref insertPos, "• ",
                                                               GetTags("Normal", indent + 1));
                            else
                            {
                                textView.Buffer.InsertWithTags(ref insertPos, $"{itemNumber}. ",
                                                               GetTags("Normal", indent + 1));
                                itemNumber++;
                            }
                            ProcessMarkdownInlines((listBlock as ParagraphBlock).Inlines, ref insertPos, indent + 1);
                            textView.Buffer.Insert(ref insertPos, "\n\n");
                        }
                    }
                }
                else if (block is CodeBlock code)
                {
                    textView.Buffer.InsertWithTags(ref insertPos, code.Text, GetTags("Code", indent + 1));
                    textView.Buffer.InsertWithTags(ref insertPos, Environment.NewLine);
                }
                else if (block is TableBlock table)
                    DisplayTable(ref insertPos, table);
                else if (block is HorizontalRuleBlock hr)
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
        private void ProcessMarkdownInlines(IList<MarkdownInline> inlines, ref TextIter insertPos, int indent)
        {
            foreach (var inline in inlines)
            {
                if (inline is TextRunInline textInline)
                    textView.Buffer.InsertWithTags(ref insertPos, textInline.Text, GetTags("Normal", indent));
                else if (inline is ItalicTextInline italicInline)
                    textView.Buffer.InsertWithTags(ref insertPos, italicInline.Inlines[0].ToString(), GetTags("Italic", indent));
                else if (inline is BoldTextInline boldInline)
                    textView.Buffer.InsertWithTags(ref insertPos, boldInline.Inlines[0].ToString(), GetTags("Bold", indent));
                else if (inline is HyperlinkInline linkInline)
                    textView.Buffer.InsertWithTags(ref insertPos, linkInline.Text, GetTags("Link", indent, linkInline.Url));
                else if (inline is MarkdownLinkInline markdownLinkInline)
                    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                    textView.Buffer.InsertWithTags(ref insertPos, codeInline.Text, GetTags("Normal", indent + 1));
                else if (inline is ImageInline imageInline)
                    DisplayImage(imageInline.Url, imageInline.Tooltip, ref insertPos);
                else if (inline is SubscriptTextInline subscript)
                    textView.Buffer.InsertWithTags(ref insertPos, string.Join("", subscript.Inlines.Select(i => i.ToString()).ToArray()), GetTags("Subscript", indent));
                else if (inline is SuperscriptTextInline superscript)
                    textView.Buffer.InsertWithTags(ref insertPos, string.Join("", superscript.Inlines.Select(i => i.ToString()).ToArray()), GetTags("Superscript", indent));
                else if (inline is LinkAnchorInline anchor)
                {
                    if (anchor.Link != null)
                        textView.Buffer.InsertWithTags(ref insertPos, anchor.Link, GetTags("Link", indent, anchor.ToString()));
                    else
                        textView.Buffer.Insert(ref insertPos, anchor.ToString());
                }
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
        private void DisplayTable(ref TextIter insertPos, TableBlock table)
        {
            Table tableWidget = new Table((uint)table.Rows.Count(), (uint)table.ColumnDefinitions.Count(), false);
            for (uint i = 0; i < table.Rows.Count(); i++)
                for (uint j = 0; j < table.ColumnDefinitions.Count(); j++)
                {
                    string text = "";
                    TableBlock.TableRow row = table.Rows[(int)i];
                    if (row.Cells.Count > j)
                        text = row.Cells[(int)j].Inlines.FirstOrDefault()?.ToString();
                    if (i == 0)
                        text = $"<b>{text}</b>";
                    tableWidget.Attach(new Label() { Markup = text, Xalign = 0 }, j, j + 1, i, i + 1, AttachOptions.Fill, AttachOptions.Fill, 5, 5);
                }

            // Add table to gtk container.
            tableWidget.ShowAll();
            Alignment alignment = new Alignment(0f, 0f, 1f, 1f);
            alignment.BottomPadding = 10;
            alignment.Add(tableWidget);
            container.PackStart(alignment, true, true, 0);

            // Insert a new textview beneath the previous one.
            textView = new TextView();
            textView.PopulatePopup += OnPopulatePopupMenu;
            textView.ShowAll();
            container.Add(textView);
            insertPos = textView.Buffer.GetIterAtOffset(0);
            CreateStyles(textView);
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

            var normal = new TextTag("Normal");
            normal.SizePoints = 12;
            normal.Weight = Pango.Weight.Normal;
            normal.Style = Pango.Style.Normal;
            normal.Indent = 0;
            textView.Buffer.TagTable.Add(normal);

            var code = new TextTag("Code");
            code.SizePoints = 12;
            code.Weight = Pango.Weight.Normal;
            code.Style = Pango.Style.Normal;
            code.Indent = 0;
            code.Font = "monospace";
            textView.Buffer.TagTable.Add(code);

            var bold = new TextTag("Bold");
            bold.SizePoints = 12;
            bold.Weight = Pango.Weight.Bold;
            textView.Buffer.TagTable.Add(bold);

            var italic = new TextTag("Italic");
            italic.SizePoints = 12;
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
                                    Process.Start(linkTag.URL);
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
