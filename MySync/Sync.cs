using System.Diagnostics;
using System.Net.Security;

namespace MySync
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Configuration;
	using System.Collections.Generic;
	//using LogService;
	using System.Reflection;
	using log4net;
	using static System.Console;

	public class Sync : ISync
	{
		private int _dirCount;
		private int _copyFileCount;
		private int _copyDirCount;

		private readonly bool _isCopy;
		private readonly IEnumerable<string> _excludeFiles;
		private readonly IEnumerable<string> _excludeDirectoriesStartingWith;
		private readonly IFileSystem _fs;
		//private readonly ILog _log;
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Sync(IFileSystem fs, ILog log)
		{
			_fs = fs;
			//_log = Log;

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
			_isCopy = yeses.All(y => string.Compare(y, isReadOnly, StringComparison.InvariantCultureIgnoreCase) != 0);
		}

		public void SyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir, DirectoryNumbers dirNums, IProgramArgs progArgs)
		{
			try
			{
				++_dirCount;
				if (progArgs.IsSkipDirCopy(pDir))
				{
					Log.Info($"Skip \"{pDir.FullName}\" and all its subdirectories");
					return;
				}
				UnsafeSyncDirectory(pDir, sDir, dirNums, progArgs);
			}
			catch (Exception ex)
			{
				//_log.Log<Sync>(LogLevel.Error, $"Error while Syncing directories:  {ex.Message}", ex);
				Log.Error($"Error while Syncing directories:  {ex.Message}", ex);
			}
        }

		private void UnsafeSyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir, DirectoryNumbers dirNums, IProgramArgs progArgs)
		{
			if (progArgs.IsSkipFileCopy(pDir))
				WriteLine($"({_dirCount:#,##0}).  Skipping \"{pDir.FullName}\"");
			else
			{
				if (_isCopy)
				{
					var sw = new Stopwatch();
					CopyFiles(pDir, sDir);
					sw.Stop();
					WriteLine($"({_copyFileCount:#,##0}/{_copyDirCount:#,##0}/{_dirCount:#,##0}).  P: {pDir.FullName}\t\tS: {sDir.FullName}\t{sw.Elapsed.TotalSeconds} [sec].  ({dirNums.NestingLevel}/{dirNums.DirectoryCount}/{dirNums.TotalDirectoryCount})");
                }
				else
				{
					WriteLine($"({_dirCount:#,##0}, {dirNums.NestingLevel}/{dirNums.DirectoryCount}/{dirNums.TotalDirectoryCount}).  P: {pDir.FullName}\t\tS: {sDir.FullName}");
				}

				CopyFiles(pDir, sDir);
			}

			// Directories
			DirectoryInfo[] pDis = pDir.GetDirectories();
			DirectoryInfo[] sDis = sDir.GetDirectories();
			Array.Sort(pDis, (e1, e2) => string.Compare(e1.Name, e2.Name, StringComparison.OrdinalIgnoreCase));
			Array.Sort(sDis, (e1, e2) => string.Compare(e1.Name, e2.Name, StringComparison.OrdinalIgnoreCase));

			int dirCnt = 0;
			int totDirCnt = pDis.Length;
			foreach (var pDi in pDis)
				CopyDirectory(pDi, sDir, sDis, dirNums.NextNumbers(++dirCnt, totDirCnt), progArgs);
		}

		private void CopyDirectory(DirectoryInfo pDi, DirectoryInfo sDir, DirectoryInfo[] sDis, DirectoryNumbers dirNums, IProgramArgs progArgs)
		{
			if (_excludeDirectoriesStartingWith.Any(e => pDi.Name.StartsWith(e, StringComparison.InvariantCultureIgnoreCase))) return;

			var sDi = sDis.FirstOrDefault(s => string.Compare(s.Name, pDi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
			if (sDi == null)
			{
				//_log.Log<Sync>(LogLevel.Info, $"[PD]\t{pDi.FullName}");
				string destination = Path.Combine(sDir.FullName, Path.GetFileName(pDi.FullName));
				if (_isCopy)
				{
					var sw = new Stopwatch();
					sw.Start();
					_fs.CopyDirectory(pDi.FullName, destination);
					++_copyDirCount;
					sw.Stop();
					Log.Info($"[PD]\t{pDi.FullName}\t{sw.Elapsed.TotalSeconds} [sec], ({dirNums.NestingLevel}/{dirNums.DirectoryCount}/{dirNums.TotalDirectoryCount})");
                }
				else
					Log.Info($"[PD]\t{pDi.FullName}\t(Report only), ({dirNums.NestingLevel}/{dirNums.DirectoryCount}/{dirNums.TotalDirectoryCount})");
            }
			else
				SyncDirectory(pDi, sDi, dirNums, progArgs);
		}

		private void CopyFiles(DirectoryInfo pDir, DirectoryInfo sDir)
		{
			FileInfo[] pFiles = pDir.GetFiles();
			FileInfo[] sFiles = sDir.GetFiles();
			Array.Sort(pFiles, (e1, e2) => string.Compare(e1.Name, e2.Name, StringComparison.OrdinalIgnoreCase));
			Array.Sort(sFiles, (e1, e2) => string.Compare(e1.Name, e2.Name, StringComparison.OrdinalIgnoreCase));

			int fCount = 0;
			int fsCount = pFiles.Length;
			foreach (var pFi in pFiles)
			{
				if (_excludeFiles.Any(e => string.Compare(e, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0)) continue;

				var sFi = sFiles.FirstOrDefault(s => string.Compare(s.Name, pFi.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (sFi == null)
				{
					//_log.Log<Sync>(LogLevel.Info, $"[PO]\t{pFi.FullName}");
					string sFile = Path.Combine(sDir.FullName, Path.GetFileName(pFi.FullName));
					if (_isCopy)
					{
						var sw = new Stopwatch();
						sw.Start();
						++_copyFileCount;
						_fs.CopyFile(pFi.FullName, sFile);
						sw.Stop();
						Log.Info($"[PO]\t{pFi.FullName}\t{sw.Elapsed.TotalSeconds} [sec], ({++fCount}/{fsCount})");
                    }
					else
						Log.Info($"[PO]\t{pFi.FullName}\t(Report only), ({++fCount}/{fsCount})");
				}
				else
				{
					var pLaccess = pFi.LastAccessTimeUtc;
					var sLaccess = sFi.LastAccessTimeUtc;
					var pLen = pFi.Length;
					var sLen = sFi.Length;
					if (pLaccess > sLaccess)
					{
						if (_isCopy)
						{
							var sw = new Stopwatch();
							sw.Start();
							++_copyFileCount;
							_fs.CopyFile(pFi.FullName, sFi.FullName);
							sw.Stop();
							//_log.Log<Sync>(LogLevel.Info, $"[TM]\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, {sLen:#,##0})");
							Log.Info($"[TM]\t({pFi.FullName}, {pLaccess}, [L: {pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [L: {sLen:#,##0}])\t{sw.Elapsed.TotalSeconds} [sec], ({++fCount}/{fsCount})");
						}
						else
						{
							//_log.Log<Sync>(LogLevel.Info, $"[TM]\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, {sLen:#,##0})");
							Log.Info($"[TM]\t({pFi.FullName}, {pLaccess}, [L: {pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [L: {sLen:#,##0}])\t(Report only), ({++fCount}/{fsCount})");
						}
					}
					else if (pLen != sLen)
					{
						if (_isCopy)
						{
							var sw = new Stopwatch();
							sw.Start();
							++_copyFileCount;
							_fs.CopyFile(pFi.FullName, sFi.FullName);
							sw.Stop();
							//_log.Log<Sync>(LogLevel.Info, $"[LN]:\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [{sLen:#,##0}])");
							Log.Info($"[LN]:\t({pFi.FullName}, {pLaccess}, [L: {pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [L: {sLen:#,##0}])\t{sw.Elapsed.TotalSeconds} [sec], ({++fCount}/{fsCount})");
						}
						else
						{
							//_log.Log<Sync>(LogLevel.Info, $"[LN]:\t({pFi.FullName}, {pLaccess}, [{pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [{sLen:#,##0}])");
							Log.Info($"[LN]:\t({pFi.FullName}, {pLaccess}, [L: {pLen:#,##0}])\t-\t({sFi.FullName}, {sLaccess}, [L: {sLen:#,##0}])\t(Report only), ({++fCount}/{fsCount})");
						}
					}
				}
			}
		}
	}
}
