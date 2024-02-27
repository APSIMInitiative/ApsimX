using Models.Core;
using Models.PMF.Phen;
using System.Globalization;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// An interface for a phenology model.
    /// </summary>
    /// <remarks>
    /// fixme - there's a lot of baggage here which should be removed.
    /// </remarks>
    public interface IPhenology : IModel
    {
        /// <summary>
        /// The current phenological phase.
        /// </summary>
        IPhase CurrentPhase { get; }

        /// <summary>
        /// A one based stage number.
        /// </summary>
        double Stage { get; set; }

        /// <summary>
        /// Gets the current zadok stage number. Used in manager scripts.
        /// </summary>
        double Zadok { get; }

        /// <summary>
        /// Gets and sets the Emerged state of the crop.
        /// </summary>
        bool Emerged { get; }

        /// <summary>
        /// Force emergence on the date called if emergence has not occurred already
        /// </summary>
        /// <param name="emergenceDate">Emergence date (dd-mmm)</param>
        public void SetEmergenceDate(string emergenceDate);

        /// <summary>
        /// Force germination on the date called if germination has not occurred already
        /// </summary>
        /// <param name="germinationDate">Germination date (dd-mmm).</param>
        public void SetGerminationDate(string germinationDate);
    }
}
