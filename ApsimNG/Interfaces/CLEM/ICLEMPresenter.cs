using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Interfaces
{
    public interface ICLEMPresenter
    {
        void AttachExtraPresenters(CLEMPresenter clemPresenter);
    }
}
