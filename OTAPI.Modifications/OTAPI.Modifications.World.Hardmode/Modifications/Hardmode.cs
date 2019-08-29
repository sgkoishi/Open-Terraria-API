﻿using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.World
{
	public class Hardmode : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking WorldGen.StartHardmode()...";
		public override void Run()
		{
			var vanilla = this.Method(() => Terraria.WorldGen.StartHardmode());

			var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.WorldGen.HardmodeBegin());
			var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.WorldGen.HardmodeEnd());

			vanilla.Wrap
			(
				beginCallback: cbkBegin,
				endCallback: cbkEnd,
				beginIsCancellable: true,
				noEndHandling: false,
				allowCallbackInstance: false
			);
		}
	}
}
