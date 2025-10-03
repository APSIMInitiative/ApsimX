using NUnit.Framework;
using System.Linq;

namespace APSIM.Core.Tests;

[TestFixture]
public class CommandLanguageTests
{
    /// <summary>Ensure the add command converts to/from string</summary>
    [Test]
    [TestCase("add new Report to [Zone]")]
    [TestCase("add child Report to [Zone] name MyReport")]
    [TestCase("add new Report to all [Zone]")]
    [TestCase("add [Report] from anotherfile.apsimx to all [Zone]")]
    public void EnsureAddLanguageParsingWorks(string commandString)
    {
        var commands = CommandLanguage.StringToCommands([commandString], parent: null);
        Assert.That(commands.First().ToString(), Is.EqualTo(commandString));
    }
}
