namespace UserInterface.Views
{
    /// <summary>Describes the public interface of a class that supports editing sheet cells.</summary>
    public interface ISheetEditor
    {
        /// <summary>Returns true if the editor is currently editing a cell.</summary>
        bool IsEditing { get; }
    }
}