namespace FeBuddyWinFormUI
{
    partial class AiracDataForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AiracDataForm));
            this.currentAiracSelection = new System.Windows.Forms.RadioButton();
            this.nextAiracSelection = new System.Windows.Forms.RadioButton();
            this.airacLabel = new System.Windows.Forms.Label();
            this.facilityIDLabel = new System.Windows.Forms.Label();
            this.convertLabel = new System.Windows.Forms.Label();
            this.convertNo = new System.Windows.Forms.RadioButton();
            this.convertYes = new System.Windows.Forms.RadioButton();
            this.convertDescriptionLabel = new System.Windows.Forms.Label();
            this.filePathLabel = new System.Windows.Forms.Label();
            this.chooseDirButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.airacCycleGroupBox = new System.Windows.Forms.GroupBox();
            this.convertGroupBox = new System.Windows.Forms.GroupBox();
            this.startGroupBox = new System.Windows.Forms.GroupBox();
            this.facilityIdCombobox = new System.Windows.Forms.ComboBox();
            this.processingDataLabel = new System.Windows.Forms.Label();
            this.processingGroupBox = new System.Windows.Forms.GroupBox();
            this.outputDirectoryLabel = new System.Windows.Forms.Label();
            this.outputLocationLabel = new System.Windows.Forms.Label();
            this.exitButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenuMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.airacCycleGroupBox.SuspendLayout();
            this.convertGroupBox.SuspendLayout();
            this.startGroupBox.SuspendLayout();
            this.processingGroupBox.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // currentAiracSelection
            // 
            this.currentAiracSelection.AutoSize = true;
            this.currentAiracSelection.Checked = true;
            this.currentAiracSelection.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.currentAiracSelection.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.currentAiracSelection.Location = new System.Drawing.Point(91, 68);
            this.currentAiracSelection.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.currentAiracSelection.Name = "currentAiracSelection";
            this.currentAiracSelection.Size = new System.Drawing.Size(129, 25);
            this.currentAiracSelection.TabIndex = 0;
            this.currentAiracSelection.TabStop = true;
            this.currentAiracSelection.Text = "Current AIRAC";
            this.currentAiracSelection.UseVisualStyleBackColor = true;
            this.currentAiracSelection.CheckedChanged += new System.EventHandler(this.CurrentAiracSelection_CheckedChanged);
            // 
            // nextAiracSelection
            // 
            this.nextAiracSelection.AutoSize = true;
            this.nextAiracSelection.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.nextAiracSelection.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.nextAiracSelection.Location = new System.Drawing.Point(232, 68);
            this.nextAiracSelection.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.nextAiracSelection.Name = "nextAiracSelection";
            this.nextAiracSelection.Size = new System.Drawing.Size(108, 25);
            this.nextAiracSelection.TabIndex = 1;
            this.nextAiracSelection.Text = "Next AIRAC";
            this.nextAiracSelection.UseVisualStyleBackColor = true;
            this.nextAiracSelection.CheckedChanged += new System.EventHandler(this.NextAiracSelection_CheckedChanged);
            this.nextAiracSelection.Click += new System.EventHandler(this.NextAiracSelection_Click);
            // 
            // airacLabel
            // 
            this.airacLabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.airacLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.airacLabel.Location = new System.Drawing.Point(1, 29);
            this.airacLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.airacLabel.Name = "airacLabel";
            this.airacLabel.Size = new System.Drawing.Size(450, 25);
            this.airacLabel.TabIndex = 2;
            this.airacLabel.Text = "Select an AIRAC Cycle by Effective Date";
            this.airacLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // facilityIDLabel
            // 
            this.facilityIDLabel.AutoSize = true;
            this.facilityIDLabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.facilityIDLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.facilityIDLabel.Location = new System.Drawing.Point(25, 32);
            this.facilityIDLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.facilityIDLabel.Name = "facilityIDLabel";
            this.facilityIDLabel.Size = new System.Drawing.Size(212, 25);
            this.facilityIDLabel.TabIndex = 3;
            this.facilityIDLabel.Text = "What is your Facility ID?";
            // 
            // convertLabel
            // 
            this.convertLabel.AutoSize = true;
            this.convertLabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertLabel.Location = new System.Drawing.Point(33, 29);
            this.convertLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.convertLabel.Name = "convertLabel";
            this.convertLabel.Size = new System.Drawing.Size(385, 25);
            this.convertLabel.TabIndex = 5;
            this.convertLabel.Text = "Would you like to Convert East Coordinates?";
            // 
            // convertNo
            // 
            this.convertNo.AutoSize = true;
            this.convertNo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertNo.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertNo.Location = new System.Drawing.Point(232, 193);
            this.convertNo.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.convertNo.Name = "convertNo";
            this.convertNo.Size = new System.Drawing.Size(49, 25);
            this.convertNo.TabIndex = 7;
            this.convertNo.TabStop = true;
            this.convertNo.Text = "No";
            this.convertNo.UseVisualStyleBackColor = true;
            // 
            // convertYes
            // 
            this.convertYes.AutoSize = true;
            this.convertYes.Checked = true;
            this.convertYes.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertYes.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertYes.Location = new System.Drawing.Point(148, 193);
            this.convertYes.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.convertYes.Name = "convertYes";
            this.convertYes.Size = new System.Drawing.Size(51, 25);
            this.convertYes.TabIndex = 6;
            this.convertYes.TabStop = true;
            this.convertYes.Text = "Yes";
            this.convertYes.UseVisualStyleBackColor = true;
            // 
            // convertDescriptionLabel
            // 
            this.convertDescriptionLabel.AutoSize = true;
            this.convertDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertDescriptionLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertDescriptionLabel.Location = new System.Drawing.Point(18, 71);
            this.convertDescriptionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.convertDescriptionLabel.Name = "convertDescriptionLabel";
            this.convertDescriptionLabel.Size = new System.Drawing.Size(425, 105);
            this.convertDescriptionLabel.TabIndex = 8;
            this.convertDescriptionLabel.Text = resources.GetString("convertDescriptionLabel.Text");
            this.convertDescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // filePathLabel
            // 
            this.filePathLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.filePathLabel.Location = new System.Drawing.Point(6, 100);
            this.filePathLabel.Name = "filePathLabel";
            this.filePathLabel.Size = new System.Drawing.Size(257, 82);
            this.filePathLabel.TabIndex = 9;
            this.filePathLabel.Text = "filePathLabel";
            this.filePathLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.filePathLabel.Visible = false;
            // 
            // chooseDirButton
            // 
            this.chooseDirButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.chooseDirButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.chooseDirButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.chooseDirButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.chooseDirButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chooseDirButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.chooseDirButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.chooseDirButton.Location = new System.Drawing.Point(40, 185);
            this.chooseDirButton.Name = "chooseDirButton";
            this.chooseDirButton.Size = new System.Drawing.Size(182, 34);
            this.chooseDirButton.TabIndex = 10;
            this.chooseDirButton.Text = "Choose Output Location";
            this.chooseDirButton.UseVisualStyleBackColor = false;
            this.chooseDirButton.Click += new System.EventHandler(this.ChooseDirButton_Click);
            // 
            // startButton
            // 
            this.startButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.startButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.startButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.startButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.startButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.startButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.startButton.Location = new System.Drawing.Point(40, 302);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(182, 34);
            this.startButton.TabIndex = 11;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = false;
            this.startButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // airacCycleGroupBox
            // 
            this.airacCycleGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.airacCycleGroupBox.Controls.Add(this.airacLabel);
            this.airacCycleGroupBox.Controls.Add(this.currentAiracSelection);
            this.airacCycleGroupBox.Controls.Add(this.nextAiracSelection);
            this.airacCycleGroupBox.Location = new System.Drawing.Point(20, 27);
            this.airacCycleGroupBox.Name = "airacCycleGroupBox";
            this.airacCycleGroupBox.Size = new System.Drawing.Size(452, 125);
            this.airacCycleGroupBox.TabIndex = 12;
            this.airacCycleGroupBox.TabStop = false;
            // 
            // convertGroupBox
            // 
            this.convertGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.convertGroupBox.Controls.Add(this.convertLabel);
            this.convertGroupBox.Controls.Add(this.convertYes);
            this.convertGroupBox.Controls.Add(this.convertNo);
            this.convertGroupBox.Controls.Add(this.convertDescriptionLabel);
            this.convertGroupBox.Location = new System.Drawing.Point(20, 158);
            this.convertGroupBox.Name = "convertGroupBox";
            this.convertGroupBox.Size = new System.Drawing.Size(452, 237);
            this.convertGroupBox.TabIndex = 13;
            this.convertGroupBox.TabStop = false;
            // 
            // startGroupBox
            // 
            this.startGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.startGroupBox.Controls.Add(this.facilityIdCombobox);
            this.startGroupBox.Controls.Add(this.facilityIDLabel);
            this.startGroupBox.Controls.Add(this.startButton);
            this.startGroupBox.Controls.Add(this.filePathLabel);
            this.startGroupBox.Controls.Add(this.chooseDirButton);
            this.startGroupBox.Location = new System.Drawing.Point(497, 27);
            this.startGroupBox.Name = "startGroupBox";
            this.startGroupBox.Size = new System.Drawing.Size(269, 365);
            this.startGroupBox.TabIndex = 14;
            this.startGroupBox.TabStop = false;
            // 
            // facilityIdCombobox
            // 
            this.facilityIdCombobox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.facilityIdCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.facilityIdCombobox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.facilityIdCombobox.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.facilityIdCombobox.FormattingEnabled = true;
            this.facilityIdCombobox.IntegralHeight = false;
            this.facilityIdCombobox.Location = new System.Drawing.Point(40, 64);
            this.facilityIdCombobox.MaxDropDownItems = 10;
            this.facilityIdCombobox.Name = "facilityIdCombobox";
            this.facilityIdCombobox.Size = new System.Drawing.Size(182, 33);
            this.facilityIdCombobox.TabIndex = 12;
            this.facilityIdCombobox.SelectedIndexChanged += new System.EventHandler(this.FacilityIdCombobox_SelectedIndexChanged);
            // 
            // processingDataLabel
            // 
            this.processingDataLabel.Enabled = false;
            this.processingDataLabel.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.processingDataLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.processingDataLabel.Location = new System.Drawing.Point(6, 17);
            this.processingDataLabel.Name = "processingDataLabel";
            this.processingDataLabel.Size = new System.Drawing.Size(545, 68);
            this.processingDataLabel.TabIndex = 15;
            this.processingDataLabel.Text = "Getting AIRAC date.";
            this.processingDataLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.processingDataLabel.Visible = false;
            // 
            // processingGroupBox
            // 
            this.processingGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.processingGroupBox.Controls.Add(this.outputDirectoryLabel);
            this.processingGroupBox.Controls.Add(this.outputLocationLabel);
            this.processingGroupBox.Controls.Add(this.exitButton);
            this.processingGroupBox.Controls.Add(this.processingDataLabel);
            this.processingGroupBox.Enabled = false;
            this.processingGroupBox.Location = new System.Drawing.Point(114, 127);
            this.processingGroupBox.Name = "processingGroupBox";
            this.processingGroupBox.Size = new System.Drawing.Size(557, 145);
            this.processingGroupBox.TabIndex = 3;
            this.processingGroupBox.TabStop = false;
            this.processingGroupBox.Visible = false;
            // 
            // outputDirectoryLabel
            // 
            this.outputDirectoryLabel.AutoSize = true;
            this.outputDirectoryLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.outputDirectoryLabel.Location = new System.Drawing.Point(522, 170);
            this.outputDirectoryLabel.Name = "outputDirectoryLabel";
            this.outputDirectoryLabel.Size = new System.Drawing.Size(96, 21);
            this.outputDirectoryLabel.TabIndex = 19;
            this.outputDirectoryLabel.Text = "<Directory>";
            this.outputDirectoryLabel.Visible = false;
            // 
            // outputLocationLabel
            // 
            this.outputLocationLabel.AutoSize = true;
            this.outputLocationLabel.Location = new System.Drawing.Point(507, 145);
            this.outputLocationLabel.Name = "outputLocationLabel";
            this.outputLocationLabel.Size = new System.Drawing.Size(152, 25);
            this.outputLocationLabel.TabIndex = 18;
            this.outputLocationLabel.Text = "Output Location:";
            this.outputLocationLabel.Visible = false;
            // 
            // exitButton
            // 
            this.exitButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.exitButton.Enabled = false;
            this.exitButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.exitButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Maroon;
            this.exitButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exitButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.exitButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.exitButton.Location = new System.Drawing.Point(346, 88);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(182, 34);
            this.exitButton.TabIndex = 17;
            this.exitButton.Text = "Return To Main Menu";
            this.exitButton.UseVisualStyleBackColor = false;
            this.exitButton.Visible = false;
            this.exitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.menuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitMenuItem,
            this.mainMenuMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(380, 1);
            this.menuStrip1.MaximumSize = new System.Drawing.Size(400, 0);
            this.menuStrip1.MinimumSize = new System.Drawing.Size(400, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(0);
            this.menuStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.menuStrip1.Size = new System.Drawing.Size(400, 24);
            this.menuStrip1.TabIndex = 16;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.exitMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(46, 24);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // mainMenuMenuItem
            // 
            this.mainMenuMenuItem.Enabled = false;
            this.mainMenuMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.mainMenuMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.mainMenuMenuItem.Name = "mainMenuMenuItem";
            this.mainMenuMenuItem.Size = new System.Drawing.Size(94, 24);
            this.mainMenuMenuItem.Text = "Main Menu";
            this.mainMenuMenuItem.Click += new System.EventHandler(this.mainMenuMenuItem_Click);
            // 
            // AiracDataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.BackgroundImage = global::FeBuddyWinFormUI.Properties.Resources.window_background;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(784, 404);
            this.ControlBox = false;
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.processingGroupBox);
            this.Controls.Add(this.startGroupBox);
            this.Controls.Add(this.convertGroupBox);
            this.Controls.Add(this.airacCycleGroupBox);
            this.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.MaximizeBox = false;
            this.Name = "AiracDataForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FE-BUDDY";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_Closing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.airacCycleGroupBox.ResumeLayout(false);
            this.airacCycleGroupBox.PerformLayout();
            this.convertGroupBox.ResumeLayout(false);
            this.convertGroupBox.PerformLayout();
            this.startGroupBox.ResumeLayout(false);
            this.startGroupBox.PerformLayout();
            this.processingGroupBox.ResumeLayout(false);
            this.processingGroupBox.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton currentAiracSelection;
        private System.Windows.Forms.RadioButton nextAiracSelection;
        private System.Windows.Forms.Label airacLabel;
        private System.Windows.Forms.Label facilityIDLabel;
        private System.Windows.Forms.Label convertLabel;
        private System.Windows.Forms.RadioButton convertNo;
        private System.Windows.Forms.RadioButton convertYes;
        private System.Windows.Forms.Label convertDescriptionLabel;
        private System.Windows.Forms.Label filePathLabel;
        private System.Windows.Forms.Button chooseDirButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox airacCycleGroupBox;
        private System.Windows.Forms.GroupBox convertGroupBox;
        private System.Windows.Forms.GroupBox startGroupBox;
        private System.Windows.Forms.Label processingDataLabel;
        private System.Windows.Forms.GroupBox processingGroupBox;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label outputDirectoryLabel;
        private System.Windows.Forms.Label outputLocationLabel;
        private System.Windows.Forms.ComboBox facilityIdCombobox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mainMenuMenuItem;
    }
}

