using Mod.Framework.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod.Framework.Emitters
{
	/// <summary>
	/// This emitter will emit a call to a field that derives from a hook delegate.
	/// </summary>
	public class HookEmitter : IEmitter<MergableMethod>
	{
		private FieldDefinition _hook_field;
		private MethodDefinition _method;
		private bool _is_cancellable;
		private bool _is_by_reference;

		private List<VariableDefinition> _variables = new List<VariableDefinition>();

		public HookEmitter(FieldDefinition hook_field, MethodDefinition method, bool is_cancellable, bool is_by_reference)
		{
			this._hook_field = hook_field;
			this._method = method;
			this._is_cancellable = is_cancellable;
			this._is_by_reference = is_by_reference;
		}

		List<Instruction> EmitCall()
		{
			var invoke_method = _hook_field.FieldType.Resolve().Method("Invoke");

			VariableDefinition
				local_field_instance = new VariableDefinition("continue_method", invoke_method.ReturnType)
			;

			InstructionReference
				call_invoke = new InstructionReference(),
				store_result = new InstructionReference()
			;

			if (this._is_cancellable)
			{
				_variables.Add(local_field_instance);
				return CecilExtensions.ParseAnonymousInstruction(
					new { OpCodes.Ldsfld, _hook_field },
					new { OpCodes.Dup },
					new { OpCodes.Brtrue_S, call_invoke },

					new { OpCodes.Pop },
					new { OpCodes.Ldc_I4_1 },
					new { OpCodes.Br_S, store_result },

					new Func<IEnumerable<object>>(() =>
					{
						IEnumerable<object> collection = null;

						if (_method.Parameters.Count > 0)
						{
							var first = _method.Parameters.First();
							call_invoke = call_invoke.Create(
								_is_by_reference && first.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg,
								first
							);
							collection = new object[]
							{
								call_invoke,
								_method.Parameters.Skip(1)
									.Select(x => new {
										OpCode = _is_by_reference && x.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg,
										Operand = x
									}),
								new { OpCodes.Callvirt, invoke_method }
							};
						}
						else
						{
							collection = new object[]
							{
								_method.Parameters
									.Select(x => new { OpCodes.Ldarg, x }),
								call_invoke = call_invoke.Create(OpCodes.Callvirt, invoke_method)
							};
						}

						return collection;
					}).Invoke(),

					store_result = store_result.Create(OpCodes.Stloc, local_field_instance),
					new { OpCodes.Ldloc, local_field_instance }
				).ToList();
			}
			else
			{
				return CecilExtensions.ParseAnonymousInstruction(
					new { OpCodes.Ldsfld, _hook_field },
					new { OpCodes.Dup },
					new { OpCodes.Brtrue_S, call_invoke },

					new { OpCodes.Pop },
					new { OpCodes.Br_S, store_result },

					new Func<IEnumerable<object>>(() =>
					{
						IEnumerable<object> collection = null;

						if (_method.Parameters.Count > 0)
						{
							var first = _method.Parameters.First();
							call_invoke = call_invoke.Create(
								_is_by_reference && first.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg,
								first
							);
							collection = new object[]
							{
								call_invoke,
								_method.Parameters.Skip(1)
									.Select(x => new {
										OpCode = _is_by_reference && x.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg,
										Operand = x
									}),
								new { OpCodes.Callvirt, invoke_method }
							};
						}
						else
						{
							collection = new object[]
							{
								call_invoke = call_invoke.Create(OpCodes.Callvirt, invoke_method)
							};
						}

						return collection;
					}).Invoke(),

					store_result = store_result.Create(invoke_method.ReturnType.FullName == "System.Void" ? OpCodes.Nop : OpCodes.Pop)

				//new Func<IEnumerable<object>>(() =>
				//{
				//	IEnumerable<object> cancellation = Enumerable.Empty<object>();

				//	if (invoke_method.ReturnType.FullName != "System.Void")
				//	{
				//		cancellation = new[]
				//		{
				//			new 
				//		};
				//	}
				//	return cancellation;
				//}).Invoke()
				).ToList();
			}
		}

		public MergableMethod Emit()
		{
			var instructions = EmitCall();

			return new MergableMethod()
			{
				Instructions = instructions,
				Variables = this._variables
			};
		}
	}
}
