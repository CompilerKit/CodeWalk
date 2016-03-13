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
            this.testReadWriteJsonBtn = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.browse_btn = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.scan_btn = new System.Windows.Forms.Button();
            this.enable_rbtn = new System.Windows.Forms.RadioButton();
            this.disable_rbtn = new System.Windows.Forms.RadioButton();
            this.decompile_btn = new System.Windows.Forms.Button();
            this.decompile_panel = new System.Windows.Forms.Panel();
            this.debug_path_txt = new System.Windows.Forms.TextBox();
            this.release_path_txt = new System.Windows.Forms.TextBox();
            this.debug_path_btn = new System.Windows.Forms.Button();
            this.release_path_btn = new System.Windows.Forms.Button();
            this.decompile_panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // testReadWriteJsonBtn
            // 
            this.testReadWriteJsonBtn.Enabled = false;
            this.testReadWriteJsonBtn.Location = new System.Drawing.Point(324, 212);
            this.testReadWriteJsonBtn.Name = "testReadWriteJsonBtn";
            this.testReadWriteJsonBtn.Size = new System.Drawing.Size(124, 35);
            this.testReadWriteJsonBtn.TabIndex = 4;
            this.testReadWriteJsonBtn.Text = "Test read and write json";
            this.testReadWriteJsonBtn.UseVisualStyleBackColor = true;
            this.testReadWriteJsonBtn.Click += new System.EventHandler(this.TestReadWriteJsonBtn_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(297, 20);
            this.textBox1.TabIndex = 5;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // browse_btn
            // 
            this.browse_btn.Location = new System.Drawing.Point(324, 10);
            this.browse_btn.Name = "browse_btn";
            this.browse_btn.Size = new System.Drawing.Size(124, 23);
            this.browse_btn.TabIndex = 6;
            this.browse_btn.Text = "Browse";
            this.browse_btn.UseVisualStyleBackColor = true;
            this.browse_btn.Click += new System.EventHandler(this.Browse_btn_Click);
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(12, 48);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(297, 199);
            this.treeView1.TabIndex = 7;
            // 
            // scan_btn
            // 
            this.scan_btn.Location = new System.Drawing.Point(324, 48);
            this.scan_btn.Name = "scan_btn";
            this.scan_btn.Size = new System.Drawing.Size(124, 23);
            this.scan_btn.TabIndex = 8;
            this.scan_btn.Text = "Scan";
            this.scan_btn.UseVisualStyleBackColor = true;
            this.scan_btn.Click += new System.EventHandler(this.Scan_Click);
            // 
            // enable_rbtn
            // 
            this.enable_rbtn.AutoSize = true;
            this.enable_rbtn.Location = new System.Drawing.Point(3, 3);
            this.enable_rbtn.Name = "enable_rbtn";
            this.enable_rbtn.Size = new System.Drawing.Size(105, 17);
            this.enable_rbtn.TabIndex = 9;
            this.enable_rbtn.TabStop = true;
            this.enable_rbtn.Text = "Enable Comment";
            this.enable_rbtn.UseVisualStyleBackColor = true;
            // 
            // disable_rbtn
            // 
            this.disable_rbtn.AutoSize = true;
            this.disable_rbtn.Location = new System.Drawing.Point(3, 26);
            this.disable_rbtn.Name = "disable_rbtn";
            this.disable_rbtn.Size = new System.Drawing.Size(107, 17);
            this.disable_rbtn.TabIndex = 10;
            this.disable_rbtn.TabStop = true;
            this.disable_rbtn.Text = "Disable Comment";
            this.disable_rbtn.UseVisualStyleBackColor = true;
            // 
            // decompile_btn
            // 
            this.decompile_btn.Location = new System.Drawing.Point(3, 49);
            this.decompile_btn.Name = "decompile_btn";
            this.decompile_btn.Size = new System.Drawing.Size(118, 31);
            this.decompile_btn.TabIndex = 11;
            this.decompile_btn.Text = "Decompile";
            this.decompile_btn.UseVisualStyleBackColor = true;
            this.decompile_btn.Click += new System.EventHandler(this.decompile_btn_Click);
            // 
            // decompile_panel
            // 
            this.decompile_panel.Controls.Add(this.disable_rbtn);
            this.decompile_panel.Controls.Add(this.enable_rbtn);
            this.decompile_panel.Controls.Add(this.decompile_btn);
            this.decompile_panel.Location = new System.Drawing.Point(324, 96);
            this.decompile_panel.Name = "decompile_panel";
            this.decompile_panel.Size = new System.Drawing.Size(124, 88);
            this.decompile_panel.TabIndex = 13;
            // 
            // debug_path_txt
            // 
            this.debug_path_txt.Location = new System.Drawing.Point(12, 271);
            this.debug_path_txt.Name = "debug_path_txt";
            this.debug_path_txt.Size = new System.Drawing.Size(297, 20);
            this.debug_path_txt.TabIndex = 16;
            this.debug_path_txt.TextChanged += new System.EventHandler(this.debug_path_txt_TextChanged);
            // 
            // release_path_txt
            // 
            this.release_path_txt.Location = new System.Drawing.Point(12, 306);
            this.release_path_txt.Name = "release_path_txt";
            this.release_path_txt.Size = new System.Drawing.Size(297, 20);
            this.release_path_txt.TabIndex = 17;
            this.release_path_txt.TextChanged += new System.EventHandler(this.release_path_txt_TextChanged);
            // 
            // debug_path_btn
            // 
            this.debug_path_btn.Location = new System.Drawing.Point(324, 269);
            this.debug_path_btn.Name = "debug_path_btn";
            this.debug_path_btn.Size = new System.Drawing.Size(124, 23);
            this.debug_path_btn.TabIndex = 18;
            this.debug_path_btn.Text = "Debug path change";
            this.debug_path_btn.UseVisualStyleBackColor = true;
            this.debug_path_btn.Click += new System.EventHandler(this.debug_path_btn_Click);
            // 
            // release_path_btn
            // 
            this.release_path_btn.Location = new System.Drawing.Point(324, 304);
            this.release_path_btn.Name = "release_path_btn";
            this.release_path_btn.Size = new System.Drawing.Size(124, 23);
            this.release_path_btn.TabIndex = 19;
            this.release_path_btn.Text = "Release path change";
            this.release_path_btn.UseVisualStyleBackColor = true;
            this.release_path_btn.Click += new System.EventHandler(this.release_path_btn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 346);
            this.Controls.Add(this.release_path_btn);
            this.Controls.Add(this.debug_path_btn);
            this.Controls.Add(this.release_path_txt);
            this.Controls.Add(this.debug_path_txt);
            this.Controls.Add(this.decompile_panel);
            this.Controls.Add(this.scan_btn);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.browse_btn);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.testReadWriteJsonBtn);
            this.MaximumSize = new System.Drawing.Size(800, 384);
            this.MinimumSize = new System.Drawing.Size(473, 384);
            this.Name = "Form1";
            this.Text = "Form1";
            this.decompile_panel.ResumeLayout(false);
            this.decompile_panel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button testReadWriteJsonBtn;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button browse_btn;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button scan_btn;
        private System.Windows.Forms.RadioButton enable_rbtn;
        private System.Windows.Forms.RadioButton disable_rbtn;
        private System.Windows.Forms.Button decompile_btn;
        private System.Windows.Forms.Panel decompile_panel;
        private System.Windows.Forms.TextBox debug_path_txt;
        private System.Windows.Forms.TextBox release_path_txt;
        private System.Windows.Forms.Button debug_path_btn;
        private System.Windows.Forms.Button release_path_btn;
    }
}

