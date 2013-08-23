
namespace UserInterface.Commands
{
    public interface ICommand
    {
        object Do();
        object Undo();
    }
}
