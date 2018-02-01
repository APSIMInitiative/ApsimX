---
title: "3. Attributes"
draft: false
---

1. **[Link(IsOptional=true)]**: Applies to class fields. When applied to a field, APSIM will locate an object  of the specified type and store a reference to it in the field. Will throw an exception if not found. When IsOptional = true, APSIM will not throw an exception when an object cannot be found.
2. **[Units(string UnitString)]**: Specifies the units of the related field or property. Units are reported in the output tables and on graph axes.
3. **[Bounds(Lower=value, Upper= value)]**: Specifies the lower and upper bounds of the related field or property. Currently APSIM doesn't use these bounds.
4. **[PresenterName(string ClassName)]**: When applied to the model class, this attribute specifies the name of the User Interface presenter class that should be used when showing the model in the ‘Right hand panel’. Class names need to include the namespace.
5. **[ViewName(string ClassName)]**: When applied to the model class, this attribute specifies the name of the User Interface view class that should be used when showing the model in the ‘Right hand panel’. Class names need to include the namespace.
6. **[Description(string Text)]**: Provides a description of the associated class, field or property. The user interface PropertyPresenter and ProfilePresenter classes uses these to display a description in their grid controls.
7. **[EventSubscribe(string eventName)]**: Indicates the method is an event handler and that the APSIM framework should call the method whenever an event of the specified name is published by a model in scope.
8. **[ValidParent(Type model)]**: When added to a model, it specifies that the model can only be a child of the specified parent model. The ExplorerView looks for these attributes to determine whether a model can be dragged and dropped (or copied and pasted) onto another model.
9. **[Display(Format="F2", ShowTotal=true, DisplayType="TableName")**: Used by the ProfileView to determine the display format (e.g. "N3") or whether a total should be shown at the top of the column.
