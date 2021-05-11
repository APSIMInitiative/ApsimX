using Cairo;

namespace UserInterface.Views
{
    public interface ISheetCellPainter
    {
        bool TextLeftJustify(int columnIndex, int rowIndex);
        bool TextBold(int columnIndex, int rowIndex);
        bool TextItalics(int columnIndex, int rowIndex);
        bool PaintCell(int columnIndex, int rowIndex);
        Color GetForegroundColour(int columnIndex, int rowIndex);
        Color GetBackgroundColour(int columnIndex, int rowIndex);
    }
}