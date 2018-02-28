// -----------------------------------------------------------------------
// <copyright file="LinearInterpolationFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Linear interpolation function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A value is returned via linear interpolation of a given set of XY pairs")]
    public class LinearInterpolationFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The ys are all the same</summary>
        private bool YsAreAllTheSame = false;

        /// <summary>The xy pairs.</summary>
        [ChildLink]
        private XYPairs xys = null;

        /// <summary>A locator object</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>The x property</summary>
        [Description("XProperty")]
        [Display(DisplayType = DisplayAttribute.DisplayTypeEnum.CultivarName)]
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
            xys = new XYPairs();
            xys.X = x;
            xys.Y = y;
        }

        /// <summary>Called when [loaded].</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded(object sender, LoadedEventArgs args)
        {
            if (xys != null)
            {
                for (int i = 1; i < xys.Y.Length; i++)
                    if (xys.Y[i] != xys.Y[i - 1])
                    {
                        YsAreAllTheSame = false;
                        return;
                    }

                // If we get this far then the Y values must all be the same.
                YsAreAllTheSame = xys.Y.Length > 0;
            }
        }

        /// <summary>Gets the value, either a double or a double[]</summary>
        public override double[] Values()
        {
            // Shortcut exit when the Y values are all the same. Runs quicker.
            if (YsAreAllTheSame)
                return xys.Y;

            object v = locator.Get(XProperty);
            if (v == null)
                throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
            if (v is IFunction)
                v = (v as IFunction).Values();

            if (v is double[])
            {
                double[] array = v as double[];
                for (int i = 0; i < array.Length; i++)
                    array[i] = xys.ValueIndexed(array[i]);
                return array;
            }
            else if (v is double)
                return new double[] { xys.ValueIndexed((double)v) };
            else
                throw new Exception("Cannot do a linear interpolation on type: " + v.GetType().Name + 
                                    " in function: " + Name);
        }

        /// <summary>Values for x.</summary>
        /// <param name="XValue">The x value.</param>
        /// <returns></returns>
        public double ValueForX(double XValue)
        {
            return xys.ValueIndexed(XValue);
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
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                // add graph and table.
                if (xys != null)
                {
                    IVariable xProperty = Apsim.GetVariableObject(this, XProperty);
                    string xName = XProperty;
                    if (xProperty != null && xProperty.UnitsLabel != string.Empty)
                        xName += " " + xProperty.UnitsLabel;

                    tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated as a function of <i>" + StringUtilities.RemoveTrailingString(XProperty, ".Value()") + "</i>", indent));

                    tags.Add(new AutoDocumentation.GraphAndTable(xys, string.Empty, xName, LookForYAxisTitle(this), indent));
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