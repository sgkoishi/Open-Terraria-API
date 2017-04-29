using Mod.Framework;
using Mod.Framework.Extensions;

namespace OTAPI.Patcher.Modules
{
	[Module("Making types public", "death")]
	public class MakeTypesPublic : RunnableModule
	{
		private ModFramework _framework;

		public MakeTypesPublic(ModFramework framework)
		{
			_framework = framework;
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
