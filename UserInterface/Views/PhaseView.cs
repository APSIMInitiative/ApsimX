using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{

    public interface IPhaseView
    {

    }

    public partial class PhaseView : UserControl
    {
        public PhaseView()
        {
            InitializeComponent();
        }
    }
}
