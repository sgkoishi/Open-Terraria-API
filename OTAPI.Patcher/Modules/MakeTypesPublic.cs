using Mod.Framework;
using Mod.Framework.Extensions;

namespace OTAPI.Patcher.Modules
{
	/// <summary>
	/// This module will iterate over each type defined by the registered assemblies, while transforming all hidden members into public members.
	/// </summary>
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
