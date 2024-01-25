namespace Models.AgPasture
{
    internal interface IOrganDigestibility
    {
        /// <summary>Digestibility of live biomass. Used by STOCK (g/m2).</summary>
        double LiveDigestibility { get; }
    
        /// <summary>Digestibility of dead biomass. Used by STOCK (g/m2).</summary>
        double DeadDigestibility { get; }
    }
}