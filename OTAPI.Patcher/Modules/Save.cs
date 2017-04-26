using Mod.Framework;
using OTAPI.Patcher.Extensions;

namespace OTAPI.Patcher.Modules
{
	[Module("File save", "death")]
	public class SaveModule : RunnableModule
	{
		private Modder _modder;

		public SaveModule(Modder modder)
		{
			_modder = modder;
		}

		public override void Run()
		{
			_modder.SaveTo("Output");
		}

		public override void Dispose()
		{

		}
	}
}
