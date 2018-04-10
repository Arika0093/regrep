using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace regrep
{
	public class regrep
	{
		public class regrepOption
		{
			public enum regrepMode
			{
				Search, Replace
			}
			public List<string> filenames = new List<string>();
			public string fileFilter;
			public string strLeft;
			public string strRight;
			public bool isRegex;
			public bool isContainFilename;
			public bool isAutoBackup;
		};

		public class regrepFileSearchResult
		{
			public string filepath;
			public string filename;
			public int line;
			public int position;
			public string matchline;
			public override string ToString()
			{
				return $"{filename}:{line}:{position}\r\n  {matchline}\r\n";
			}
		}

		public async Task<List<regrepFileSearchResult>> fileSearch(regrepOption rop)
		{
			var rsts = new List<regrepFileSearchResult>();
			var searches = rop.strLeft.Replace("\r\n", "\n").Split('\n');

			foreach(var f in rop.filenames) {
				var filedir = Path.GetDirectoryName(f);
				var filename = Path.GetFileName(f);

				// read
				var rst = new StreamReader(f);
				var filetext = await rst.ReadToEndAsync();
				rst.Close();

				// check
				foreach(var s in searches) {
					var rs = stringSearch(filetext, s, rop.isRegex);
					foreach(var r in rs) {
						r.filepath = f;
						r.filename = filename;
					}
					rsts.AddRange(rs);
				}
			}

			return rsts;
		}


		public async Task<List<string>> fileReplace(regrepOption rop)
		{
			var befs = rop.strLeft.Replace("\r\n", "\n").Split('\n');
			var afts = rop.strRight.Replace("\r\n", "\n").Split('\n');
			var is_regex = rop.isRegex;
			var is_contain_filename = rop.isContainFilename;
			var is_autobackup = rop.isAutoBackup;
			var completed = new List<string>();

			foreach(var f in rop.filenames) {
				var filedir = Path.GetDirectoryName(f);
				var filename = Path.GetFileName(f);
				var filename_aft = filename;

				// is matched
				/*
				var match = rop.fileFilter;
				if(match != "" && !Regex.IsMatch(filename, match)) {
					continue;
				}
				*/

				// read
				var rst = new StreamReader(f);
				var filetext = await rst.ReadToEndAsync();
				var filetext_aft = filetext;
				rst.Close();

				// replace
				for(var i = 0; i < befs.Length; i++) {
					var b = befs[i];
					var a = (i < afts.Length ? afts[i] : "");
					filetext_aft = stringReplace(filetext_aft, b, a, is_regex);
					if(is_contain_filename) {
						filename_aft = stringReplace(filename_aft, b, a, is_regex);
					}
				}

				// write backup
				if(is_autobackup && (filename == filename_aft)) {
					var wst_b = new StreamWriter(filedir + "/" + filename + ".bak");
					await wst_b.WriteAsync(filetext);
					wst_b.Close();
				}

				// write
				var wst = new StreamWriter(filedir + "/" + filename_aft);
				await wst.WriteAsync(filetext_aft);

				// end
				wst.Close();

				completed.Add(filename_aft);
			}

			return completed;
		}

		private List<regrepFileSearchResult> stringSearch(string basetext, string matched, bool is_regex)
		{
			var rsts = new List<regrepFileSearchResult>();
			var matches = matched.Replace("\r\n", "\n").Split('\n');
			var text = basetext.Replace("\r\n", "\n").Split('\n');
			Func<string, int, int, bool> matchContentAdd = (t, line, pos) => {
				var rst = new regrepFileSearchResult();
				rst.matchline = t;
				rst.line = line;
				rst.position = pos;
				rsts.Add(rst);
				return true;
			};
			for(var line = 1; line <= text.Length; line++) {
				foreach(var m in matches) {
					var t = text[line - 1];
					var pos = -1;
					if(!is_regex) {
						pos = t.IndexOf(m);
						if(pos >= 0) {
							matchContentAdd(t, line, pos);
						}
					}
					else {
						var reg = new Regex(m);
						var mc = reg.Matches(t);
						foreach(Match mt in mc) {
							var mt_g = mt.Groups[0];
							matchContentAdd(t, line, mt_g.Index);
						}
					}
				}
			}
			return rsts;
		}

		private string stringReplace(string basetext, string before, string after, bool is_regex)
		{
			if(!is_regex) {
				return basetext.Replace(before, after);
			}
			else {
				return Regex.Replace(basetext, before, after);
			}
			// return "";
		}


	}
}
