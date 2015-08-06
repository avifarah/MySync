namespace MySync
{
	using System;
	using System.Configuration;
	using System.IO;
	using System.Reflection;
	using log4net;
	using log4net.Config;
	using System.Diagnostics;
	using IocContainer;
	using Microsoft.Practices.Unity;

	class Program
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static UnityDependencyResolver DependencyResolver;

		static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			log.Info(".");
			log.Info(".");

			RegisterTypes();
			ISync sync = DependencyResolver.Container.Resolve<ISync>();

			string primary = ConfigurationManager.AppSettings["Primary"];
			if (!Directory.Exists(primary)) { log.Error($"Primary directory: \"{primary}\" does not exist as a directory"); return; }
			log.Info($"  Primary: {primary}");

			string secondary = ConfigurationManager.AppSettings["Secondary"];
			if (!Directory.Exists(secondary)) { log.Error($"Secondary directory: \"{secondary}\" does not exist as a directory"); return; }
			log.Info($"Secondary: {secondary}");

			DirectoryInfo pDi, sDi;
			try
			{
				pDi = new DirectoryInfo(primary);
				sDi = new DirectoryInfo(secondary);
			}
			catch (Exception ex)
			{
				log.Error($"Error while synching \"{primary}\" -> \"{secondary}\".  {ex.Message}", ex);
				return;
			}

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
			sync.SyncDirectory(pDi, sDi);
			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			log.Info($"Elapsed time: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
		}

		private static void RegisterTypes()
		{
			DependencyResolver = new UnityDependencyResolver();
			DependencyResolver.RegisterAll();
		}
	}
}
