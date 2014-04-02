
using UserInterface.Views;
namespace UserInterface.Presenters
{
    public interface IPresenter
    {
        void Attach(object model, object view, ExplorerPresenter explorerPresenter);

        void Detach();
    }
}
