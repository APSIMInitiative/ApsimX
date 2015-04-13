// -----------------------------------------------------------------------
// <copyright file="NeedContextItemsArgs.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.EventArguments
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using System.Linq;
    using ICSharpCode.NRefactory.CSharp;
    using ICSharpCode.NRefactory;
    using System.Reflection;
using Models.Core;
using System.Text;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The editor view asks the presenter for context items. This structure
    /// is used to do that
    /// </summary>
    public class NeedContextItemsArgs : EventArgs
    {
        /// <summary>
        /// The name of the object that needs context items.
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The items returned from the presenter back to the view
        /// </summary>
        public List<ContextItem> AllItems;

        /// <summary>
        /// Context item information
        /// </summary>
        public List<string> Items;

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        public static List<ContextItem> ExamineTypeForContextItems(Type atype, bool properties, bool methods, bool events)
        {
            List<ContextItem> allItems = new List<ContextItem>();

            // find the properties and methods
            if (atype != null)
            {
                if (properties)
                {
                    foreach (PropertyInfo property in atype.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        VariableProperty var = new VariableProperty(atype, property);
                        NeedContextItemsArgs.ContextItem item = new NeedContextItemsArgs.ContextItem();
                        item.Name = var.Name;
                        item.IsProperty = true;
                        item.IsEvent = false;
                        item.IsWriteable = !var.IsReadOnly;
                        item.TypeName = var.DataType.Name;
                        item.Descr = var.Description;
                        item.Units = var.Units;
                        allItems.Add(item);
                    }
                }

                if (methods)
                {
                    foreach (MethodInfo method in atype.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                        {
                            DescriptionAttribute descriptionAttribute = ReflectionUtilities.GetAttribute(atype, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                            NeedContextItemsArgs.ContextItem item = new NeedContextItemsArgs.ContextItem();
                            item.Name = method.Name;
                            item.IsProperty = false;
                            item.IsEvent = true;
                            item.IsWriteable = false;
                            item.TypeName = method.ReturnType.Name;
                            if (descriptionAttribute != null)
                                item.Descr = descriptionAttribute.ToString();
                            item.Units = string.Empty;

                            // build a parameter string representation
                            ParameterInfo[] allparams = method.GetParameters();
                            StringBuilder paramText = new StringBuilder("( ");
                            if (allparams.Count() > 0)
                            {
                                for (int p = 0; p < allparams.Count(); p++)
                                {
                                    ParameterInfo parameter = allparams[p];
                                    paramText.Append(parameter.ParameterType.Name + " " + parameter.Name);
                                    if (p < allparams.Count() - 1)
                                        paramText.Append(", ");
                                }
                            }
                            paramText.Append(" )");
                            item.ParamString = paramText.ToString();

                            allItems.Add(item);
                        }
                    }
                }

                if (events)
                {
                    foreach (EventInfo evnt in atype.GetEvents(BindingFlags.Instance | BindingFlags.Public))
                    {
                        NeedContextItemsArgs.ContextItem item = new NeedContextItemsArgs.ContextItem();
                        item.Name = evnt.Name;
                        item.IsProperty = true;
                        item.IsEvent = true;
                        item.IsWriteable = false;
                        item.TypeName = evnt.ReflectedType.Name;
                        item.Descr = "";
                        item.Units = "";
                        allItems.Add(item);
                    }
                }
            }

            allItems.Sort(delegate(ContextItem c1, ContextItem c2) { return c1.Name.CompareTo(c2.Name); });
            return allItems;
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        public static List<ContextItem> ExamineObjectForContextItems(object o, bool properties, bool methods, bool events)
        {
            List<ContextItem> allItems = ExamineTypeForContextItems(o.GetType(), properties, methods, events);

            // add in the child models.
            if (o != null && o is IModel)
            {
                foreach (IModel model in (o as IModel).Children)
                {
                    if (allItems.Find(m => m.Name == model.Name) == null)
                    {
                        NeedContextItemsArgs.ContextItem item = new NeedContextItemsArgs.ContextItem();
                        item.Name = model.Name;
                        item.IsProperty = false;
                        item.IsEvent = false;
                        item.IsWriteable = false;
                        item.TypeName = model.GetType().Name;
                        item.Units = string.Empty;
                        allItems.Add(item);
                    }
                }

                allItems.Sort(delegate(ContextItem c1, ContextItem c2) { return c1.Name.CompareTo(c2.Name); });
            }
            return allItems;
        }


        /// <summary>
        /// Complete context item information
        /// </summary>
        public class ContextItem
        {
            /// <summary>
            /// Name of the item
            /// </summary>
            public string Name;

            /// <summary>
            /// The return type as a string
            /// </summary>
            public string TypeName;

            /// <summary>
            /// Units string
            /// </summary>
            public string Units;

            /// <summary>
            /// The description string
            /// </summary>
            public string Descr;

            /// <summary>
            /// This is an event/method
            /// </summary>
            public bool IsEvent;

            /// <summary>
            /// String that represents the parameter list
            /// </summary>
            public string ParamString;

            /// <summary>
            /// This is a property
            /// </summary>
            public bool IsProperty;

            /// <summary>
            /// This property is writeable
            /// </summary>
            public bool IsWriteable;

            /// <summary>
            /// The property is a child model.
            /// </summary>
            public bool IsChildModel;
        }
    } 
}
