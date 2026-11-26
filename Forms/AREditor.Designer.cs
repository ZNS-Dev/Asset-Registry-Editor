using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Asset_Registry_Editor;

public partial class AREditor
	{
	private const string LoadingRegistryText = "Loading AssetRegistry file...";
	private const string SearchNoMatchText = "No matching text was found.";
	private const string SelectAssetRegistryDialogTitle = "Select an AssetRegistry file";
	private const string SelectOutputFolderTitle = "Select an output folder for extracted JSON files";
	private IContainer components = new Container();
	private RichTextBox RTBassetregistryparsedread;
	private Button BTNsearch;
	private TextBox TBsearch;

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

private void InitializeComponent()
	{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AREditor));
            this.RTBassetregistryparsedread = new System.Windows.Forms.RichTextBox();
            this.LBLassets = new System.Windows.Forms.Label();
            this.CLBassets = new System.Windows.Forms.CheckedListBox();
            this.LBLselectedcount = new System.Windows.Forms.Label();
            this.TBassetfilter = new System.Windows.Forms.TextBox();
            this.BTNselectallassets = new System.Windows.Forms.Button();
            this.BTNclearassetselection = new System.Windows.Forms.Button();
            this.BTNextractselected = new System.Windows.Forms.Button();
            this.TBsearch = new System.Windows.Forms.TextBox();
            this.BTNsearch = new System.Windows.Forms.Button();
            this.LBLprofiles = new System.Windows.Forms.Label();
            this.LBLprofilename = new System.Windows.Forms.Label();
            this.TBprofilename = new System.Windows.Forms.TextBox();
            this.BTNcreateprofile = new System.Windows.Forms.Button();
            this.CBprofiles = new System.Windows.Forms.ComboBox();
            this.BTNopeninjectfolder = new System.Windows.Forms.Button();
            this.BTNinjectprofile = new System.Windows.Forms.Button();
            this.BTNsavefiletojson = new System.Windows.Forms.Button();
            this.BTNsavefiletobin = new System.Windows.Forms.Button();
            this.BTNselectarpath3 = new System.Windows.Forms.Button();
            this.LBLeditor = new System.Windows.Forms.Label();
            this.LBLdumper = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // RTBassetregistryparsedread
            // 
            this.RTBassetregistryparsedread.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.RTBassetregistryparsedread.Font = new System.Drawing.Font("Microsoft Tai Le", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RTBassetregistryparsedread.ForeColor = System.Drawing.Color.White;
            this.RTBassetregistryparsedread.HideSelection = false;
            this.RTBassetregistryparsedread.Location = new System.Drawing.Point(277, 145);
            this.RTBassetregistryparsedread.Name = "RTBassetregistryparsedread";
            this.RTBassetregistryparsedread.Size = new System.Drawing.Size(495, 441);
            this.RTBassetregistryparsedread.TabIndex = 1;
            this.RTBassetregistryparsedread.Text = "";
            // 
            // LBLassets
            // 
            this.LBLassets.AutoSize = true;
            this.LBLassets.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LBLassets.Location = new System.Drawing.Point(13, 32);
            this.LBLassets.Name = "LBLassets";
            this.LBLassets.Size = new System.Drawing.Size(45, 16);
            this.LBLassets.TabIndex = 10;
            this.LBLassets.Text = "Assets:";
            // 
            // CLBassets
            // 
            this.CLBassets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.CLBassets.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CLBassets.CheckOnClick = true;
            this.CLBassets.Enabled = false;
            this.CLBassets.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold);
            this.CLBassets.ForeColor = System.Drawing.Color.White;
            this.CLBassets.FormattingEnabled = true;
            this.CLBassets.HorizontalScrollbar = true;
            this.CLBassets.IntegralHeight = false;
            this.CLBassets.Location = new System.Drawing.Point(12, 121);
            this.CLBassets.Name = "CLBassets";
            this.CLBassets.Size = new System.Drawing.Size(259, 465);
            this.CLBassets.TabIndex = 2;
            this.CLBassets.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.CLBassets_ItemCheck);
            this.CLBassets.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CLBassets_MouseUp);
            // 
            // LBLselectedcount
            // 
            this.LBLselectedcount.AutoSize = true;
            this.LBLselectedcount.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold);
            this.LBLselectedcount.Location = new System.Drawing.Point(13, 48);
            this.LBLselectedcount.Name = "LBLselectedcount";
            this.LBLselectedcount.Size = new System.Drawing.Size(58, 14);
            this.LBLselectedcount.TabIndex = 11;
            this.LBLselectedcount.Text = "0 selected";
            // 
            // TBassetfilter
            // 
            this.TBassetfilter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBassetfilter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBassetfilter.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold);
            this.TBassetfilter.ForeColor = System.Drawing.Color.White;
            this.TBassetfilter.Location = new System.Drawing.Point(12, 65);
            this.TBassetfilter.MaxLength = 128;
            this.TBassetfilter.Name = "TBassetfilter";
            this.TBassetfilter.Size = new System.Drawing.Size(121, 21);
            this.TBassetfilter.TabIndex = 15;
            this.TBassetfilter.TextChanged += new System.EventHandler(this.TBassetfilter_TextChanged);
            // 
            // BTNselectallassets
            // 
            this.BTNselectallassets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNselectallassets.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNselectallassets.Enabled = false;
            this.BTNselectallassets.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNselectallassets.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.BTNselectallassets.Location = new System.Drawing.Point(139, 65);
            this.BTNselectallassets.Name = "BTNselectallassets";
            this.BTNselectallassets.Size = new System.Drawing.Size(70, 22);
            this.BTNselectallassets.TabIndex = 12;
            this.BTNselectallassets.Text = "Select all";
            this.BTNselectallassets.UseVisualStyleBackColor = false;
            this.BTNselectallassets.Click += new System.EventHandler(this.BTNselectallassets_Click);
            // 
            // BTNclearassetselection
            // 
            this.BTNclearassetselection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNclearassetselection.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNclearassetselection.Enabled = false;
            this.BTNclearassetselection.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNclearassetselection.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.BTNclearassetselection.Location = new System.Drawing.Point(215, 65);
            this.BTNclearassetselection.Name = "BTNclearassetselection";
            this.BTNclearassetselection.Size = new System.Drawing.Size(56, 22);
            this.BTNclearassetselection.TabIndex = 13;
            this.BTNclearassetselection.Text = "Clear";
            this.BTNclearassetselection.UseVisualStyleBackColor = false;
            this.BTNclearassetselection.Click += new System.EventHandler(this.BTNclearassetselection_Click);
            // 
            // BTNextractselected
            // 
            this.BTNextractselected.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNextractselected.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNextractselected.Enabled = false;
            this.BTNextractselected.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNextractselected.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNextractselected.Location = new System.Drawing.Point(12, 592);
            this.BTNextractselected.Name = "BTNextractselected";
            this.BTNextractselected.Size = new System.Drawing.Size(259, 24);
            this.BTNextractselected.TabIndex = 14;
            this.BTNextractselected.Text = "Extract selected as JSON";
            this.BTNextractselected.UseVisualStyleBackColor = false;
            this.BTNextractselected.Click += new System.EventHandler(this.BTNextractselected_Click);
            // 
            // TBsearch
            // 
            this.TBsearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBsearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBsearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TBsearch.ForeColor = System.Drawing.Color.White;
            this.TBsearch.Location = new System.Drawing.Point(370, 115);
            this.TBsearch.MaxLength = 64;
            this.TBsearch.Name = "TBsearch";
            this.TBsearch.Size = new System.Drawing.Size(402, 24);
            this.TBsearch.TabIndex = 0;
            this.TBsearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TBsearch_KeyDown);
            // 
            // BTNsearch
            // 
            this.BTNsearch.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNsearch.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BTNsearch.Location = new System.Drawing.Point(277, 116);
            this.BTNsearch.Name = "BTNsearch";
            this.BTNsearch.Size = new System.Drawing.Size(87, 23);
            this.BTNsearch.TabIndex = 0;
            this.BTNsearch.Text = "Search";
            this.BTNsearch.UseVisualStyleBackColor = false;
            this.BTNsearch.Click += new System.EventHandler(this.BTNsearch_Click);
            // 
            // LBLprofiles
            // 
            this.LBLprofiles.AutoSize = true;
            this.LBLprofiles.Font = new System.Drawing.Font("Microsoft Tai Le", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LBLprofiles.Location = new System.Drawing.Point(273, 9);
            this.LBLprofiles.Name = "LBLprofiles";
            this.LBLprofiles.Size = new System.Drawing.Size(77, 23);
            this.LBLprofiles.TabIndex = 0;
            this.LBLprofiles.Text = "Profiles";
            // 
            // LBLprofilename
            // 
            this.LBLprofilename.AutoSize = true;
            this.LBLprofilename.Font = new System.Drawing.Font("Microsoft Tai Le", 9.25F, System.Drawing.FontStyle.Bold);
            this.LBLprofilename.Location = new System.Drawing.Point(356, 11);
            this.LBLprofilename.Name = "LBLprofilename";
            this.LBLprofilename.Size = new System.Drawing.Size(47, 16);
            this.LBLprofilename.TabIndex = 1;
            this.LBLprofilename.Text = "Name:";
            // 
            // TBprofilename
            // 
            this.TBprofilename.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBprofilename.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBprofilename.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TBprofilename.ForeColor = System.Drawing.Color.White;
            this.TBprofilename.Location = new System.Drawing.Point(409, 9);
            this.TBprofilename.MaxLength = 29;
            this.TBprofilename.Name = "TBprofilename";
            this.TBprofilename.Size = new System.Drawing.Size(181, 21);
            this.TBprofilename.TabIndex = 2;
            // 
            // BTNcreateprofile
            // 
            this.BTNcreateprofile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNcreateprofile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNcreateprofile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNcreateprofile.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNcreateprofile.Location = new System.Drawing.Point(277, 34);
            this.BTNcreateprofile.Name = "BTNcreateprofile";
            this.BTNcreateprofile.Size = new System.Drawing.Size(161, 23);
            this.BTNcreateprofile.TabIndex = 3;
            this.BTNcreateprofile.Text = "Create Profile";
            this.BTNcreateprofile.UseVisualStyleBackColor = false;
            this.BTNcreateprofile.Click += new System.EventHandler(this.BTNcreateprofile_Click);
            // 
            // CBprofiles
            // 
            this.CBprofiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.CBprofiles.DropDownHeight = 55;
            this.CBprofiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CBprofiles.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.CBprofiles.ForeColor = System.Drawing.Color.White;
            this.CBprofiles.FormattingEnabled = true;
            this.CBprofiles.IntegralHeight = false;
            this.CBprofiles.ItemHeight = 13;
            this.CBprofiles.Location = new System.Drawing.Point(598, 10);
            this.CBprofiles.Name = "CBprofiles";
            this.CBprofiles.Size = new System.Drawing.Size(176, 21);
            this.CBprofiles.TabIndex = 4;
            this.CBprofiles.SelectedIndexChanged += new System.EventHandler(this.CBprofiles_SelectedIndexChanged);
            // 
            // BTNopeninjectfolder
            // 
            this.BTNopeninjectfolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNopeninjectfolder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNopeninjectfolder.Enabled = false;
            this.BTNopeninjectfolder.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNopeninjectfolder.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNopeninjectfolder.Location = new System.Drawing.Point(277, 60);
            this.BTNopeninjectfolder.Name = "BTNopeninjectfolder";
            this.BTNopeninjectfolder.Size = new System.Drawing.Size(161, 24);
            this.BTNopeninjectfolder.TabIndex = 5;
            this.BTNopeninjectfolder.Text = "Open Inject Folder";
            this.BTNopeninjectfolder.UseVisualStyleBackColor = false;
            this.BTNopeninjectfolder.Click += new System.EventHandler(this.BTNopeninjectfolder_Click);
            // 
            // BTNinjectprofile
            // 
            this.BTNinjectprofile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNinjectprofile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNinjectprofile.Enabled = false;
            this.BTNinjectprofile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNinjectprofile.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNinjectprofile.Location = new System.Drawing.Point(277, 88);
            this.BTNinjectprofile.Name = "BTNinjectprofile";
            this.BTNinjectprofile.Size = new System.Drawing.Size(161, 23);
            this.BTNinjectprofile.TabIndex = 6;
            this.BTNinjectprofile.Text = "Inject";
            this.BTNinjectprofile.UseVisualStyleBackColor = false;
            this.BTNinjectprofile.Click += new System.EventHandler(this.BTNinjectprofile_Click);
            // 
            // BTNsavefiletojson
            // 
            this.BTNsavefiletojson.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNsavefiletojson.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNsavefiletojson.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNsavefiletojson.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNsavefiletojson.Location = new System.Drawing.Point(277, 592);
            this.BTNsavefiletojson.Name = "BTNsavefiletojson";
            this.BTNsavefiletojson.Size = new System.Drawing.Size(245, 23);
            this.BTNsavefiletojson.TabIndex = 5;
            this.BTNsavefiletojson.Text = "Save as JSON";
            this.BTNsavefiletojson.UseVisualStyleBackColor = false;
            this.BTNsavefiletojson.Click += new System.EventHandler(this.BTNsavefiletojson_Click);
            // 
            // BTNsavefiletobin
            // 
            this.BTNsavefiletobin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNsavefiletobin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNsavefiletobin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNsavefiletobin.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNsavefiletobin.Location = new System.Drawing.Point(528, 592);
            this.BTNsavefiletobin.Name = "BTNsavefiletobin";
            this.BTNsavefiletobin.Size = new System.Drawing.Size(244, 23);
            this.BTNsavefiletobin.TabIndex = 6;
            this.BTNsavefiletobin.Text = "Save as BIN";
            this.BTNsavefiletobin.UseVisualStyleBackColor = false;
            this.BTNsavefiletobin.Click += new System.EventHandler(this.BTNsavefiletobin_Click);
            // 
            // BTNselectarpath3
            // 
            this.BTNselectarpath3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNselectarpath3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNselectarpath3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNselectarpath3.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNselectarpath3.Location = new System.Drawing.Point(12, 93);
            this.BTNselectarpath3.Name = "BTNselectarpath3";
            this.BTNselectarpath3.Size = new System.Drawing.Size(259, 22);
            this.BTNselectarpath3.TabIndex = 4;
            this.BTNselectarpath3.Text = "Select an AssetRegistry.bin";
            this.BTNselectarpath3.UseVisualStyleBackColor = false;
            this.BTNselectarpath3.Click += new System.EventHandler(this.BTNselectarpath3_Click);
            // 
            // LBLeditor
            // 
            this.LBLeditor.AutoSize = true;
            this.LBLeditor.Font = new System.Drawing.Font("Microsoft Tai Le", 14.25F, System.Drawing.FontStyle.Bold);
            this.LBLeditor.Location = new System.Drawing.Point(708, 89);
            this.LBLeditor.Name = "LBLeditor";
            this.LBLeditor.Size = new System.Drawing.Size(64, 23);
            this.LBLeditor.TabIndex = 8;
            this.LBLeditor.Text = "Editor";
            // 
            // LBLdumper
            // 
            this.LBLdumper.AutoSize = true;
            this.LBLdumper.Font = new System.Drawing.Font("Microsoft Tai Le", 14.25F, System.Drawing.FontStyle.Bold);
            this.LBLdumper.Location = new System.Drawing.Point(12, 9);
            this.LBLdumper.Name = "LBLdumper";
            this.LBLdumper.Size = new System.Drawing.Size(92, 23);
            this.LBLdumper.TabIndex = 6;
            this.LBLdumper.Text = "Extractor";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(741, 618);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "1.1.1";
            // 
            // AREditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(784, 640);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TBsearch);
            this.Controls.Add(this.CLBassets);
            this.Controls.Add(this.LBLselectedcount);
            this.Controls.Add(this.TBassetfilter);
            this.Controls.Add(this.BTNselectallassets);
            this.Controls.Add(this.BTNclearassetselection);
            this.Controls.Add(this.BTNextractselected);
            this.Controls.Add(this.LBLassets);
            this.Controls.Add(this.BTNsearch);
            this.Controls.Add(this.BTNinjectprofile);
            this.Controls.Add(this.RTBassetregistryparsedread);
            this.Controls.Add(this.LBLdumper);
            this.Controls.Add(this.BTNopeninjectfolder);
            this.Controls.Add(this.LBLprofiles);
            this.Controls.Add(this.CBprofiles);
            this.Controls.Add(this.BTNsavefiletojson);
            this.Controls.Add(this.LBLeditor);
            this.Controls.Add(this.BTNcreateprofile);
            this.Controls.Add(this.BTNsavefiletobin);
            this.Controls.Add(this.TBprofilename);
            this.Controls.Add(this.BTNselectarpath3);
            this.Controls.Add(this.LBLprofilename);
            this.ForeColor = System.Drawing.Color.White;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(782, 679);
            this.Name = "AREditor";
            this.Text = "Asset Registry Editor";
            this.Load += new System.EventHandler(this.AREditor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

	}
    private Button BTNinjectprofile;
    private Button BTNopeninjectfolder;
    private ComboBox CBprofiles;
    private Button BTNcreateprofile;
    private TextBox TBprofilename;
    private Label LBLprofilename;
    private Label LBLprofiles;
    private Button BTNsavefiletojson;
    private Button BTNsavefiletobin;
    private Button BTNselectarpath3;
    private Label LBLeditor;
    private Label LBLdumper;
    private Label label1;
    private Label LBLassets;
    private System.Windows.Forms.CheckedListBox CLBassets;
    private Label LBLselectedcount;
    private TextBox TBassetfilter;
    private Button BTNselectallassets;
    private Button BTNclearassetselection;
    private Button BTNextractselected;
}
