using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod.Framework
{
	/// <summary>
	/// Provides the base class for modifications
	/// </summary>
	public abstract class Module : IDisposable
	{
		public string Name { get; private set; }

		public string[] Authors { get; private set; }

		public int Order { get; private set; }

		public IEnumerable<String> AssemblyTargets { get; set; }

		public IEnumerable<AssemblyDefinition> Assemblies { get; set; }

		public Module()
		{
			ModuleAttribute attribute = (ModuleAttribute)Attribute.GetCustomAttribute(
				this.GetType(),
				typeof(ModuleAttribute)
			);

			AssemblyTargets = ((AssemblyTargetAttribute[])Attribute.GetCustomAttributes(
				this.GetType(),
				typeof(AssemblyTargetAttribute)
			)).Select(x => x.AssemblyName);

			this.Name = attribute.Name;
			this.Authors = attribute.Authors;
			this.Order = attribute.Order;
		}

		public virtual void Dispose() { }
	}
}
