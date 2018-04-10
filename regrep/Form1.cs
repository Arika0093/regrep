using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace regrep
{
	public partial class Form1 : Form
	{
		public bool taskRunning = false;

		public class fileLocationData
		{
			public string filename;
			public string filepath;
			public string relativePathName;
		}

		public List<fileLocationData> filepaths = new List<fileLocationData>();
		public List<fileLocationData> matchedfilepaths = new List<fileLocationData>();

		public Form1()
		{
			InitializeComponent();
		}

		private void 開くOToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var fssel = new OpenFileDialog();
			fssel.Multiselect = true;
			fssel.Filter = "All Files|*.*";
			if(fssel.ShowDialog(this) == DialogResult.OK) {
				filepaths.Clear();
				listBox1.Items.Clear();
				openFiles(fssel.FileNames.ToList());
				splitContainer1.Enabled = true;
				filterChanged();
			}
		}

		private void ディレクトリを開くDToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var fd = new CommonOpenFileDialog();
			fd.IsFolderPicker = true;
			fd.Multiselect = true;
			if(fd.ShowDialog() == CommonFileDialogResult.Ok) {
				filepaths.Clear();
				listBox1.Items.Clear();
				openDirectorys(fd.FileNames.ToList());
				splitContainer1.Enabled = true;
				filterChanged();
			}
		}

		private void 終了XToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			textBox1.Enabled = (checkBox1.Checked);
			filterChanged();
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			filterChanged();
		}

		private void radioButton1_CheckedChanged(object sender, EventArgs e)
		{
			textBox3.Text = "";
			textBox3.ReadOnly = true;
		}

		private void radioButton2_CheckedChanged(object sender, EventArgs e)
		{
			textBox3.Text = "";
			textBox3.ReadOnly = false;
		}

		private async void button1_Click(object sender, EventArgs e)
		{
			taskRunningEnableChanged(true);
			await startTask();
			taskRunningEnableChanged(false);
		}

		private void openFiles(List<string> filenames, string rootpath = "")
		{
			foreach(var f in filenames) {
				var path = new fileLocationData();
				var fn = Path.GetFileName(f);

				path.filepath = f;
				path.filename = fn;
				if(rootpath != "") {
					var u1 = new Uri(f);
					var u2 = new Uri(rootpath);
					path.relativePathName = u2.MakeRelativeUri(u1).ToString();
				}
				else {
					path.relativePathName = fn;
				}
				filepaths.Add(path);

				listBox1.Items.Add(path.relativePathName);
			}
		}

		private void openDirectorys(List<string> dirnames)
		{
			foreach(var dir in dirnames) {
				var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
				openFiles(files.ToList(), dir + "/");
			}
		}

		private void filterChanged()
		{
			matchedfilepaths.Clear();
			listBox2.Items.Clear();
			if(!checkBox1.Checked) {
				listBox2.Items.Add("(全ファイルが対象になっています)");
				matchedfilepaths.AddRange(filepaths);
				return;
			}
			try {
				var reg = new Regex(textBox1.Text);
				foreach(var path in filepaths) {
					if(reg.IsMatch(path.relativePathName)) {
						matchedfilepaths.Add(path);
						listBox2.Items.Add(path.relativePathName);
					}
				}
			}
			catch(Exception) {
				listBox2.Items.Add("(無効な正規表現です)");
				matchedfilepaths.Clear();
				return;
			}
		}

		private async Task<bool> startTask()
		{
			var regrep = new regrep();
			var ropt = new regrep.regrepOption();
			var isReplaceMode = radioButton2.Checked;


			ropt.filenames = matchedfilepaths.Select(e => e.filepath).ToList();
			ropt.strLeft = textBox2.Text;
			ropt.strRight = textBox3.Text;
			ropt.isRegex = checkBox2.Checked;
			ropt.isContainFilename = checkBox3.Checked;
			ropt.isAutoBackup = checkBox4.Checked;

			if(ropt.strLeft.Trim() == string.Empty) {
				MessageBox.Show($"置換/検索対象の文字が空白です。");
				return false;
			}

			if(!isReplaceMode) {
				// Search
				var rsts = await regrep.fileSearch(ropt);
				textBox3.Text = "";
				if(rsts.Count > 0) {
					foreach(var rst in rsts) {
						textBox3.Text += $"{rst.ToString()}////////////////////////////\r\n";
					}
				} else {
					textBox3.Text = "(一致する結果はありませんでした)";
				}

			} else {
				// Replace
				var rsts = await regrep.fileReplace(ropt);
				MessageBox.Show($"置換完了!\n------------------\n{string.Join("\n", rsts)}");
			}

			return true;
		}

		public void taskRunningEnableChanged(bool flag)
		{
			taskRunning = flag;
			splitContainer1.Enabled = !flag;
			menuStrip1.Enabled = !flag;
		}
	}
}
