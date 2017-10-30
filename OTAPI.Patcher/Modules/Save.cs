using Mod.Framework;
using OTAPI.Patcher.Extensions;

namespace OTAPI.Patcher.Modules
{
	/// <summary>
	/// This module will save any cecil changes to disk. This is generally the last module to run.
	/// </summary>
	[Module("File save", "death", 100)]
	public class SaveModule : RunnableModule
	{
		private ModFramework _framework;

		public SaveModule(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			_framework.SaveTo("Output");
		}
	}
}
