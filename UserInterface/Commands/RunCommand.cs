using UserInterface.Views;
using Model.Core;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private ISimulation Simulation;
        private Simulations Simulations;
        public bool ok { get; set; }

        public RunCommand(Simulations Simulations, ISimulation Simulation)
        {
            this.Simulations = Simulations;
            this.Simulation = Simulation;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public object Do()
        {
            if (Simulation == null)
                foreach (ISimulation sim in Simulations.Sims)
                    ok = Simulations.Run(sim);
            else
                ok = Simulations.Run(Simulation);
            return null;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public object Undo()
        {
            // Do nothing.
            return null;
        }

    }
}
