using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Organs
{
    public class GenericAboveGroundOrgan : GenericOrgan, AboveGround
    {
        [Link]
        Summary Summary = null;

        #region Event handlers
        [EventSubscribe("Prune")]
        private void OnPrune(PruneType Prune)
        {
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Pruning " + Name + " from " + Plant.Name;
            Summary.WriteMessage(FullPath, "");
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));

            Live.Clear();
            Dead.Clear();
        }
        [EventSubscribe("Cut")]
        private void OnCut()
        {
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Cutting " + Name + " from " + Plant.Name;
            Summary.WriteMessage(FullPath, "");
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));

            Live.Clear();
            Dead.Clear();
        }
        #endregion
    }
}
