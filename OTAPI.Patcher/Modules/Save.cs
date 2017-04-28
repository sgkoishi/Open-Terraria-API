using Mod.Framework;
using OTAPI.Patcher.Extensions;

namespace OTAPI.Patcher.Modules
{
	[Module("File save", "death")]
	public class SaveModule : RunnableModule
	{
		private ModFramework _modder;

		public SaveModule(ModFramework modder)
		{
			_modder = modder;
		}

		public override void Run()
		{
			_modder.SaveTo("Output");
		}
	}
}
