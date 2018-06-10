namespace MySync.LogService
{
	internal interface ILogLocator
	{
		void AddService<T>(T t);

		void AddService<T>(string name, T t);

		T GetService<T>();

		T GetService<T>(string serviceName);
	}
}