using System.Collections;
using System.Configuration;
using System.Reflection;

namespace MySync.LogService
{
	internal class LogLocator : ILogLocator
	{
		private readonly Hashtable _services = new Hashtable();

		public LogLocator(string serviceName)
		{
			var loggerEntry = ConfigurationManager.AppSettings[serviceName];
			var loggingObject = Assembly.GetExecutingAssembly().CreateInstance(loggerEntry);
			AddService(serviceName, loggingObject);
		}

		public void AddService<T>(T t) => _services.Add(typeof(T).Name, t);

		public void AddService<T>(string name, T t) => _services.Add(name, t);

		public T GetService<T>() => (T)_services[typeof(T).Name];

		public T GetService<T>(string serviceName) => (T)_services[serviceName];
	}
}
