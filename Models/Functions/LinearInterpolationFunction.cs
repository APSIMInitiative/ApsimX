using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.Collections;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// Linear interpolation function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A value is returned via linear interpolation of a given set of XY pairs")]
    public class LinearInterpolationFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The ys are all the same</summary>
        private bool YsAreAllTheSame = false;

        /// <summary>Gets the xy pairs.</summary>
        [Link]
        private XYPairs XYPairs = null;

        [Link]
        private ILocator locator = null;

        private Dictionary<double, double> cache = new Dictionary<double, double>();

        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }

        /// <summary>Constructor</summary>
        public LinearInterpolationFunction() { }

        /// <summary>Constructor</summary>
        /// <param name="xproperty">x property</param>
        /// <param name="x">x values.</param>
        /// <param name="y">y values.</param>
        public LinearInterpolationFunction(string xproperty, double[] x, double[] y)
        {
            XProperty = xproperty;
            XYPairs = new XYPairs();
            XYPairs.X = x;
            XYPairs.Y = y;
        }

        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
        {
            if (XYPairs != null)
            {
                for (int i = 1; i < XYPairs.Y.Length; i++)
                    if (XYPairs.Y[i] != XYPairs.Y[i - 1])
                    {
                        YsAreAllTheSame = false;
                        return;
                    }

                // If we get this far then the Y values must all be the same.
                YsAreAllTheSame = XYPairs.Y.Length > 0;
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot find value for  + Name +  XProperty:  + XProperty</exception>
        public double Value(int arrayIndex = -1)
        {
            // Shortcut exit when the Y values are all the same. Runs quicker.
            if (YsAreAllTheSame)
                return XYPairs.Y[0];

            
            string PropertyName = XProperty;
            object v = locator.Get(PropertyName);
            if (v == null)
                throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
            double XValue;
            if (v is Array)
                XValue = (double)(v as Array).GetValue(arrayIndex);
            else if (v is IFunction)
                XValue = (v as IFunction).Value(arrayIndex);
            else
                XValue = Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
            return XYPairs.ValueIndexed(XValue);
        }

        /// <summary>Values for x.</summary>
        /// <param name="XValue">The x value.</param>
        /// <returns></returns>
        public double ValueForX(double XValue)
        {
            return XYPairs.ValueIndexed(XValue);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel+1, indent);

                // add graph and table.
                if (XYPairs != null)
                {
                    IVariable xProperty = Apsim.GetVariableObject(this, XProperty);
                    string xName = XProperty;
                    if (xProperty != null && xProperty.UnitsLabel != string.Empty)
                        xName += " " + xProperty.UnitsLabel;

                    tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated as a function of <i>" + StringUtilities.RemoveTrailingString(XProperty, ".Value()") + "</i>", indent));

                    tags.Add(new AutoDocumentation.GraphAndTable(XYPairs, string.Empty, xName, LookForYAxisTitle(this), indent));
                }
            }
        }

        /// <summary>
        /// Return the y axis title.
        /// </summary>
        /// <returns></returns>
        public static string LookForYAxisTitle(IModel model)
        {
            IModel modelContainingLinkField = model.Parent;
            FieldInfo linkField = modelContainingLinkField.GetType().GetField(model.Name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (linkField != null)
            {
                UnitsAttribute units = ReflectionUtilities.GetAttribute(linkField, typeof(UnitsAttribute), true) as UnitsAttribute;
                if (units != null)
                    return model.Name + " (" + units.ToString() + ")";
            }
            return model.Name;
        }

    }

}