namespace ManualILSpy
{
    partial class Form1
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
            this.enableCommentBtn = new System.Windows.Forms.Button();
            this.disableCommentBtn = new System.Windows.Forms.Button();
            this.testReadWriteJsonBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button3
            // 
            this.enableCommentBtn.Location = new System.Drawing.Point(46, 121);
            this.enableCommentBtn.Name = "button3";
            this.enableCommentBtn.Size = new System.Drawing.Size(116, 41);
            this.enableCommentBtn.TabIndex = 2;
            this.enableCommentBtn.Text = "Enable JsonValue Comment";
            this.enableCommentBtn.UseVisualStyleBackColor = true;
            this.enableCommentBtn.Click += new System.EventHandler(this.EnableCommentBtn_Click);
            // 
            // button4
            // 
            this.disableCommentBtn.Location = new System.Drawing.Point(219, 121);
            this.disableCommentBtn.Name = "button4";
            this.disableCommentBtn.Size = new System.Drawing.Size(124, 41);
            this.disableCommentBtn.TabIndex = 3;
            this.disableCommentBtn.Text = "Disable JsonValue Comment";
            this.disableCommentBtn.UseVisualStyleBackColor = true;
            this.disableCommentBtn.Click += new System.EventHandler(this.DisableCommentBtn_Click);
            // 
            // button5
            // 
            this.testReadWriteJsonBtn.Location = new System.Drawing.Point(141, 190);
            this.testReadWriteJsonBtn.Name = "button5";
            this.testReadWriteJsonBtn.Size = new System.Drawing.Size(93, 35);
            this.testReadWriteJsonBtn.TabIndex = 4;
            this.testReadWriteJsonBtn.Text = "Test read and write json";
            this.testReadWriteJsonBtn.UseVisualStyleBackColor = true;
            this.testReadWriteJsonBtn.Click += new System.EventHandler(this.TestReadWriteJsonBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 261);
            this.Controls.Add(this.testReadWriteJsonBtn);
            this.Controls.Add(this.disableCommentBtn);
            this.Controls.Add(this.enableCommentBtn);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button enableCommentBtn;
        private System.Windows.Forms.Button disableCommentBtn;
        private System.Windows.Forms.Button testReadWriteJsonBtn;
    }
}

