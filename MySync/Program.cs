using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySync
{
	using System.Configuration;
	using System.IO;
	using System.Reflection;
	using log4net;
	using log4net.Config;
	using System.Diagnostics;

	class Program
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			log.Info(".");
			log.Info(".");
			string primary = ConfigurationManager.AppSettings["Primary"];
			if (!Directory.Exists(primary)) { log.ErrorFormat("Primary directory: \"{0}\" does not exist as a directory", primary); return; }
			log.Info($"  Primary: {primary}");

			string secondary = ConfigurationManager.AppSettings["Secondary"];
			if (!Directory.Exists(secondary)) { log.ErrorFormat("Primary directory: \"{0}\" does not exist as a directory", secondary); return; }
			log.Info($"Secondary: {secondary}");

			DirectoryInfo pDi = new DirectoryInfo(primary);
			DirectoryInfo sDi = new DirectoryInfo(secondary);

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
			Sync.SyncDirectory(pDi, sDi);
			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			log.InfoFormat("Elapsed time: {0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
		}
	}
}
