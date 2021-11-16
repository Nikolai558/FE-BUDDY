using FeBuddyLibrary;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace FeBuddyWinFormUI
{
    public partial class Processing : Form
    {
        public Processing()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Change the Form Tilte
        /// </summary>
        /// <param name="titleText">String New Title</param>
        public void ChangeTitle(string titleText) { Text = titleText; }

        /// <summary>
        /// Change the Processing Label Text
        /// </summary>
        /// <param name="text">String Text for Label</param>
        public void ChangeProcessingLabel(string text) { processingLabel.Text = text; }

        /// <summary>
        /// Change Processing Label Location
        /// </summary>
        /// <param name="location">Point Location (X, Y)</param>
        public void ChangeProcessingLabel(Point location) { processingLabel.Location = location; }

        //public void ChangeProccessingLabel(int newSize) { processingLabel.Font; }

        /// <summary>
        /// Change the Pannel Size
        /// </summary>
        /// <param name="newSize">Size (Width, Height)</param>
        public void ChangeUpdatePanel(Size newSize) { updatePanel.Size = newSize; }

        public void ChangeUpdatePanel(Point newPoint) { updatePanel.Location = newPoint; }

        public void DisplayMessages(bool visible) 
        {
            processingLabel.Enabled = !visible;
            processingLabel.Visible = !visible;

            questionHeaderlabel.Enabled = visible;
            questionHeaderlabel.Visible = visible;

            programVersionLabel.Enabled = visible;
            programVersionLabel.Visible = visible;

            githubVersionLabel.Enabled = visible;
            githubVersionLabel.Visible = visible;

            githubMessagelabel.Enabled = visible;
            githubMessagelabel.Visible = visible;

            questionLabel.Enabled = visible;
            questionLabel.Visible = visible;

            yesButton.Enabled = visible;
            yesButton.Visible = visible;

            noButton.Enabled = visible;
            noButton.Visible = visible;

            if (visible)
            {
                InputVariables();
            }
        }

        private string ReadChangeLog()
        {
            string output = "";
            string content = "";

            string url = "https://raw.githubusercontent.com/Nikolai558/FE-BUDDY/development/ChangeLog.md";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                content = reader.ReadToEnd();
            }

            foreach (string line in content.Split('\n'))
            {
                if (line.Contains("## Version "))
                {
                    string version = line.Substring(13, 5);

                    if (GlobalConfig.ProgramVersion == version)
                    {
                        break;
                    }
                }
                output += line + '\n';
            }

            return output;
        }

        private void InputVariables() 
        {
            string msg = ReadChangeLog();

            githubMessagelabel.Text = msg;
            programVersionLabel.Text = $"Your program version: {GlobalConfig.ProgramVersion}";
            githubVersionLabel.Text = $"Latest release version: {GlobalConfig.GithubVersion}";
        }

        private void yesButton_Click(object sender, EventArgs e)
        {
            GlobalConfig.updateProgram = true;
            this.Close();
        }

        private void noButton_Click(object sender, EventArgs e)
        {
            GlobalConfig.updateProgram = false;
            this.Close();
        }
    }
}
