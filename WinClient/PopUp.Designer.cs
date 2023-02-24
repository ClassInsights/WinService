namespace WinClient
{
    partial class PopUp
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblInfo = new System.Windows.Forms.Label();
            this.panelBG = new System.Windows.Forms.Panel();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.panelBG.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblInfo
            // 
            this.lblInfo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblInfo.ForeColor = System.Drawing.Color.White;
            this.lblInfo.Location = new System.Drawing.Point(0, 19);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(408, 39);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "Wollen Sie den Computer herunterfahren?";
            this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelBG
            // 
            this.panelBG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(84)))), ((int)(((byte)(142)))));
            this.panelBG.Controls.Add(this.btnYes);
            this.panelBG.Controls.Add(this.btnNo);
            this.panelBG.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBG.Location = new System.Drawing.Point(0, 94);
            this.panelBG.Name = "panelBG";
            this.panelBG.Size = new System.Drawing.Size(408, 62);
            this.panelBG.TabIndex = 3;
            // 
            // btnYes
            // 
            this.btnYes.Location = new System.Drawing.Point(12, 27);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(100, 23);
            this.btnYes.TabIndex = 3;
            this.btnYes.Text = "Ja";
            this.btnYes.UseVisualStyleBackColor = true;
            // 
            // btnNo
            // 
            this.btnNo.Location = new System.Drawing.Point(311, 27);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(85, 23);
            this.btnNo.TabIndex = 2;
            this.btnNo.Text = "Nein";
            this.btnNo.UseVisualStyleBackColor = true;
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // PopUp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(159)))), ((int)(((byte)(134)))), ((int)(((byte)(192)))));
            this.ClientSize = new System.Drawing.Size(408, 156);
            this.Controls.Add(this.panelBG);
            this.Controls.Add(this.lblInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PopUp";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Automatischer Shutdown";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.PopUp_Shown);
            this.panelBG.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Label lblInfo;
        private Panel panelBG;
        private Button btnYes;
        private Button btnNo;
    }
}