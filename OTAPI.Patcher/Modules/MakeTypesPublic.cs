using Mod.Framework;
using Mod.Framework.Extensions;

namespace OTAPI.Patcher.Modules
{
	[Module("Making types public", "death")]
	[AssemblyTarget("TerrariaServer, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null")]
	public class MakeTypesPublic : RunnableModule
	{
		private Modder _modder;

		public MakeTypesPublic(Modder modder)
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

		public override void Dispose()
		{

		}
	}
}
