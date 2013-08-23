
namespace UserInterface.Commands
{
    class ChangePropertyCommand : ICommand
    {
        private object Obj;
        private string Name;
        private object Value;
        private object OriginalValue;

        public ChangePropertyCommand(object Obj, string PropertyName, object PropertyValue)
        {
            this.Obj = Obj;
            this.Name = PropertyName;
            this.Value = PropertyValue;
        }

        public object Do()
        {
            // Get original value of property so that we can restore it in Undo if needed.
            OriginalValue = Utility.Reflection.GetValueOfFieldOrProperty(Name, Obj);

            // Set the new property value.
            if (Utility.Reflection.SetValueOfFieldOrProperty(Name, Obj, Value))
                return Obj;
            return null;
        }

        public object Undo()
        {
            if (Utility.Reflection.SetValueOfFieldOrProperty(Name, Obj, OriginalValue))
                return Obj;
            return null;
        }
    }
}
