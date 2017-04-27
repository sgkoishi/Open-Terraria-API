using System;

namespace Mod.Framework
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class AssemblyTargetAttribute : Attribute
	{
		public string AssemblyName { get; set; }

		public AssemblyTargetAttribute(string assemblyName)
		{
			this.AssemblyName = assemblyName;
		}
	}
}
