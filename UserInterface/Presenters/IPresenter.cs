
namespace UserInterface.Presenters
{
    public interface IPresenter
    {
        void Attach(object Model, object View, CommandHistory CommandHistory);
    }
}
