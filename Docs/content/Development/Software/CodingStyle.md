---
title: "Coding Style"
draft: false
---

To ensure ease of maintenance and development, it is important that a consistent design is used when implementing models in APSIM Next Generation.  To achieve this, APSIM follows many of the [Microsoft C Sharp code conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [C Sharp coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) with some variations for code that has been ported from others languages such as Fortran. If you intend on contributing code we recommend following these guides and conventions.

# Naming variables

All source code should be written using the Microsoft naming conventions.

In particular please note these guidelines:

When creating a public identifier, capitalize the first letter of each word and do not use underscores using the PascalCasing format. The PascalCasing convention, capitalizes the first character of each word (including acronyms over two letters in length), as shown in the following examples:

```c#
PropertyDescriptor
HtmlTag
```

A special case is made for two-letter acronyms in which both letters are capitalized, as shown in the following identifier:

```c#
IOStream
```

When creating private identifiers, the camelCasing convention should be used, which capitalizes the first character of each word except the first word, as shown in the following examples. We also append an underscore to the start in order to signal it is local to the class. As the example also shows, two-letter acronyms that begin a camel-cased identifier are both lowercase.

```c#
_propertyDescriptor
_ioStream
_htmlTag
```

Example:

```c#
public class ModelA : Model
{
    private double _aVariable;            // camelCase naming convention for private field
    public double BVariable { get; }    // PascalCase naming convention for properties

    public void AMethod(double argument)   // camelCase the argument.
    {
		// Do something
        _aVariable = argument
    }
}
```

We also use the Visual Studio default brace indention and tab settings (4 spaces). Line endings should be LF.

Instance variables should be named differently to the type/object names. e.g.

```c#
public Soil Soil;    // BAD - A class link like this should be private

private Soil soil;    // BAD - does not follow private naming scheme

private Soil _soil;    // GOOD

[Link]
private Soil _soil;    // BEST - if property is actually a reference to another model, a Link tag should be used
```

# Region Usage

Do not use #region.

# Inheritance

Try and avoid inheritance. For a software engineering view on this see: https://codingdelight.com/2014/01/16/favor-composition-over-inheritance-part-1/. There is general consensus that inheriting from an interface is GOOD but inheriting from a base class is BAD. 

There are always exceptions of course. We've been migrating away from having a BaseOrgan and instead put code that is common across organs in a 'library' or 'utility' somewhere and simply call methods in that library. An example of this is in the way GenericOrgan relies on a [Link] BiomassRemovalModel  to remove biomass from the organ. A simpler way would be to create a class called say PMFLibrary and put in static methods e.g. 'RemoveBiomass' ...

# Code Formatting

Use [Allman-style](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) braces, where curly braces start on a new line. A single line statement block should go without braces but must still be indented on its own line. e.g.

```c#
if (x)
{
	foo();
	bar();
}
else
	baz();
```

# Order of declarations in a C# class

## 1. Links

Links should be first in a class declaration. They specify the dependencies to other models in the simulation. They should be private c# fields and be preceded with Link attributes. ##Optional links should be avoided##. They obscure from the developer what the defaults are when a model is missing. Better to be explicit e.g. define a non optional function called Photosynthesis in GenericOrgan. If an organ doesn't photosynthesise then the developer has to think about it and add a constant function called Photosynthesis and set a value of 0. The auto-documentation will then show that an organ doesn't photosynthesise which is nice.

```c#
	/// <summary>The parent plant</summary>
	[Link]
	private Plant _parentPlant = null;

	/// <summary>The surface organic matter model</summary>
	[Link]
	private ISurfaceOrganicMatter _surfaceOrganicMatter = null;
```

For links to functions that must be child functions use:

```c#
	/// <summary>The senescence rate function</summary>
    [ChildLinkByName]
    [Units("/d")]
    private IFunction senescenceRate = null;

    /// <summary>The detachment rate function</summary>
    [ChildLinkByName]
    [Units("/d")]
    private IFunction detachmentRateFunction = null;
```

This will avoid the situation where the same named function exists in different places.

## 2. Private and protected fields

Private and protected fields and enums are next:

```c#
	/// <summary>The dry matter supply</summary>
	private BiomassSupplyType _dryMatterSupply = new BiomassSupplyType();

	/// <summary>The nitrogen supply</summary>
	private BiomassSupplyType _nitrogenSupply = new BiomassSupplyType();
```

## 3. The constructor

The constructor (if any) comes next:

```c#
	/// <summary>Constructor</summary>
	public GenericOrgan()
	{
		Live = new Biomass();
		Dead = new Biomass();
	}
```

## 4. Public events and enums

If a class needs to define public events (that other models can subscribe to) or enums then they come after the constructor.

```c#
	/// <summary>Occurs when a plant is about to be sown.</summary>
	public event EventHandler Sowing;
```

## 5. Public properties

Public properties (outputs) are next and **should have a trivial implementation**.

```c#
	/// <summary>The live biomass</summary>
	[XmlIgnore]
	public Biomass Live { get; private set; }

	/// <summary>The dead biomass</summary>
	[XmlIgnore]
	public Biomass Dead { get; private set; }

	/// <summary>Gets the DM demand for this computation round.</summary>
	[XmlIgnore]
	public BiomassPoolType DMDemand { get { return _dryMatterDemand; } }
```

## 6. Public methods

Public methods come next. These will be callable from other models including manager scripts.

```c#
	/// <summary>Calculate and return the dry matter supply (g/m2)</summary>
	public virtual BiomassSupplyType CalculateDryMatterSupply()
	{
	}
```

## 7. Private and protected methods

Private and protected methods come last. This includes all APSIM event handlers (which should always be private or protected)

```c#
	/// <summary>Called when crop is ending</summary>
	/// <param name="sender">The sender.</param>
	/// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
	[EventSubscribe("PlantSowing")]
	private void OnPlantSowing(object sender, SowPlant2Type data)
	{
	}
```