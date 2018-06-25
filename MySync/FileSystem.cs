
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
					try { File.Copy(src, dstPathFull, true); }
					catch (Exception ex) { Log.Error($"File copy: \"{src}\" to: \"{dstPathFull}\" failed with an error: {ex.Message}", ex); }
				}
				else
					Log.Warn($"Destination folder: \"{dst}\" was not found");
				return;
			}

			// Destination is neither a file nor a directory
			Log.Warn($"Source does not match destination: \"{src}\" destination: \"{dst}\"");
		}

		public void CopyDirectory(DirectoryInfo src, DirectoryInfo dst) => CopyDirectory(src.FullName, dst.FullName);

		public void CopyDirectory(string src, string dst)
		{
			try
			{
				UnsafeCopyDirectory(src, dst);
			}
			catch (Exception ex)
			{
				Log.Error($"Error while coping directories: src: \"{src}\", dst: \"{dst}\".  {ex.Message}", ex);
			}
		}

		private void UnsafeCopyDirectory(string src, string dst)
		{
			var lineCount = 0;
			var errCount = 0;
			var output = new StringBuilder();
			var errOut = new StringBuilder();
			var disposed = false;

			var proc = new Process { EnableRaisingEvents = true };

			proc.Disposed += (sender, args) => disposed = true;
			proc.Exited += (s, e) => {
				if (disposed) return;
				try
				{
					if (proc.ExitCode == 0) return;
					string exMsg = (lineCount == 0) ? string.Empty : $"\n{output.ToString()}";
					Log.Warn($"XCopy exit code: {proc.ExitCode} indicating a potential error. ({proc.StartTime} - {proc.ExitTime}){exMsg}");
				}
				catch (Exception ex)
				{
					Log.Error($"{src} -> {dst}.  Failed.  msg: {ex.Message}", ex);
				}
			};

			proc.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) output.AppendLine($"[{++lineCount}]: {e.Data}"); };
			proc.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) errOut.AppendLine($"[{++errCount}]: {e.Data}"); };

			proc.StartInfo.FileName = "XCopy";
			//  /S Copies directories and subdirectories except empty ones.
			//  /E Copies directories and subdirectories, including empty ones.  Same as /S /E.May be used to modify /T.
			//  /C Continues copying even if errors occur.
			//  /I If destination does not exist and copying more than one file, assumes that destination must be a directory.
			//  /H Copies hidden and system files also.
			//  /R Overwrites read-only files.
			//  /Y Suppresses prompting to confirm you want to overwrite an existing destination file.
			proc.StartInfo.Arguments = $"\"{src}\" \"{dst}\" /S/E/C/I/H/R/Y";
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.CreateNoWindow = true;

			try
			{
				proc.Start();

				// Asynchronously read the standard output of the spawned process.  
				// This raises OutputDataReceived events for each line of output.
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
				proc.WaitForExit();
			}
			catch (Exception ex)
			{
				string extraMsg = (lineCount == 0) ? string.Empty : $"\nOther output:\n{output.ToString()}";
				Log.Error($"XCopy did not succeed. ({proc.StartTime} - {proc.ExitTime})  Error: {ex.Message}{extraMsg}");
			}
		}
	}
}
