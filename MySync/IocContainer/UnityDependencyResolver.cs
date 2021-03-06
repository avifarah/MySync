﻿using System;
using System.Reflection;
using log4net;
using Microsoft.Practices.Unity;
using MySync.LogService;

namespace MySync.IocContainer
{
	using static System.Console;

	public class UnityDependencyResolver
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly IUnityContainer UnityContainer;

		public static readonly UnityDependencyResolver Inst = new();

		public bool IsInitialized { get; private set; }

		private UnityDependencyResolver() { }

		static UnityDependencyResolver()
		{
			UnityContainer = new UnityContainer();
			Ioc.Initialize(UnityContainer);
		}

		public void RegisterAll()
		{
			try
			{
				//UnityContainer.RegisterType<IFileSystem, FileSystem>(new ContainerControlledLifetimeManager());
				UnityContainer.RegisterType<IFileSystem, FileSystem>();
				UnityContainer.RegisterType<ISync, Sync>();

				//string loggerName = "logger";
				//InjectionMember im = new InjectionConstructor(loggerName);
				//UnityContainer.RegisterType<ILogLocator, LogLocator>(new ContainerControlledLifetimeManager(), im);

				////ILog log = null;
				//	var fs = UnityContainer.Resolve<IFileSystem>();
				//	//var locator = UnityContainer.Resolve<ILogLocator>(loggerName);
				//	//log = locator.GetService<ILog>(loggerName);

				//	InjectionMember imFs = new InjectionConstructor(fs);
				//	//InjectionMember imLog = new InjectionConstructor(log);
				//	UnityContainer.RegisterType<ISync, Sync>(new ContainerControlledLifetimeManager()/*, imFs, imLog*/);

				IsInitialized = true;
			}
			catch (Exception ex)
			{
				//log?.Log<UnityDependencyResolver>(LogLevel.Fatal, $"Could not instatiate Sync().  {ex.Message}", ex);
				Log.Error("Registration of container not successful", ex);
				//WriteLine($"{ex.Message}\n{ex.StackTrace}");
			}
		}

		public IUnityContainer Container => UnityContainer;
	}
}
