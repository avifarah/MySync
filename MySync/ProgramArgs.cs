using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace MySync
{
	public class ProgramArgs : IProgramArgs
	{
		public ProgramArgs(string primary, string secondary, string skipTil)
		{
			if (primary != null && Directory.Exists(primary))
				MySyncConfiguration.Inst.PrimaryDir = Path.GetFullPath(primary);

			if (secondary != null && Directory.Exists(secondary))
				MySyncConfiguration.Inst.SecondaryDir = Path.GetFullPath(secondary);

			if (!string.IsNullOrWhiteSpace(skipTil) && Directory.Exists(skipTil))
				MySyncConfiguration.Inst.SkippingTillDir = Path.GetFullPath(skipTil);

			_primaryParts = BreakDirParts(Primary);
			_skipParts = BreakDirParts(SkipTil);

			if (_skipParts.Count >= _primaryParts.Count)
			{
				bool allEqual = _primaryParts.Select((p, i) => string.Compare(p, _skipParts[i], StringComparison.InvariantCultureIgnoreCase) == 0).All(b => b);
				if (allEqual)
				{
					_stopSkippingFiles = false;
					_stopSkippingDirectories = false;
				}
			}
		}

		public string Primary => MySyncConfiguration.Inst.PrimaryDir;

		public string Secondary => MySyncConfiguration.Inst.SecondaryDir;

		public string SkipTil => MySyncConfiguration.Inst.SkippingTillDir;

		private bool _stopSkippingFiles = true;
		private bool _stopSkippingDirectories = true;
		private readonly List<string> _skipParts;
		private readonly List<string> _primaryParts;

		public bool IsPrimaryLegit() => Directory.Exists(Primary);

		public bool IsSecondaryLegit() => Directory.Exists(Secondary);

		/// <inheritdoc />
		/// <summary>
		///  SkipTill may be:
		/// 		&gt;	Empty
		/// 		&gt;	Must exits as a directory
		/// 		&gt;	It must be a superset of the primary directory
		///  </summary>
		///  <returns></returns>
		public bool IsSkipTilLegit()
		{
			if (string.IsNullOrWhiteSpace(SkipTil)) return true;
			if (!IsDirectoryExist(SkipTil)) return false;
			if (_skipParts.Count < _primaryParts.Count) return false;

			bool allEqual = _primaryParts.Select((p, i) => string.Compare(p, _skipParts[i], StringComparison.InvariantCultureIgnoreCase) == 0).All(b => b);
			return allEqual;
		}

		/// <summary>
		/// Needed for unit testing
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		protected virtual bool IsDirectoryExist(string dir) => Directory.Exists(dir);

		private List<string> BreakDirParts(string dir)
		{
			var parts = new List<string>();
			if (string.IsNullOrWhiteSpace(dir)) return parts;
			if (dir.EndsWith("\\")) dir = dir.Substring(0, dir.Length - 1);

			for (;;)
			{
				string file = Path.GetFileName(dir);
				if (string.IsNullOrWhiteSpace(file))
				{
					parts.Add(dir);
					break;
				}

				var dirNext = Path.GetDirectoryName(dir);
				if (string.IsNullOrEmpty(dirNext))
				{
					parts.Add(dir);
					break;
				}

				parts.Add(file);
				dir = dirNext;
			}

			parts.Reverse();
			return parts;
		}

		public bool IsSkipFileCopy(DirectoryInfo dir) => IsSkipFileCopy(dir.FullName);

		public bool IsSkipFileCopy(string dir)
		{
			if (_stopSkippingFiles) return false;

			var dirFullPath = Path.GetFullPath(dir);
			bool allEqual = string.Compare(SkipTil, dirFullPath, StringComparison.InvariantCultureIgnoreCase) == 0;
			if (allEqual) _stopSkippingFiles = true;
			return true;
		}

		public bool IsSkipDirCopy(DirectoryInfo dir) => IsSkipDirCopy(dir.FullName);

		public bool IsSkipDirCopy(string dir)
		{
			if (_stopSkippingDirectories) return false;

			var dirFullPath = Path.GetFullPath(dir);
			var dirParts = BreakDirParts(dirFullPath);

			for (int i = 0; i < Math.Min(dirParts.Count, _skipParts.Count); ++i)
				if (string.Compare(dirParts[i], _skipParts[i], StringComparison.OrdinalIgnoreCase) != 0) return true;

			if (dirParts.Count == _skipParts.Count) _stopSkippingDirectories = true;
			return false;
		}
	}
}
