namespace Models.Interfaces
{
    using System.Collections.Generic;

    /// <summary>This interface defines a model as having solutes.</summary>
    public interface IHasSolutes
    {
        /// <summary>Get a list of solutes</summary>
        List<ISolute> GetSolutes();
    }
}