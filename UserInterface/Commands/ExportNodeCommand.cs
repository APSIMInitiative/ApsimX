using UserInterface.Views;
using Models.Core;
using UserInterface.Presenters;
using System.IO;
using System.Reflection;
using System;
using Models.Factorial;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    class ExportNodeCommand : ICommand
    {
        private ExplorerPresenter ExplorerPresenter;
        private string NodePath;
        private string FolderPath;

        // Setup a list of model types that we will recurse down through.
        private static Type[] modelTypesToRecurseDown = new Type[] {typeof(Folder),
                                                                    typeof(Simulations),
                                                                    typeof(Simulation),
                                                                    typeof(Experiment)};

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExportNodeCommand(ExplorerPresenter explorerPresenter,
                                 string nodePath,
                                 string folderPath)
        {
            this.ExplorerPresenter = explorerPresenter;
            this.NodePath = nodePath;
            this.FolderPath = folderPath;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            // Get the model we are to export.
            Model modelToExport = ExplorerPresenter.ApsimXFile.Get(NodePath) as Model;
            if (modelToExport != null)
                DoExport(modelToExport, FolderPath);
        }


        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

        /// <summary>
        /// Main export code.
        /// </summary>
        public void DoExport(Model modelToExport, string folderPath)
        {
            // Make sure the specified folderPath exists because we're going to 
            // write to it.
            Directory.CreateDirectory(folderPath);

            //Load CSS resource
            Assembly assembly = Assembly.GetExecutingAssembly();
            StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("UserInterface.Resources.Export.css"));
            string css = reader.ReadToEnd();

            //write image files
            using (FileStream file = new FileStream(Path.Combine(folderPath, "apsim_logo.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.apsim_logo.png").CopyTo(file);
            }

            using (FileStream file = new FileStream(Path.Combine(folderPath, "hd_bg.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.hd_bg.png").CopyTo(file);
            }

            if (modelToExport is Simulation)
                DoExportSimulation(modelToExport, folderPath);
            else
            {
                // Create index.html
                StreamWriter index = new StreamWriter(Path.Combine(folderPath, "Index.html"));
                index.WriteLine("<!DOCTYPE html><html lang=\"en-AU\"><head><style type=text/css>" + css + "</style></head>");
                index.WriteLine("<body>");
                index.WriteLine("<div id=\"content\"><div id=\"left\"><img src=\"apsim_logo.png\" /></div>");
                index.WriteLine("<div id=\"right\"><img src=\"hd_bg.png\" /></div>");
                index.WriteLine("<h2>" + modelToExport.Name + "</h2>");

                // Look for child models that are a folder or simulation etc
                // that we need to recurse down through.
                foreach (Model child in modelToExport.Children)
                {
                    if (Array.IndexOf(modelTypesToRecurseDown, child.GetType()) != -1)
                    {
                        string childFolderPath = Path.Combine(folderPath, child.Name);

                        string childFileName = Path.Combine(childFolderPath, "Index.html");
                        index.WriteLine("<p><a href=file://{0}>{1}</a></p>",
                                        new object[] {Utility.String.DQuote(childFileName),
                                                    child.Name});

                        DoExport(child, childFolderPath);
                    }
                }

                // Write out any models that are under this model.
                DoExportZone(modelToExport, folderPath, index);

                index.WriteLine("</div>");
                index.WriteLine("</body>");
                index.WriteLine("</html>");
                index.Close();
            }
        }

        /// <summary>
        /// Main export code.
        /// </summary>
        public void DoExportSimulation(Model modelToExport, string folderPath)
        {
            //Load CSS resource

            // Make sure the specified folderPath exists because we're going to 
            // write to it.
            Directory.CreateDirectory(folderPath);

            //Load CSS resource
            Assembly assembly = Assembly.GetExecutingAssembly();
            StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("UserInterface.Resources.Export.css"));
            string css = reader.ReadToEnd();

            // Create index.html
            StreamWriter index = new StreamWriter(Path.Combine(folderPath, "Index.html"));
            index.WriteLine("<!DOCTYPE html><html lang=\"en-AU\"><head><style type=text/css>" + css + "</style></head>");
            index.WriteLine("<body>");

            DoExportZone(modelToExport, folderPath, index);

            index.WriteLine("</body>");
            index.WriteLine("</html>");
            index.Close();

        }

        /// <summary>
        /// Export the specified zone.
        /// </summary>
        /// <param name="modelToExport"></param>
        /// <param name="folderPath"></param>
        /// <param name="index"></param>
        private void DoExportZone(Model modelToExport, string folderPath, StreamWriter index)
        {
            foreach (Model child in modelToExport.Children)
            {
                if (child is Zone)
                    DoExportZone(child, folderPath, index);
                else
                {
                    // Select the node in the tree.
                    ExplorerPresenter.SelectNode(Apsim.FullPath(child));

                    // If the presenter is exportable then simply export this child.
                    // Otherwise, if it is one of a folder, simulation, experiment or zone then
                    // recurse down.
                    if (ExplorerPresenter.CurrentPresenter is IExportable)
                    {
                        string html = (ExplorerPresenter.CurrentPresenter as IExportable).ConvertToHtml(folderPath);
                        index.WriteLine("<p>" + html + "</p>");
                    }
                }
            }
        }


    }
}
