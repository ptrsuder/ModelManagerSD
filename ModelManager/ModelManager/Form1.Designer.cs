namespace ModelManager
{
    partial class Form1
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
            splitMain = new SplitContainer();
            flowModels = new FlowLayoutPanel();
            groupModelInfo = new GroupBox();
            webModelInfo = new WebBrowser();
            trackModelScale = new TrackBar();
            btnSelectFolder = new Button();
            chkSearchSubfolders = new CheckBox();
            dtpCreatedAfter = new DateTimePicker();
            cmbBaseModel = new ComboBox();
            panelTop = new Panel();
            txtModelNameFilter = new TextBox();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            groupModelInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackModelScale).BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 98);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(flowModels);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(groupModelInfo);
            splitMain.Size = new Size(916, 503);
            splitMain.SplitterDistance = 594;
            splitMain.TabIndex = 0;
            // 
            // flowModels
            // 
            flowModels.AutoScroll = true;
            flowModels.Dock = DockStyle.Fill;
            flowModels.Location = new Point(0, 0);
            flowModels.Name = "flowModels";
            flowModels.Size = new Size(594, 503);
            flowModels.TabIndex = 0;
            // 
            // groupModelInfo
            // 
            groupModelInfo.Controls.Add(webModelInfo);
            groupModelInfo.Dock = DockStyle.Fill;
            groupModelInfo.Location = new Point(0, 0);
            groupModelInfo.Name = "groupModelInfo";
            groupModelInfo.Size = new Size(318, 503);
            groupModelInfo.TabIndex = 0;
            groupModelInfo.TabStop = false;
            groupModelInfo.Text = "Model Info";
            // 
            // webModelInfo
            // 
            webModelInfo.Dock = DockStyle.Fill;
            webModelInfo.Location = new Point(3, 19);
            webModelInfo.Name = "webModelInfo";
            webModelInfo.Size = new Size(312, 481);
            webModelInfo.TabIndex = 0;
            // 
            // trackModelScale
            // 
            trackModelScale.LargeChange = 32;
            trackModelScale.Location = new Point(12, 45);
            trackModelScale.Maximum = 512;
            trackModelScale.Minimum = 64;
            trackModelScale.Name = "trackModelScale";
            trackModelScale.Size = new Size(647, 45);
            trackModelScale.SmallChange = 8;
            trackModelScale.TabIndex = 4;
            trackModelScale.TickFrequency = 16;
            trackModelScale.Value = 128;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(12, 12);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(120, 28);
            btnSelectFolder.TabIndex = 0;
            btnSelectFolder.Text = "Open Folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // chkSearchSubfolders
            // 
            chkSearchSubfolders.Checked = true;
            chkSearchSubfolders.CheckState = CheckState.Checked;
            chkSearchSubfolders.Location = new Point(140, 12);
            chkSearchSubfolders.Name = "chkSearchSubfolders";
            chkSearchSubfolders.Size = new Size(140, 30);
            chkSearchSubfolders.TabIndex = 1;
            chkSearchSubfolders.Text = "Search subfolders";
            // 
            // dtpCreatedAfter
            // 
            dtpCreatedAfter.CustomFormat = "yyyy-MM-dd";
            dtpCreatedAfter.Format = DateTimePickerFormat.Custom;
            dtpCreatedAfter.Location = new Point(551, 15);
            dtpCreatedAfter.Name = "dtpCreatedAfter";
            dtpCreatedAfter.Size = new Size(108, 23);
            dtpCreatedAfter.TabIndex = 2;
            dtpCreatedAfter.Value = new DateTime(2024, 7, 24, 0, 0, 0, 0);
            // 
            // cmbBaseModel
            // 
            cmbBaseModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBaseModel.Location = new Point(425, 15);
            cmbBaseModel.Name = "cmbBaseModel";
            cmbBaseModel.Size = new Size(120, 23);
            cmbBaseModel.TabIndex = 3;
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.FromArgb(245, 245, 245);
            panelTop.Controls.Add(txtModelNameFilter);
            panelTop.Controls.Add(btnSelectFolder);
            panelTop.Controls.Add(chkSearchSubfolders);
            panelTop.Controls.Add(dtpCreatedAfter);
            panelTop.Controls.Add(cmbBaseModel);
            panelTop.Controls.Add(trackModelScale);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(916, 98);
            panelTop.TabIndex = 3;
            // 
            // txtModelNameFilter
            // 
            txtModelNameFilter.Location = new Point(261, 15);
            txtModelNameFilter.Name = "txtModelNameFilter";
            txtModelNameFilter.Size = new Size(158, 23);
            txtModelNameFilter.TabIndex = 5;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(916, 601);
            Controls.Add(splitMain);
            Controls.Add(panelTop);
            MinimumSize = new Size(770, 640);
            Name = "Form1";
            Text = "Stable Diffusion Model Manager";
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            groupModelInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)trackModelScale).EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.FlowLayoutPanel flowModels;
        private System.Windows.Forms.GroupBox groupModelInfo;
        private System.Windows.Forms.WebBrowser webModelInfo;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.CheckBox chkSearchSubfolders;
        private System.Windows.Forms.DateTimePicker dtpCreatedAfter;
        private System.Windows.Forms.ComboBox cmbBaseModel;
        private System.Windows.Forms.TrackBar trackModelScale;
        private System.Windows.Forms.Panel panelTop;
        private TextBox txtModelNameFilter;
    }
}
