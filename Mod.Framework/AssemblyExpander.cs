using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mod.Framework
{
	public class AssemblyExpander
	{
		private List<object> _context;
		private List<TypeMeta> _results = new List<TypeMeta>();

		private static Dictionary<string, List<TypeMeta>> _assembly_cache = new Dictionary<string, List<TypeMeta>>();

		public AssemblyExpander()
		{

		}

		public IEnumerable<TypeMeta> Results
		{
			get { return _results; }
		}

		public void SetContext(IEnumerable<object> context)
		{
			var context_map = new List<object>();

			foreach (var item in context)
			{
				context_map.Add(item);
			}

			this._context = context_map;
		}

		// TODO: Add IL patterns etc (plus a switch to turn off as this is very intensive)
		//public void Expand(MethodDefinition method, Instruction instruction, bool add = true)
		//{
		//	//if (add) this._context.Add(parameter);
		//	if (add) this._results.Add(new TypeMeta()
		//	{
		//		Instance = instruction,
		//		AssemblyName = method.DeclaringType.Module.Assembly.Name.Name,
		//		//FullName = $"{method.FullName}#{instruction.ToString()}"
		//		FullName = $"{method.DeclaringType.FullName}.{method.Name}{GenerateParameters(method.Parameters)}#{instruction.ToString()}"
		//	});
		//}

		public void Expand(ParameterDefinition parameter, bool add = true)
		{
			if (add) this._results.Add(new TypeMeta()
			{
				Instance = parameter,
				AssemblyName = parameter.ParameterType.Module.Assembly.Name.Name,
				FullName = parameter.ParameterType.Name
			});
		}

		public void Expand(PropertyDefinition property, bool add = true)
		{
			if (add) this._results.Add(new TypeMeta()
			{
				Instance = property,
				AssemblyName = property.PropertyType.Module.Assembly.Name.Name,
				FullName = property.DeclaringType.FullName + '.' + property.Name
			});
		}

		string GenerateParameters(IEnumerable<ParameterDefinition> parameters)
		{
			var sb = new StringBuilder();

			sb.Append('(');

			bool commar = false;
			foreach (var param in parameters)
			{
				if (commar) sb.Append(',');
				commar = true;
				sb.Append(param.ParameterType.FullName);
			}

			sb.Append(')');

			return sb.ToString();
		}

		public void Expand(MethodDefinition method, bool add = true)
		{
			if (add) this._results.Add(new TypeMeta()
			{
				Instance = method,
				AssemblyName = method.DeclaringType.Module.Assembly.Name.Name,
				FullName = $"{method.DeclaringType.FullName}.{method.Name}{GenerateParameters(method.Parameters)}"
			});

			foreach (var parameter in method.Parameters)
			{
				Expand(parameter);
			}

			//if (method.HasBody)
			//{
			//	foreach (var instruction in method.Body.Instructions)
			//	{
			//		Expand(method, instruction);
			//	}
			//}
		}
		public void Expand(TypeDefinition type, bool add = true)
		{
			if (add) this._results.Add(new TypeMeta()
			{
				Instance = type,
				AssemblyName = type.Module.Assembly.Name.Name,
				FullName = type.FullName
			});

			foreach (var nested in type.NestedTypes)
			{
				Expand(nested);
			}
			foreach (var method in type.Methods)
			{
				Expand(method);
			}
			foreach (var prop in type.Properties)
			{
				Expand(prop);
			}
		}

		public void Expand(ModuleDefinition module, bool add = true)
		{
			if (add) this._results.Add(new TypeMeta()
			{
				Instance = module,
				AssemblyName = module.Assembly.Name.Name,
				FullName = module.Name
			});

			foreach (var type in module.Types)
			{
				Expand(type);
			}
		}

		public void Expand(AssemblyDefinition assembly, bool add = true)
		{
			List<TypeMeta> meta = null;
			if (!_assembly_cache.TryGetValue(assembly.FullName, out meta) || meta == null)
			{
				Console.Write($"Expanding assembly {assembly.FullName}...");

				if (add) this._results.Add(new TypeMeta()
				{
					Instance = assembly,
					AssemblyName = assembly.Name.Name,
					FullName = assembly.FullName
				});

				foreach (var module in assembly.Modules)
				{
					Expand(module);
				}

				System.Console.WriteLine($"found {this._results.Count} item(s)");
				_assembly_cache.Add(assembly.FullName, this._results);
			}
			else
			{
				Console.WriteLine($"Cache hit for assembly: {assembly.FullName}");
				this._results = meta;
			}
		}
		
		public void Expand()
		{
			var initial = this._context.ToArray();
			var self = this.GetType().GetMethods()
				.Where(x => x.Name == "Expand")
				.Select(x => new { Method = x, Parameters = x.GetParameters() })
				.Where(x => x.Parameters.Count() == 2);

			foreach (var item in initial)
			{
				var item_type = item.GetType();
				var expand_match = self
					.SingleOrDefault(x => x.Parameters[0].ParameterType.IsAssignableFrom(item_type))
				;

				if (expand_match != null)
				{
					expand_match.Method.Invoke(this, new[] { item, true });
				}
				else
				{
					throw new InvalidOperationException("No Expand match");
				}
			}
		}
	}
}
