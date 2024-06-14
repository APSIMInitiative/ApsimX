using Shared.Utilities;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public interface IManagerView
    {
        /// <summary>
        /// Provides access to the properties grid.
        /// </summary>
        /// <remarks>
        /// Change type to IPropertyView when ready to release new property view.
        /// </remarks>
        IPropertyView PropertyEditor { get; }

        /// <summary>
        /// Provides access to the editor.
        /// </summary>
        IEditorView Editor { get; }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }

        /// <summary>
        /// The values for the cursor and scrollbar position in the script editor
        /// </summary>
        ManagerCursorLocation CursorLocation { get; set; }
    }
}
