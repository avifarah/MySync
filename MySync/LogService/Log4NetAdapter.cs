//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Practices.Unity;

//namespace MySync.LogService
//{
//	using System.Reflection;
//	using log4net.Config;

//	public class Log4NetAdapter :  ILog
//	{
//		private static readonly log4net.ILog SLog = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
//		private IDictionary<Type, log4net.ILog> logsMap; 

//		public Log4NetAdapter()
//		{
//			XmlConfigurator.Configure();
//			logsMap = new Dictionary<Type, log4net.ILog>();
//        }

//		public void Log<T>(LogLevel level, string txt, params object[] p)
//		{
//			log4net.ILog log = RetrieveLog(typeof(T));
//			if (log == null) log = SLog;

//			switch (level)
//			{
//				case LogLevel.Debug:
//					if (p.Length == 0)
//						log.Debug(txt);
//					else
//						log.DebugFormat(txt, p);
//					break;

//				case LogLevel.Info:
//					if (p.Length == 0)
//						log.Info(txt);
//					else
//						log.InfoFormat(txt, p);
//					break;

//				case LogLevel.Warn:
//					if (p.Length == 0)
//						log.Warn(txt);
//					else
//						log.WarnFormat(txt, p);
//					break;

//				case LogLevel.Error:
//					if (p.Length == 0)
//						log.Error(txt);
//					else
//						log.ErrorFormat(txt, p);
//					break;

//				case LogLevel.Fatal:
//					if (p.Length == 0)
//						log.Fatal(txt);
//					else
//						log.FatalFormat(txt, p);
//					break;

//				case LogLevel.Off:
//					break;
//			}
//		}

//		public void Log<T>(LogLevel level, string txt, Exception ex)
//		{
//			log4net.ILog log = RetrieveLog(typeof(T));
//			if (log == null) log = SLog;

//			switch (level)
//			{
//				case LogLevel.Debug:
//					log.Debug(txt, ex);
//					break;

//				case LogLevel.Info:
//					log.Info(txt, ex);
//					break;

//				case LogLevel.Warn:
//					log.Warn(txt, ex);
//					break;

//				case LogLevel.Error:
//					log.Error(txt, ex);
//					break;

//				case LogLevel.Fatal:
//					log.Fatal(txt, ex);
//					break;

//				case LogLevel.Off:
//					break;
//			}
//		}

//		private log4net.ILog RetrieveLog(Type t)
//		{
//			if (logsMap.ContainsKey(t))
//			{
//				log4net.ILog log = log4net.LogManager.GetLogger(t);
//				logsMap.Add(t, log);
//				return log;
//			}

//			return logsMap[t];
//		}
//	}
//}
