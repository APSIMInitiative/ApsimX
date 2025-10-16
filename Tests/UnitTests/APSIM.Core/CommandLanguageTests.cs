﻿using NUnit.Framework;
using System;
using System.Linq;

namespace APSIM.Core.Tests;

[TestFixture]
public class CommandLanguageTests
{
    /// <summary>Test the command language can convert commands to/from strings</summary>
    [Test]
    [TestCase("add new Report to [Zone]")]
    [TestCase("add new Report to all [Zone]")]
    [TestCase("add [Report] to [Zone] name MyReport")]
    [TestCase("add [Report] from anotherfile.apsimx to all [Zone]")]
    [TestCase("delete [Zone].Report")]
    [TestCase("duplicate [Zone].Report")]
    [TestCase("duplicate [Zone].Report name NewName")]
    [TestCase("save C:\\temp\\test.apsimx")]
    [TestCase("load C:\\temp\\test.apsimx")]
    [TestCase("[Simulation].Name=NewName")]
    [TestCase("[Simulation].Name=<test.txt")]
    [TestCase("[Cultivar].Commands-=NewName")]
    [TestCase("[Cultivar].Commands+=NewName")]
    public void EnsureAddLanguageParsingWorks(string commandString)
    {
        var commands = CommandLanguage.StringToCommands([commandString], relativeTo: null, relativeToDirectory: null);
        Assert.That(commands.First().ToString(), Is.EqualTo(commandString));
    }

    // Ensure commented lines are ignored.
    [Test]
    [TestCase("# Commented line")]
    public void EnsureCommentedLinesAreIgnored(string commandString)
    {
        var commands = CommandLanguage.StringToCommands([commandString], relativeTo: null, relativeToDirectory: null);
        Assert.That(commands.Any(), Is.False);
    }

    /// <summary>Test that invalid commands throw.</summary>
    [Test]
    [TestCase("add Report to", ExpectedResult = "Invalid command: add Report to")]
    [TestCase("add new Report", ExpectedResult = "Invalid command: add new Report")]
    [TestCase("add new Report to [Simulation] name", ExpectedResult = "Invalid command: add new Report to [Simulation] name")]
    [TestCase("delete", ExpectedResult = "Invalid command: delete")]
    [TestCase("duplicate", ExpectedResult = "Invalid command: duplicate")]
    [TestCase("save", ExpectedResult = "Invalid command: save")]
    [TestCase("load", ExpectedResult = "Invalid command: load")]
    [TestCase("[Simulation].Filename=", ExpectedResult = "Invalid command: [Simulation].Filename=")]
    [TestCase("[Simulation].Filename", ExpectedResult = "Unknown command: [Simulation].Filename")]
    [TestCase("[Simulation].Filename=<", ExpectedResult = "Invalid command: [Simulation].Filename=<")]
    public string EnsureInvalidCommandsThrow(string commandString)
    {
        try
        {
            var commands = CommandLanguage.StringToCommands([commandString], relativeTo: null, relativeToDirectory: null);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        return null;
    }
}
