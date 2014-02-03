using UserInterface.Views;
using Models.Core;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private Model ModelClicked;
        private Simulations Simulations;
        public bool ok { get; set; }

        public RunCommand(Simulations Simulations, Model Simulation)
        {
            this.Simulations = Simulations;
            this.ModelClicked = Simulation;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            if (ModelClicked == null || ModelClicked is Simulations)
                ok = Simulations.Run();
            else
                ok = Simulations.Run(ModelClicked);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

    }
}
