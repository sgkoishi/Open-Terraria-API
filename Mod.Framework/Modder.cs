using Mono.Cecil;
using Ninject;
using Ninject.Extensions.Conventions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mod.Framework
{
	public class Modder : IDisposable
	{
		private StandardKernel _kernel;
		private NugetAssemblyResolver _resolver;
		private ReaderParameters _readerParameters;

		public List<Assembly> Assemblies { get; set; } = new List<Assembly>();
		public List<AssemblyDefinition> CecilAssemblies { get; private set; } = new List<AssemblyDefinition>();

		public string DefaultModuleGlob = @"../../../Mod.Framework.**/bin/Debug/Mod.Framework.**.dll";

		public Modder(params Assembly[] module_assemblies)
		{
			this.Assemblies.Add(Assembly.GetExecutingAssembly());
			this.Assemblies.AddRange(module_assemblies);

			_resolver = new NugetAssemblyResolver();
			_readerParameters = new ReaderParameters(ReadingMode.Immediate)
			{
				AssemblyResolver = _resolver
			};

			_kernel = new StandardKernel();

			_kernel.Bind<Modder>().ToConstant(this);

			LoadExternalModules();

			_kernel.Bind(c => c.From(this.Assemblies)
				.SelectAllClasses()
				.WithAttribute<ModuleAttribute>()
				.BindBase()
			);
		}

		private void LoadExternalModules()
		{
			this.LoadFileModules(this.DefaultModuleGlob);
		}

		private void UpdateCecilAssemblies()
		{
			foreach (var assembly in this.Assemblies)
			{
				if (!CecilAssemblies.Any(x => x.FullName == assembly.FullName))
				{
					var def = AssemblyDefinition.ReadAssembly(assembly.Location, _readerParameters);
					CecilAssemblies.Add(def);
				}
			}
		}

		string EnsureCopied(FileSystemInfo file, string extension)
		{
			var file_path = Path.ChangeExtension(file.FullName, extension);
			var file_name = Path.GetFileName(file_path);
			var new_path = Path.Combine(Environment.CurrentDirectory, file_name);

			try
			{
				if (File.Exists(new_path))
					File.Delete(new_path);

				if (File.Exists(file.FullName))
					File.Copy(file.FullName, new_path);
			}
			catch (Exception ex)
			{

			}

			return new_path;
		}

		public void LoadFileModules(params string[] globs)
		{
			foreach (var glob in globs)
			{
				foreach (var file in Glob.Glob.Expand(glob))
				{
					//// for debugging to pick up pdb's
					//var file_path = EnsureCopied(file, "dll");
					//EnsureCopied(file, "pdb");
					//EnsureCopied(file, "xml");

					var assembly = Assembly.LoadFile(file.FullName);

					if (assembly == null || String.IsNullOrEmpty(assembly.Location))
						throw new Exception($"Invalid assembly at: {file.FullName}");

					this.Assemblies.Add(assembly);
				}
			}

			this.UpdateCecilAssemblies();
		}

		public void RunModules()
		{
			foreach (RunnableModule module in _kernel.GetAll<RunnableModule>().OrderBy(x => x.Order))
			{
				module.Assemblies = module.AssemblyTargets.Count() == 0 ?
					this.CecilAssemblies
					: this.CecilAssemblies.Where(asm => module.AssemblyTargets.Any(t => t == asm.FullName))
				;

				Console.WriteLine($"\t-> Running module: {module.Name}");
				module.Run();
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					_kernel.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Modder() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}