namespace MySync
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using log4net;
	using System.Configuration;
	using System.Collections.Generic;
	using Microsoft.Practices.Unity;
	using IocContainer;

	public class Sync : ISync
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private int _dirCount = 0;
		private readonly bool _isCopy;
		private readonly IEnumerable<string> _excludeFiles;
		private readonly IEnumerable<string> _excludeDirectoriesStartingWith;
		private static readonly UnityDependencyResolver DependencyResolver;
		private readonly IFileSystem _fs;

		static Sync()
		{
			DependencyResolver = new UnityDependencyResolver();
		}

		public Sync()
		{
			_fs = DependencyResolver.Container.Resolve<IFileSystem>();

			string exclude = ConfigurationManager.AppSettings["ExcludeFiles"];
			_excludeFiles = string.IsNullOrWhiteSpace(exclude)
				? new List<string>()
				: exclude.Split(';').Where(e => !string.IsNullOrWhiteSpace(e));

			exclude = ConfigurationManager.AppSettings["ExcludeDirectoriesStartingWith"];
			_excludeDirectoriesStartingWith = string.IsNullOrWhiteSpace(exclude)
				? new List<string>()
				: exclude.Split(';').Where(e => !string.IsNullOrWhiteSpace(e));

			var yeses = new List<string> { "Y", "Yes", "T", "True", "OK", "1" };
			//var nos = new List<string> { "N", "No", "F", "False", "0" };
			string isReadOnly = ConfigurationManager.AppSettings["ReportOnly"];
			_isCopy = yeses.Any(y => string.Compare(y, isReadOnly, StringComparison.InvariantCultureIgnoreCase) == 0);
		}

		public void SyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir)
		{
			try
			{
				UnsafeSyncDirectory(pDir, sDir);
			}
			catch (Exception ex)
			{
				log.Error($"Error while Syncing directories:  {ex.Message}", ex);
			}
        }

		private void UnsafeSyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir)
		{
			++_dirCount;
			FileInfo[] pFiles = pDir.GetFiles();
			FileInfo[] sFiles = sDir.GetFiles();

			Console.WriteLine($"{_dirCount,9:#,##0}.  Primary: {pDir.FullName}\t\tSecondary: {sDir.FullName}");

			foreach (var pFi in pFiles)
			{
				if (_excludeFiles.Any(e => string.Compare(e, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0)) continue;

				var sFi = sFiles.FirstOrDefault(s => string.Compare(s.Name, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (sFi == null)
				{
					log.Info($"[PO]\t{pFi.FullName}");
					string sFile = Path.Combine(sDir.FullName, Path.GetFileName(pFi.FullName));
					if (_isCopy) _fs.CopyFile(pFi.FullName, sFile);
				}
				else
				{
					var pLaccess = pFi.LastAccessTimeUtc;
					var sLaccess = sFi.LastAccessTimeUtc;
					var pLen = pFi.Length;
					var sLen = sFi.Length;
					if (pLaccess > sLaccess)
					{
						log.Info($"[TM]\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, {sLen:#,##0})");
						if (_isCopy) _fs.CopyFile(pFi.FullName, sFi.FullName);
					}
					else if (pLen != sLen)
					{
						log.Info($"[LN]:\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [{sLen:#,##0}])");
						if (_isCopy) _fs.CopyFile(pFi.FullName, sFi.FullName);
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
				if (_excludeDirectoriesStartingWith.Any(e => pDi.Name.StartsWith(e, StringComparison.InvariantCultureIgnoreCase))) continue;

				var sDi = sDis.FirstOrDefault(s => string.Compare(s.Name, pDi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (sDi == null)
				{
					log.Info($"[PD]\t{pDi.FullName}");
					string destination = Path.Combine(sDir.FullName, Path.GetFileName(pDi.FullName));
					_fs.CopyDirectory(pDi.FullName, destination);
				}
				else
					SyncDirectory(pDi, sDi);
			}
		}
	}
}
