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
            this.BTNdirtoexctractallpath = new System.Windows.Forms.Button();
            this.TBdirtoexctractallpath = new System.Windows.Forms.TextBox();
            this.BTNextract = new System.Windows.Forms.Button();
            this.BTNselectarpath2 = new System.Windows.Forms.Button();
            this.TBarpath2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // RTBassetregistryparsedread
            // 
            this.RTBassetregistryparsedread.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.RTBassetregistryparsedread.Font = new System.Drawing.Font("Microsoft Tai Le", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RTBassetregistryparsedread.ForeColor = System.Drawing.Color.White;
            this.RTBassetregistryparsedread.Location = new System.Drawing.Point(376, 92);
            this.RTBassetregistryparsedread.Name = "RTBassetregistryparsedread";
            this.RTBassetregistryparsedread.Size = new System.Drawing.Size(396, 494);
            this.RTBassetregistryparsedread.TabIndex = 1;
            this.RTBassetregistryparsedread.Text = "";
            // 
            // TBsearch
            // 
            this.TBsearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBsearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBsearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TBsearch.ForeColor = System.Drawing.Color.White;
            this.TBsearch.Location = new System.Drawing.Point(469, 62);
            this.TBsearch.MaxLength = 64;
            this.TBsearch.Name = "TBsearch";
            this.TBsearch.Size = new System.Drawing.Size(303, 24);
            this.TBsearch.TabIndex = 0;
            // 
            // BTNsearch
            // 
            this.BTNsearch.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNsearch.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BTNsearch.Location = new System.Drawing.Point(376, 63);
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
            this.LBLprofiles.Location = new System.Drawing.Point(12, 153);
            this.LBLprofiles.Name = "LBLprofiles";
            this.LBLprofiles.Size = new System.Drawing.Size(77, 23);
            this.LBLprofiles.TabIndex = 0;
            this.LBLprofiles.Text = "Profiles";
            // 
            // LBLprofilename
            // 
            this.LBLprofilename.AutoSize = true;
            this.LBLprofilename.Font = new System.Drawing.Font("Microsoft Tai Le", 9.25F, System.Drawing.FontStyle.Bold);
            this.LBLprofilename.Location = new System.Drawing.Point(13, 188);
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
            this.TBprofilename.Location = new System.Drawing.Point(66, 185);
            this.TBprofilename.MaxLength = 24;
            this.TBprofilename.Name = "TBprofilename";
            this.TBprofilename.Size = new System.Drawing.Size(173, 21);
            this.TBprofilename.TabIndex = 2;
            // 
            // BTNcreateprofile
            // 
            this.BTNcreateprofile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNcreateprofile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNcreateprofile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNcreateprofile.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNcreateprofile.Location = new System.Drawing.Point(245, 184);
            this.BTNcreateprofile.Name = "BTNcreateprofile";
            this.BTNcreateprofile.Size = new System.Drawing.Size(125, 23);
            this.BTNcreateprofile.TabIndex = 3;
            this.BTNcreateprofile.Text = "Create Profile";
            this.BTNcreateprofile.UseVisualStyleBackColor = false;
            this.BTNcreateprofile.Click += new System.EventHandler(this.BTNcreateprofile_Click);
            // 
            // CBprofiles
            // 
            this.CBprofiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.CBprofiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CBprofiles.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.CBprofiles.ForeColor = System.Drawing.Color.White;
            this.CBprofiles.FormattingEnabled = true;
            this.CBprofiles.Location = new System.Drawing.Point(13, 213);
            this.CBprofiles.Name = "CBprofiles";
            this.CBprofiles.Size = new System.Drawing.Size(164, 21);
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
            this.BTNopeninjectfolder.Location = new System.Drawing.Point(245, 213);
            this.BTNopeninjectfolder.Name = "BTNopeninjectfolder";
            this.BTNopeninjectfolder.Size = new System.Drawing.Size(125, 23);
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
            this.BTNinjectprofile.Location = new System.Drawing.Point(183, 213);
            this.BTNinjectprofile.Name = "BTNinjectprofile";
            this.BTNinjectprofile.Size = new System.Drawing.Size(56, 23);
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
            this.BTNsavefiletojson.Location = new System.Drawing.Point(376, 592);
            this.BTNsavefiletojson.Name = "BTNsavefiletojson";
            this.BTNsavefiletojson.Size = new System.Drawing.Size(190, 23);
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
            this.BTNsavefiletobin.Location = new System.Drawing.Point(582, 592);
            this.BTNsavefiletobin.Name = "BTNsavefiletobin";
            this.BTNsavefiletobin.Size = new System.Drawing.Size(190, 23);
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
            this.BTNselectarpath3.Location = new System.Drawing.Point(376, 35);
            this.BTNselectarpath3.Name = "BTNselectarpath3";
            this.BTNselectarpath3.Size = new System.Drawing.Size(396, 21);
            this.BTNselectarpath3.TabIndex = 4;
            this.BTNselectarpath3.Text = "Select an AssetRegistry.bin";
            this.BTNselectarpath3.UseVisualStyleBackColor = false;
            this.BTNselectarpath3.Click += new System.EventHandler(this.BTNselectarpath3_Click);
            // 
            // LBLeditor
            // 
            this.LBLeditor.AutoSize = true;
            this.LBLeditor.Font = new System.Drawing.Font("Microsoft Tai Le", 14.25F, System.Drawing.FontStyle.Bold);
            this.LBLeditor.Location = new System.Drawing.Point(708, 9);
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
            // BTNdirtoexctractallpath
            // 
            this.BTNdirtoexctractallpath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNdirtoexctractallpath.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNdirtoexctractallpath.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNdirtoexctractallpath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNdirtoexctractallpath.Location = new System.Drawing.Point(12, 118);
            this.BTNdirtoexctractallpath.Name = "BTNdirtoexctractallpath";
            this.BTNdirtoexctractallpath.Size = new System.Drawing.Size(183, 23);
            this.BTNdirtoexctractallpath.TabIndex = 7;
            this.BTNdirtoexctractallpath.Text = "Select output folder";
            this.BTNdirtoexctractallpath.UseVisualStyleBackColor = false;
            this.BTNdirtoexctractallpath.Click += new System.EventHandler(this.BTNdirtoexctractallpath_Click);
            // 
            // TBdirtoexctractallpath
            // 
            this.TBdirtoexctractallpath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBdirtoexctractallpath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBdirtoexctractallpath.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TBdirtoexctractallpath.ForeColor = System.Drawing.Color.White;
            this.TBdirtoexctractallpath.Location = new System.Drawing.Point(12, 91);
            this.TBdirtoexctractallpath.Name = "TBdirtoexctractallpath";
            this.TBdirtoexctractallpath.Size = new System.Drawing.Size(358, 21);
            this.TBdirtoexctractallpath.TabIndex = 6;
            // 
            // BTNextract
            // 
            this.BTNextract.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNextract.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNextract.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNextract.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNextract.Location = new System.Drawing.Point(201, 118);
            this.BTNextract.Name = "BTNextract";
            this.BTNextract.Size = new System.Drawing.Size(169, 23);
            this.BTNextract.TabIndex = 5;
            this.BTNextract.Text = "Extract";
            this.BTNextract.UseVisualStyleBackColor = false;
            this.BTNextract.Click += new System.EventHandler(this.BTNextract_Click);
            // 
            // BTNselectarpath2
            // 
            this.BTNselectarpath2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.BTNselectarpath2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BTNselectarpath2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BTNselectarpath2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.BTNselectarpath2.Location = new System.Drawing.Point(12, 62);
            this.BTNselectarpath2.Name = "BTNselectarpath2";
            this.BTNselectarpath2.Size = new System.Drawing.Size(182, 23);
            this.BTNselectarpath2.TabIndex = 3;
            this.BTNselectarpath2.Text = "Select an AssetRegistry.bin";
            this.BTNselectarpath2.UseVisualStyleBackColor = false;
            this.BTNselectarpath2.Click += new System.EventHandler(this.BTNselectarpath2_Click);
            // 
            // TBarpath2
            // 
            this.TBarpath2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.TBarpath2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TBarpath2.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TBarpath2.ForeColor = System.Drawing.Color.White;
            this.TBarpath2.Location = new System.Drawing.Point(12, 35);
            this.TBarpath2.Name = "TBarpath2";
            this.TBarpath2.Size = new System.Drawing.Size(358, 21);
            this.TBarpath2.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(741, 618);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "1.0.0";
            // 
            // AREditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(784, 640);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TBsearch);
            this.Controls.Add(this.BTNsearch);
            this.Controls.Add(this.BTNinjectprofile);
            this.Controls.Add(this.RTBassetregistryparsedread);
            this.Controls.Add(this.LBLdumper);
            this.Controls.Add(this.BTNopeninjectfolder);
            this.Controls.Add(this.BTNdirtoexctractallpath);
            this.Controls.Add(this.LBLprofiles);
            this.Controls.Add(this.CBprofiles);
            this.Controls.Add(this.BTNsavefiletojson);
            this.Controls.Add(this.LBLeditor);
            this.Controls.Add(this.TBarpath2);
            this.Controls.Add(this.BTNcreateprofile);
            this.Controls.Add(this.BTNsavefiletobin);
            this.Controls.Add(this.TBdirtoexctractallpath);
            this.Controls.Add(this.BTNselectarpath2);
            this.Controls.Add(this.TBprofilename);
            this.Controls.Add(this.BTNselectarpath3);
            this.Controls.Add(this.BTNextract);
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
    private Button BTNdirtoexctractallpath;
    private TextBox TBdirtoexctractallpath;
    private Button BTNextract;
    private Button BTNselectarpath2;
    private TextBox TBarpath2;
    private Label label1;
}
