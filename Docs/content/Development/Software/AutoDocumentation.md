---
title: "Auto documentation"
draft: false
---

The APSIM infrastructure has the ability to create a PDF file by examining the validation .apsimx file and the source code. The .apsimx file needs to have the same name as the .apsimx file (e.g. Maize.apsimx for the Maize model). To generate the document, the user can right click on the top level node (Simulations) in the user interface and select 'Create documentation'. 

## Model design to support auto documentation

To enable auto-documentation, every model must have a 'Document' method with this signature:

```c#
/// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
/// <param name="tags">The list of tags to add to.</param>
/// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
/// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent);
```

If a model is derived from 'Model', it can rely on the base class to provide a default implementation of a documentation method. in a lot of cases, this default implementation will be good enough to provide basic documentation. It writes a heading to the PDF, the text inside the \<summary\> tags for the class and then proceeds to call 'Document' in all children.

To provide specialised functionality, a model can have it's own Document method. For example, LinearInterpolation.cs has this method:

```c#
/// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
/// <param name="tags">The list of tags to add to.</param>
/// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
/// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
{
    // add a heading.
    tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
 
    // write memos.
    foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
        memo.Document(tags, -1, indent);
 
    // add graph and table.
    if (XYPairs != null)
    {
        IVariable xProperty = Apsim.GetVariableObject(this, XProperty);
        string xName = XProperty;
        if (xProperty != null && xProperty.Units != string.Empty)
            xName += " (" + xProperty.Units + ")";
 
        tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated as a function of <i>" + xName + "</i>", indent));
 
        tags.Add(new AutoDocumentation.GraphAndTable(XYPairs, string.Empty, xName, Name, indent));
    }
}
```

This method adds a heading, writes any child memos and then adds a bit of text in italics (using HTML) and finally adds a graph and a table.

The first argument to the method is a list of 'tags' that can be added to. These tags are used to create the PDF. The AutoDocumentation class has various tags that can be created (Heading, Paragraph, GraphAndTable). It also has some static helper methods to get the units and description of a declaration and the description of a class:

```c#
/// <summary>Gets the units from a declaraion.</summary>
/// <param name="model">The model containing the declaration field.</param>
/// <param name="fieldName">The declaration field name.</param>
/// <returns>The units (no brackets) or any empty string.</returns>
public static string GetUnits(IModel model, string fieldName);
 
/// <summary>Gets the description from a declaraion.</summary>
/// <param name="model">The model containing the declaration field.</param>
/// <param name="fieldName">The declaration field name.</param>
/// <returns>The description or any empty string.</returns>
public static string GetDescription(IModel model, string fieldName);
 
/// <summary>Writes the description of a class to the tags.</summary>
/// <param name="model">The model to get documentation for.</param>
/// <param name="tags">The tags to add to.</param>
/// <param name="indent">The indentation level.</param>
public static void GetClassDescription(object model, List<ITag> tags, int indent);
```

## Examining the .apsimx file

The auto documentation code will also walk through all nodes in the .apsimx file, writing any 'Memo' and 'Graph' models that it finds. For graphs, the menu option 'Include in auto-documentation?' (right click on graph) needs to be checked. This allows the model developer to optionally include graphs in the PDF and exclude others.

