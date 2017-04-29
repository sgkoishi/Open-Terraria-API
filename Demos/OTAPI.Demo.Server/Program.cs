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
			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

			#region Auto generated hooks
			ModFramework.ModHooks.Chest.PreFindChest = (ref int x, ref int y, ref int result) =>
			{
				Console.WriteLine($"{nameof(ModFramework.ModHooks.Chest.PreFindChest)} X={x},Y={y},r={result}");
				return true;
			};
			ModFramework.ModHooks.Main.PreInitialize = () =>
			{
				Console.WriteLine(nameof(ModFramework.ModHooks.Main.PreInitialize));
				return true;
			};
			ModFramework.ModHooks.MessageBuffer.PreGetData = (ref int start, ref int length, out int messageType) =>
			{
				messageType = 0;
				Console.WriteLine(nameof(ModFramework.ModHooks.MessageBuffer.PreGetData));
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

		private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{

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
