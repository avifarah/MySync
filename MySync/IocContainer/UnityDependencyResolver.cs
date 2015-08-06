using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
namespace MySync.IocContainer
{
	public class UnityDependencyResolver
	{
		private static readonly IUnityContainer UnityContainer;

		static UnityDependencyResolver()
		{
			UnityContainer = new UnityContainer();
			Ioc.Initialize(UnityContainer);
		}

		public void RegisterAll()
		{
			UnityContainer.RegisterType<ISync, Sync>(new ContainerControlledLifetimeManager());
			UnityContainer.RegisterType<IFileSystem, FileSystem>(new ContainerControlledLifetimeManager());
		}

		public IUnityContainer Container => UnityContainer;
	}
}
