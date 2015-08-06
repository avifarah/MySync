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
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void CopyFile(string srcP, string dstP)
		{
			try
			{
				UnsafeCopyFile(srcP, dstP);
			}
			catch (Exception ex)
			{
				log.Error($"Error while copying file.  src: \"{srcP}\".  dst: \"{dstP}\".  {ex.Message}", ex);
			}
		}

		private void UnsafeCopyFile(string srcP, string dstP)
		{
			if (!File.Exists(srcP))
			{
				log.Error($"Source file: \"{srcP}\" does not exist");
				return;
			}

			// Is destination file or directory
			string srcFile = Path.GetFileName(srcP);
			string dstFile = Path.GetFileName(dstP);
			if (string.Compare(srcFile, dstFile, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				string dstDirF = Path.GetDirectoryName(dstP);
				if (Directory.Exists(dstDirF))
				{
					try { File.Copy(srcP, dstP, true); }
					catch (Exception ex) { log.Error($"File copy: \"{srcP}\" to \"{dstP}\" failed with an error: {ex.Message}", ex); }
				}
				else
					log.Error($"Destination folder: \"{dstDirF}\" was not found.");
				return;
			}

			// Potentially destP, destination Path, is a directory
			string srcDirF = Path.GetDirectoryName(srcP);
			string srcDir1 = Path.GetFileName(srcDirF);
			string dstDir1 = Path.GetFileName(dstP);
			if (string.Compare(srcDir1, dstDir1, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				if (Directory.Exists(dstP))
				{
					string dstPathFull = Path.Combine(dstP, srcFile);
					try { File.Copy(srcP, dstPathFull, true); }
					catch (Exception ex) { log.Error($"File copy: \"{srcP}\" to: \"{dstPathFull}\" failed with an error: {ex.Message}", ex); }
				}
				else
					log.Error($"Destination folder: \"{dstP}\" was not found");
				return;
			}

			// Desination is neither a file nor a directory
			log.Error($"Source does not match destination: \"{srcP}\" destination: \"{dstP}\"");
		}

		public void CopyDirectory(DirectoryInfo src, DirectoryInfo dst)
		{
			CopyDirectory(src.FullName, dst.FullName);
		}

		public void CopyDirectory(string src, string dst)
		{
			try
			{
				UnsafeCopyDirectory(src, dst);
			}
			catch (Exception ex)
			{
				log.Error($"Error while coping directories: src: \"{src}\", dst: \"{dst}\".  {ex.Message}", ex);
			}
		}

		private void UnsafeCopyDirectory(string src, string dst)
		{
			int lineCount = 0;
			int errCount = 0;
			var output = new StringBuilder();
			var errOut = new StringBuilder();

			using (var proc = new Process())
			{
				proc.EnableRaisingEvents = true;
				proc.Exited += (s, e) => {
					if (proc.ExitCode > 0)
					{
						string exMsg = (lineCount == 0) ? string.Empty : $"\n{output.ToString()}";
						log.Error($"XCopy exit code: {proc.ExitCode} indicating a potential error. ({proc.StartTime} - {proc.ExitTime}){exMsg}");
					}
				};
				proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
					if (!String.IsNullOrEmpty(e.Data)) output.AppendLine($"[{++lineCount}]: {e.Data}");
				});
				proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
					if (!String.IsNullOrWhiteSpace(e.Data)) errOut.AppendLine($"[{++errCount}]: {e.Data}");
				};

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
					log.Error($"XCopy did not succeed. ({proc.StartTime} - {proc.ExitTime})  Error: {ex.Message}{extraMsg}");
				}
			}
		}
	}
}
