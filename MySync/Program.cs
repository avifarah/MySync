using Microsoft.Practices.Unity;

namespace MySync
{
	using System;
	using System.Configuration;
	using System.IO;
	using System.Diagnostics;
	using IocContainer;
	//using Microsoft.Practices.Unity;
	//using LogService;
	using System.Reflection;
	using log4net.Config;
	using static System.Console;

	class Program
	{
		//private static ILog _log;
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static Program()
		{
			XmlConfigurator.Configure();
			UnityDependencyResolver.Inst.RegisterAll();
		}

		static void Main(string[] args)
		{
			if (UnityDependencyResolver.Inst == null)
			{
				WriteLine("Dependency resolver did not initialize...");
				return;
			}

			if (!UnityDependencyResolver.Inst.IsInitialized)
			{
				Log.Error("UnityDependencyResolver was not initialized properly");
				return;
			}

			//ISync sync = UnityDependencyResolver.Inst.Container.Resolve<ISync>();
			IFileSystem fs = new FileSystem();
			ISync sync = new Sync(fs);

			//ILogLocator logLocator = UnityDependencyResolver.Inst.Container.Resolve<ILogLocator>("Logger");
			//_log = logLocator.GetService<ILog>("logger");

			string sConBuffWidth = ConfigurationManager.AppSettings["Console.BufferWidth"];
			if (!string.IsNullOrWhiteSpace(sConBuffWidth))
			{
				bool rc = int.TryParse(sConBuffWidth, out int w);
				if (rc)
					try { BufferHeight = w; }
					catch { /* swallow exception */ }
			}

			string sConBuffHeight = ConfigurationManager.AppSettings["Console.BufferHeight"];
			if (!string.IsNullOrWhiteSpace(sConBuffHeight))
			{
				bool rc = int.TryParse(sConBuffHeight, out int h);
				if (rc)
					try { BufferHeight = h; }
					catch { /* swallow exception */ }
			}

			//_log.Log<Program>(LogLevel.Info, ".");
			//_log.Log<Program>(LogLevel.Info, ".");
			Log.Info(".");
			Log.Info(".");
			Log.Info("[PO]\t-\tPrimary Only");
			Log.Info("[TM]\t-\tTime, source time is later than destination");
			Log.Info("[LN]\t-\tLength difference");

			string primary = null;
			string secondary = null;
			string skipTil = null;
			if (args.Length >= 2)
			{
				primary = args[0];
				secondary = args[1];
				if (args.Length >= 3) skipTil = args[2];
			}

			var progArgs = new ProgramArgs(primary, secondary, skipTil);
			if (!progArgs.IsPrimaryLegit())
			{
				//_log.Log<Program>(LogLevel.Error, $"Primary directory: \"{primary}\" does not exist as a directory"); return;
				Log.Error($"Primary directory: \"{primary}\" does not exist as a directory");
				return;
			}
			//_log.Log<Program>(LogLevel.Info, $"  Primary: {primary}");
			Log.Info($"  Primary: {progArgs.Primary}");

			if (!progArgs.IsSecondaryLegit())
			{
				//_log.Log<Program>(LogLevel.Error, $"Secondary directory: \"{secondary}\" does not exist as a directory"); return;
				Log.Error($"Secondary directory: \"{progArgs.Secondary}\" does not exist as a directory");
				return;
			}
			//_log.Log<Program>(LogLevel.Info, $"Secondary: {secondary}");
			Log.Info($"Secondary: {progArgs.Secondary}");

			if (!progArgs.IsSkipTilLegit())
			{
				Log.Error($"SkipTil directory, \"{progArgs.SkipTil}\", is not legitimate.  It is either not a directory or it does not match a proper subdirectory of source");
				return;
			}

			if (!string.IsNullOrWhiteSpace(progArgs.SkipTil))
				Log.Info($"Skip: \"{progArgs.SkipTil}\"");

			DirectoryInfo pDi, sDi;
			try
			{
				pDi = new DirectoryInfo(progArgs.Primary);
				sDi = new DirectoryInfo(progArgs.Secondary);
			}
			catch (Exception ex)
			{
				//_log.Log<Program>(LogLevel.Error, $"Error while synching \"{primary}\" -> \"{secondary}\".  {ex.Message}", ex);
				Log.Error($"Error while synching \"{primary}\" -> \"{secondary}\".  {ex.Message}", ex);
				return;
			}

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			//
			//	Do it to it
			//
			sync.SyncDirectory(pDi, sDi, new DirectoryNumbers(0, 0, 1), progArgs);

			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			//_log.Log<Program>(LogLevel.Info, $"Elapsed time: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
			Log.Info($"Elapsed time: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");

			WriteLine("Press any key to end...");
			ReadKey();
		}
	}
}
