using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// Used to store completion information about a method.
    /// </summary>
    class MethodCompletion
    {
        /// <summary>
        /// The method's signature - e.g.
        /// void Parse(string text, bool recurse = true)
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// The text inside the method's summary documentation tag.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// The parameter documentation stored in the parameter documenation tags.
        /// This should contain newline-delimited documentation for all parameters,
        /// stored in a single string.
        /// </summary>
        public string ParameterDocumentation { get; set; }
    }
}
