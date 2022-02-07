using System;
using System.Linq;

namespace UserInterface.Views
{
    public class EnumDropDownView<T> : DropDownView where T : struct
    {
        public EnumDropDownView(ViewBase owner) : base(owner)
        {
            string[] enumValues = Enum.GetValues(typeof(T)).Cast<object>().Select(e => e.ToString()).ToArray();
            Values = enumValues;
        }

        public T SelectedEnumValue
        {
            get
            {
                return Enum.Parse<T>(SelectedValue);
            }
            set
            {
                SelectedValue = value.ToString();
            }
        }
    }
}