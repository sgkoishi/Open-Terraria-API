using Mod.Framework;
using Mod.Framework.Extensions;

namespace OTAPI.Patcher.Modules
{
	[Module("Making types public", "death")]
	public class MakeTypesPublic : RunnableModule
	{
		private ModFramework _modder;

		public MakeTypesPublic(ModFramework modder)
		{
			_modder = modder;
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies)
			{
				foreach (var type in asm.MainModule.Types)
					type.MakePublic();
			}
		}
	}
}
