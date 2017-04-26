using System;

namespace Mod.Framework
{
	public abstract class Module : IDisposable
	{
		public string Name { get; private set; }

		public string Author { get; private set; }

		public int Order { get; private set; }

		public Module()
		{
			ModuleAttribute attribute = (ModuleAttribute)Attribute.GetCustomAttribute(
				this.GetType(),
				typeof(ModuleAttribute)
			);

			this.Name = attribute.Name;
			this.Author = attribute.Author;
			this.Order = attribute.Order;
		}

		public abstract void Dispose();
	}

	public abstract class RunnableModule : Module
	{
		public abstract void Run();
	}
}
