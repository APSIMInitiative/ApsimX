---
title: "Science contributions"
draft: false
weight: 200
---

### Documentation

* Documentation is automatically generated from the submitted validation .apsimx file.
* A science document may also be submitted by the model author (e.g. [see AgPasture science document](https://apsimnextgeneration.netlify.app/modeldocumentation/)).
* All equations shall have units on all variables.

### Examples

* Good examples are important in showing the user different ways of configuring and parameterising the model.
* Asingle example file must be submitted by the model author. When it makes sense to do so it should contain:
  * A short memo describing the model and example
  * A simple simulation with good manager scripts that show a typical usage of the model. The report model should show the main outputs from the model. The example should also have one or more  graphs showing model outputs.
  * Additional simulations or experiments should be added that show the model in typical usage scenarios. These may include:
	- a plant model in rotation with another plant model.
	- a plant model that is intercropped with another plant model.
	- a plant model being consumed by a plant consumer. This could be stock, a pest / disease or by using the SimpleGrazing model.

### Validation tests

Model submissions will provide evidence that the model works. This is normally done via validation tests that show predicted with observed data, along with validation statistics. The validation tests need to be accompanied by [memo](/usage/memo) text that describe the experiment and treatments. The validation .apsimx file is also converted to a PDF via auto documentation.

The auto documentation code will walk through all nodes in the .apsimx file, writing any ‘Memo’ and ‘Graph’ models that it finds. For graphs, the menu option ‘Include in auto-documentation?’ (right click on graph) needs to be checked. This allows the model developer to optionally include validation graphs in the PDF and exclude others.

### Sensibility tests

Sensibility tests will be provided to broaden the validation tests into other GxExM scenarios. This is particularly important when the validation is limited in its scope, in particular for GxExM situations that are thought to be important, but where there is no data. Sensibility tests need to be accompanied by [memo](/usage/memo) text that describes what the sensibility plots show and why the results ‘make sense’.

### Move a model to release
Once a model has been approved by the APSIM Initiative it can be moved into the release.

1. Copy the plant model node from the Replacements node in the prototype simulation
![Copy model from replacements](/images/Development.Contribute.MoveModelToRelease.CopyModel.png)
2. Paste this into a text editor and save as a json file into ApsimX\Models\Resources folder.
3. Add the new file into the APSIMX solution. In VisualStudio, use the Solution Explorer tab
to locate Model\Resources in the Models project, right-click on Resources to open a pop-up menu,
select Add>Existing Item. Navigate to the folder where the newly created json file was saved,
select it and click Add. The file should now appear under Model\Resources.
![Add model json as resource](/images/Development.Contribute.MoveModelToRelease.AddModelXmlAsResource.png)
5. Locate it and right-click, select properties and change Build Action to ‘None’.
6. Add a reference to the model as an Apsim resource. For this, open the Resources.resx file
 (locate under Model/Properties). Copy a node from a similar existing model and change the name
 and value to match the new model.  Note that this is case sensitive.
![Add to resX file](/images/Development.Contribute.MoveModelToRelease.AddToResXFile.png)
7. Add reference to the model into the standard toolbox. For this, open ApsimNG\Resources\Toolboxes\StandardToolbox.apsimx
in a text editor. Copy a node of an existing similar model and modify it with references to the new model.
Note that this is also case sensitive.
![Add to standard toolbox](/images/Development.Contribute.MoveModelToRelease.AddToStandardToolbox.png)
8. Add icons for the model. For this, create png images, with the same name as the model, and save in
ApsimNG\Resources\LargeImages (32pixels) and ApsimNG\Resources\TreeViewImages (16 pixels).
These should be added to the solution (as per step 3 and 4 above).
After adding the images, locate them in Solution Explorer, right click on each, select properties
and change Build Action to ‘Embedded Resource’.
9.	Add an example simulation to Examples folder.
10.	Copy the prototype simulation from the Prototype folder into its own folder in ApsimX\Tests\Validation\.
In this simulation, delete the model node in Replacements.
11.	Delete the model’s simulation and folder from the Prototypes folder
12.	Commit all, and create a Pull Request to have all these changes merged into the APSIM repository.
