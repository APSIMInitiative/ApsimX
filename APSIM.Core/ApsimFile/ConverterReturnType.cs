﻿using System.Xml;
using Newtonsoft.Json.Linq;

namespace APSIM.Core;

/// <summary>A class for holding return values from Converter.DoConvert method.</summary>
internal class ConverterReturnType
{
    /// <summary>The JSON root node ready to be deserialised.</summary>
    public JObject Root { get; set; }

    /// <summary>The XML root node ready to be deserialised.</summary>
    public XmlDocument RootXml { get; set; }

    /// <summary>Set to true the converter did something.</summary>
    public bool DidConvert { get; set; }

    /// <summary>A model being converted</summary>
    public object NewModel { get; set; }


}
