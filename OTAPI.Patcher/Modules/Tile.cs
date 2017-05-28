using Mod.Framework;
using Mod.Framework.Extensions;
using System.Linq;

namespace OTAPI.Patcher.Modules
{
	[Module("Tile change", "death")]
	public class TileModule : RunnableModule
	{
		private ModFramework _framework;

		public TileModule(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			// change the fields to properties so we can override them
			var tile = new Query("Terraria.Tile", this.Assemblies).Run().Single().Instance as Mono.Cecil.TypeDefinition;
			foreach (var field in tile.Fields.Where(x => !x.HasConstant))
			{
				var property = field.FieldToProperty();
				property.AsVirtual();
				field.ReplaceWith(property);
			}

			// generate the ITile interface
			var emitter = new Mod.Framework.Emitters.InterfaceEmitter(tile);
			var itile = emitter.Emit();
			itile.Namespace = "OTAPI.Tile";

			// change Tile to implement ITile
			tile.Interfaces.Add(itile);

			// transform everything to be virtual
			// note this will also allow the interface to be implemented correctly as
			tile.MakeVirtual();

			// replace Tile with ITile
			tile.ReplaceWith(itile);
		}
	}
}
