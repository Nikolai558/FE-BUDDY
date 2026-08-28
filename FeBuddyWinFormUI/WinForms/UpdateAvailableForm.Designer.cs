namespace FeBuddyWinFormUI
{
    partial class UpdateAvailableForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.questionPanel = new System.Windows.Forms.Panel();
            this.questionLabel = new System.Windows.Forms.Label();
            this.releaseNotesPanel = new System.Windows.Forms.Panel();
            this.releaseNotesLabel = new System.Windows.Forms.Label();
            this.newVersionLabel = new System.Windows.Forms.Label();
            this.currentVersionLabel = new System.Windows.Forms.Label();
            this.headerLabel = new System.Windows.Forms.Label();
            this.yesButton = new System.Windows.Forms.Button();
            this.noButton = new System.Windows.Forms.Button();
            this.downloadPanel = new System.Windows.Forms.Panel();
            this.downloadStatusLabel = new System.Windows.Forms.Label();
            this.downloadProgressBar = new System.Windows.Forms.ProgressBar();
            this.downloadingLabel = new System.Windows.Forms.Label();
            this.questionPanel.SuspendLayout();
            this.releaseNotesPanel.SuspendLayout();
            this.downloadPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // questionPanel
            //
            this.questionPanel.Controls.Add(this.questionLabel);
            this.questionPanel.Controls.Add(this.releaseNotesPanel);
            this.questionPanel.Controls.Add(this.newVersionLabel);
            this.questionPanel.Controls.Add(this.currentVersionLabel);
            this.questionPanel.Controls.Add(this.headerLabel);
            this.questionPanel.Controls.Add(this.yesButton);
            this.questionPanel.Controls.Add(this.noButton);
            this.questionPanel.Location = new System.Drawing.Point(0, 0);
            this.questionPanel.Name = "questionPanel";
            this.questionPanel.Size = new System.Drawing.Size(600, 500);
            this.questionPanel.TabIndex = 0;
            //
            // questionLabel
            //
            this.questionLabel.AutoSize = true;
            this.questionLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.questionLabel.Location = new System.Drawing.Point(60, 405);
            this.questionLabel.Name = "questionLabel";
            this.questionLabel.Size = new System.Drawing.Size(400, 25);
            this.questionLabel.TabIndex = 5;
            this.questionLabel.Text = "Download and install this update now?";
            //
            // releaseNotesPanel
            //
            this.releaseNotesPanel.AutoScroll = true;
            this.releaseNotesPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.releaseNotesPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.releaseNotesPanel.Controls.Add(this.releaseNotesLabel);
            this.releaseNotesPanel.Location = new System.Drawing.Point(20, 120);
            this.releaseNotesPanel.Name = "releaseNotesPanel";
            this.releaseNotesPanel.Size = new System.Drawing.Size(560, 270);
            this.releaseNotesPanel.TabIndex = 4;
            //
            // releaseNotesLabel
            //
            this.releaseNotesLabel.AutoSize = true;
            this.releaseNotesLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.releaseNotesLabel.Location = new System.Drawing.Point(8, 8);
            this.releaseNotesLabel.MaximumSize = new System.Drawing.Size(530, 0);
            this.releaseNotesLabel.Name = "releaseNotesLabel";
            this.releaseNotesLabel.Size = new System.Drawing.Size(200, 19);
            this.releaseNotesLabel.TabIndex = 0;
            this.releaseNotesLabel.Text = "<release notes>";
            //
            // newVersionLabel
            //
            this.newVersionLabel.AutoSize = true;
            this.newVersionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.newVersionLabel.Location = new System.Drawing.Point(20, 90);
            this.newVersionLabel.Name = "newVersionLabel";
            this.newVersionLabel.Size = new System.Drawing.Size(300, 21);
            this.newVersionLabel.TabIndex = 3;
            this.newVersionLabel.Text = "New version available: <version>";
            //
            // currentVersionLabel
            //
            this.currentVersionLabel.AutoSize = true;
            this.currentVersionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentVersionLabel.Location = new System.Drawing.Point(20, 65);
            this.currentVersionLabel.Name = "currentVersionLabel";
            this.currentVersionLabel.Size = new System.Drawing.Size(220, 21);
            this.currentVersionLabel.TabIndex = 2;
            this.currentVersionLabel.Text = "Your program version: <version>";
            //
            // headerLabel
            //
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.headerLabel.Location = new System.Drawing.Point(180, 20);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(252, 25);
            this.headerLabel.TabIndex = 1;
            this.headerLabel.Text = "*** UPDATE AVAILABLE ***";
            //
            // yesButton
            //
            this.yesButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.yesButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.yesButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.yesButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.yesButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.yesButton.Location = new System.Drawing.Point(166, 445);
            this.yesButton.Name = "yesButton";
            this.yesButton.Size = new System.Drawing.Size(110, 40);
            this.yesButton.TabIndex = 6;
            this.yesButton.Text = "Yes";
            this.yesButton.UseVisualStyleBackColor = true;
            this.yesButton.Click += new System.EventHandler(this.YesButton_Click);
            //
            // noButton
            //
            this.noButton.DialogResult = System.Windows.Forms.DialogResult.No;
            this.noButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.noButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.noButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.noButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.noButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noButton.Location = new System.Drawing.Point(329, 445);
            this.noButton.Name = "noButton";
            this.noButton.Size = new System.Drawing.Size(110, 40);
            this.noButton.TabIndex = 7;
            this.noButton.Text = "Not Now";
            this.noButton.UseVisualStyleBackColor = true;
            //
            // downloadPanel
            //
            this.downloadPanel.Controls.Add(this.downloadStatusLabel);
            this.downloadPanel.Controls.Add(this.downloadProgressBar);
            this.downloadPanel.Controls.Add(this.downloadingLabel);
            this.downloadPanel.Location = new System.Drawing.Point(0, 0);
            this.downloadPanel.Name = "downloadPanel";
            this.downloadPanel.Size = new System.Drawing.Size(600, 500);
            this.downloadPanel.TabIndex = 1;
            this.downloadPanel.Visible = false;
            //
            // downloadStatusLabel
            //
            this.downloadStatusLabel.AutoSize = true;
            this.downloadStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.downloadStatusLabel.Location = new System.Drawing.Point(60, 280);
            this.downloadStatusLabel.Name = "downloadStatusLabel";
            this.downloadStatusLabel.Size = new System.Drawing.Size(80, 19);
            this.downloadStatusLabel.TabIndex = 2;
            this.downloadStatusLabel.Text = "0%";
            //
            // downloadProgressBar
            //
            this.downloadProgressBar.Location = new System.Drawing.Point(60, 245);
            this.downloadProgressBar.Name = "downloadProgressBar";
            this.downloadProgressBar.Size = new System.Drawing.Size(480, 28);
            this.downloadProgressBar.TabIndex = 1;
            //
            // downloadingLabel
            //
            this.downloadingLabel.AutoSize = true;
            this.downloadingLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.downloadingLabel.Location = new System.Drawing.Point(190, 200);
            this.downloadingLabel.Name = "downloadingLabel";
            this.downloadingLabel.Size = new System.Drawing.Size(220, 32);
            this.downloadingLabel.TabIndex = 0;
            this.downloadingLabel.Text = "Downloading Update...";
            //
            // UpdateAvailableForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.ClientSize = new System.Drawing.Size(600, 500);
            this.Controls.Add(this.downloadPanel);
            this.Controls.Add(this.questionPanel);
            this.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateAvailableForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FE-BUDDY Update";
            this.questionPanel.ResumeLayout(false);
            this.questionPanel.PerformLayout();
            this.releaseNotesPanel.ResumeLayout(false);
            this.releaseNotesPanel.PerformLayout();
            this.downloadPanel.ResumeLayout(false);
            this.downloadPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel questionPanel;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Label currentVersionLabel;
        private System.Windows.Forms.Label newVersionLabel;
        private System.Windows.Forms.Panel releaseNotesPanel;
        private System.Windows.Forms.Label releaseNotesLabel;
        private System.Windows.Forms.Label questionLabel;
        private System.Windows.Forms.Button yesButton;
        private System.Windows.Forms.Button noButton;
        private System.Windows.Forms.Panel downloadPanel;
        private System.Windows.Forms.Label downloadingLabel;
        private System.Windows.Forms.ProgressBar downloadProgressBar;
        private System.Windows.Forms.Label downloadStatusLabel;
    }
}
