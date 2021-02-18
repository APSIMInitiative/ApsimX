using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// A class to hold data for a completion option representing an entity.
    /// </summary>
    public class EntityCompletionData : CompletionData, IEntityCompletionData
    {
        /// <summary>
        /// The entity.
        /// </summary>
        private readonly IEntity entity;

        /// <summary>
        /// Description text of the entity.
        /// </summary>
        private string description;

        /// <summary>
        /// Generates tooltips using C# syntax.
        /// </summary>
        private static readonly CSharpAmbience CsharpAmbience = new CSharpAmbience();

        /// <summary>
        /// Gets or sets the entity which this completion data represents.
        /// </summary>
        public IEntity Entity
        {
            get { return entity; }
        }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="entity">The entity. Cannot be null.</param>
        public EntityCompletionData(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            this.entity = entity;
            IAmbience ambience = new CSharpAmbience();
            ambience.ConversionFlags = entity is ITypeDefinition ? ConversionFlags.ShowTypeParameterList : ConversionFlags.None;
            DisplayText = entity.Name;
            CompletionText = ambience.ConvertSymbol(entity);
            ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
            if (entity is ITypeDefinition)
            {
                // Show fully qualified Type name
                ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedTypeNames;
            }
            Image = CompletionImage.GetImage(entity);
            Units = entity.GetAttribute(new FullTypeName(typeof(Models.Core.UnitsAttribute).FullName))?.PositionalArguments?.First()?.ConstantValue.ToString();
            ReturnType = GetReturnType();
        }

        /// <summary>
        /// Gets or sets the entity's description.
        /// </summary>
        public override string Description
        {
            get
            {
                if (description == null)
                {
                    description = GetText(Entity);
                    if (HasOverloads)
                    {
                        description += " (+" + OverloadedData.Count() + " overloads)";
                    }
                    description = /*.NewLine + */XmlDocumentationToText(Entity.Documentation);
                }
                return description;
            }
            set
            {
                description = value;
            }
        }

        /// <summary>
        /// Generates an entity's description from its XML documentation.
        /// </summary>
        /// <param name="xmlDoc">XML documentation of the entity.</param>
        /// <returns>String containing the entity's description.</returns>
        public static string XmlDocumentationToText(string xmlDoc)
        {
            //.Diagnostics.Debug.WriteLine(xmlDoc);
            StringBuilder b = new StringBuilder();
            try
            {
                using (XmlTextReader reader = new XmlTextReader(new StringReader("<root>" + xmlDoc + "</root>")))
                {
                    reader.XmlResolver = null;
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Text:
                                b.Append(reader.Value);
                                break;
                            case XmlNodeType.Element:
                                switch (reader.Name)
                                {
                                    case "filterpriority":
                                        reader.Skip();
                                        break;
                                    case "returns":
                                        b.AppendLine();
                                        b.Append("Returns: ");
                                        break;
                                    case "param":
                                        b.AppendLine();
                                        b.Append(reader.GetAttribute("name") + ": ");
                                        break;
                                    case "remarks":
                                        b.AppendLine();
                                        b.Append("Remarks: ");
                                        break;
                                    case "see":
                                        if (reader.IsEmptyElement)
                                        {
                                            b.Append(reader.GetAttribute("cref"));
                                        }
                                        else
                                        {
                                            reader.MoveToContent();
                                            if (reader.HasValue)
                                            {
                                                b.Append(reader.Value);
                                            }
                                            else
                                            {
                                                b.Append(reader.GetAttribute("cref"));
                                            }
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }
                return b.ToString();
            }
            catch (XmlException)
            {
                return xmlDoc;
            }
        }

        /// <summary>
        /// Gets the return type of an entity.
        /// </summary>
        /// <param name="withNamespace">If true, the namespace of the type will be included.</param>
        /// <returns>Type (and possibly namespace) of the return type, as a string.</returns>
        public string GetReturnType(bool withNamespace = false)
        {
            if (entity == null)
                return null;

            // The problem is that a lot - but not all - of the entity types defined by NRefactory (e.g. AbstractResolvedMember, 
            // ReducedExtensionMethod, etc) contain a property called ReturnType. If it exists, this property is exactly what we want.
            // This property is not inherited from a base class or interface, so the best way to check if it exists is via reflection.
            try
            {
                System.Reflection.PropertyInfo returnType = entity.GetType().GetProperty("ReturnType");
                if (returnType != null)
                {
                    IType returnTypeValue = returnType.GetValue(entity) as IType;
                    if (returnTypeValue != null)
                        return GetTypeName(returnTypeValue, withNamespace);
                }
            }
            catch { }
            
            if (entity is DefaultResolvedTypeDefinition)
            {
                DefaultResolvedTypeDefinition resolvedMember = this.entity as DefaultResolvedTypeDefinition;
                return withNamespace ? resolvedMember?.Kind.ToString() ?? "" : resolvedMember?.Kind.ToString() ?? "";
            }
            return "Unknown";
        }

        private static string GetTypeName(IType type, bool withNamespace = false)
        {
            string name = withNamespace ? type.FullName : type.Name;
            if (type.TypeParameterCount > 0)
            {
                string[] typeArgNames = type.TypeArguments.Select(t => GetTypeName(t)).ToArray();
                name += $"<{string.Join(", ", typeArgNames)}>";
            }
            return name;
        }

        /// <summary>
        /// Converts a member to text.
        /// Returns the declaration of the member as C# or VB code, e.g.
        /// "public void MemberName(string parameter)"
        /// </summary>
        private static string GetText(IEntity entity)
        {
            IAmbience ambience = CsharpAmbience;
            ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
            if (entity is ITypeDefinition)
            {
                // Show fully qualified Type name
                ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedTypeNames;
            }
            if (entity is IMethod)
            {
                //if the method is an extension method we wanna see the whole method for the description
                //the original method (not reduced) can be obtained by calling ReducedFrom
                var reducedFromMethod = ((IMethod)entity).ReducedFrom;
                if (reducedFromMethod != null)
                    entity = reducedFromMethod;
            }
            return ambience.ConvertSymbol(entity);
        }
    }
}
