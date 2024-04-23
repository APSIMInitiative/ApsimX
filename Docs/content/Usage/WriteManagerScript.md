---
title: "Write Manager Scripts"
draft: false
---

Writing manager scripts is for advanced users. **It is always much easier modifying an existing script that starting one from scratch.**. C# is a case sensitive language. Upper and lower case matters.

For this tutorial, we will be using the 'Fertilise on fixed dates (advanced version)' manager script in the management toolbox. There are a number of input parameters for this manager script:

![Properties](/images/Usage.ManagerScript.Parameters.png)  

The parameters for this script show a range of types (checkboxes, drop down, string array etc)

**Structure of a manager script.**

![Structure](/images/Usage.ManagerScript.ScriptStructure.png)  

**namespaces**

A namespace is a collection of classes. In the screenshot above, the *Models* namespace contains one class called *Script*. All c# classes must live in a namespace. APSIM manager scripts must be in a *Models* namespace.

**used namespaces**

C# manager scripts will need to reference other namespaces to be able to use classes in those 
namespaces. For example, the *Models.Core* namespace contains many APSIM definitions like *Link*, *Clock*, *Weather*. It is best to copy lists of namespaces from other scripts.

**class**

A class is collection of fields, properties, event handlers and methods. Classes encapsulate all the manager script functionality i.e. sowing on fixed dates in this example. Classes in manager scripts should always be 'public' i.e. callable from other models in APSIM. They should also derive from Model i.e. have a *: Model* at the end of the class line.

**links**

A link defines a dependancy on another model in APSIM. If your manager script needs the value of a variable from another model then you will need to add a link to that model. An example is getting the current simulation date from the clock model. Another example would be getting the maximum temperature from the weather model. The format for links is:

![Link](/images/Usage.ManagerScript.ScriptStructure.Link.png)

*Model type* is the class name of the model. *Variable name* is the name of the variable that clock will be known as in the manager script. For PMF models, the *Model type* will be *Plant* while the *Variable name* could be *wheat*. For AgPasture, the *Model type* will be *PastureSpecies* and the *Variable name* could be *AGPRyegrass*. If there is a model in the simulation tree, then a manager script can link to it. If you hover the mouse over the model in the simulation tree, the tool tip will show the *Model type*. The name of the model in the simulation tree will be the *Variable name*.

**fields**

Quite often a manager script will need to define fields (variables) that are private to the class i.e. cannot be accessed by another model in the simulation. The format for private fields is:

![Link](/images/Usage.ManagerScript.ScriptStructure.Field.png)

*Data type* is the type of variable e.g. 

* int: an integer variable with no decimal places,
* double: a variable with decimal places,
* string: a variable that can contain characters, 
* DateTime: a variable that can contain a date and/or time,

These data types can also be arrays i.e. by appending *[]* to the end of the type e.g. *double[]* for an array of double. *Variable name* is the name that the variable will be known as in the manager script.

**properties**

Properties are often used as placeholders for mapping of values from the user-input *parameters* tab and for publically available outputs that can be reported from a script.

![Link](/images/Usage.ManagerScript.ScriptStructure.Property.png)

* They are formatted the same as for field but with *{get; set;}* appended to them. The same data types are supported. 
* If they are to be used for user-input parameters then they need to have a *[Description]* attribute, the text of which appears in the *Parameters* tab. 
* They can optionally have a *[Separator]* attribute to define a visual separator on the *Parameters* tab.
* If the *DataType* is an enum then a drop down list will be shown to the user. For example defining ```
enum DropDownMembers { A, B, C, D }
``` and then using this DataType in the property will cause the user interface to show a drop down list of A, B, C, D.
* They can optionally have a *[Display]* attribute that instructs the user interface to show format the user-input cell in different ways. Examples of using this attribute:
    * [Display(Type=DisplayType.TableName)]: The user interface will show a drop down list of tables in the datastore.
	* [Display(Type=DisplayType.FieldName)]: Ther user interface will show a drop down list of fields (columns) in the table specified by a table property in the same script.
	* [Display(Type=DisplayType.FileName)]: The user interface will show a browse button to let user select a file.
	* [Display(Type=DisplayType.CultivarName,Plant="Wheat")]: The user interface will show a drop down list of cultivars for the specified plant model.
	* [Display(Type=DisplayType.ResidueName)]: The user interface will show a drop down list of residue types from the surface organic matter modle.
	* [Display(Type=DisplayType.Model)]: The user interface will show a drop down list of all models in scope.
	* [Display(Type=DisplayType.Model,ModelType=typeof(Plant))]: The user interface will show a drop down list of all plant model in scope.
	

**methods**

Methods (functions) are where lines of code are put to perform calculations.

![Link](/images/Usage.ManagerScript.ScriptStructure.Method.png)

Methods can return the value of a single variable. The data type of the return variable is specified by *Return type* in the image above. It can be void, meaning the method returns nothing, or the method can return a variable of one of the data types listed for fields above. Arguments of the method are varaibles that are passed into the method to be used by the method. They are comma separated ``` DataType VariableName ``` format similar to fields. Most methods in manager scripts are private i.e. are not callable from other models in APSIM. They can be declared public if necssary.

If a method has a *[EventSubscribe]* attribute, like the image above, then APSIM (usually the clock model) will call this method when the event specified is invoked. In the example above, when the clock model publises a *StartOfSimulation* event at the beginning of each simulation run, this method will be called. This is a good place for performing script initialisation. Clock also publishes *DoManagement* at the beginning of each day (before other models do their daily calculations) and *DoManagementCalculations* at the end of each day (after all other models have completed there calculations). Many manager scripts subscribe to these events.

