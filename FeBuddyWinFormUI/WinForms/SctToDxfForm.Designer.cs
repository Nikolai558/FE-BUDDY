namespace FeBuddyWinFormUI
{
    partial class SctToDxfForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SctToDxfForm));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.informationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.InstructionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RoadmapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FAQMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ChangeLogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreditsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.discordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportIssuesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.allowBetaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UninstallMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.inputButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.filePathLabel = new System.Windows.Forms.Label();
            this.outputDirButton = new System.Windows.Forms.Button();
            this.convertGroupBox = new System.Windows.Forms.GroupBox();
            this.convertDescriptionLabel = new System.Windows.Forms.Label();
            this.airacCycleGroupBox = new System.Windows.Forms.GroupBox();
            this.airacLabel = new System.Windows.Forms.Label();
            this.currentAiracSelection = new System.Windows.Forms.RadioButton();
            this.nextAiracSelection = new System.Windows.Forms.RadioButton();
            this.menuStrip.SuspendLayout();
            this.startGroupBox.SuspendLayout();
            this.convertGroupBox.SuspendLayout();
            this.airacCycleGroupBox.SuspendLayout();
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
            this.discordToolStripMenuItem.Size = new System.Drawing.Size(72, 22);
            this.discordToolStripMenuItem.Text = "Buddies";
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
            this.startGroupBox.Controls.Add(this.label1);
            this.startGroupBox.Controls.Add(this.inputButton);
            this.startGroupBox.Controls.Add(this.startButton);
            this.startGroupBox.Controls.Add(this.filePathLabel);
            this.startGroupBox.Controls.Add(this.outputDirButton);
            this.startGroupBox.Location = new System.Drawing.Point(496, 18);
            this.startGroupBox.Name = "startGroupBox";
            this.startGroupBox.Size = new System.Drawing.Size(269, 365);
            this.startGroupBox.TabIndex = 19;
            this.startGroupBox.TabStop = false;
            // 
            // label1
            // 
            this.label1.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.label1.Location = new System.Drawing.Point(6, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(257, 36);
            this.label1.TabIndex = 12;
            this.label1.Text = "Select Input Location";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.label1.Visible = false;
            // 
            // inputButton
            // 
            this.inputButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.inputButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.inputButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.inputButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Black;
            this.inputButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inputButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.inputButton.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.inputButton.Location = new System.Drawing.Point(40, 57);
            this.inputButton.Name = "inputButton";
            this.inputButton.Size = new System.Drawing.Size(182, 34);
            this.inputButton.TabIndex = 13;
            this.inputButton.Text = "No File";
            this.inputButton.UseVisualStyleBackColor = false;
            this.inputButton.Click += new System.EventHandler(this.inputButton_Click);
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
            this.startButton.Text = "Convert";
            this.startButton.UseVisualStyleBackColor = false;
            // 
            // filePathLabel
            // 
            this.filePathLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.filePathLabel.Location = new System.Drawing.Point(6, 131);
            this.filePathLabel.Name = "filePathLabel";
            this.filePathLabel.Size = new System.Drawing.Size(257, 36);
            this.filePathLabel.TabIndex = 9;
            this.filePathLabel.Text = "Select Output Location";
            this.filePathLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.filePathLabel.Visible = false;
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
            this.outputDirButton.Location = new System.Drawing.Point(40, 170);
            this.outputDirButton.Name = "outputDirButton";
            this.outputDirButton.Size = new System.Drawing.Size(182, 34);
            this.outputDirButton.TabIndex = 10;
            this.outputDirButton.Text = "No File";
            this.outputDirButton.UseVisualStyleBackColor = false;
            this.outputDirButton.Click += new System.EventHandler(this.outputDirButton_Click);
            // 
            // convertGroupBox
            // 
            this.convertGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.convertGroupBox.Controls.Add(this.convertDescriptionLabel);
            this.convertGroupBox.Location = new System.Drawing.Point(19, 149);
            this.convertGroupBox.Name = "convertGroupBox";
            this.convertGroupBox.Size = new System.Drawing.Size(452, 237);
            this.convertGroupBox.TabIndex = 18;
            this.convertGroupBox.TabStop = false;
            // 
            // convertDescriptionLabel
            // 
            this.convertDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.convertDescriptionLabel.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.convertDescriptionLabel.Location = new System.Drawing.Point(9, 15);
            this.convertDescriptionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.convertDescriptionLabel.Name = "convertDescriptionLabel";
            this.convertDescriptionLabel.Size = new System.Drawing.Size(434, 222);
            this.convertDescriptionLabel.TabIndex = 8;
            this.convertDescriptionLabel.Text = resources.GetString("convertDescriptionLabel.Text");
            this.convertDescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // airacCycleGroupBox
            // 
            this.airacCycleGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.airacCycleGroupBox.Controls.Add(this.airacLabel);
            this.airacCycleGroupBox.Controls.Add(this.currentAiracSelection);
            this.airacCycleGroupBox.Controls.Add(this.nextAiracSelection);
            this.airacCycleGroupBox.Location = new System.Drawing.Point(19, 18);
            this.airacCycleGroupBox.Name = "airacCycleGroupBox";
            this.airacCycleGroupBox.Size = new System.Drawing.Size(452, 125);
            this.airacCycleGroupBox.TabIndex = 17;
            this.airacCycleGroupBox.TabStop = false;
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
            this.airacLabel.Text = "Select Conversion Type";
            this.airacLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.currentAiracSelection.Size = new System.Drawing.Size(114, 25);
            this.currentAiracSelection.TabIndex = 0;
            this.currentAiracSelection.TabStop = true;
            this.currentAiracSelection.Text = "SCT2 to DXF";
            this.currentAiracSelection.UseVisualStyleBackColor = true;
            // 
            // nextAiracSelection
            // 
            this.nextAiracSelection.AutoSize = true;
            this.nextAiracSelection.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.nextAiracSelection.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.nextAiracSelection.Location = new System.Drawing.Point(232, 68);
            this.nextAiracSelection.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.nextAiracSelection.Name = "nextAiracSelection";
            this.nextAiracSelection.Size = new System.Drawing.Size(114, 25);
            this.nextAiracSelection.TabIndex = 1;
            this.nextAiracSelection.Text = "DXF to SCT2";
            this.nextAiracSelection.UseVisualStyleBackColor = true;
            // 
            // SctToDxfForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.BackgroundImage = global::FeBuddyWinFormUI.Properties.Resources.window_background;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(784, 404);
            this.Controls.Add(this.startGroupBox);
            this.Controls.Add(this.convertGroupBox);
            this.Controls.Add(this.airacCycleGroupBox);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.MaximizeBox = false;
            this.Name = "SctToDxfForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FE-BUDDY";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SctToDxfForm_Closing);
            this.Load += new System.EventHandler(this.AiracDataForm_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.startGroupBox.ResumeLayout(false);
            this.convertGroupBox.ResumeLayout(false);
            this.airacCycleGroupBox.ResumeLayout(false);
            this.airacCycleGroupBox.PerformLayout();
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button inputButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label filePathLabel;
        private System.Windows.Forms.Button outputDirButton;
        private System.Windows.Forms.GroupBox convertGroupBox;
        private System.Windows.Forms.Label convertDescriptionLabel;
        private System.Windows.Forms.GroupBox airacCycleGroupBox;
        private System.Windows.Forms.Label airacLabel;
        private System.Windows.Forms.RadioButton currentAiracSelection;
        private System.Windows.Forms.RadioButton nextAiracSelection;
    }
}

