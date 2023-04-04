# if NETCOREAPP
using TreeModel = Gtk.ITreeModel;
#endif
using Gtk;

namespace UserInterface.Views
{


    public class IkonView : IconView
    {
        public IkonView(TreeModel model) : base(model) { }


    }
}
