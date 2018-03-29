using System;
using Gtk;
namespace ApsimNG.Classes
{
    class CustomButton : Button
    {
        public CustomButton(string label, int id) : base(label)
        {
            ID = id;
        }
        public int ID { get; set; }
    }
}
