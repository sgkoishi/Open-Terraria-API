using Mod.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;

namespace OTAPI.Patcher.Modules
{
	/// <summary>
	/// This module will prepare the program to patch TerrariaServer.exe.
	/// It should grab anything the program needs from the official binaries.
	/// </summary>
	[Module("Command line arguments", "death", -1)]
	[AssemblyTarget("TerrariaServer, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null")]
	[AssemblyTarget("Terraria, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null")]
	public class Setup : RunnableModule
	{
		const String TerrariaWebsite = "https://terraria.org";

		private ModFramework _framework;

		public Setup(ModFramework framework)
		{
			_framework = framework;
		}

		string GetZipUrl()
		{
			using (var client = new HttpClient())
			{
				var data = client.GetByteArrayAsync(TerrariaWebsite).Result;
				var html = System.Text.Encoding.UTF8.GetString(data);

				const String Lookup = ">Dedicated Server";

				var offset = html.IndexOf(Lookup, StringComparison.CurrentCultureIgnoreCase);
				if (offset == -1) throw new NotSupportedException();

				var attr_character = html[offset - 1];

				var url = html.Substring(0, offset - 1);
				var url_begin_offset = url.LastIndexOf(attr_character);
				if (url_begin_offset == -1) throw new NotSupportedException();

				url = url.Remove(0, url_begin_offset + 1);

				return TerrariaWebsite + url;
			}
		}

		string DownloadZip(string url)
		{
			var uri = new Uri(url);
			string filename = Path.GetFileName(uri.AbsolutePath);
			if (!String.IsNullOrWhiteSpace(filename))
			{
				var savePath = Path.Combine(Environment.CurrentDirectory, filename);

				if (!File.Exists(savePath))
				{
					using (var client = new HttpClient())
					{
						var data = client.GetByteArrayAsync(url).Result;
						File.WriteAllBytes(savePath, data);
					}
				}

				return savePath;
			}
			else throw new NotSupportedException();
		}

		string ExtractZip(string zipPath)
		{
			var directory = Path.GetFileNameWithoutExtension(zipPath);
			var info = new DirectoryInfo(directory);

			if (!info.Exists || info.GetDirectories().Length == 0)
				ZipFile.ExtractToDirectory(zipPath, directory);

			return directory;
		}

		public override void Run()
		{
			// download website html
			// find the dedicated server url
			// download it, and extract
			// copy requirements into place

			var zipUrl = GetZipUrl();
			var zipPath = DownloadZip(zipUrl);
			var extracted = ExtractZip(zipPath);

			var serverAssembly = Assembly.LoadFrom(Path.Combine(extracted, "1353", "Windows", "TerrariaServer.exe"));
			_framework.RegisterAssemblies(serverAssembly);
		}
	}
}