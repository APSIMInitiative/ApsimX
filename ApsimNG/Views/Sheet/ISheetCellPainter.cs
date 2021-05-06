using Cairo;

namespace UserInterface.Views
{
    public interface ISheetCellPainter
    {
        bool PaintCell(int columnIndex, int rowIndex);
        Color GetForegroundColour(int columnIndex, int rowIndex);
        Color GetBackgroundColour(int columnIndex, int rowIndex);
    }
}