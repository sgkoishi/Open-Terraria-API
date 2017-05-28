using Mod.Framework;
using System;

namespace OTAPI.Patcher
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Open Terraria API v3.0");

			try
			{
				using (var framework = new ModFramework())
				{
					framework.RegisterAssemblies(typeof(Program).Assembly);
					framework.RunModules();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				Console.ReadKey();
			}
		}
	}
}
