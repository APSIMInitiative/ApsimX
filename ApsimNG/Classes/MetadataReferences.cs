#nullable enable
namespace UserInterface.Intellisense
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    
    /// <summary>
    /// Metadata references used to create test projects.
    /// </summary>
    /// <remarks>
    /// It appears that, due to a bug in roslyn, XML
    /// documentation is not loaded for for symbols defined in
    /// an assembly added as a reference to the compilation:
    /// https://github.com/dotnet/roslyn/issues/23673
    /// The workaround comes from here:
    /// https://github.com/dotnet/roslyn-sdk/blob/66b2eb24a0e1e8fe31ff777a8255ceecbcbddb51/src/Microsoft.CodeAnalysis.Testing/Microsoft.CodeAnalysis.Analyzer.Testing/MetadataReferences.cs
    /// </remarks>
    internal static class MetadataReferences
    {
        private static readonly Func<string, DocumentationProvider?> s_createDocumentationProvider;

        static MetadataReferences()
        {
            Func<string, DocumentationProvider?> createDocumentationProvider = _ => null;

            var xmlDocumentationProvider = typeof(Workspace).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.XmlDocumentationProvider");
            if (xmlDocumentationProvider is object)
            {
                var createFromFile = xmlDocumentationProvider.GetTypeInfo().GetMethod("CreateFromFile", new[] { typeof(string) });
                if (createFromFile is object)
                {
                    var xmlDocCommentFilePath = Expression.Parameter(typeof(string), "xmlDocCommentFilePath");
                    var body = Expression.Convert(
                        Expression.Call(createFromFile, xmlDocCommentFilePath),
                        typeof(DocumentationProvider));
                    var expression = Expression.Lambda<Func<string, DocumentationProvider>>(body, xmlDocCommentFilePath);
                    createDocumentationProvider = expression.Compile();
                }
            }

            s_createDocumentationProvider = createDocumentationProvider;
        }

        internal static MetadataReference CreateFromFile(string path)
        {
            var documentationFile = Path.ChangeExtension(path, ".xml");
            return MetadataReference.CreateFromFile(path, documentation: s_createDocumentationProvider(documentationFile));
        }
    }
}
#nullable disable