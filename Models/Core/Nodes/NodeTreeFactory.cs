using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Core.ApsimFile;

namespace Models.Core;

/// <summary>
/// This class encapsulates the code to construct a ModelNodeTree from a file or a resource. It also
/// configures all models so that the tree is ready to be displayed in the user interface or run.
/// </summary>
public class NodeTreeFactory
{
    /// <summary>
    ///
    /// </summary>[]
    /// <param name="fileName"></param>
    /// <param name="errorHandler"></param>
    /// <param name="initInBackground"></param>
    /// <param name="compileManagerScripts"></param>
    public static NodeTree CreateFromFile(string fileName, Action<Exception> errorHandler, bool initInBackground, bool compileManagerScripts = true)
    {
        var tree = FileFormat.ReadFromFile1(fileName, errorHandler, initInBackground, compileManagerScripts);

        // Give file name to Simulations instance and all Simulation instances.
        if (tree.Root.Model is Simulations simulations)
            simulations.FileName = fileName;
        foreach (Simulation s in tree.Models.Where(model => model is Simulation))
            s.FileName = fileName;

        InitialiseModel(tree, initInBackground, errorHandler, compileManagerScripts);
        return tree;
    }

    /// <summary>
    ///
    /// </summary>[]
    /// <param name="st"></param>
    /// <param name="errorHandler"></param>
    /// <param name="initInBackground"></param>
    /// <param name="compileManagerScripts"></param>
    public static NodeTree CreateFromString(string st, Action<Exception> errorHandler, bool initInBackground, bool compileManagerScripts = true)
    {
        var tree = FileFormat.ReadFromString1(st, errorHandler, initInBackground);

        InitialiseModel(tree, initInBackground, errorHandler, compileManagerScripts);
        return tree;
    }

    /// <summary>
    /// Create tree from a collection of child models
    /// </summary>
    /// <param name="childModels">The child models</param>
    public static NodeTree Create(IEnumerable<IModel> childModels)
    {
        Simulations newSimulations = new Core.Simulations();
        newSimulations.Children.AddRange(childModels.Cast<Model>());
        NodeTree tree = new();
        tree.Initialise(newSimulations, false);
        InitialiseModel(tree,
                        initInBackground: false,
                        errorHandler: (e) => throw e,
                        compileManagers: false);
        return tree;
    }

    /// <summary>
    /// Initialise the simulation.
    /// </summary>
    private static void InitialiseModel(NodeTree tree, bool initInBackground, Action<Exception> errorHandler, bool compileManagers = true)
    {
        // Replace all models that have a ResourceName with the official, released models from resources.
        var nodesThatNeedRescan = Resource.Instance.Replace(tree);
        foreach (var node in nodesThatNeedRescan)
            tree.Rescan(node);

        // Give a ScriptCompiler instance to all manager models.
        ScriptCompiler compiler = new();
        foreach (Manager manager in tree.Models.Where(model => model is Manager))
            manager.SetCompiler(compiler);

        // Call created in all models.
        if (initInBackground)
            Task.Run(() => InitialiseModel(tree, errorHandler, compileManagers));
        else
            InitialiseModel(tree, errorHandler, compileManagers);
    }

    /// <summary>
    /// Initialise a model
    /// </summary>
    /// <param name="tree"></param>
    /// <param name="errorHandler"></param>
    /// <param name="compileManagers"></param>
    private static void InitialiseModel(NodeTree tree, Action<Exception> errorHandler, bool compileManagers = true)
    {
        foreach (Simulation s in tree.Models.Where(model => model is Simulation))
            s.IsInitialising = true;

        try
        {
            foreach (var model in tree.WalkModels)
            {
                try
                {
                    (model as IModel)?.OnCreated();

                    if (compileManagers)
                        if (model is Manager manager)
                            manager.RebuildScriptModel();
                }
                catch (Exception err)
                {
                    errorHandler(err);
                }
            }
        }
        finally
        {
            foreach (Simulation s in tree.Models.Where(model => model is Simulation))
                s.IsInitialising = false;
        }
    }
}