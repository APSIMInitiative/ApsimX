using System;

namespace UserInterface.Views
{
    public class TabChangedEventArgs : EventArgs
    {
        public string TabName { get; set; }

        public TabChangedEventArgs(string myString)
        {
            this.TabName = myString;
        }
    }
}
