namespace APSIM.Core;

public interface IStructureDependency
{
    /// <summary>Structure instance supplied by APSIM.core.</summary>
    public IStructure Structure { set; }
}