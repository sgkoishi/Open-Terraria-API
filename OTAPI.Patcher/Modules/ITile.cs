using Mod.Framework;
using Mod.Framework.Extensions;
using System.Linq;

namespace OTAPI.Patcher.Modules
{
	/// <summary>
	/// This module will generate a new OTAPI.Tile interface based off Terraria.Tile.
	/// It will then make Terraria.Tile extend OTAPI.Tile, and replace any Terraria.Tile reference with OTAPI.Tile so that tile implementations can be hot swappable.
	/// </summary>
	[Module("Implementing the ITile interface", "death")]
	public class ITileModule : RunnableModule
	{
		private ModFramework _framework;

		public ITileModule(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			// change the fields to properties so we can override them
			var tile = new Query("Terraria.Tile", this.Assemblies).Run().Single().Instance as Mono.Cecil.TypeDefinition;
			foreach (var field in tile.Fields.Where(x => !x.HasConstant))
			{
				var property = field.ChangeToProperty().AsVirtual();
				field.ReplaceWith(property);
			}

			// generate the ITile interface
			var emitter = new Mod.Framework.Emitters.InterfaceEmitter(tile);
			var itile = emitter.Emit();
			//itile.Namespace = "OTAPI.Tile";

			// change Tile to implement ITile
			tile.Interfaces.Add(itile);

			// transform everything to be virtual
			// this will also allow the interface to be implemented correctly
			tile.MakeVirtual();

			// replace Tile with ITile
			tile.ReplaceWith(itile);
		}
	}
}
