using System.Collections.Generic;
using APSIM.Shared.Utilities;
using NUnit.Framework;

namespace UnitTests.APSIMShared;

/// <summary>
/// Tests for Macro class.
/// </summary>
[TestFixture]
public class MacroTests
{
    [Test]
    [TestCase("Some text$key_1.value", ExpectedResult = "Some textvalue1.value")]
    [TestCase("[Soil]=SoilLibrary.apsimx;[$key_1]", ExpectedResult = "[Soil]=SoilLibrary.apsimx;[value1]")]
    [TestCase("[$key-2].FileName=$key-2.met", ExpectedResult = "[value2].FileName=value2.met")]
    [TestCase("$key3", ExpectedResult = "value3")]
    [TestCase("$key_1,$key-2,$key3", ExpectedResult = "value1,value2,value3")]
    public string TestMacroReplacement(string testCase)
    {
        Dictionary<string, string> values = new()
        {
            { "key_1", "value1" },
            { "key-2", "value2" },
            { "key3", "value3" }
        };
        return Macro.Replace(testCase, values);
    }
}