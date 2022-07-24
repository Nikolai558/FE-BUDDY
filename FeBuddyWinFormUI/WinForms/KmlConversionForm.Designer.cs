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
            this.startGroupBox = new System.Windows.Forms.GroupBox();
            this.sourceFileLabel = new System.Windows.Forms.Label();
            this.sourceFileButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.outputLabel = new System.Windows.Forms.Label();
            this.outputDirButton = new System.Windows.Forms.Button();
            this.convertGroupBox = new System.Windows.Forms.GroupBox();
            this.FormLabel = new System.Windows.Forms.Label();
            this.convertDescriptionLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.categoryCheckBoxList = new System.Windows.Forms.CheckedListBox();
            this.menuStrip.SuspendLayout();
            this.startGroupBox.SuspendLayout();
            this.convertGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
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
            // startGroupBox
            // 
            this.startGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.startGroupBox.Controls.Add(this.sourceFileLabel);
            this.startGroupBox.Controls.Add(this.sourceFileButton);
            this.startGroupBox.Controls.Add(this.startButton);
            this.startGroupBox.Controls.Add(this.outputLabel);
            this.startGroupBox.Controls.Add(this.outputDirButton);
            this.startGroupBox.Location = new System.Drawing.Point(496, 29);
            this.startGroupBox.Name = "startGroupBox";
            this.startGroupBox.Size = new System.Drawing.Size(269, 354);
            this.startGroupBox.TabIndex = 19;
            this.startGroupBox.TabStop = false;
            // 
            // sourceFileLabel
            // 
            this.sourceFileLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.sourceFileLabel.Location = new System.Drawing.Point(6, 21);
            this.sourceFileLabel.Name = "sourceFileLabel";
            this.sourceFileLabel.Size = new System.Drawing.Size(257, 36);
            this.sourceFileLabel.TabIndex = 12;
            this.sourceFileLabel.Text = "Select Source File";
            this.sourceFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // sourceFileButton
            // 
            this.sourceFileButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.sourceFileButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.sourceFileButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.sourceFileButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.sourceFileButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sourceFileButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.sourceFileButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.sourceFileButton.Location = new System.Drawing.Point(43, 60);
            this.sourceFileButton.Name = "sourceFileButton";
            this.sourceFileButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.sourceFileButton.Size = new System.Drawing.Size(182, 30);
            this.sourceFileButton.TabIndex = 13;
            this.sourceFileButton.Text = "No File";
            this.sourceFileButton.UseVisualStyleBackColor = false;
            this.sourceFileButton.Click += new System.EventHandler(this.inputButton_Click);
            this.sourceFileButton.MouseHover += new System.EventHandler(this.inputButton_MouseHover);
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
            this.startButton.Location = new System.Drawing.Point(43, 305);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(182, 34);
            this.startButton.TabIndex = 11;
            this.startButton.Text = "Convert";
            this.startButton.UseVisualStyleBackColor = false;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // outputLabel
            // 
            this.outputLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.outputLabel.Location = new System.Drawing.Point(6, 111);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Size = new System.Drawing.Size(257, 36);
            this.outputLabel.TabIndex = 9;
            this.outputLabel.Text = "Select Output Location";
            this.outputLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // outputDirButton
            // 
            this.outputDirButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.outputDirButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.outputDirButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.outputDirButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.outputDirButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.outputDirButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.outputDirButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.outputDirButton.Location = new System.Drawing.Point(43, 147);
            this.outputDirButton.Margin = new System.Windows.Forms.Padding(0);
            this.outputDirButton.Name = "outputDirButton";
            this.outputDirButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.outputDirButton.Size = new System.Drawing.Size(182, 30);
            this.outputDirButton.TabIndex = 10;
            this.outputDirButton.Text = "No Directory";
            this.outputDirButton.UseVisualStyleBackColor = false;
            this.outputDirButton.Click += new System.EventHandler(this.outputDirButton_Click);
            this.outputDirButton.MouseHover += new System.EventHandler(this.outputDirButton_MouseHover);
            // 
            // convertGroupBox
            // 
            this.convertGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.convertGroupBox.Controls.Add(this.FormLabel);
            this.convertGroupBox.Controls.Add(this.convertDescriptionLabel);
            this.convertGroupBox.Location = new System.Drawing.Point(19, 29);
            this.convertGroupBox.Name = "convertGroupBox";
            this.convertGroupBox.Size = new System.Drawing.Size(452, 147);
            this.convertGroupBox.TabIndex = 18;
            this.convertGroupBox.TabStop = false;
            // 
            // FormLabel
            // 
            this.FormLabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.FormLabel.Location = new System.Drawing.Point(0, 21);
            this.FormLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.FormLabel.Name = "FormLabel";
            this.FormLabel.Size = new System.Drawing.Size(452, 39);
            this.FormLabel.TabIndex = 2;
            this.FormLabel.Text = "KML (Google Earth) Conversions";
            this.FormLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // convertDescriptionLabel
            // 
            this.convertDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertDescriptionLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertDescriptionLabel.Location = new System.Drawing.Point(9, 68);
            this.convertDescriptionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.convertDescriptionLabel.Name = "convertDescriptionLabel";
            this.convertDescriptionLabel.Size = new System.Drawing.Size(434, 66);
            this.convertDescriptionLabel.TabIndex = 8;
            this.convertDescriptionLabel.Text = "This process will convert your .SCT2/.KML file into .KML/.SCT2 file.";
            this.convertDescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.groupBox1.Controls.Add(this.categoryCheckBoxList);
            this.groupBox1.Location = new System.Drawing.Point(19, 176);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(452, 207);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            // 
            // categoryCheckBoxList
            // 
            this.categoryCheckBoxList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.categoryCheckBoxList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.categoryCheckBoxList.CheckOnClick = true;
            this.categoryCheckBoxList.ColumnWidth = 150;
            this.categoryCheckBoxList.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.categoryCheckBoxList.FormattingEnabled = true;
            this.categoryCheckBoxList.Items.AddRange(new object[] {
            "ALL",
            "Colors",
            "[INFO]",
            "[VOR]",
            "[NDB]",
            "[AIRPORT]",
            "[RUNWAY]",
            "[FIXES]",
            "[ARTCC]",
            "[ARTCC HIGH]",
            "[ARTCC LOW]",
            "[SID]",
            "[STAR]",
            "[LOW AIRWAY]",
            "[HIGH AIRWAY]",
            "[GEO]",
            "[REGIONS]",
            "[LABELS]"});
            this.categoryCheckBoxList.Location = new System.Drawing.Point(6, 26);
            this.categoryCheckBoxList.Name = "categoryCheckBoxList";
            this.categoryCheckBoxList.Size = new System.Drawing.Size(440, 168);
            this.categoryCheckBoxList.TabIndex = 1;
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
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.startGroupBox);
            this.Controls.Add(this.convertGroupBox);
            this.Controls.Add(this.menuStrip);
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
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.KMLConversionForm_Closing);
            this.Load += new System.EventHandler(this.KMLConversionForm_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.startGroupBox.ResumeLayout(false);
            this.convertGroupBox.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
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
        private System.Windows.Forms.GroupBox startGroupBox;
        private System.Windows.Forms.Label sourceFileLabel;
        private System.Windows.Forms.Button sourceFileButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label outputLabel;
        private System.Windows.Forms.Button outputDirButton;
        private System.Windows.Forms.GroupBox convertGroupBox;
        private System.Windows.Forms.Label convertDescriptionLabel;
        private System.Windows.Forms.Label FormLabel;
        private System.Windows.Forms.ToolStripMenuItem newsToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckedListBox categoryCheckBoxList;
    }
}

