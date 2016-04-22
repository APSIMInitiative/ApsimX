using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>FIXME: This can be generalised to a IF function</summary>
    [Serializable]
    [Description("If the ValueToTest.Value is less than the LessThanCriteria.Value returns ValueIfTrue.Value else returns ValueIfFalse.Value")]
    public class LessThanFunction : Model, IFunction
    {
        /// <summary>The value to test if less that criteria</summary>
        [Link]
        IFunction ValueToTest = null;
        /// <summary>The Criteria</summary>
        [Link]
        IFunction LessThanCriteria = null;
        /// <summary>The value to return if test value is less than criteria</summary>
        [Link]
        IFunction ValueIfTrue = null;
        /// <summary>The value to return if test value is not less than criteria</summary>
        [Link]
        IFunction ValueIfFalse = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ValueToTest.Value < LessThanCriteria.Value)
                    return ValueIfTrue.Value;
                else
                    return ValueIfFalse.Value;
            }
        }

    }
}