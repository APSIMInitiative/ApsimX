using System.Drawing;
using System.Windows.Forms;

namespace Utility
{
    /// <summary>
    /// Displays a Colour picker in a DataGridViewCell. Clicking on the cell will bring up the Colour 
    /// Dialog Box.
    /// </summary>
    public class ColorPickerCell : DataGridViewButtonCell
    {

        /// <summary>
        /// Paint the cell
        /// </summary>
        protected override void Paint(Graphics graphics, Rectangle clipBounds,
                                      Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState,
                                      object value, object formattedValue, string errorText,
                                      DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle
                                      advancedBorderStyle, DataGridViewPaintParts paintParts)
        {

            // draw the button
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value,
                       formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            // draw a rectangle over the button
            using (Pen darkPen = new Pen(SystemColors.ControlDark))
            {
                Rectangle rc = new Rectangle(cellBounds.X, cellBounds.Y,
                                             cellBounds.Width,
                                             cellBounds.Height);

                graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)Value)), rc);
                graphics.DrawRectangle(darkPen, rc);
            }
        }


        
    }
}
