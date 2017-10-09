using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UserInterface.Presenters;

namespace UserInterface.Views
{
    interface IWFMasterView
    {
        /// <summary>
        /// Property to provide access to the model type label.
        /// </summary>
        string ModelTypeText { get; set; }

        /// <summary>
        /// Property to provide access to the model description text label.
        /// </summary>
        string ModelDescriptionText { get; set; }

        /// <summary>
        /// Property to provide the text color for model type label.
        /// </summary>
        Color ModelTypeTextColour { get; set; }

        /// <summary>
        /// Property to provide access to the model help URL.
        /// </summary>
        string ModelHelpURL { get; set; }

        ///// <summary>
        ///// Property to provide access to the lower presenter.
        ///// </summary>
        //IPresenter LowerPresenter { get; }

        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddLowerView(object Control);
    }

    public partial class WFMasterView : UserControl, IWFMasterView
    {
        public WFMasterView()
        {
            InitializeComponent();
        }

        public string ModelTypeText
        {
            get
            {
                return ModelTypeLabel.Text;
            }
            set
            {
                ModelTypeLabel.Text = value;
            }
        }

        public string ModelDescriptionText
        {
            get
            {
                return DescriptionLabel.Text;
            }
            set
            {
                DescriptionLabel.Text = value;
                DescriptionLabel.Visible = (DescriptionLabel.Text != "");
                DescriptionLabel.MaximumSize = new Size(DescriptionLabel.Width, 0);
                DescriptionLabel.Height = DescriptionLabel.PreferredHeight;
                DescriptionLabel.MaximumSize = new Size(0, 0);
            }
        }

        public Color ModelTypeTextColour
        {
            get
            {
                return ModelTypeLabel.ForeColor;
            }
            set
            {
                ModelTypeLabel.ForeColor = value;
            }
        }

        public string ModelHelpURL { get; set; }

        //IPresenter LowerPresenter
        //{
        //    get
        //    {
        //        return LowerPanel;
        //    }
        //    set
        //    {
        //        ModelTypeLabel.ForeColor = value;
        //    }
        //}



    private void HelpLinkLabel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.csiro.au");
        }

        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddLowerView(object control)
        {
            LowerPanel.Controls.Clear();
            UserControl userControl = control as UserControl;
            if (userControl != null)
            {
                LowerPanel.Controls.Add(userControl);
                userControl.Dock = DockStyle.Fill;
            }
        }

    }
}
