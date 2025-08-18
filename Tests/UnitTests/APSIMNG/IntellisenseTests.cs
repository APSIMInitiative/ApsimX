using APSIM.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnitTests;
using UserInterface.EventArguments;

namespace APSIM.NG.Tests;

/// <summary>This is a test class for the intellisense functions.</summary>
[TestFixture]
public class IntellisenseTests
{
  /// <summary>Ensure basic intellisense works for models</summary>
  [Test]
  public void EnsureIntellisenseWorksWorksWithModels()
  {
    var clock = new MockClock() { };
    Node.Create(clock);

    var items = NeedContextItemsArgs.ExamineModelForContextItemsV2(clock, "MockClock", true, false, false, false);

    Assert.That(items.Count, Is.EqualTo(14));
    Assert.That(items.Select(i => i.Name), Is.EqualTo(new List<string>
      {"Children", "Enabled", "EndDate", "FractionComplete", "FullPath", "IsHidden", "Name", "Node",
       "NumberOfTicks", "Parent", "ReadOnly", "ResourceName", "StartDate", "Today" }));

    Assert.That(items.Select(i => i.TypeName), Is.EqualTo(new List<string>
      { "List<IModel>", "Boolean", "DateTime", "Double", "String", "Boolean", "String", "Node",
        "Int32", "IModel", "Boolean", "String", "DateTime" ,"DateTime"}));
  }
}
