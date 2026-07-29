namespace APSIM.Core;

/// <summary>
/// An interface for a model command that references an external file
/// Such as Load, Replace, Add
/// </summary>
public interface ICommandReferenceExternalFile
{
    /// <summary>
    /// Returns the potential filepath for this command.
    /// </summary>
    public string GetFilePath();
}