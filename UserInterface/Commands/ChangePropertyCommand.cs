
using System.Collections.Generic;
using System;
namespace UserInterface.Commands
{
    class ChangePropertyCommand : ICommand
    {
        class Property
        {
            public string Name;
            public object Value;
            public object OldValue;
            public bool WasModified;
        }

        private object Obj;
        private List<Property> Properties = new List<Property>();

        public ChangePropertyCommand(object Obj, string PropertyName, object PropertyValue)
        {
            this.Obj = Obj;
            Property P = new Property();
            P.Name = PropertyName;
            P.Value = PropertyValue;
            Properties.Add(P);
        }


        public ChangePropertyCommand(object Obj, string[] PropertyNames, object[] PropertyValues)
        {
            if (PropertyNames.Length != PropertyValues.Length)
                throw new Exception("Bad call to ChangePropertyCommand constructor");

            this.Obj = Obj;
            for (int i = 0; i < PropertyNames.Length; i++)
            {
                Property P = new Property();
                P.Name = PropertyNames[i];
                P.Value = PropertyValues[i];
                Properties.Add(P);
            }
        }

        public void Do(CommandHistory CommandHistory)
        {
            foreach (Property P in Properties)
            {
                // Get original value of property so that we can restore it in Undo if needed.
                P.OldValue = Utility.Reflection.GetValueOfFieldOrProperty(P.Name, Obj);

                // Set the new property value.
                P.WasModified = Utility.Reflection.SetValueOfProperty(P.Name, Obj, P.Value);
            }
            CommandHistory.InvokeModelChanged(Obj);
        }

        public void Undo(CommandHistory CommandHistory)
        {
            foreach (Property P in Properties)
            {
                if (P.WasModified)
                    Utility.Reflection.SetValueOfProperty(P.Name, Obj, P.OldValue);
            }
            CommandHistory.InvokeModelChanged(Obj);
        }
    }
}
