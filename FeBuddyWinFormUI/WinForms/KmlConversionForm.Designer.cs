namespace FeBuddyWinFormUI
{
    partial class KmlConversionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KmlConversionForm));
            this.airacLabel = new System.Windows.Forms.Label();
            this.facilityIDLabel = new System.Windows.Forms.Label();
            this.convertLabel = new System.Windows.Forms.Label();
            this.convertDescriptionLabel = new System.Windows.Forms.Label();
            this.filePathLabel = new System.Windows.Forms.Label();
            this.chooseDirButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.airacCycleGroupBox = new System.Windows.Forms.GroupBox();
            this.currentAiracSelection = new System.Windows.Forms.RadioButton();
            this.nextAiracSelection = new System.Windows.Forms.RadioButton();
            this.convertGroupBox = new System.Windows.Forms.GroupBox();
            this.startGroupBox = new System.Windows.Forms.GroupBox();
            this.facilityIdCombobox = new System.Windows.Forms.ComboBox();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.informationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.InstructionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RoadmapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FAQMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ChangeLogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreditsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.discordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportIssuesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.allowBetaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UninstallMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.airacCycleGroupBox.SuspendLayout();
            this.convertGroupBox.SuspendLayout();
            this.startGroupBox.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
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
            this.chooseDirButton.Text = "Select Output Location";
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
            this.startButton.Click += new System.EventHandler(this.AiracDataStartButton_Click);
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
            // convertGroupBox
            // 
            this.convertGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.convertGroupBox.Controls.Add(this.convertLabel);
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
            this.facilityIdCombobox.Click += new System.EventHandler(this.facilityIdCombobox_Click);
            // 
            // menuStrip
            // 
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.menuStrip.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.informationToolStripMenuItem,
            this.discordToolStripMenuItem,
            this.reportIssuesToolStripMenuItem,
            this.newsToolStripMenuItem,
            this.settingsToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(784, 26);
            this.menuStrip.TabIndex = 16;
            this.menuStrip.Text = "menuStrip1";
            // 
            // informationToolStripMenuItem
            // 
            this.informationToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.informationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.InstructionsMenuItem,
            this.RoadmapMenuItem,
            this.FAQMenuItem,
            this.ChangeLogMenuItem,
            this.CreditsMenuItem});
            this.informationToolStripMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.informationToolStripMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.informationToolStripMenuItem.Name = "informationToolStripMenuItem";
            this.informationToolStripMenuItem.Size = new System.Drawing.Size(94, 22);
            this.informationToolStripMenuItem.Text = "Information";
            // 
            // InstructionsMenuItem
            // 
            this.InstructionsMenuItem.BackColor = System.Drawing.Color.Black;
            this.InstructionsMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.InstructionsMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.InstructionsMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.InstructionsMenuItem.Name = "InstructionsMenuItem";
            this.InstructionsMenuItem.Size = new System.Drawing.Size(155, 22);
            this.InstructionsMenuItem.Text = "Instructions";
            this.InstructionsMenuItem.Click += new System.EventHandler(this.InstructionsMenuItem_Click);
            // 
            // RoadmapMenuItem
            // 
            this.RoadmapMenuItem.BackColor = System.Drawing.Color.Black;
            this.RoadmapMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.RoadmapMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.RoadmapMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.RoadmapMenuItem.Name = "RoadmapMenuItem";
            this.RoadmapMenuItem.Size = new System.Drawing.Size(155, 22);
            this.RoadmapMenuItem.Text = "Roadmap";
            this.RoadmapMenuItem.Click += new System.EventHandler(this.RoadmapMenuItem_Click);
            // 
            // FAQMenuItem
            // 
            this.FAQMenuItem.BackColor = System.Drawing.Color.Black;
            this.FAQMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.FAQMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.FAQMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.FAQMenuItem.Name = "FAQMenuItem";
            this.FAQMenuItem.Size = new System.Drawing.Size(155, 22);
            this.FAQMenuItem.Text = "FAQ";
            this.FAQMenuItem.Click += new System.EventHandler(this.FAQMenuItem_Click);
            // 
            // ChangeLogMenuItem
            // 
            this.ChangeLogMenuItem.BackColor = System.Drawing.Color.Black;
            this.ChangeLogMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ChangeLogMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ChangeLogMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.ChangeLogMenuItem.Name = "ChangeLogMenuItem";
            this.ChangeLogMenuItem.Size = new System.Drawing.Size(155, 22);
            this.ChangeLogMenuItem.Text = "Change Log";
            this.ChangeLogMenuItem.Click += new System.EventHandler(this.ChangeLogMenuItem_Click);
            // 
            // CreditsMenuItem
            // 
            this.CreditsMenuItem.BackColor = System.Drawing.Color.Black;
            this.CreditsMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CreditsMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.CreditsMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.CreditsMenuItem.Name = "CreditsMenuItem";
            this.CreditsMenuItem.Size = new System.Drawing.Size(155, 22);
            this.CreditsMenuItem.Text = "Credits";
            this.CreditsMenuItem.Click += new System.EventHandler(this.CreditsMenuItem_Click);
            // 
            // discordToolStripMenuItem
            // 
            this.discordToolStripMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.discordToolStripMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.discordToolStripMenuItem.Name = "discordToolStripMenuItem";
            this.discordToolStripMenuItem.Size = new System.Drawing.Size(70, 22);
            this.discordToolStripMenuItem.Text = "Discord";
            this.discordToolStripMenuItem.Click += new System.EventHandler(this.discordToolStripMenuItem_Click);
            // 
            // reportIssuesToolStripMenuItem
            // 
            this.reportIssuesToolStripMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.reportIssuesToolStripMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.reportIssuesToolStripMenuItem.Name = "reportIssuesToolStripMenuItem";
            this.reportIssuesToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.reportIssuesToolStripMenuItem.Text = "Report Issues";
            this.reportIssuesToolStripMenuItem.Click += new System.EventHandler(this.reportIssuesToolStripMenuItem_Click);
            // 
            // newsToolStripMenuItem
            // 
            this.newsToolStripMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.newsToolStripMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.newsToolStripMenuItem.Name = "newsToolStripMenuItem";
            this.newsToolStripMenuItem.Size = new System.Drawing.Size(57, 22);
            this.newsToolStripMenuItem.Text = "News";
            this.newsToolStripMenuItem.Click += new System.EventHandler(this.newsToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allowBetaMenuItem,
            this.UninstallMenuItem});
            this.settingsToolStripMenuItem.Font = new System.Drawing.Font("Romantic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsToolStripMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(70, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // allowBetaMenuItem
            // 
            this.allowBetaMenuItem.BackColor = System.Drawing.Color.Black;
            this.allowBetaMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.allowBetaMenuItem.Enabled = false;
            this.allowBetaMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.allowBetaMenuItem.Name = "allowBetaMenuItem";
            this.allowBetaMenuItem.Size = new System.Drawing.Size(195, 22);
            this.allowBetaMenuItem.Text = "Dev Testing Mode";
            this.allowBetaMenuItem.Visible = false;
            this.allowBetaMenuItem.Click += new System.EventHandler(this.allowBetaMenuItem_Click);
            // 
            // UninstallMenuItem
            // 
            this.UninstallMenuItem.BackColor = System.Drawing.Color.Black;
            this.UninstallMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.UninstallMenuItem.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.UninstallMenuItem.Name = "UninstallMenuItem";
            this.UninstallMenuItem.Size = new System.Drawing.Size(195, 22);
            this.UninstallMenuItem.Text = "Uninstall";
            this.UninstallMenuItem.Click += new System.EventHandler(this.UninstallMenuItem_Click);
            // 
            // KmlConversionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.BackgroundImage = global::FeBuddyWinFormUI.Properties.Resources.window_background;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(784, 404);
            this.Controls.Add(this.menuStrip);
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
            this.Name = "KmlConversionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FE-BUDDY";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AiracDataForm_Closing);
            this.Load += new System.EventHandler(this.AiracDataForm_Load);
            this.Shown += new System.EventHandler(this.AiracDataForm_Shown);
            this.airacCycleGroupBox.ResumeLayout(false);
            this.airacCycleGroupBox.PerformLayout();
            this.convertGroupBox.ResumeLayout(false);
            this.convertGroupBox.PerformLayout();
            this.startGroupBox.ResumeLayout(false);
            this.startGroupBox.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label airacLabel;
        private System.Windows.Forms.Label facilityIDLabel;
        private System.Windows.Forms.Label convertLabel;
        private System.Windows.Forms.Label convertDescriptionLabel;
        private System.Windows.Forms.Label filePathLabel;
        private System.Windows.Forms.Button chooseDirButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox airacCycleGroupBox;
        private System.Windows.Forms.GroupBox convertGroupBox;
        private System.Windows.Forms.GroupBox startGroupBox;
        private System.Windows.Forms.ComboBox facilityIdCombobox;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem informationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem InstructionsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RoadmapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FAQMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ChangeLogMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CreditsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reportIssuesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem allowBetaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UninstallMenuItem;
        private System.Windows.Forms.ToolStripMenuItem discordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newsToolStripMenuItem;
        private System.Windows.Forms.RadioButton currentAiracSelection;
        private System.Windows.Forms.RadioButton nextAiracSelection;
    }
}

