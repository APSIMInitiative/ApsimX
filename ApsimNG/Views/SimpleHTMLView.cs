#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>
    /// We don't have a true HTMLView widget yet in gtk3. This is intended
    /// to be used as a stand-in for the time being. All it does is display
    /// the raw markup on the screen.
    /// </summary>
    public class HTMLView : ViewBase, IHTMLView
    {
        private MarkdownView markdownView;

        public HTMLView(ViewBase owner) : base(owner)
        {
            markdownView = new MarkdownView(this);
            mainWidget = markdownView.MainWidget;
        }

        /// <summary>
        /// Dont' think this is even used...
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// This won't be used in this view...
        /// </summary>
#pragma warning disable CS0067
        public event EventHandler<CopyEventArgs> Copy;
#pragma warning restore CS0067

        /// <summary>
        /// TBI
        /// </summary>
        /// <returns></returns>
        public string GetMarkdown()
        {
            return markdownView.Text;
        }

        /// <summary>
        /// This at least should work, according to some definitions of the word 'work'.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="allowModification"></param>
        /// <param name="isURI"></param>
        public void SetContents(string contents, bool allowModification, bool isURI = false)
        {
            markdownView.Text = contents;
        }

        /// <summary>
        /// TBI
        /// </summary>
        public void UseMonoSpacedFont()
        {
            throw new NotImplementedException();
        }
    }
}
#endif