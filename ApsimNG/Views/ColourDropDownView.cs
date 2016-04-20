// -----------------------------------------------------------------------
// <copyright file="ColourDropDownView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Drawing;
    using Gtk;
    /// using System.Windows.Forms;

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
        private ColourDropTypeEnum type = ColourDropTypeEnum.Colour;

        private ComboBox combobox1;
        private ListStore comboModel = new ListStore(typeof(string), typeof(Gdk.Color));
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>Constructor</summary>
        public ColourDropDownView(ViewBase owner) : base(owner)
        {
            combobox1 = new ComboBox(comboModel);
            _mainWidget = combobox1;
            combobox1.PackStart(comboRender, true);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.SetCellDataFunc(comboRender, OnDrawColourCombo);
            combobox1.Changed += OnChanged;
        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Get or sets the list of valid values. Can be Color or string objects.</summary>
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
                        if (type == ColourDropTypeEnum.Text)
                          result[i++] = comboModel.GetValue(iter, 0);
                        else
                        {
                            Gdk.Color color = (Gdk.Color)comboModel.GetValue(iter, 1);
                            result[i++] = Color.FromArgb(color.Red * 255 / 65535, color.Green * 255 / 65535, color.Blue * 255 / 65535);
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
                    if (val.GetType() == typeof(Color))
                    {
                        type = ColourDropTypeEnum.Colour;
                        text = "";
                        color = new Gdk.Color(((Color)val).R, ((Color)val).G, ((Color)val).B);
                    }
                    else
                    {
                        type = ColourDropTypeEnum.Text;
                        text = (string)val;
                        Gdk.Color.Parse(text, ref color);
                    }
                    comboModel.AppendValues(text, color);
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
                    if (type == ColourDropTypeEnum.Text)
                        return comboModel.GetValue(iter, 0);
                    else
                    {
                        Gdk.Color color = (Gdk.Color)comboModel.GetValue(iter, 1);
                        return Color.FromArgb(color.Red * 255 / 65535, color.Green * 255 / 65535, color.Blue * 255 / 65535);
                    }
                else
                    return null;
            }

            set
            {
                TreeIter iter;
                if (comboModel.GetIterFirst(out iter))
                {
                    if (type == ColourDropTypeEnum.Text)
                    {
                        string entry = (string)comboModel.GetValue(iter, 0);
                        while (!entry.Equals((string)value, StringComparison.InvariantCultureIgnoreCase) && comboModel.IterNext(ref iter)) // Should the text matching be case-insensitive?
                            entry = (string)comboModel.GetValue(iter, 0);
                        if (entry.Equals((string)value, StringComparison.InvariantCultureIgnoreCase))
                            combobox1.SetActiveIter(iter);
                        else // Could not find a matching entry; perhaps this should result in appending a new entry?
                            combobox1.Active = -1;
                    }
                    else
                    {
                        Gdk.Color entry = (Gdk.Color)comboModel.GetValue(iter, 1);
                        Color rgb = Color.FromArgb(entry.Red, entry.Green, entry.Blue);
                        while (!rgb.Equals((Color)value) && comboModel.IterNext(ref iter)) // Should the text matching be case-insensitive?
                        {
                            entry = (Gdk.Color)comboModel.GetValue(iter, 1);
                            rgb = Color.FromArgb(entry.Red * 255 / 65535, entry.Green * 255 / 65535, entry.Blue * 255 / 65535);
                        }
                        if (rgb.Equals((Color)value))
                            combobox1.SetActiveIter(iter);
                        else // Could not find a matching entry; perhaps this should result in appending a new entry?
                            combobox1.Active = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the DrawItem combo box event to display colours.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDrawColourCombo(CellLayout cell_layout, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            cell.CellBackgroundGdk = (Gdk.Color)model.GetValue(iter, 1);
        }

        /// <summary>User has changed the selected colour.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }
    }
}
