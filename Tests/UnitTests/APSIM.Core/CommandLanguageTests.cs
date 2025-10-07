using NUnit.Framework;
using System.Linq;

namespace APSIM.Core.Tests;

[TestFixture]
public class CommandLanguageTests
{
    /// <summary>Test the command language can convert commands to/from strings</summary>
    [Test]
    [TestCase("add new Report to [Zone]")]
    [TestCase("add child Report to [Zone] name MyReport")]
    [TestCase("add new Report to all [Zone]")]
    [TestCase("add [Report] from anotherfile.apsimx to all [Zone]")]
    [TestCase("delete [Zone].Report")]
    [TestCase("duplicate [Zone].Report")]
    [TestCase("duplicate [Zone].Report name NewName")]
    [TestCase("save C:\\temp\\test.apsimx")]
    [TestCase("[Simulation].Name=NewName")]
    public void EnsureAddLanguageParsingWorks(string commandString)
    {
        var commands = CommandLanguage.StringToCommands([commandString], relativeTo: null);
        Assert.That(commands.First().ToString(), Is.EqualTo(commandString));
    }
}
