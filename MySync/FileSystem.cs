
using System.Threading;
using System.Threading.Tasks;


namespace MySync
{
	using System;
	using System.Text;
	using System.Reflection;
	using System.IO;
	using log4net;
	using System.Diagnostics;

	public class FileSystem : IFileSystem
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void CopyFile(string src, string dst)
		{
			try
			{
				UnsafeCopyFile(src, dst);
			}
			catch (Exception ex)
			{
				Log.Error($"Error while copying file.  src: \"{src}\".  dst: \"{dst}\".  {ex.Message}", ex);
			}
		}

		private void UnsafeCopyFile(string src, string dst)
		{
			if (!File.Exists(src))
			{
				Log.Warn($"Source file does not exist (src.Length = {src.Length}): \"{src}\"");
				return;
			}

			// Is destination file or directory
			var srcFile = Path.GetFileName(src);
			var dstFile = Path.GetFileName(dst);
			if (string.Compare(srcFile, dstFile, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				var dstDirF = Path.GetDirectoryName(dst);
				if (Directory.Exists(dstDirF))
				{
					try { File.Copy(src, dst, true); }
					catch (Exception ex) { Log.Error($"File copy: \"{src}\" to \"{dst}\" failed with an error: {ex.Message}", ex); }
				}
				else
					Log.Warn($"Destination folder: \"{dstDirF}\" was not found.");
				return;
			}

			// Potentially dest, destination Path, is a directory
			var srcDirF = Path.GetDirectoryName(src);
			var srcDir1 = Path.GetFileName(srcDirF);
			var dstDir1 = Path.GetFileName(dst);
			if (string.Compare(srcDir1, dstDir1, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				if (Directory.Exists(dst))
				{
					var dstPathFull = Path.Combine(dst, srcFile);
					var waitTime = MySyncConfiguration.Inst.MaxTimeToWaitForFileCopy;
					var rc = XcopyFile(src, dstPathFull, waitTime);
					if (!rc) Log.Error($"Copy file \"{src}\" timed out.  after {waitTime.TotalHours:#,##0.000} hours.  Copy the file manually");
				}
				else
					Log.Warn($"Destination folder: \"{dst}\" was not found");
				return;
			}

			// Destination is neither a file nor a directory
			Log.Warn($"Source does not match destination: \"{src}\" destination: \"{dst}\"");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dstPathFull"></param>
		/// <param name="waitTime"></param>
		/// <returns>T signifies retrie, F do not retry</returns>
		private bool XcopyFile(string src, string dstPathFull, TimeSpan waitTime)
		{
			var tCopy = new Task(() => File.Copy(src, dstPathFull, true));
			try
			{
				var rc = tCopy.Wait(waitTime);
				return !rc;
			}
			catch (Exception ex)
			{
				Log.Error($"File copy: \"{src}\" to: \"{dstPathFull}\" failed with an error: {ex.Message}", ex);
				return false;
			}
		}

		public void CopyDirectory(DirectoryInfo src, DirectoryInfo dst) => CopyDirectory(src.FullName, dst.FullName);

		public void CopyDirectory(string src, string dst)
		{
			try
			{
				XcopyDirectory(src, dst);
			}
			catch (Exception ex)
			{
				Log.Error($"Error while coping directories: src: \"{src}\", dst: \"{dst}\".  {ex.Message}", ex);
			}
		}

		//private void UnsafeCopyDirectory(string src, string dst)
		//{
		//	for (var retryCount = 0; retryCount < MySyncConfiguration.Inst.RetryCountOnCopyFailure; ++retryCount)
		//	{
		//		var waitTime = (retryCount + 1) * (int)MySyncConfiguration.Inst.MaxTimeToWaitForDirectoryCopy.TotalMilliseconds;
		//		var rc = XcopyDirectory(src, dst, waitTime);
		//		if (rc) break;
		//	}
		//}

		private static object _lastTouched = DateTime.Now;
		private static readonly AutoResetEvent ExitProcSignal = new(false);

		private static void XcopyDirectory(string src, string dst)
		{
			var lineCount = 0;
			var errCount = 0;
			//var output = new StringBuilder();
			//var errOut = new StringBuilder();

            using var proc = new Process { EnableRaisingEvents = true };
            proc.Disposed += (sender, args) => { };

            proc.OutputDataReceived += (sender, e) =>
            {
                Interlocked.Exchange(ref _lastTouched, DateTime.Now);
                if (!string.IsNullOrEmpty(e.Data))
                    Log.Info($"[{++lineCount}]: {e.Data}");
            };

            proc.ErrorDataReceived += (sender, e) =>
            {
                Interlocked.Exchange(ref _lastTouched, DateTime.Now);
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Log.Error($"XCopy reported an error [{++errCount}]: {e.Data}");
            };

            proc.StartInfo.FileName = "XCopy";
            //	/A	...	Copies only files with the archive attribute set, doesn't change the attribute.
            //	/M	...	Copies only files with the archive attribute set, turns off the archive attribute.
            //	/D:m-d-y	Copies files changed on or after the specified date.  If no date is given, copies only those files whose source time is newer than the destination time.
            //	/EXCLUDE:file1[+file2][+file3]...	Specifies a list of files containing strings. Each string should be in a separate line in the files.  When any of the strings 
            //										match any part of the absolute path of the file to be copied, that file will be excluded from being copied.  For example, specifying 
            //										a string like \obj\ or.obj will exclude all files underneath the directory obj or all files with the .obj extension respectively.
            //	/P	...	Prompts you before creating each destination file.
            //  /S	...	Copies directories and subdirectories except empty ones.
            //  /E	...	Copies directories and subdirectories, including empty ones.  Same as /S /E.May be used to modify /T.
            //	/V	...	Verifies the size of each new file.
            //	/W	...	Prompts you to press a key before copying.
            //  /C	...	Continues copying even if errors occur.
            //  /I	...	If destination does not exist and copying more than one file, assumes that destination must be a directory.
            //	/Q	...	Does not display file names while copying.
            //	/F	...	Displays full source and destination file names while copying.
            //	/L	...	Displays files that would be copied.
            //	/G	...	Allows the copying of encrypted files to destination that does not support encryption.
            //  /H	...	Copies hidden and system files also.
            //  /R	...	Overwrites read-only files.
            //	/T	...	Creates directory structure, but does not copy files. Does not include empty directories or subdirectories. /T /E includes empty directories and subdirectories.
            //	/U	...	Copies only files that already exist in destination.
            //	/K	...	Copies attributes. Normal Xcopy will reset read-only attributes.
            //	/N	...	Copies using the generated short names.
            //	/O	...	Copies file ownership and ACL information.
            //	/X	...	Copies file audit settings (implies /O).
            //	/Y	...	Suppresses prompting to confirm you want to overwrite an existing destination file.
            //	/-Y	...	Causes prompting to confirm you want to overwrite an existing destination file.
            //	/Z	...	Copies networked files in restartable mode.
            //	/B	...	Copies the Symbolic Link itself versus the target of the link.
            //	/J	...	Copies using unbuffered I/O. Recommended for very large files.
            proc.StartInfo.Arguments = $"\"{src}\" \"{dst}\" /E/C/I/H/R/K/Y";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;

            var tsk = RunProcessAsync(proc, src, dst);
            for (; ; )
            {
                if (tsk.IsCompleted)
                {
                    var rc = tsk.Result;
                    if (rc == 0) return;

                    Log.Info($"Xcopy returned code: {rc}");
                    var waitTime = MySyncConfiguration.Inst.MaxWaitForExitProcToComplete;
                    var waitOneRc = ExitProcSignal.WaitOne(waitTime);
                    if (!waitOneRc) Log.Error("Process Time of waiting expired");
                    return;
                }


                if (DateTime.Now - (DateTime)_lastTouched > MySyncConfiguration.Inst.MaxTimeToWaitForFileCopy) break;
                Thread.Sleep(500);
            }

            return;
        }

		private static Task<int> RunProcessAsync(Process proc, string src, string dst)
		{
			var tcs = new TaskCompletionSource<int>();

			proc.Exited += (s, e) => {
				try
				{
					var exitCode = proc.ExitCode;
					var startTime = proc.StartTime;
					var exitTime = proc.ExitTime;
					ExitProcSignal.Set();
					tcs.SetResult(exitCode);
					if (exitCode == 0) return;
					//string exMsg = lineCount == 0 ? string.Empty : $"{Environment.NewLine}{output.ToString()}";
					Log.Warn($"XCopy exit code: {exitCode} indicating a potential error. ({startTime} - {exitTime})");
				}
				catch (Exception ex)
				{
					tcs.SetResult(-1);
					Log.Error($"{src} -> {dst}.  Failed.  msg: {ex.Message}", ex);
				}
			};

			try
			{
				var started = proc.Start();
				if (!started)
				{
					Log.Error("Could not start Xcopy");
					tcs.SetResult(-1);
					return tcs.Task;
				}

				// Asynchronously read the standard output of the spawned process.  
				// This raises OutputDataReceived events for each line of output.
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
			}
			catch (Exception ex)
			{
				//var extraMsg = lineCount == 0 ? string.Empty : $"\nOther output:\n{output}";
				Log.Error($"XCopy did not succeed. ({proc.StartTime} - {proc.ExitTime})  Error: {ex.Message}");
			}

			return tcs.Task;
		}
	}
}
