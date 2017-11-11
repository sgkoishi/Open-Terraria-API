using Mod.Framework;
using Mod.Framework.Extensions;
using Mono.Cecil;
using System.Linq;

namespace OTAPI.Patcher.Modules
{
	/// <summary>
	/// This module will find every array of ITile[] and swap it to use ITileCollection.
	/// </summary>
	[Module("Implementing the ITileCollection interface", "death", dependsOn: typeof(ITileModule))]
	public class ITileCollectionModule : RunnableModule
	{
		private ModFramework _framework;

		public ITileCollectionModule(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			var tiles = new Query("Terraria.Main.tile", this.Assemblies).Run().Single().Instance as FieldDefinition;

			tiles.ReplaceWithInterface("ITileCollection");
		}
	}
}
