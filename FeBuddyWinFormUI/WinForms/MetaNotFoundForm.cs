using FeBuddyLibrary.Helpers;
using System;
using System.Windows.Forms;

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
