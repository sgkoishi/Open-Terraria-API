using System;
using System.Linq;
using System.Reflection;

namespace OTAPI.v3.Demo.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveTerrariaReferences;

			#region Auto generated hooks
			Terraria.Chest.ModHooks.PreFindChest = (ref int x, ref int y) =>
			{
				Console.WriteLine(nameof(Terraria.Chest.ModHooks.PreFindChest));
			};
			Terraria.Main.ModHooks.PreInitialize = () =>
			{
				Console.WriteLine(nameof(Terraria.Main.ModHooks.PreInitialize));
				return true;
			};
			#endregion

			try
			{
				// call this to force check IL etc. if an exception arises that required you to check
				// LoaderExceptions then you must put Terraria's implementation here and wrap try/catch
				// until you find the problematic IL
				Terraria.Program.ForceLoadAssembly(typeof(Terraria.Program).Assembly, true);

				// start the application
				Terraria.WindowsLaunch.Main(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private static System.Reflection.Assembly ResolveTerrariaReferences(object sender, ResolveEventArgs args)
		{
			var assembly = typeof(Terraria.Program).Assembly;
			var resources = assembly.GetManifestResourceNames();
			var resourceName = new AssemblyName(args.Name).Name + ".dll";

			var match = resources.SingleOrDefault(x => x.EndsWith(resourceName));
			if (match != null)
			{
				using (var stream = assembly.GetManifestResourceStream(match))
				{
					var array = new byte[stream.Length];
					stream.Read(array, 0, array.Length);
					return Assembly.Load(array);
				}
			}

			return null;
		}
	}
}
