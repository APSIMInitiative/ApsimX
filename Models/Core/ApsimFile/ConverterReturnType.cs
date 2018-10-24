namespace Models.Core.ApsimFile
{
    using Newtonsoft.Json.Linq;

    /// <summary>A class for holding return values from Converter.DoConvert method.</summary>
    public class ConverterReturnType
    {
        /// <summary>The JSON root node ready to be deserialised.</summary>
        public JObject Root { get; set; }

        /// <summary>Set to true the converter did something.</summary>
        public bool DidConvert { get; set; }
    }
}
