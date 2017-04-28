using Mod.Framework;
using System;

namespace OTAPI.Patcher
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Open Terraria API v3.0");

			using (var mod = new ModFramework(typeof(Program).Assembly))
			{
				mod.RunModules();
			}
		}
	}
}
