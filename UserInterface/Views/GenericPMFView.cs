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
    public partial class GenericPMFView : UserControl
    {

        public GridView DependenciesGrid
        {
            get
            {
                return gridDependencies;
            }
        }

        public GridView ParametersGrid
        {
            get
            {
                return gridParamters;
            }
        }

        public GridView PropertiesGrid
        {
            get
            {
                return gridProperties;
            }
        }

        public GenericPMFView()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
