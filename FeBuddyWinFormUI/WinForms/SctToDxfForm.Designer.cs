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
            this.airacCycleGroupBox = new System.Windows.Forms.GroupBox();
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
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // airacCycleGroupBox
            // 
            this.airacCycleGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.airacCycleGroupBox.Location = new System.Drawing.Point(20, 29);
            this.airacCycleGroupBox.Name = "airacCycleGroupBox";
            this.airacCycleGroupBox.Size = new System.Drawing.Size(752, 363);
            this.airacCycleGroupBox.TabIndex = 12;
            this.airacCycleGroupBox.TabStop = false;
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
            // SctToDxfForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.BackgroundImage = global::FeBuddyWinFormUI.Properties.Resources.window_background;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(784, 404);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.airacCycleGroupBox);
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
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.GroupBox airacCycleGroupBox;
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
    }
}

