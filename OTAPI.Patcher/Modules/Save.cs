using Mod.Framework;
using OTAPI.Patcher.Extensions;

namespace OTAPI.Patcher.Modules
{
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
