using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>
    /// A test
    /// </summary>
    [Serializable]
    public class Test
    {
        /// <summary>
        /// 
        /// </summary>
        public enum TestType 
        {
            /// <summary>All position</summary>
            AllPos,

            /// <summary>The greater than</summary>
            GreaterThan,

            /// <summary>The less than</summary>
            LessThan,

            /// <summary>The between</summary>
            Between,

            /// <summary>The mean</summary>
            Mean,

            /// <summary>The tolerance</summary>
            Tolerance,

            /// <summary>The equal to</summary>
            EqualTo,

            /// <summary>The compare to input</summary>
            CompareToInput 
        };

        /// <summary>Gets or sets the name of the simulation.</summary>
        /// <value>The name of the simulation.</value>
        public string SimulationName { get; set; }
        /// <summary>Gets or sets the name of the table.</summary>
        /// <value>The name of the table.</value>
        public string TableName { get; set; }
        /// <summary>Gets or sets the type.</summary>
        /// <value>The type.</value>
        public TestType Type { get; set; }
        /// <summary>Gets or sets the column names.</summary>
        /// <value>The column names.</value>
        public string ColumnNames { get; set; }
        /// <summary>Gets or sets the parameters.</summary>
        /// <value>The parameters.</value>
        public string Parameters { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.TestView")]
    [PresenterName("UserInterface.Presenters.TestPresenter")]
    public class Tests : Model
    {
        /// <summary>Gets or sets all tests.</summary>
        /// <value>All tests.</value>
        [XmlElement("Test")]
        public Test[] AllTests { get; set; }
    }
}
