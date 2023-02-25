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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PopUp));
            this.panelBG = new System.Windows.Forms.Panel();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.panelTitle = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.panelContent = new System.Windows.Forms.Panel();
            this.lblHeading = new System.Windows.Forms.Label();
            this.lblInfo = new System.Windows.Forms.Label();
            this.panelBG.SuspendLayout();
            this.panelTitle.SuspendLayout();
            this.panelContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelBG
            // 
            this.panelBG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(84)))), ((int)(((byte)(142)))));
            this.panelBG.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelBG.Controls.Add(this.btnYes);
            this.panelBG.Controls.Add(this.btnNo);
            this.panelBG.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBG.Location = new System.Drawing.Point(0, 162);
            this.panelBG.Name = "panelBG";
            this.panelBG.Size = new System.Drawing.Size(407, 42);
            this.panelBG.TabIndex = 3;
            // 
            // btnYes
            // 
            this.btnYes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(162)))), ((int)(((byte)(127)))), ((int)(((byte)(168)))));
            this.btnYes.FlatAppearance.BorderSize = 0;
            this.btnYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnYes.ForeColor = System.Drawing.Color.White;
            this.btnYes.Location = new System.Drawing.Point(12, 8);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(85, 25);
            this.btnYes.TabIndex = 3;
            this.btnYes.Text = "Ja";
            this.btnYes.UseVisualStyleBackColor = false;
            // 
            // btnNo
            // 
            this.btnNo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(162)))), ((int)(((byte)(127)))), ((int)(((byte)(168)))));
            this.btnNo.FlatAppearance.BorderSize = 0;
            this.btnNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNo.ForeColor = System.Drawing.Color.White;
            this.btnNo.Location = new System.Drawing.Point(310, 8);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(85, 25);
            this.btnNo.TabIndex = 2;
            this.btnNo.Text = "Nein";
            this.btnNo.UseVisualStyleBackColor = false;
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // panelTitle
            // 
            this.panelTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(25)))), ((int)(((byte)(66)))));
            this.panelTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelTitle.Controls.Add(this.lblTitle);
            this.panelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitle.Location = new System.Drawing.Point(0, 0);
            this.panelTitle.Name = "panelTitle";
            this.panelTitle.Size = new System.Drawing.Size(407, 31);
            this.panelTitle.TabIndex = 5;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(405, 29);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Automatischer Shutdown";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Auto Shutdown";
            this.notifyIcon.Visible = true;
            // 
            // panelContent
            // 
            this.panelContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(104)))), ((int)(((byte)(152)))));
            this.panelContent.Controls.Add(this.lblHeading);
            this.panelContent.Controls.Add(this.lblInfo);
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.ForeColor = System.Drawing.Color.White;
            this.panelContent.Location = new System.Drawing.Point(0, 31);
            this.panelContent.Name = "panelContent";
            this.panelContent.Size = new System.Drawing.Size(407, 131);
            this.panelContent.TabIndex = 6;
            // 
            // lblHeading
            // 
            this.lblHeading.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHeading.Font = new System.Drawing.Font("Calibri", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblHeading.ForeColor = System.Drawing.Color.White;
            this.lblHeading.Location = new System.Drawing.Point(0, 0);
            this.lblHeading.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeading.Name = "lblHeading";
            this.lblHeading.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeading.Size = new System.Drawing.Size(407, 51);
            this.lblHeading.TabIndex = 6;
            this.lblHeading.Text = "Wollen Sie den Computer herunterfahren?";
            this.lblHeading.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblInfo
            // 
            this.lblInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblInfo.Font = new System.Drawing.Font("Calibri", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblInfo.ForeColor = System.Drawing.Color.White;
            this.lblInfo.Location = new System.Drawing.Point(0, 51);
            this.lblInfo.Margin = new System.Windows.Forms.Padding(0);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblInfo.Size = new System.Drawing.Size(407, 80);
            this.lblInfo.TabIndex = 5;
            this.lblInfo.Text = "Es ist keine weitere Stunde in WebUntis eingetragen.\r\nKlicken Sie bitte auf Nein," +
    " um das Herunterfahren zu verhindern!";
            // 
            // PopUp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(159)))), ((int)(((byte)(134)))), ((int)(((byte)(192)))));
            this.ClientSize = new System.Drawing.Size(407, 204);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelTitle);
            this.Controls.Add(this.panelBG);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PopUp";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Automatischer Shutdown";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.PopUp_Shown);
            this.panelBG.ResumeLayout(false);
            this.panelTitle.ResumeLayout(false);
            this.panelContent.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Panel panelBG;
        private Button btnYes;
        private Button btnNo;
        private Panel panelTitle;
        private Label lblTitle;
        private NotifyIcon notifyIcon;
        private Panel panelContent;
        private Label lblHeading;
        private Label lblInfo;
    }
}