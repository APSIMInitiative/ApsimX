using System.Reflection;
using Models.Core;
using Models.Utilities;
using NUnit.Framework;
using UserInterface.Classes;

namespace UnitTests.UtilityTests;

[TestFixture]
public class AttributeUtilitiesTests
{
    private enum TestEnum
    {
        [Description("First option")]
        First,
        Second
    }

    private class InvalidEnumHolder
    {
        public TestEnum Value { get; set; } = (TestEnum)999;
    }

    [Test]
    public void GetEnumDescriptionReturnsDescriptionForDefinedEnumValue()
    {
        Assert.That(AttributeUtilities.GetEnumDescription(TestEnum.First), Is.EqualTo("First option"));
    }

    [Test]
    public void GetEnumDescriptionReturnsEmptyStringForUndefinedEnumValue()
    {
        Assert.That(AttributeUtilities.GetEnumDescription((TestEnum)999), Is.EqualTo(string.Empty));
    }

    [Test]
    public void PropertyHandlesUndefinedEnumValue()
    {
        var holder = new InvalidEnumHolder();
        PropertyInfo metadata = holder.GetType().GetProperty(nameof(InvalidEnumHolder.Value));

        var property = new Property(holder, metadata);

        Assert.That(property.Value, Is.EqualTo(string.Empty));
    }
}
