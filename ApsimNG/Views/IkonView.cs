# if NETCOREAPP
using TreeModel = Gtk.ITreeModel;
#endif

namespace UserInterface.Views
{
    using Gtk;

    public class IkonView : IconView
    {
        public IkonView(TreeModel model) : base(model) { }


    }
}
