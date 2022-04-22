using System;
using System.Windows.Forms;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    public partial class MetaNotFoundForm : Form
    {
        public MetaNotFoundForm()
        {
            InitializeComponent();
        }
        private void MetaNotFoundForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "META NOT FOUND FORM CLOSING");
        }
    }
}
