using Mod.Framework;
using Mod.Framework.Extensions;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OTAPI.Patcher.Modules
{
	[Module("Command line arguments", "death")]
	public class CLI : RunnableModule
	{
		private Modder _modder;

		public CLI(Modder modder)
		{
			_modder = modder;
		}

		private HookFlags ParseFromPattern(ref string pattern)
		{
			var flags = HookFlags.None;
			var segments = pattern.Split('$');

			if (segments.Length == 2)
			{
				// valid
				pattern = segments[0];

				var character_flags = segments[1].ToCharArray();
				foreach(var flag in character_flags)
				{
					switch(flag)
					{
						case 'b': // begin hook
							flags |= HookFlags.Pre;
							break;
						case 'e': // end hook
							flags |= HookFlags.Post;
							break;
						case 'r': // reference parameters
							flags |= HookFlags.PreReferenceParameters;
							break;
						case 'c': // begin hook can cancel
							flags |= HookFlags.Cancellable;
							break;
						case 'a': // begin hook can alter non-void method return value
							flags |= HookFlags.AlterResult;
							break;
						default:
							throw new Exception($"Assembly Modification Pattern Flag is not valid: `{flag}`");
					}
				}
			}
			else if (segments.Length > 2)
			{
				throw new Exception("Assembly Modification Patterns (AMP) only support flags defined at the end of the pattern");
			}

			return flags;
		}

		public override void Run()
		{
			List<string> inputs = new List<string>();
			List<string> modifications = new List<string>();
			OptionSet options = null;

			Console.WriteLine("Parsing command line arguments");

			var args = Environment.GetCommandLineArgs().Skip(1);
			if (args.Count() == 0)
			{
				args = new[]
				{
					//@"-m=[TerrariaServer]Terraria.*,[TerrariaServer]ReLogic.*/rbe",
					@"-m=Terraria.Chest.Find*$ber",
					@"-m=Terraria.Main.Initialize()$bec",
					@"-a=../../../TerrariaServer.exe",
				};
			}

			options = new OptionSet();
			options.Add("m=|mod=|modification=", "Specify an assembly modification pattern (AMP)",
				op => modifications.Add(op));
			options.Add("a=|asm=|assembly=", "Specify an file glob for input assemblies",
				op => inputs.Add(op));

			options.Parse(args);

			if (modifications.Count == 0 || inputs.Count == 0)
			{
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			_modder.LoadFileModules(inputs.ToArray());

			foreach (var pattern in modifications)
			{
				string query_pattern = pattern;
				var flags = ParseFromPattern(ref query_pattern);
				var res = new Query(query_pattern, _modder.CecilAssemblies)
					.Run()
					.Hook(flags)
				;
			}

			//var q = new Query("[TerrariaServer]Terraria.*&&[TerrariaServer]ReLogic.*", _modder.Assemblies);
			//var sub = q.Run();

			//var q2 = new Query("[TerrariaServer]Terraria.Chat.*", _modder.Assemblies);
			//var sub2 = q2.Run();

			//var q3 = new Query("Terraria.Chat.*", _modder.Assemblies);
			//var sub3 = q3.Run();
			//sub3.Hook();

			// hook only the Terraria.Main.Initialize method
			//new Query("Terraria.Main.Initialize()", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook()
			//;
			//new Query("Terraria.Chest.AddShop(Terraria.Item)", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook()
			//;
			//new Query("Terraria.Chest.Initialize()", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook()
			//;
			//var asddd = new Query("Terraria.Chest.Find*", _modder.CecilAssemblies)
			//	.Run()
			//	.Select(x => x.Instance as MethodDefinition)
			//	.Where(x => x != null)
			//	.ToArray()
			//;

			//new Query("Terraria.Chest.FindEmptyChest(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook(HookFlags.Pre | HookFlags.Post | HookFlags.PreReferenceParameters)
			//;
			//var res = new Query("Terraria.Chest.Find*", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook(HookFlags.Pre | HookFlags.Post | HookFlags.PreReferenceParameters)
			//;
			//// hook every method in Terraria.Main
			//new Query("Terraria.MessageBuffer.*&&Terraria.Netplay.*", _modder.CecilAssemblies)
			//	.Run()
			//	.Hook(HookFlags.Pre | HookFlags.Post | HookFlags.Cancellable)
			//;
		}

		public override void Dispose()
		{

		}
	}
}
