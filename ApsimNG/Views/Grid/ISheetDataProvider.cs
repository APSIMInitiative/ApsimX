using System.Collections.Generic;

namespace UserInterface.Views
{
    public interface ISheetDataProvider
    {
        int ColumnCount { get;  }

        int RowCount { get; }
        string GetCellContents(int colIndex, int rowIndex);

        void SetCellContents(int colIndex, int rowIndex, string value);

    }
}