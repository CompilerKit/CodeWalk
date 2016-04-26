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
            this.browsePathTb = new System.Windows.Forms.TextBox();
            this.browse_btn = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.scan_btn = new System.Windows.Forms.Button();
            this.enable_rbtn = new System.Windows.Forms.RadioButton();
            this.disable_rbtn = new System.Windows.Forms.RadioButton();
            this.decompile_btn = new System.Windows.Forms.Button();
            this.decompile_all_btn = new System.Windows.Forms.Button();
            this.typesListView = new System.Windows.Forms.ListView();
            this.lbSuccessCounter = new System.Windows.Forms.Label();
            this.errorLogBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lbCountAll = new System.Windows.Forms.Label();
            this.lbErrorCounter = new System.Windows.Forms.Label();
            this.pauseBtn = new System.Windows.Forms.Button();
            this.stopBtn = new System.Windows.Forms.Button();
            this.decompileErrorBtn = new System.Windows.Forms.Button();
            this.jsonOutRBtn = new System.Windows.Forms.RadioButton();
            this.csharpOutRBtn = new System.Windows.Forms.RadioButton();
            this.bothOutRBtn = new System.Windows.Forms.RadioButton();
            this.outOptGroup = new System.Windows.Forms.GroupBox();
            this.decompileOptGroup = new System.Windows.Forms.GroupBox();
            this.clearBtn = new System.Windows.Forms.Button();
            this.outOptGroup.SuspendLayout();
            this.decompileOptGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // browsePathTb
            // 
            this.browsePathTb.Location = new System.Drawing.Point(12, 12);
            this.browsePathTb.Name = "browsePathTb";
            this.browsePathTb.Size = new System.Drawing.Size(297, 20);
            this.browsePathTb.TabIndex = 5;
            this.browsePathTb.TextChanged += new System.EventHandler(this.browsePathTb_TextChanged);
            // 
            // browse_btn
            // 
            this.browse_btn.Location = new System.Drawing.Point(324, 10);
            this.browse_btn.Name = "browse_btn";
            this.browse_btn.Size = new System.Drawing.Size(124, 23);
            this.browse_btn.TabIndex = 6;
            this.browse_btn.Text = "Browse";
            this.browse_btn.UseVisualStyleBackColor = true;
            this.browse_btn.Click += new System.EventHandler(this.browse_btn_Click);
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(12, 48);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(297, 301);
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
            this.scan_btn.Click += new System.EventHandler(this.scan_Click);
            // 
            // enable_rbtn
            // 
            this.enable_rbtn.AutoSize = true;
            this.enable_rbtn.Location = new System.Drawing.Point(6, 19);
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
            this.disable_rbtn.Location = new System.Drawing.Point(6, 42);
            this.disable_rbtn.Name = "disable_rbtn";
            this.disable_rbtn.Size = new System.Drawing.Size(107, 17);
            this.disable_rbtn.TabIndex = 10;
            this.disable_rbtn.TabStop = true;
            this.disable_rbtn.Text = "Disable Comment";
            this.disable_rbtn.UseVisualStyleBackColor = true;
            // 
            // decompile_btn
            // 
            this.decompile_btn.Location = new System.Drawing.Point(3, 66);
            this.decompile_btn.Name = "decompile_btn";
            this.decompile_btn.Size = new System.Drawing.Size(118, 31);
            this.decompile_btn.TabIndex = 11;
            this.decompile_btn.Text = "Decompile Selected";
            this.decompile_btn.UseVisualStyleBackColor = true;
            this.decompile_btn.Click += new System.EventHandler(this.decompileSelected_btn_Click);
            // 
            // decompile_all_btn
            // 
            this.decompile_all_btn.Location = new System.Drawing.Point(3, 103);
            this.decompile_all_btn.Name = "decompile_all_btn";
            this.decompile_all_btn.Size = new System.Drawing.Size(118, 31);
            this.decompile_all_btn.TabIndex = 12;
            this.decompile_all_btn.Text = "Decompile All";
            this.decompile_all_btn.UseVisualStyleBackColor = true;
            this.decompile_all_btn.Click += new System.EventHandler(this.decompileAll_btn_Click);
            // 
            // typesListView
            // 
            this.typesListView.Location = new System.Drawing.Point(459, 77);
            this.typesListView.Name = "typesListView";
            this.typesListView.Size = new System.Drawing.Size(313, 244);
            this.typesListView.TabIndex = 20;
            this.typesListView.UseCompatibleStateImageBehavior = false;
            // 
            // lbSuccessCounter
            // 
            this.lbSuccessCounter.AutoSize = true;
            this.lbSuccessCounter.Location = new System.Drawing.Point(456, 29);
            this.lbSuccessCounter.Name = "lbSuccessCounter";
            this.lbSuccessCounter.Size = new System.Drawing.Size(104, 13);
            this.lbSuccessCounter.TabIndex = 21;
            this.lbSuccessCounter.Text = "Success 0 / N types";
            // 
            // errorLogBtn
            // 
            this.errorLogBtn.Location = new System.Drawing.Point(667, 10);
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
            this.label1.Location = new System.Drawing.Point(456, 336);
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
            this.lbCountAll.Size = new System.Drawing.Size(81, 13);
            this.lbCountAll.TabIndex = 24;
            this.lbCountAll.Text = "dll have 0 types";
            // 
            // lbErrorCounter
            // 
            this.lbErrorCounter.AutoSize = true;
            this.lbErrorCounter.Location = new System.Drawing.Point(456, 48);
            this.lbErrorCounter.Name = "lbErrorCounter";
            this.lbErrorCounter.Size = new System.Drawing.Size(85, 13);
            this.lbErrorCounter.TabIndex = 25;
            this.lbErrorCounter.Text = "Error 0 / N types";
            // 
            // pauseBtn
            // 
            this.pauseBtn.Location = new System.Drawing.Point(616, 48);
            this.pauseBtn.Name = "pauseBtn";
            this.pauseBtn.Size = new System.Drawing.Size(75, 23);
            this.pauseBtn.TabIndex = 26;
            this.pauseBtn.Text = "Pause";
            this.pauseBtn.UseVisualStyleBackColor = true;
            this.pauseBtn.Click += new System.EventHandler(this.pauseBtn_Click);
            // 
            // stopBtn
            // 
            this.stopBtn.Location = new System.Drawing.Point(697, 48);
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.Size = new System.Drawing.Size(75, 23);
            this.stopBtn.TabIndex = 27;
            this.stopBtn.Text = "Stop";
            this.stopBtn.UseVisualStyleBackColor = true;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // decompileErrorBtn
            // 
            this.decompileErrorBtn.Location = new System.Drawing.Point(3, 140);
            this.decompileErrorBtn.Name = "decompileErrorBtn";
            this.decompileErrorBtn.Size = new System.Drawing.Size(118, 37);
            this.decompileErrorBtn.TabIndex = 28;
            this.decompileErrorBtn.Text = "Decompile ErrorTypes";
            this.decompileErrorBtn.UseVisualStyleBackColor = true;
            this.decompileErrorBtn.Click += new System.EventHandler(this.decompileError_Click);
            // 
            // jsonOutRBtn
            // 
            this.jsonOutRBtn.AutoSize = true;
            this.jsonOutRBtn.Location = new System.Drawing.Point(6, 19);
            this.jsonOutRBtn.Name = "jsonOutRBtn";
            this.jsonOutRBtn.Size = new System.Drawing.Size(82, 17);
            this.jsonOutRBtn.TabIndex = 0;
            this.jsonOutRBtn.TabStop = true;
            this.jsonOutRBtn.Text = "Json Output";
            this.jsonOutRBtn.UseVisualStyleBackColor = true;
            // 
            // csharpOutRBtn
            // 
            this.csharpOutRBtn.AutoSize = true;
            this.csharpOutRBtn.Location = new System.Drawing.Point(6, 40);
            this.csharpOutRBtn.Name = "csharpOutRBtn";
            this.csharpOutRBtn.Size = new System.Drawing.Size(74, 17);
            this.csharpOutRBtn.TabIndex = 1;
            this.csharpOutRBtn.TabStop = true;
            this.csharpOutRBtn.Text = "C# Output";
            this.csharpOutRBtn.UseVisualStyleBackColor = true;
            // 
            // bothOutRBtn
            // 
            this.bothOutRBtn.AutoSize = true;
            this.bothOutRBtn.Location = new System.Drawing.Point(6, 60);
            this.bothOutRBtn.Name = "bothOutRBtn";
            this.bothOutRBtn.Size = new System.Drawing.Size(47, 17);
            this.bothOutRBtn.TabIndex = 2;
            this.bothOutRBtn.TabStop = true;
            this.bothOutRBtn.Text = "Both";
            this.bothOutRBtn.UseVisualStyleBackColor = true;
            // 
            // outOptGroup
            // 
            this.outOptGroup.Controls.Add(this.bothOutRBtn);
            this.outOptGroup.Controls.Add(this.jsonOutRBtn);
            this.outOptGroup.Controls.Add(this.csharpOutRBtn);
            this.outOptGroup.Location = new System.Drawing.Point(324, 77);
            this.outOptGroup.Name = "outOptGroup";
            this.outOptGroup.Size = new System.Drawing.Size(124, 81);
            this.outOptGroup.TabIndex = 29;
            this.outOptGroup.TabStop = false;
            this.outOptGroup.Text = "Output Options";
            // 
            // decompileOptGroup
            // 
            this.decompileOptGroup.Controls.Add(this.decompileErrorBtn);
            this.decompileOptGroup.Controls.Add(this.enable_rbtn);
            this.decompileOptGroup.Controls.Add(this.decompile_all_btn);
            this.decompileOptGroup.Controls.Add(this.disable_rbtn);
            this.decompileOptGroup.Controls.Add(this.decompile_btn);
            this.decompileOptGroup.Location = new System.Drawing.Point(324, 164);
            this.decompileOptGroup.Name = "decompileOptGroup";
            this.decompileOptGroup.Size = new System.Drawing.Size(124, 185);
            this.decompileOptGroup.TabIndex = 30;
            this.decompileOptGroup.TabStop = false;
            this.decompileOptGroup.Text = "Decompile Options";
            // 
            // clearBtn
            // 
            this.clearBtn.Location = new System.Drawing.Point(697, 327);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(75, 23);
            this.clearBtn.TabIndex = 31;
            this.clearBtn.Text = "Clear List";
            this.clearBtn.UseVisualStyleBackColor = true;
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 361);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.decompileOptGroup);
            this.Controls.Add(this.outOptGroup);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.pauseBtn);
            this.Controls.Add(this.lbErrorCounter);
            this.Controls.Add(this.lbCountAll);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.errorLogBtn);
            this.Controls.Add(this.lbSuccessCounter);
            this.Controls.Add(this.typesListView);
            this.Controls.Add(this.scan_btn);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.browse_btn);
            this.Controls.Add(this.browsePathTb);
            this.MaximumSize = new System.Drawing.Size(800, 400);
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "Form1";
            this.Text = "Form1";
            this.outOptGroup.ResumeLayout(false);
            this.outOptGroup.PerformLayout();
            this.decompileOptGroup.ResumeLayout(false);
            this.decompileOptGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox browsePathTb;
        private System.Windows.Forms.Button browse_btn;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button scan_btn;
        private System.Windows.Forms.RadioButton enable_rbtn;
        private System.Windows.Forms.RadioButton disable_rbtn;
        private System.Windows.Forms.Button decompile_btn;
        private System.Windows.Forms.Button decompile_all_btn;
        private System.Windows.Forms.ListView typesListView;
        private System.Windows.Forms.Label lbSuccessCounter;
        private System.Windows.Forms.Button errorLogBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbCountAll;
        private System.Windows.Forms.Label lbErrorCounter;
        private System.Windows.Forms.Button pauseBtn;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.Button decompileErrorBtn;
        private System.Windows.Forms.RadioButton jsonOutRBtn;
        private System.Windows.Forms.RadioButton csharpOutRBtn;
        private System.Windows.Forms.RadioButton bothOutRBtn;
        private System.Windows.Forms.GroupBox outOptGroup;
        private System.Windows.Forms.GroupBox decompileOptGroup;
        private System.Windows.Forms.Button clearBtn;
    }
}

