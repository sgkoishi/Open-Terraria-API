using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Mod.Framework.Emitters
{
	public class HookDelegateEmitter : IEmitter<TypeDefinition>
	{
		private string _prefix;
		private MethodDefinition _method;
		private HookFlags _flags;

		public HookDelegateEmitter(string prefix, MethodDefinition method, HookFlags flags)
		{
			this._prefix = prefix;
			this._method = method;
			this._flags = flags;
		}

		public TypeDefinition Emit()
		{
			var delegate_parameters = _method.Parameters
				.Select(x => new ParameterDefinition(x.Name, x.Attributes, x.ParameterType))
				.ToList();

			if ((_flags & HookFlags.PreReferenceParameters) != 0)
			{
				foreach (var param in delegate_parameters.Where(x => x.ParameterType.IsValueType))
				{
					param.ParameterType = new ByReferenceType(param.ParameterType);
				}
			}

			if (
				(_flags & HookFlags.AlterResult) != 0
				&& _method.ReturnType != _method.DeclaringType.Module.TypeSystem.Void
			)
			{
				delegate_parameters.Add(new ParameterDefinition(
					"result",
					ParameterAttributes.None,
					new ByReferenceType(_method.ReturnType)
				));
			}

			TypeReference return_type = (_flags & HookFlags.Cancellable) != 0 ?
				_method.DeclaringType.Module.TypeSystem.Boolean
				: _method.DeclaringType.Module.TypeSystem.Void;

			var delegate_emitter = new DelegateEmitter(
				_prefix + _method.Name,
				return_type,
				delegate_parameters,
				_method.DeclaringType.Module
			);

			return delegate_emitter.Emit();
		}
	}
}
