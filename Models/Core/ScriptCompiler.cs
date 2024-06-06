using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using MessagePack;

namespace Models.Core
{

    /// <summary>Encapsulates the ability to compile a c# script into an assembly.</summary>
    [Serializable]
    public class ScriptCompiler
    {
        private static bool haveTrappedAssemblyResolveEvent = false;
        private static object haveTrappedAssemblyResolveEventLock = new object();

        private static object compilingScriptLock = new object();

        private const string tempFileNamePrefix = "APSIM";
        
        [NonSerialized]
        private List<PreviousCompilation> previousCompilations = new List<PreviousCompilation>();

        [NonSerialized]
        private List<(string, string)> runtimeClasses = new List<(string, string)>();

        /// <summary>Constructor.</summary>
        public ScriptCompiler()
        {
            // This looks weird but I'm trying to avoid having to call lock
            // everytime we come through here. If I remove this locking then
            // Jenkins runs very slowly (5 times slower for each sim). Presumably
            // this is because each simulation being run (from APSIMRunner) does the 
            // cleanup below.
            if (!haveTrappedAssemblyResolveEvent)
            {
                lock (haveTrappedAssemblyResolveEventLock)
                {
                    if (!haveTrappedAssemblyResolveEvent)
                    {
                        haveTrappedAssemblyResolveEvent = true;

                        // Trap the assembly resolve event.
                        AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(ResolveManagerAssemblies);
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveManagerAssemblies);

                        // Clean up apsimx manager .dll files.
                        Cleanup();
                    }
                }
            }
        }

        /// <summary>Compile a c# script.</summary>
        /// <param name="code">The c# code to compile.</param>
        /// <param name="model">The model owning the script.</param>
        /// <param name="referencedAssemblies">Optional referenced assemblies.</param>
        /// <param name="allowDuplicateClassName">Optional to not throw if this has a duplicate class name (used when copying script node)</param>
        /// <returns>Compile errors or null if no errors.</returns>
        public Results Compile(string code, IModel model, IEnumerable<MetadataReference> referencedAssemblies = null, bool allowDuplicateClassName = false)
        {
            string errors = null;

            PreviousCompilation compilation = null;
            bool newlyCompiled;

            lock (compilingScriptLock)
            {

                if (code != null)
                {
                    // See if we have compiled the code already. If so then no need to compile again.
                    compilation = previousCompilations?.Find(c => c.Code == code);

                    string modifiedCode = "";
                    if (compilation == null || compilation.Code != code)
                    {
                        Regex regex = new Regex("(public class\\s)(\\w+)(\\s+:\\s+[\\w.]+)");
                        Match m = regex.Match(code);

                        modifiedCode = code;
                        if (m.Success)
                        {
                            int position;
                            string className = m.Groups[2].Value;
                            string path = model.FullPath;
                            //only do this if the script class has not been renamed
                            if (className.CompareTo("Script") == 0) {
                                //remove existing class name
                                position = modifiedCode.IndexOf(className);
                                modifiedCode = modifiedCode.Remove(position, className.Length);
                                //add unique class name in
                                string newClassName = $"Script{StringUtilities.CleanStringOfSymbols(path)}";
                                modifiedCode = modifiedCode.Insert(position, newClassName);
                            } else {
                                //we have a custom script name, make sure we haven't compiled with this before
                                foreach ((string, string) name in runtimeClasses) {
                                    if (name.Item1.CompareTo(className) == 0)
                                    {
                                        //check if the code from the matching class is this code
                                        if (name.Item2.CompareTo(path) != 0) {
                                            //check if the model from the other path still exists (model may have moved)
                                            IModel matchingClass = model.FindAncestor<Simulations>().Locator.Get(name.Item2) as IModel;
                                            if (matchingClass != null && !allowDuplicateClassName)
                                                throw new Exception($"Errors found: Manager Script {model.Name} has a custom class name that matches another manager script. Scripts with custom names must have a different name to avoid namespace conflicts.");
                                        }
                                    }
                                }
                                runtimeClasses.Remove((className, path));
                                runtimeClasses.Add((className, path));
                            }
                            //Add IScriptBase parent to class so we can type check it
                            position = modifiedCode.IndexOf(m.Groups[3].Value) + m.Groups[3].Value.Length;
                            modifiedCode = modifiedCode.Insert(position, ", IScript");
                        } 
                        else
                        {
                            throw new Exception($"Errors found: Manager Script {model.Name} must contain a class definition of \"public class Script : Model\"");
                        }
                    
                        newlyCompiled = true;
                        bool withDebug = System.Diagnostics.Debugger.IsAttached;
                        bool isRunningInVS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VisualStudioEdition"));

                        IEnumerable<MetadataReference> assemblies = GetReferenceAssemblies(referencedAssemblies, model.Name);

                        // We haven't compiled the code so do it now.
                        string sourceName;
                        Compilation compiled = CompileTextToAssembly(modifiedCode, assemblies, isRunningInVS, out sourceName);

                        MemoryStream ms = new MemoryStream();
                        MemoryStream xmlDocumentationStream = new MemoryStream();
                        MemoryStream pdbStream = new MemoryStream();
                        
                        EmitResult emitResult;
                        if (isRunningInVS) 
                        {
                            emitResult = compiled.Emit(
                                            peStream: ms,
                                            xmlDocumentationStream: xmlDocumentationStream,
                                            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded)
                                        );
                        }
                        else
                        {
                            emitResult = compiled.Emit(
                                            peStream: ms,
                                            pdbStream: withDebug ? pdbStream : null,
                                            xmlDocumentationStream: xmlDocumentationStream,
                                            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb)
                                        );
                        }
                            

                        if (!emitResult.Success)
                        {
                            // Errors were found. Add then to the return error string.
                            errors = null;
                            foreach (Diagnostic diag in emitResult.Diagnostics)
                                if (diag.Severity == DiagnosticSeverity.Error)
                                    errors += $"{diag.ToString()}{Environment.NewLine}";

                            compilation = null;
                        }
                        else
                        {
                            // No errors.
                            // If we don't have a previous compilation, create one.
                            if (compilation == null)
                            {
                                compilation = new PreviousCompilation() { ModelFullPath = model.FullPath };
                                if (previousCompilations == null)
                                    previousCompilations = new List<PreviousCompilation>();
                                previousCompilations.Add(compilation);
                            }

                            // Write the assembly to disk if this is a GUI run.
                            if (Path.GetFileName(Assembly.GetEntryAssembly().Location) == "ApsimNG.dll" && withDebug)
                            {
                                if (!isRunningInVS) 
                                {
                                    // Write pdb Documentation file.
                                    ms.Seek(0, SeekOrigin.Begin);
                                    string fileName = Path.Combine(Path.GetTempPath(), compiled.AssemblyName + ".dll");
                                    using (FileStream file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                                        ms.WriteTo(file);

                                    // Write XML Documentation file.
                                    string documentationFile = Path.ChangeExtension(fileName, ".xml");
                                    xmlDocumentationStream.Seek(0, SeekOrigin.Begin);
                                    using (FileStream documentationWriter = new FileStream(documentationFile, FileMode.Create, FileAccess.Write))
                                        xmlDocumentationStream.WriteTo(documentationWriter);

                                    // Write pdb Documentation file.
                                    string pdbFile = Path.ChangeExtension(fileName, ".pdb");
                                    pdbStream.Seek(0, SeekOrigin.Begin);
                                    using (FileStream pdbWriter = new FileStream(pdbFile, FileMode.Create, FileAccess.Write))
                                        pdbStream.WriteTo(pdbWriter);

                                    pdbStream.Seek(0, SeekOrigin.Begin);
                                }

                                ms.Seek(0, SeekOrigin.Begin);
                                compilation.Code = code;
                                compilation.Reference = compiled.ToMetadataReference();
                                compilation.CompiledAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms, pdbStream);
                            }
                            else
                            {
                                // Set the compilation properties.
                                ms.Seek(0, SeekOrigin.Begin);
                                compilation.Code = code;
                                compilation.Reference = compiled.ToMetadataReference();
                                compilation.CompiledAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
                            }
                            
                            // We have a compiled assembly so get the class name.
                            var regEx = new Regex(@"class\s+(\w+)\s");
                            var match = regEx.Match(modifiedCode);
                            if (!match.Success)
                                throw new Exception($"Cannot find a class declaration in script:{Environment.NewLine}{modifiedCode}");
                            compilation.ClassName = match.Groups[1].Value;
                            compilation.InstanceType = compilation.CompiledAssembly.GetTypes().ToList().Find(t => t.Name == compilation.ClassName);
                        }
                    }
                    else 
                    {
                        modifiedCode = compilation.Code;
                        newlyCompiled = false;
                    }

                    if (compilation != null)
                    {
                        // Original Class name for node
                        var regEx = new Regex(@"class\s+(\w+)\s");
                        var match = regEx.Match(code);
                        if (!match.Success)
                            throw new Exception($"Cannot find a class declaration in script:{Environment.NewLine}{code}");
                        var originalName = match.Groups[1].Value;

                        return new Results(compilation.CompiledAssembly, compilation.InstanceType.FullName, newlyCompiled);
                    }
                    else
                        return new Results(errors);
                }
            }

            return null;
        }

        /// <summary>Gets a list of assembly names that are needed for compiling.</summary>
        /// <param name="referencedAssemblies"></param>
        /// <param name="modelName">Name of model.</param>
        private IEnumerable<MetadataReference> GetReferenceAssemblies(IEnumerable<MetadataReference> referencedAssemblies, string modelName)
        {
            string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            IEnumerable<MetadataReference> references = new MetadataReference[]
            {
               MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "netstandard.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "mscorlib.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Collections.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Linq.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Runtime.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Core.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Data.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Runtime.Extensions.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.ReaderWriter.dll")),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Private.Xml.dll")),
               MetadataReference.CreateFromFile(typeof(MathUtilities).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(IModel).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(APSIM.Shared.Documentation.CodeDocumentation).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(MathNet.Numerics.Fit).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonIgnoreAttribute).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Drawing.Color).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Data.DataTable).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.ComponentModel.TypeConverter).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.IO.File).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.IO.Pipes.PipeStream).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(NetMQ.Sockets.ResponseSocket).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(MessagePackSerializer).Assembly.Location),
               MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Memory.dll"))

            };

            if (previousCompilations != null)
                references = references.Concat(previousCompilations.Where(p => !p.ModelFullPath.Contains($".{modelName}"))
                                                                   .Select(p => p.Reference));
            if (referencedAssemblies != null)
                references = references.Concat(referencedAssemblies);

            return references.Where(r => r != null);
        }

        /// <summary>
        /// Compile the specified 'code' into an executable assembly. If 'assemblyFileName'
        /// is null then compile to an in-memory assembly.
        /// </summary>
        /// <param name="code">The code to compile.</param>
        /// <param name="referencedAssemblies">Any referenced assemblies.</param>
        /// <param name="embedded">Is it embedded?</param>
        /// <param name="sourceName">Path to a file on disk containing the source.</param>
        /// <returns>Any compile errors or null if compile was successful.</returns>
        private Compilation CompileTextToAssembly(string code, IEnumerable<MetadataReference> referencedAssemblies, bool embedded, out string sourceName)
        {
            string assemblyFileNameToCreate = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), tempFileNamePrefix + Guid.NewGuid().ToString()), ".dll");

            Compilation compilation;
            sourceName = Path.GetFileNameWithoutExtension(assemblyFileNameToCreate) + ".cs";

            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            byte[] buffer = encoding.GetBytes(code);
            string fileName = Path.Combine(Path.GetTempPath() + sourceName);
            using (FileStream file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                file.Write(buffer, 0, buffer.Length);

            string readText = File.ReadAllText(fileName);

            SyntaxTree syntaxTree;
            if (embedded) {
                syntaxTree = CSharpSyntaxTree.ParseText(
                                readText,
                                new CSharpParseOptions(),
                                encoding: encoding
                            );
            }
            else
            {
                syntaxTree = CSharpSyntaxTree.ParseText(
                                readText,
                                new CSharpParseOptions(),
                                encoding: encoding,
                                path: fileName
                            );
            }

            OptimizationLevel optimization = OptimizationLevel.Release;
            if (System.Diagnostics.Debugger.IsAttached)
                optimization = OptimizationLevel.Debug;

            compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(assemblyFileNameToCreate),
                new[] { syntaxTree },
                referencedAssemblies,
                new CSharpCompilationOptions(optimizationLevel: optimization,
                                            outputKind: OutputKind.DynamicallyLinkedLibrary,
                                            metadataImportOptions: MetadataImportOptions.All
                                            )
                );

            return compilation;
        }

        /// <summary>A handler to resolve the loading of manager assemblies when binary deserialization happens.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static Assembly ResolveManagerAssemblies(object sender, ResolveEventArgs args)
        {
            foreach (string fileName in Directory.GetFiles(Path.GetTempPath(), tempFileNamePrefix + "*.dll"))
                if (args.Name.Split(',')[0] == Path.GetFileNameWithoutExtension(fileName))
                    return Assembly.LoadFrom(fileName);
            return null;
        }

        /// <summary>Cleanup old files.</summary>
        private void Cleanup()
        {
            string[] extensionsToCleanUp = new[] { ".dll", ".xml" };
            var filesToCleanup = Directory.GetFiles(Path.GetTempPath(), "APSIM*.*")
                                          .Where(f => extensionsToCleanUp.Contains(Path.GetExtension(f)))
                                          .Where(f => (DateTime.Now - File.GetLastAccessTime(f)).Hours > 1);

            foreach (string fileName in filesToCleanup)
            {
                try
                {
                    TimeSpan timeSinceLastAccess = DateTime.Now - File.GetLastAccessTime(fileName);
                    File.Delete(fileName);
                }
                catch (Exception)
                {
                    // File locked?
                }
            }
        }

        /// <summary>Encapsulates results from a compile.</summary>
        public class Results
        {
            private Assembly compiledAssembly;
            private string instanceTypeName;

            /// <summary>Constructor.</summary>
            public Results(Assembly assembly, string typeName, bool newlyCompiled)
            {
                compiledAssembly = assembly;
                instanceTypeName = typeName;
                WasCompiled = newlyCompiled;
            }

            /// <summary>Constructor.</summary>
            public Results(string errors)
            {
                WasCompiled = true;
                ErrorMessages = errors;
            }

            /// <summary>Compile error messages. Null for no errors.</summary>
            public string ErrorMessages { get; }

            /// <summary>Was the script compiled or was it already up to date?</summary>
            public bool WasCompiled { get; }

            /// <summary>A newly created instance.</summary>
            public object Instance { get { return compiledAssembly.CreateInstance(instanceTypeName); } }
        }

        /// <summary>Encapsulates a previous compilation.</summary>
        [Serializable]
        private class PreviousCompilation
        {
            /// <summary>The model full path.</summary>
            public string ModelFullPath { get; set; }

            /// <summary>The code that was compiled.</summary>
            public string Code { get; set; }

            /// <summary>The compiled assembly.</summary>
            public Assembly CompiledAssembly { get; set; }

            /// <summary>
            /// A reference to the compiled assembly
            /// </summary>
            public MetadataReference Reference { get; set; }

            /// <summary>The model full path.</summary>
            public Type InstanceType { get; set; }

            /// <summary>The model full path.</summary>
            public string ClassName { get; set; }
        }
    }
}