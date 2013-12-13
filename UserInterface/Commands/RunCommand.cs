using UserInterface.Views;
using Models.Core;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private Simulation Simulation;
        private Simulations Simulations;
        public bool ok { get; set; }

        public RunCommand(Simulations Simulations, Simulation Simulation)
        {
            this.Simulations = Simulations;
            this.Simulation = Simulation;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            if (Simulation == null)
                ok = Simulations.Run();
            else
                ok = Simulations.Run(Simulation);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

    }
}
