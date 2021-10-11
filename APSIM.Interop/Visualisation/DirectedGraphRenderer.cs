using System.Collections.Generic;
using System.Linq;
using APSIM.Interop.Drawing;
using APSIM.Shared.Graphing;

namespace APSIM.Interop.Visualisation
{
    /// <summary>
    /// This class can draw a directed graph on an <see cref="IDrawContext"/>.
    /// </summary>
    public class DirectedGraphRenderer
    {
        /// <summary>
        /// The directed graph to be rendered.
        /// </summary>
        public DirectedGraph Graph { get; private set; }

        /// <summary>
        /// Create a new <see cref="DirectedGraph"/> instance.
        /// </summary>
        /// <param name="graph">The directed graph to be rendered.</param>
        public DirectedGraphRenderer(DirectedGraph graph)
        {
            Graph = graph;
        }

        /// <summary>
        /// Draw the directed graph on the given drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        public void Draw(IDrawContext context)
        {
            Draw(context, Graph);
        }

        /// <summary>
        /// Draw the given <see cref="DirectedGraph"/> on a drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="graph">The directed graph instance to be drawn.</param>
        public static void Draw(IDrawContext context, DirectedGraph graph)
        {
            Draw(context, graph.Arcs, graph.Nodes);
        }

        /// <summary>
        /// Draw the given arcs and nodes from a directed graph on a drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="arcs">Arcs to be drawn.</param>
        /// <param name="nodes">Nodes to be drawn.</param>
        public static void Draw(IDrawContext context, IEnumerable<Arc> arcs, IEnumerable<Node> nodes)
        {
            IEnumerable<DGNode> dgNodes = nodes.Select(n => new DGNode(n)).ToList();
            IEnumerable<DGArc> dgArcs = arcs.Select(a => new DGArc(a, dgNodes));
            Draw(context, dgArcs, dgNodes);
        }

        /// <summary>
        /// Draw the nodes and arcs on the given drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="arcs">The arcs in the directed graph.</param>
        /// <param name="nodes">The nodes in the directed graph.</param>
        public static void Draw(IDrawContext context, IEnumerable<DGArc> arcs, IEnumerable<DGNode> nodes)
        {
            foreach (DGArc tmpArc in arcs)
                tmpArc.Paint(context);
            foreach (DGNode tmpNode in nodes)
                tmpNode.Paint(context);
        }
    }
}