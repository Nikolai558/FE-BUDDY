namespace FeBuddyWinFormUI
{
    partial class CreditsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreditsForm));
            this.developedByHeader = new System.Windows.Forms.Label();
            this.nikolasBolingLabel = new System.Windows.Forms.Label();
            this.kyleSandersLabel = new System.Windows.Forms.Label();
            this.logoHeader = new System.Windows.Forms.Label();
            this.johnLewisLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // developedByHeader
            // 
            this.developedByHeader.AutoSize = true;
            this.developedByHeader.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.developedByHeader.Location = new System.Drawing.Point(12, 9);
            this.developedByHeader.Name = "developedByHeader";
            this.developedByHeader.Size = new System.Drawing.Size(166, 30);
            this.developedByHeader.TabIndex = 0;
            this.developedByHeader.Text = "DEVELOPED BY:";
            // 
            // nikolasBolingLabel
            // 
            this.nikolasBolingLabel.AutoSize = true;
            this.nikolasBolingLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nikolasBolingLabel.Location = new System.Drawing.Point(32, 49);
            this.nikolasBolingLabel.Name = "nikolasBolingLabel";
            this.nikolasBolingLabel.Size = new System.Drawing.Size(360, 63);
            this.nikolasBolingLabel.TabIndex = 1;
            this.nikolasBolingLabel.Text = "Nikolas Boling\r\n    VATSIM CID: 1474952 \r\n    GITHUB PROFILE: https://github.com/" +
    "Nikolai558";
            this.nikolasBolingLabel.Click += new System.EventHandler(this.nikolasBolingLabel_Click);
            // 
            // kyleSandersLabel
            // 
            this.kyleSandersLabel.AutoSize = true;
            this.kyleSandersLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.kyleSandersLabel.Location = new System.Drawing.Point(32, 137);
            this.kyleSandersLabel.Name = "kyleSandersLabel";
            this.kyleSandersLabel.Size = new System.Drawing.Size(385, 63);
            this.kyleSandersLabel.TabIndex = 2;
            this.kyleSandersLabel.Text = "Kyle Sanders\r\n    VATSIM CID: 1187148\r\n    GITHUB PROFILE: https://github.com/KSa" +
    "nders7070";
            this.kyleSandersLabel.Click += new System.EventHandler(this.kyleSandersLabel_Click);
            // 
            // logoHeader
            // 
            this.logoHeader.AutoSize = true;
            this.logoHeader.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logoHeader.Location = new System.Drawing.Point(12, 238);
            this.logoHeader.Name = "logoHeader";
            this.logoHeader.Size = new System.Drawing.Size(169, 30);
            this.logoHeader.TabIndex = 3;
            this.logoHeader.Text = "ICON/LOGO BY:";
            // 
            // johnLewisLabel
            // 
            this.johnLewisLabel.AutoSize = true;
            this.johnLewisLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.johnLewisLabel.Location = new System.Drawing.Point(32, 277);
            this.johnLewisLabel.Name = "johnLewisLabel";
            this.johnLewisLabel.Size = new System.Drawing.Size(178, 42);
            this.johnLewisLabel.TabIndex = 4;
            this.johnLewisLabel.Text = "John Lewis\r\n    VATSIM CID: 1476059";
            this.johnLewisLabel.Click += new System.EventHandler(this.johnLewisLabel_Click);
            // 
            // CreditsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.ClientSize = new System.Drawing.Size(458, 354);
            this.Controls.Add(this.johnLewisLabel);
            this.Controls.Add(this.logoHeader);
            this.Controls.Add(this.kyleSandersLabel);
            this.Controls.Add(this.nikolasBolingLabel);
            this.Controls.Add(this.developedByHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreditsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Credits";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label developedByHeader;
        private System.Windows.Forms.Label nikolasBolingLabel;
        private System.Windows.Forms.Label kyleSandersLabel;
        private System.Windows.Forms.Label logoHeader;
        private System.Windows.Forms.Label johnLewisLabel;
    }
}