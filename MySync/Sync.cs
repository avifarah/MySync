namespace MySync
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using log4net;
	using System.Configuration;
	using System.Collections.Generic;
	using static FileSystem;

	public static class Sync
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static int dirCount = 0;
		static IEnumerable<string> excludeFiles;
		static IEnumerable<string> excludeDirectoriesStartingWith;
		static bool isCopy;

		static Sync()
		{
			string exclude = ConfigurationManager.AppSettings["ExcludeFiles"];
			if (string.IsNullOrWhiteSpace(exclude)) excludeFiles = new List<string>();
			else excludeFiles = exclude.Split(new[] { ';' }).Where(e => !string.IsNullOrWhiteSpace(e));

			exclude = ConfigurationManager.AppSettings["ExcludeDirectoriesStartingWith"];
			if (string.IsNullOrWhiteSpace(exclude)) excludeDirectoriesStartingWith = new List<string>();
			else excludeDirectoriesStartingWith = exclude.Split(new[] { ';' }).Where(e => !string.IsNullOrWhiteSpace(e));

			var yeses = new List<string> { "Y", "Yes", "T", "True", "OK", "1" };
			//var nos = new List<string> { "N", "No", "F", "False", "0" };
			string isReadOnly = ConfigurationManager.AppSettings["ReportOnly"];
			isCopy = yeses.Any(y => string.Compare(y, isReadOnly, StringComparison.InvariantCultureIgnoreCase) == 0);
		}

		public static void SyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir)
		{
			++dirCount;
			FileInfo[] pFiles = pDir.GetFiles();
			FileInfo[] sFiles = sDir.GetFiles();

			Console.WriteLine("{0,9:#,##0}.  Primary: {1}\t\tSecondary: {2}", dirCount, pDir.FullName, sDir.FullName);

			foreach (var pFi in pFiles)
			{
				if (excludeFiles.Any(e => string.Compare(e, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0)) continue;

                var sFi = sFiles.FirstOrDefault(s => string.Compare(s.Name, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (sFi == null)
				{
					log.InfoFormat("Primary file only: {0}", pFi.FullName);
					string sFile = Path.Combine(sDir.FullName, Path.GetFileName(pFi.FullName));
					if (isCopy) FileCopy(pFi.FullName, sFile);
				}
				else
				{
					var pLaccess = pFi.LastAccessTimeUtc;
					var sLaccess = sFi.LastAccessTimeUtc;
					var pLen = pFi.Length;
					var sLen = sFi.Length;
					if (pLaccess > sLaccess)
					{
						log.InfoFormat("Later time: Primary: ({0}, {1}, [{2:#,##0}]).  Secondary: ({3}, {4}, {5:#,##0})", pFi.FullName, pLaccess, pLen, sFi.FullName, sLaccess, sLen);
						if (isCopy) FileCopy(pFi.FullName, sFi.FullName);
					}
					else if (pLen != sLen)
					{
						log.InfoFormat("Legth different: Primary: ({0}, {1}, [{2:#,##0}]).  Secondary: ({3}, {4}, [{5:#,##0}])", pFi.FullName, pLaccess, pLen, sFi.FullName, sLaccess, sLen);
						if (isCopy) FileCopy(pFi.FullName, sFi.FullName);
					}
				}
			}

			pFiles = null;
			sFiles = null;

			// Directories
			DirectoryInfo[] pDis = pDir.GetDirectories();
			DirectoryInfo[] sDis = sDir.GetDirectories();

			foreach (var pDi in pDis)
			{
				if (excludeDirectoriesStartingWith.Any(e => pDi.Name.StartsWith(e, StringComparison.InvariantCultureIgnoreCase))) continue;

				var sDi = sDis.FirstOrDefault(s => string.Compare(s.Name, pDi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (!pDi.Name.StartsWith("."))
					if (sDi == null)
					{
						log.InfoFormat("Primary only directory: {0}", pDi.FullName);
						string destination = Path.Combine(sDir.FullName, Path.GetFileName(pDi.FullName));
						CopyDirectory(pDi.FullName, destination);
					}
					else
						SyncDirectory(pDi, sDi);
			}
		}
	}
}
