namespace ChatServer
{
    partial class ServerScreen
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
            this.txtMessageLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtMessageLog
            // 
            this.txtMessageLog.Location = new System.Drawing.Point(58, 51);
            this.txtMessageLog.Multiline = true;
            this.txtMessageLog.Name = "txtMessageLog";
            this.txtMessageLog.ReadOnly = true;
            this.txtMessageLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtMessageLog.Size = new System.Drawing.Size(399, 303);
            this.txtMessageLog.TabIndex = 0;
            // 
            // ServerScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(527, 450);
            this.Controls.Add(this.txtMessageLog);
            this.Name = "ServerScreen";
            this.Text = "ServerScreen";
            this.Load += new System.EventHandler(this.ServerScreen_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtMessageLog;
    }
}