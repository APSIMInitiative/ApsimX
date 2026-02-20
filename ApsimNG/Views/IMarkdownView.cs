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
}
