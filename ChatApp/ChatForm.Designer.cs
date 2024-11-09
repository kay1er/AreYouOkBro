namespace ChatClient
{
    partial class ChatForm
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
            this.txtChatInput = new System.Windows.Forms.TextBox();
            this.txtChatDisplay = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtChatInput
            // 
            this.txtChatInput.Location = new System.Drawing.Point(48, 251);
            this.txtChatInput.Name = "txtChatInput";
            this.txtChatInput.Size = new System.Drawing.Size(385, 20);
            this.txtChatInput.TabIndex = 0;
            // 
            // txtChatDisplay
            // 
            this.txtChatDisplay.Location = new System.Drawing.Point(48, 47);
            this.txtChatDisplay.Multiline = true;
            this.txtChatDisplay.Name = "txtChatDisplay";
            this.txtChatDisplay.ReadOnly = true;
            this.txtChatDisplay.Size = new System.Drawing.Size(385, 198);
            this.txtChatDisplay.TabIndex = 1;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(358, 277);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // ChatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 343);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtChatDisplay);
            this.Controls.Add(this.txtChatInput);
            this.Name = "ChatForm";
            this.Text = "ChatScreen";
            this.Load += new System.EventHandler(this.ChatForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtChatInput;
        private System.Windows.Forms.TextBox txtChatDisplay;
        private System.Windows.Forms.Button btnSend;
    }
}