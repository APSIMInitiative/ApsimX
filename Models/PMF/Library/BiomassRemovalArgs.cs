using PdfSharpCore.Pdf.Content.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Library
{
    /// <summary>
    /// Class holding arguments sent with biomass removal event
    /// </summary>
    public class BiomassRemovalArgs
    {
        /// <summary>
        /// The type of biomass removal.
        /// </summary>
        public string RemovalType;
    }

    /// <summary>
    /// List of possible biomass removal types
    /// </summary>
    public enum RemovalTypes
    {
        /// <summary>Bioimass is cut</summary>
        Cutting,
        /// <summary>Bioimass is grazed</summary>
        Grazing,
        /// <summary>Bioimass is harvested</summary>
        Harvesting,
        /// <summary>Bioimass is pruned</summary>
        Pruning
    }
}
