namespace FeBuddyWinFormUI
{
    partial class UpdateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            this.processingLabel = new System.Windows.Forms.Label();
            this.updatePanel = new System.Windows.Forms.Panel();
            this.githubMessagelabel = new System.Windows.Forms.Label();
            this.githubVersionLabel = new System.Windows.Forms.Label();
            this.programVersionLabel = new System.Windows.Forms.Label();
            this.questionLabel = new System.Windows.Forms.Label();
            this.questionHeaderlabel = new System.Windows.Forms.Label();
            this.yesButton = new System.Windows.Forms.Button();
            this.noButton = new System.Windows.Forms.Button();
            this.updatePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // processingLabel
            // 
            this.processingLabel.AutoSize = true;
            this.processingLabel.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.processingLabel.Location = new System.Drawing.Point(325, 286);
            this.processingLabel.MaximumSize = new System.Drawing.Size(550, 2500);
            this.processingLabel.Name = "processingLabel";
            this.processingLabel.Size = new System.Drawing.Size(228, 32);
            this.processingLabel.TabIndex = 0;
            this.processingLabel.Text = "Processing Update";
            // 
            // updatePanel
            // 
            this.updatePanel.AutoScroll = true;
            this.updatePanel.Controls.Add(this.githubMessagelabel);
            this.updatePanel.Controls.Add(this.githubVersionLabel);
            this.updatePanel.Controls.Add(this.processingLabel);
            this.updatePanel.Controls.Add(this.programVersionLabel);
            this.updatePanel.Location = new System.Drawing.Point(12, 37);
            this.updatePanel.Name = "updatePanel";
            this.updatePanel.Size = new System.Drawing.Size(560, 370);
            this.updatePanel.TabIndex = 1;
            // 
            // githubMessagelabel
            // 
            this.githubMessagelabel.AutoSize = true;
            this.githubMessagelabel.Enabled = false;
            this.githubMessagelabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.githubMessagelabel.Location = new System.Drawing.Point(3, 76);
            this.githubMessagelabel.Name = "githubMessagelabel";
            this.githubMessagelabel.Size = new System.Drawing.Size(189, 21);
            this.githubMessagelabel.TabIndex = 4;
            this.githubMessagelabel.Text = "<insert github msg here>";
            this.githubMessagelabel.Visible = false;
            // 
            // githubVersionLabel
            // 
            this.githubVersionLabel.AutoSize = true;
            this.githubVersionLabel.Enabled = false;
            this.githubVersionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.githubVersionLabel.Location = new System.Drawing.Point(3, 31);
            this.githubVersionLabel.Name = "githubVersionLabel";
            this.githubVersionLabel.Size = new System.Drawing.Size(298, 21);
            this.githubVersionLabel.TabIndex = 3;
            this.githubVersionLabel.Text = "Latest release version: <version number>";
            this.githubVersionLabel.Visible = false;
            // 
            // programVersionLabel
            // 
            this.programVersionLabel.AutoSize = true;
            this.programVersionLabel.Enabled = false;
            this.programVersionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.programVersionLabel.Location = new System.Drawing.Point(3, 7);
            this.programVersionLabel.Name = "programVersionLabel";
            this.programVersionLabel.Size = new System.Drawing.Size(301, 21);
            this.programVersionLabel.TabIndex = 2;
            this.programVersionLabel.Text = "Your program version: <version number>";
            this.programVersionLabel.Visible = false;
            // 
            // questionLabel
            // 
            this.questionLabel.AutoSize = true;
            this.questionLabel.Enabled = false;
            this.questionLabel.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.questionLabel.Location = new System.Drawing.Point(106, 434);
            this.questionLabel.Name = "questionLabel";
            this.questionLabel.Size = new System.Drawing.Size(372, 32);
            this.questionLabel.TabIndex = 2;
            this.questionLabel.Text = "Would you like to update now?";
            this.questionLabel.Visible = false;
            // 
            // questionHeaderlabel
            // 
            this.questionHeaderlabel.AutoSize = true;
            this.questionHeaderlabel.Enabled = false;
            this.questionHeaderlabel.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.questionHeaderlabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.questionHeaderlabel.Location = new System.Drawing.Point(166, 9);
            this.questionHeaderlabel.Name = "questionHeaderlabel";
            this.questionHeaderlabel.Size = new System.Drawing.Size(252, 25);
            this.questionHeaderlabel.TabIndex = 1;
            this.questionHeaderlabel.Text = "*** UPDATE AVAILABLE ***";
            this.questionHeaderlabel.Visible = false;
            // 
            // yesButton
            // 
            this.yesButton.Enabled = false;
            this.yesButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.yesButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.yesButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.yesButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.yesButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.yesButton.Location = new System.Drawing.Point(166, 478);
            this.yesButton.Name = "yesButton";
            this.yesButton.Size = new System.Drawing.Size(89, 40);
            this.yesButton.TabIndex = 3;
            this.yesButton.Text = "Yes";
            this.yesButton.UseVisualStyleBackColor = true;
            this.yesButton.Visible = false;
            this.yesButton.Click += new System.EventHandler(this.yesButton_Click);
            // 
            // noButton
            // 
            this.noButton.Enabled = false;
            this.noButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.noButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.noButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.noButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.noButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noButton.Location = new System.Drawing.Point(329, 478);
            this.noButton.Name = "noButton";
            this.noButton.Size = new System.Drawing.Size(89, 40);
            this.noButton.TabIndex = 4;
            this.noButton.Text = "No";
            this.noButton.UseVisualStyleBackColor = true;
            this.noButton.Visible = false;
            this.noButton.Click += new System.EventHandler(this.noButton_Click);
            // 
            // UpdateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.ClientSize = new System.Drawing.Size(584, 561);
            this.Controls.Add(this.noButton);
            this.Controls.Add(this.yesButton);
            this.Controls.Add(this.questionLabel);
            this.Controls.Add(this.questionHeaderlabel);
            this.Controls.Add(this.updatePanel);
            this.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Processing Data";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UpdateForm_Closing);
            this.updatePanel.ResumeLayout(false);
            this.updatePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label processingLabel;
        private System.Windows.Forms.Panel updatePanel;
        private System.Windows.Forms.Label questionLabel;
        private System.Windows.Forms.Label questionHeaderlabel;
        private System.Windows.Forms.Label githubMessagelabel;
        private System.Windows.Forms.Label githubVersionLabel;
        private System.Windows.Forms.Label programVersionLabel;
        private System.Windows.Forms.Button yesButton;
        private System.Windows.Forms.Button noButton;
    }
}