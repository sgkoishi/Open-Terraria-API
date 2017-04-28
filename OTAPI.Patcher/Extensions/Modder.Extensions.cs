using Mod.Framework;

namespace OTAPI.Patcher.Extensions
{
	public static class ModderExtensions
	{
		public static void SaveTo(this ModFramework modder, string save_directory)
		{
			System.IO.Directory.CreateDirectory(save_directory);
			foreach (var asm in modder.CecilAssemblies)
			{
				var save_to = System.IO.Path.Combine(save_directory, asm.MainModule.Name);
				if (System.IO.File.Exists(save_to))
					System.IO.File.Delete(save_to);

				asm.Write(save_to);
			}
		}
	}
}
