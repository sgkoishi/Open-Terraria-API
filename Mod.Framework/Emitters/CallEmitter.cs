using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Mod.Framework.Emitters
{
	public class CallEmitter : IEmitter<MergableMethod>
	{
		private MethodDefinition _method;
		private MethodDefinition _callback;
		private Instruction _insert_before;
		public bool _reference_args;

		public CallEmitter(MethodDefinition method, MethodDefinition callback, Instruction insert_before)
		{
			this._method = method;
			this._callback = callback;
			this._insert_before = insert_before;
		}

		public MergableMethod Emit()
		{
			var instructions = new List<Instruction>();
			//var processor = _method.Body.GetILProcessor();

			// if the callback is an instance method then add in the instance (this)
			if (!_callback.IsStatic)
			{
				instructions.AddRange(Extensions.CecilExtensions.ParseAnonymousInstruction(
					new { OpCodes.Ldarg_0 }
				));
			}

			foreach (var parameter in _callback.Parameters)
			{
				var opcode = parameter.ParameterType.IsByReference ? OpCodes.Ldarga : OpCodes.Ldarg;

				instructions.AddRange(Extensions.CecilExtensions.ParseAnonymousInstruction(
					new { opcode, parameter }
				));
			}

			instructions.AddRange(Extensions.CecilExtensions.ParseAnonymousInstruction(
				new { OpCodes.Call, _callback }
			));

			return new MergableMethod()
			{
				Instructions = instructions
			};
		}
	}
}
