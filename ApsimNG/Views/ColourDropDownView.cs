namespace UserInterface.Views
{
    using System;
    using System.Drawing;
    using global::UserInterface.Extensions;
    using Gtk;

#if NETCOREAPP
    using CellLayout = Gtk.ICellLayout;
    using TreeModel = Gtk.ITreeModel;
#endif

    /// <summary>An interface for a drop down</summary>
    public interface IColourDropDownView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Get or sets the list of valid values. Can be Color or string objects.</summary>
        object[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        object SelectedValue { get; set; }
    }

    /// <summary>A colour drop down capable of showing colours and/or strings.</summary>
    public class ColourDropDownView : ViewBase, IColourDropDownView
    {
        public enum ColourDropTypeEnum { Text, Colour };

        private ComboBox combobox1;
        private ListStore comboModel = new ListStore(typeof(string), typeof(Gdk.Color), typeof(int)); // 3 values: text string, color, and an int enumerating whether we want to display text or colour
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>Constructor</summary>
        public ColourDropDownView(ViewBase owner) : base(owner)
        {
            combobox1 = new ComboBox(comboModel);
            mainWidget = combobox1;
            combobox1.PackStart(comboRender, true);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.SetCellDataFunc(comboRender, OnDrawColourCombo);
            combobox1.Changed += OnChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                combobox1.Changed -= OnChanged;
                combobox1.SetCellDataFunc(comboRender, null);
                comboModel.Dispose();
                comboRender.Dispose();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Get or sets the list of valid values. Can be Color or string objects.</summary>
        /// <remarks>fixme - why is this of type object[]?</remarks>
        public object[] Values
        {
            get
            {
                int nVals = comboModel.IterNChildren();
                object[] result = new object[nVals];
                TreeIter iter;
                int i = 0;
                if (combobox1.GetActiveIter(out iter))
                    do
                    {
                        ColourDropTypeEnum typeEnum = (ColourDropTypeEnum)comboModel.GetValue(iter, 2);
                        if (typeEnum == ColourDropTypeEnum.Text)
                            result[i++] = (string)comboModel.GetValue(iter, 0);
                        else
                            result[i++] = Utility.Colour.FromGtk((Gdk.Color)comboModel.GetValue(iter, 1));
                    }
                    while (comboModel.IterNext(ref iter) && i < nVals);
                return result;
            }
            set
            {
                comboModel.Clear();
                foreach (object val in value)
                {
                    string text;
                    Gdk.Color color = Gdk.Color.Zero;
                    Type valType = val.GetType();
                    ColourDropTypeEnum typeEnum;
                    if (valType == typeof(Color))
                    {
                        typeEnum = ColourDropTypeEnum.Colour;
                        text = "";
                        color = new Gdk.Color(((Color)val).R, ((Color)val).G, ((Color)val).B);
                    }
                    else
                    {
                        typeEnum = ColourDropTypeEnum.Text;
                        text = (string)val;
#if NETFRAMEWORK
                        color = combobox1.Style.Base(StateType.Normal);
#else
                        // This is the old (obsolete) way of doing things. Can't just get rid of this
                        // because changing the background of each cell is the whole point of this view.
                        // Needs to be reimplemented for gtk3, so I won't suppress this warning.
                        color = combobox1.GetBackgroundColour(StateFlags.Normal);
#endif
                    }
                    comboModel.AppendValues(text, color, (int)typeEnum);
                }
                if (comboModel.IterNChildren() > 0)
                    combobox1.Active = 0;
                else
                    combobox1.Active = -1;
            }
        }


        /// <summary>Gets or sets the selected value. Can be colour or string.</summary>
        public object SelectedValue
        {
            get
            {
                TreeIter iter;
                if (combobox1.GetActiveIter(out iter))
                {
                    ColourDropTypeEnum typeEnum = (ColourDropTypeEnum)comboModel.GetValue(iter, 2);
                    if (typeEnum == ColourDropTypeEnum.Text)
                        return (string)comboModel.GetValue(iter, 0);
                    else
                        return Utility.Colour.FromGtk((Gdk.Color)comboModel.GetValue(iter, 1));
                }
                else
                    return null;
            }

            set
            {
                TreeIter iter;
                if (comboModel.GetIterFirst(out iter))
                {
                    do
                    {
                        ColourDropTypeEnum typeEnum = (ColourDropTypeEnum)comboModel.GetValue(iter, 2);
                        if (value.GetType() == typeof(Color))
                        {
                            Gdk.Color entry = (Gdk.Color)comboModel.GetValue(iter, 1);
                            Color rgb = Utility.Colour.FromGtk((Gdk.Color)comboModel.GetValue(iter, 1));
                            if (rgb.Equals((Color)value))
                            {
                                combobox1.SetActiveIter(iter);
                                return;
                            }
                        }
                        else if (typeEnum == ColourDropTypeEnum.Text)
                        {
                            string entry = (string)comboModel.GetValue(iter, 0);
                            if (string.Equals(value as string, entry, StringComparison.InvariantCultureIgnoreCase))
                            {
                                combobox1.SetActiveIter(iter);
                                return;
                            }
                        }
                    } while (comboModel.IterNext(ref iter));
                    // Could not find a matching entry; perhaps this should result in appending a new entry?
                    combobox1.Active = -1;
                }
            }
        }

        /// <summary>
        /// Handles the DrawItem combo box event to display colours.
        /// </summary>
        /// <param name="cell_layout">The cell layout.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The TreeIter.</param>
        private void OnDrawColourCombo(CellLayout cell_layout, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            try
            {
                cell.CellBackgroundGdk = (Gdk.Color)model.GetValue(iter, 1);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User has changed the selected colour.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e)
        {
            try
            {
                if (Changed != null)
                    Changed.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
