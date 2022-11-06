namespace UserInterface.Presenters
{
    /// <summary>Defines an interface for presenters that are exportable.</summary>
    public interface IExportable
    {
        /// <summary>Export the object to a png file and return the file name</summary>
        string ExportToPNG(string folder);
    }
}
