using System;
using Gtk;

namespace ApsimNG.Classes
{
    public class ListViewArgs : EventArgs
    {
        public QueryTooltipArgs QueryTooltipArgs { get; set; }

        public int ListViewRowIndex { get; set; }
    }
}
