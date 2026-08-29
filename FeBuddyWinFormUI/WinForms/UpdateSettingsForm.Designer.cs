namespace FeBuddyWinFormUI
{
    partial class UpdateSettingsForm
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.stableRadioButton = new System.Windows.Forms.RadioButton();
            this.stableDescriptionLabel = new System.Windows.Forms.Label();
            this.rcRadioButton = new System.Windows.Forms.RadioButton();
            this.rcDescriptionLabel = new System.Windows.Forms.Label();
            this.betaRadioButton = new System.Windows.Forms.RadioButton();
            this.betaDescriptionLabel = new System.Windows.Forms.Label();
            this.alphaRadioButton = new System.Windows.Forms.RadioButton();
            this.alphaDescriptionLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(20, 15);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(180, 30);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Update Channel";
            //
            // descriptionLabel
            //
            this.descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.descriptionLabel.Location = new System.Drawing.Point(20, 52);
            this.descriptionLabel.MaximumSize = new System.Drawing.Size(420, 0);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(420, 40);
            this.descriptionLabel.TabIndex = 1;
            this.descriptionLabel.Text = "Choose which type of releases FE-BUDDY should check for and offer to install. A" +
    "nything above Stable may be unfinished or contain bugs.";
            //
            // stableRadioButton
            //
            this.stableRadioButton.AutoSize = true;
            this.stableRadioButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stableRadioButton.Location = new System.Drawing.Point(20, 105);
            this.stableRadioButton.Name = "stableRadioButton";
            this.stableRadioButton.Size = new System.Drawing.Size(190, 24);
            this.stableRadioButton.TabIndex = 2;
            this.stableRadioButton.Text = "Stable (recommended)";
            this.stableRadioButton.UseVisualStyleBackColor = true;
            //
            // stableDescriptionLabel
            //
            this.stableDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stableDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.stableDescriptionLabel.Location = new System.Drawing.Point(40, 130);
            this.stableDescriptionLabel.Name = "stableDescriptionLabel";
            this.stableDescriptionLabel.Size = new System.Drawing.Size(400, 20);
            this.stableDescriptionLabel.TabIndex = 3;
            this.stableDescriptionLabel.Text = "Only fully released, finished versions.";
            //
            // rcRadioButton
            //
            this.rcRadioButton.AutoSize = true;
            this.rcRadioButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rcRadioButton.Location = new System.Drawing.Point(20, 160);
            this.rcRadioButton.Name = "rcRadioButton";
            this.rcRadioButton.Size = new System.Drawing.Size(140, 24);
            this.rcRadioButton.TabIndex = 4;
            this.rcRadioButton.Text = "Release Candidate";
            this.rcRadioButton.UseVisualStyleBackColor = true;
            //
            // rcDescriptionLabel
            //
            this.rcDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rcDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.rcDescriptionLabel.Location = new System.Drawing.Point(40, 185);
            this.rcDescriptionLabel.Name = "rcDescriptionLabel";
            this.rcDescriptionLabel.Size = new System.Drawing.Size(400, 20);
            this.rcDescriptionLabel.TabIndex = 5;
            this.rcDescriptionLabel.Text = "Believed ready to ship; final testing before release.";
            //
            // betaRadioButton
            //
            this.betaRadioButton.AutoSize = true;
            this.betaRadioButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.betaRadioButton.Location = new System.Drawing.Point(20, 215);
            this.betaRadioButton.Name = "betaRadioButton";
            this.betaRadioButton.Size = new System.Drawing.Size(70, 24);
            this.betaRadioButton.TabIndex = 6;
            this.betaRadioButton.Text = "Beta";
            this.betaRadioButton.UseVisualStyleBackColor = true;
            //
            // betaDescriptionLabel
            //
            this.betaDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.betaDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.betaDescriptionLabel.Location = new System.Drawing.Point(40, 240);
            this.betaDescriptionLabel.Name = "betaDescriptionLabel";
            this.betaDescriptionLabel.Size = new System.Drawing.Size(400, 20);
            this.betaDescriptionLabel.TabIndex = 7;
            this.betaDescriptionLabel.Text = "Feature-complete; being tested for bugs and polish.";
            //
            // alphaRadioButton
            //
            this.alphaRadioButton.AutoSize = true;
            this.alphaRadioButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.alphaRadioButton.Location = new System.Drawing.Point(20, 270);
            this.alphaRadioButton.Name = "alphaRadioButton";
            this.alphaRadioButton.Size = new System.Drawing.Size(75, 24);
            this.alphaRadioButton.TabIndex = 8;
            this.alphaRadioButton.Text = "Alpha";
            this.alphaRadioButton.UseVisualStyleBackColor = true;
            //
            // alphaDescriptionLabel
            //
            this.alphaDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.alphaDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.alphaDescriptionLabel.Location = new System.Drawing.Point(40, 295);
            this.alphaDescriptionLabel.Name = "alphaDescriptionLabel";
            this.alphaDescriptionLabel.Size = new System.Drawing.Size(400, 20);
            this.alphaDescriptionLabel.TabIndex = 9;
            this.alphaDescriptionLabel.Text = "Early, in-progress builds. May be incomplete or unstable.";
            //
            // saveButton
            //
            this.saveButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.saveButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.saveButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.saveButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.Location = new System.Drawing.Point(220, 335);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 36);
            this.saveButton.TabIndex = 10;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.cancelButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.cancelButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelButton.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(330, 335);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 36);
            this.cancelButton.TabIndex = 11;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // UpdateSettingsForm
            //
            this.AcceptButton = this.saveButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(460, 392);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.alphaDescriptionLabel);
            this.Controls.Add(this.alphaRadioButton);
            this.Controls.Add(this.betaDescriptionLabel);
            this.Controls.Add(this.betaRadioButton);
            this.Controls.Add(this.rcDescriptionLabel);
            this.Controls.Add(this.rcRadioButton);
            this.Controls.Add(this.stableDescriptionLabel);
            this.Controls.Add(this.stableRadioButton);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.titleLabel);
            this.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Update Channel";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.RadioButton stableRadioButton;
        private System.Windows.Forms.Label stableDescriptionLabel;
        private System.Windows.Forms.RadioButton rcRadioButton;
        private System.Windows.Forms.Label rcDescriptionLabel;
        private System.Windows.Forms.RadioButton betaRadioButton;
        private System.Windows.Forms.Label betaDescriptionLabel;
        private System.Windows.Forms.RadioButton alphaRadioButton;
        private System.Windows.Forms.Label alphaDescriptionLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
