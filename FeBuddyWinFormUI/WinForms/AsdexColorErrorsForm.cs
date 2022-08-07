using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FeBuddyLibrary.DataAccess;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    public partial class AsdexColorErrorsForm : Form
    {
        private readonly GeoJson _converter;

        private List<string> dataSource = new List<string>()
            {
                "runway",
                "taxiway",
                "apron",
                "structure"
            };

        public AsdexColorErrorsForm(List<string> unknownColors, GeoJson converter)
        {

            InitializeComponent();

            

            int count = 0;
            foreach (string colorName in unknownColors)
            {
                Label label = new Label()
                {
                    Text = colorName,
                    Name = $"colorLabel{count}",
                    Location = new System.Drawing.Point(10, 20 + (50 * count)),
                    ForeColor = System.Drawing.SystemColors.AppWorkspace,
                    Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point),
                    Size = new System.Drawing.Size(127, 21),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                ComboBox dropDown = new ComboBox()
                {
                    Name = $"colorLabel{count}_Dropdown",
                    Location = new System.Drawing.Point(300, 20 + (50 * count)),
                    DataSource = new List<string>(dataSource)
                };

                panel1.Controls.Add(label);
                panel1.Controls.Add(dropDown);

                count += 1;
            }

            _converter = converter;
        }
        private void AsdexColorErrorsForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "META NOT FOUND FORM CLOSING");
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {

            foreach (var controlItem in panel1.Controls)
            {
                if (controlItem.GetType() == typeof(Label))
                {
                    continue;
                }
                else
                {
                    ComboBox controlItem1 = (ComboBox)controlItem;
                    string colorName = panel1.Controls[controlItem1.Name.Split('_')[0]].Text;
                    _converter.asdexColorDef[dataSource[controlItem1.SelectedIndex]].Add(colorName);
                }
            }
        }
    }
}
