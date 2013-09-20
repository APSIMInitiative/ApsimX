
namespace UserInterface.Commands
{
    public interface ICommand
    {
        void Do(CommandHistory CommandHistory);
        void Undo(CommandHistory CommandHistory);
    }
}
