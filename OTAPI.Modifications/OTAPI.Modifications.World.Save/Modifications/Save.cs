﻿using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.World.IO
{
	public class Save : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking WorldFile.saveWorld(bool,bool)...";
		public override void Run()
		{
			var vanilla = this.SourceDefinition.MainModule.Type("Terraria.IO.WorldFile").Method("saveWorld");

			bool tmp = false;
			var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.WorldFile.SaveWorldBegin(ref tmp, ref tmp));
			var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.WorldFile.SaveWorldEnd(tmp, tmp));

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
