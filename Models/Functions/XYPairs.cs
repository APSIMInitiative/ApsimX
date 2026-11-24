using System;
using System.Collections.Generic;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// This function is calculated from an XY matrix which returns a value for Y
    /// interpolated from the Xvalue provided.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.QuadView")]
    [PresenterName("UserInterface.Presenters.QuadPresenter")]
    [Description("Returns the corresponding Y value for a given X value, based on the line shape defined by the specified XY matrix.")]
    public class XYPairs : Model, IFunction, IIndexedFunction
    {
        private double[] _x;

        private double[] _y;

        /// <summary>
        /// Is set when X or Y arrays are changed, shortcut if all y values are equal
        /// </summary>
        private bool _allEqual = false;
        private double _allEqualValue = double.NaN;

        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        [Display]
        public double[] X 
        { 
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        [Display]
        public double[] Y
        {
            get 
            {
                return _y;
            }
            set 
            {
                _y = value;
                CheckIfAllYAreTheSame();
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value on XYPairs function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            // Shortcut exit when the Y values are all the same. Runs quicker.
            if (_allEqual)
                return _allEqualValue;

            try
            {
                return MathUtilities.LinearInterpReal(dX, X, Y, out bool DidInterpolate);
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to evaluate linear interpolation function {FullPath}", err);
            }
        }

        /// <summary>Values the indexed.</summary>
        public string GetXName()
        {
            if (Parent != null)
            {
                List<IModel> siblings = Parent.Children;
                foreach(IModel child in siblings)
                    if (child is VariableReference)
                        return (child as VariableReference).VariableName;
            }
            return "X";
        }

        /// <summary>
        /// Return the y axis title.
        /// </summary>
        /// <returns>The axis title</returns>
        public string GetYName()
        {
            if (Parent != null)
                return Parent.Name;
            else
                return "Y";
        }

        /// <summary>
        /// Check if all Y values are the same
        /// Will return false if y is null or empty or if values differ
        /// </summary>
        private void CheckIfAllYAreTheSame()
        {
            //reset checks
            _allEqual = false;
            _allEqualValue = double.NaN;

            //quit earlier if no Ys
            if (Y == null || Y.Length == 0)
                return;
            
            //check Ys
            _allEqual = true;
            for (int i = 1; i < Y.Length; i++)
                if (Y[i] != Y[i - 1])
                    _allEqual = false;
            
            //set equal value if all Ys are equal
            if (_allEqual)
                _allEqualValue = Y[0];
        }
    }
}
