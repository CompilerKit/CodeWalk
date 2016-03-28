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
            this.browsePathTb = new System.Windows.Forms.TextBox();
            this.browse_btn = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.scan_btn = new System.Windows.Forms.Button();
            this.enable_rbtn = new System.Windows.Forms.RadioButton();
            this.disable_rbtn = new System.Windows.Forms.RadioButton();
            this.decompile_btn = new System.Windows.Forms.Button();
            this.decompile_panel = new System.Windows.Forms.Panel();
            this.decompile_all_btn = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.lbSuccessCounter = new System.Windows.Forms.Label();
            this.errorLogBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lbCountAll = new System.Windows.Forms.Label();
            this.lbErrorCounter = new System.Windows.Forms.Label();
            this.pauseBtn = new System.Windows.Forms.Button();
            this.stopBtn = new System.Windows.Forms.Button();
            this.decompile_panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // testReadWriteJsonBtn
            // 
            this.testReadWriteJsonBtn.Enabled = false;
            this.testReadWriteJsonBtn.Location = new System.Drawing.Point(324, 292);
            this.testReadWriteJsonBtn.Name = "testReadWriteJsonBtn";
            this.testReadWriteJsonBtn.Size = new System.Drawing.Size(124, 35);
            this.testReadWriteJsonBtn.TabIndex = 4;
            this.testReadWriteJsonBtn.Text = "Test read and write json";
            this.testReadWriteJsonBtn.UseVisualStyleBackColor = true;
            this.testReadWriteJsonBtn.Click += new System.EventHandler(this.TestReadWriteJsonBtn_Click);
            // 
            // browsePathTb
            // 
            this.browsePathTb.Location = new System.Drawing.Point(12, 12);
            this.browsePathTb.Name = "browsePathTb";
            this.browsePathTb.Size = new System.Drawing.Size(297, 20);
            this.browsePathTb.TabIndex = 5;
            this.browsePathTb.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
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
            this.treeView1.Size = new System.Drawing.Size(297, 279);
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
            this.decompile_btn.Text = "Decompile Selected";
            this.decompile_btn.UseVisualStyleBackColor = true;
            this.decompile_btn.Click += new System.EventHandler(this.decompile_btn_Click);
            // 
            // decompile_panel
            // 
            this.decompile_panel.Controls.Add(this.decompile_all_btn);
            this.decompile_panel.Controls.Add(this.disable_rbtn);
            this.decompile_panel.Controls.Add(this.enable_rbtn);
            this.decompile_panel.Controls.Add(this.decompile_btn);
            this.decompile_panel.Location = new System.Drawing.Point(324, 77);
            this.decompile_panel.Name = "decompile_panel";
            this.decompile_panel.Size = new System.Drawing.Size(124, 129);
            this.decompile_panel.TabIndex = 13;
            // 
            // decompile_all_btn
            // 
            this.decompile_all_btn.Location = new System.Drawing.Point(3, 86);
            this.decompile_all_btn.Name = "decompile_all_btn";
            this.decompile_all_btn.Size = new System.Drawing.Size(118, 31);
            this.decompile_all_btn.TabIndex = 12;
            this.decompile_all_btn.Text = "Decompile All";
            this.decompile_all_btn.UseVisualStyleBackColor = true;
            this.decompile_all_btn.Click += new System.EventHandler(this.decompile_all_btn_Click);
            // 
            // listView1
            // 
            this.listView1.Location = new System.Drawing.Point(459, 77);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(300, 230);
            this.listView1.TabIndex = 20;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // lbSuccessCounter
            // 
            this.lbSuccessCounter.AutoSize = true;
            this.lbSuccessCounter.Location = new System.Drawing.Point(456, 29);
            this.lbSuccessCounter.Name = "lbSuccessCounter";
            this.lbSuccessCounter.Size = new System.Drawing.Size(97, 13);
            this.lbSuccessCounter.TabIndex = 21;
            this.lbSuccessCounter.Text = "Success 0 / N files";
            // 
            // errorLogBtn
            // 
            this.errorLogBtn.Location = new System.Drawing.Point(654, 10);
            this.errorLogBtn.Name = "errorLogBtn";
            this.errorLogBtn.Size = new System.Drawing.Size(105, 23);
            this.errorLogBtn.TabIndex = 22;
            this.errorLogBtn.Text = "Error List Log File";
            this.errorLogBtn.UseVisualStyleBackColor = true;
            this.errorLogBtn.Click += new System.EventHandler(this.errorLogBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(456, 314);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(209, 13);
            this.label1.TabIndex = 23;
            this.label1.Text = "**Double-click on item to open selected file";
            // 
            // lbCountAll
            // 
            this.lbCountAll.AutoSize = true;
            this.lbCountAll.Location = new System.Drawing.Point(456, 12);
            this.lbCountAll.Name = "lbCountAll";
            this.lbCountAll.Size = new System.Drawing.Size(74, 13);
            this.lbCountAll.TabIndex = 24;
            this.lbCountAll.Text = "dll have 0 files";
            // 
            // lbErrorCounter
            // 
            this.lbErrorCounter.AutoSize = true;
            this.lbErrorCounter.Location = new System.Drawing.Point(456, 48);
            this.lbErrorCounter.Name = "lbErrorCounter";
            this.lbErrorCounter.Size = new System.Drawing.Size(78, 13);
            this.lbErrorCounter.TabIndex = 25;
            this.lbErrorCounter.Text = "Error 0 / N files";
            // 
            // pauseBtn
            // 
            this.pauseBtn.Location = new System.Drawing.Point(568, 48);
            this.pauseBtn.Name = "pauseBtn";
            this.pauseBtn.Size = new System.Drawing.Size(75, 23);
            this.pauseBtn.TabIndex = 26;
            this.pauseBtn.Text = "Pause";
            this.pauseBtn.UseVisualStyleBackColor = true;
            this.pauseBtn.Click += new System.EventHandler(this.pauseBtn_Click);
            // 
            // stopBtn
            // 
            this.stopBtn.Location = new System.Drawing.Point(650, 48);
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.Size = new System.Drawing.Size(75, 23);
            this.stopBtn.TabIndex = 27;
            this.stopBtn.Text = "Stop";
            this.stopBtn.UseVisualStyleBackColor = true;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(768, 345);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.pauseBtn);
            this.Controls.Add(this.lbErrorCounter);
            this.Controls.Add(this.lbCountAll);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.errorLogBtn);
            this.Controls.Add(this.lbSuccessCounter);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.decompile_panel);
            this.Controls.Add(this.scan_btn);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.browse_btn);
            this.Controls.Add(this.browsePathTb);
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
        private System.Windows.Forms.TextBox browsePathTb;
        private System.Windows.Forms.Button browse_btn;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button scan_btn;
        private System.Windows.Forms.RadioButton enable_rbtn;
        private System.Windows.Forms.RadioButton disable_rbtn;
        private System.Windows.Forms.Button decompile_btn;
        private System.Windows.Forms.Panel decompile_panel;
        private System.Windows.Forms.Button decompile_all_btn;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label lbSuccessCounter;
        private System.Windows.Forms.Button errorLogBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbCountAll;
        private System.Windows.Forms.Label lbErrorCounter;
        private System.Windows.Forms.Button pauseBtn;
        private System.Windows.Forms.Button stopBtn;
    }
}

