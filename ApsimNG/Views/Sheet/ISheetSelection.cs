using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Views
{
    public interface ISheetSelection
    {
        bool IsSelected(int columnIndex, int rowIndex);
    }
}
