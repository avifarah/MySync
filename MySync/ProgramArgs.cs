namespace MySync
{
	using System;
	using System.Linq;
	using System.Configuration;
	using System.IO;
	using System.Collections.Generic;

	public class ProgramArgs : IProgramArgs
	{
		public ProgramArgs(string primary, string secondary, string skipTil)
		{
			Primary = string.IsNullOrWhiteSpace(primary) ? ConfigurationManager.AppSettings["Primary"] : primary.Trim();
			if (!string.IsNullOrWhiteSpace(Primary)) Primary = Path.GetFullPath(Primary);

			Secondary = string.IsNullOrWhiteSpace(secondary) ? ConfigurationManager.AppSettings["Secondary"] : secondary.Trim();
			if (!string.IsNullOrWhiteSpace(Secondary)) Secondary = Path.GetFullPath(Secondary);

			SkipTil = string.IsNullOrWhiteSpace(skipTil) ? ConfigurationManager.AppSettings["SkippingTill"] : skipTil.Trim();
			SkipTil = (string.IsNullOrWhiteSpace(SkipTil)) ? null : Path.GetFullPath(SkipTil);

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

		public string Primary { get; private set; }

		public string Secondary { get; private set; }

		public string SkipTil { get; private set; }

		private bool _stopSkippingFiles = true;
		private bool _stopSkippingDirectories = true;
		private readonly List<string> _skipParts;
		private readonly List<string> _primaryParts;

		public bool IsPrimaryLegit()
		{
			var primaryExists = Directory.Exists(Primary);
			return primaryExists;
		}

		public bool IsSecondaryLegit()
		{
			var secondaryExists = Directory.Exists(Secondary);
			return secondaryExists;
		}

		/// <summary>
		/// SkipTill may be:
		///		>	Empty
		///		>	Must exits as a directory
		///		>	It must be a superset of the primary directory
		/// </summary>
		/// <returns></returns>
		public bool IsSkipTilLegit()
		{
			if (string.IsNullOrWhiteSpace(SkipTil)) return true;
			if (!IsDirectoryExist(SkipTil)) return false;
			if (_skipParts.Count < _primaryParts.Count) return false;

			bool allEqual = _primaryParts.Select((p, i) => string.Compare(p, _skipParts[i], StringComparison.InvariantCultureIgnoreCase) == 0).All(b => b);
			return allEqual;
		}

		public virtual bool IsDirectoryExist(string dir)
		{
			bool rc = Directory.Exists(dir);
			return rc;
		}

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
