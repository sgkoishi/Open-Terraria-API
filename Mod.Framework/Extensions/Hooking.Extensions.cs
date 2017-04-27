using Mod.Framework.Emitters;
using Mod.Framework.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod.Framework.Extensions
{
	public static class HookingExtensions
	{
		#region Hooking
		public static MergableMethod GenerateBeginHook(this MethodDefinition method, HookFlags flags = HookFlags.Default)
		{
			// emit call at offset 0

			// add or get the types where we are storing out auto generated hooks & their delegates
			var hooks_type = method.DeclaringType.GetHooksType();
			var hooks_delegates_type = method.DeclaringType.GetHooksDelegateType();

			// generate the hook handler delegate
			var hook_delegate_emitter = new HookDelegateEmitter("OnPre", method, flags);
			var hook_handler = hook_delegate_emitter.Emit();
			hooks_delegates_type.NestedTypes.Add(hook_handler);

			// generate the api hook external modules can attach to
			var hook_field = new FieldDefinition("Pre" + method.Name, FieldAttributes.Public | FieldAttributes.Static, hook_handler);
			hooks_type.Fields.Add(hook_field);

			// generate the call to the delegate
			var hook_emitter = new HookEmitter(hook_field, method,
				(flags & HookFlags.Cancellable) != 0,
				(flags & HookFlags.PreReferenceParameters) != 0
			);
			var result = hook_emitter.Emit();
			//instructions.MergeInto(method, 0);

			// end of method

			var invoke_method = hook_handler.Resolve().Method("Invoke");
			if (invoke_method.ReturnType.FullName != "System.Void")
			{
				result.Instructions = result.Instructions.Concat(new[]
				{
					Instruction.Create(OpCodes.Brtrue_S, method.Body.Instructions.First()),
					Instruction.Create(OpCodes.Br_S, method.Body.Instructions.Last())
				});
			}

			return result;
		}

		public static MergableMethod GenerateEndHook(this MethodDefinition method, HookFlags flags = HookFlags.Default)
		{
			// emit call at each ret instruction

			// add or get the types where we are storing out auto generated hooks & their delegates
			var hooks_type = method.DeclaringType.GetHooksType();
			var hooks_delegates_type = method.DeclaringType.GetHooksDelegateType();

			// generate the hook handler delegate
			var hook_delegate_emitter = new HookDelegateEmitter("OnPost", method, flags & ~(
				HookFlags.PreReferenceParameters |
				HookFlags.Cancellable
			));
			var hook_handler = hook_delegate_emitter.Emit();
			hooks_delegates_type.NestedTypes.Add(hook_handler);

			// generate the api hook external modules can attach to
			var hook_field = new FieldDefinition("Post" + method.Name, FieldAttributes.Public | FieldAttributes.Static, hook_handler);
			hooks_type.Fields.Add(hook_field);

			// generate the call to the delegate
			var hook_emitter = new HookEmitter(hook_field, method, false, false);
			var result = hook_emitter.Emit();
			//instructions.MergeInto(method, 0);

			// end of method

			return result;
		}

		public static QueryResult Hook(this QueryResult results, HookFlags flags = HookFlags.Default)
		{
			var context = results
				.Select(x => x.Instance as MethodDefinition)
				.Where(x => x != null);

			foreach (var method in context)
			{
				var new_method = method.Clone();

				// rename method to be suffixed with Direct
				method.Name += "Direct";
				method.DeclaringType.Methods.Add(new_method);
				method.Attributes &= ~MethodAttributes.Virtual;

				var processor = new_method.Body.GetILProcessor();
				var ins_return = Instruction.Create(OpCodes.Ret);
				processor.Append(ins_return);

				var call_emitter = new CallEmitter(new_method, method, new_method.Body.Instructions.First());

				var call = call_emitter.Emit();

				VariableDefinition return_variable = null;
				var nop = call.Instructions
					.SingleOrDefault(x => x.OpCode == OpCodes.Pop); // expect one here as the CallEmitter should only handle one. if it changes this needs to change
				if (nop != null)
				{
					return_variable = new VariableDefinition("direct_result", method.ReturnType);
					new_method.Body.Variables.Add(return_variable);
					nop.Operand = return_variable;
					nop.OpCode = OpCodes.Stloc_S;
				}
				call.MergeInto(new_method, 0);

				if ((flags & HookFlags.Pre) != 0)
				{
					var hook = new_method.GenerateBeginHook(flags);

					if ((flags & HookFlags.Cancellable) != 0
						&& (flags & HookFlags.AlterResult) == 0
						&& method.ReturnType.FullName != "System.Void")
					{
						// TODO: this functionality will be desired - idea: just generate the "default(T)"
						// what to do if you get here: add HookFlags.AlterResult to your flags or don't try cancelling
						throw new NotSupportedException("Attempt to cancel a non-void method without allowing the callback to alter the result.");
					}

					hook.MergeInto(new_method, 0);
				}

				if ((flags & HookFlags.Post) != 0)
				{
					var hook = new_method.GenerateEndHook(flags);

					//var last_ret = new_method.Body.Instructions.Last(x => x.OpCode == OpCodes.Ret);
					//last_ret.ReplaceTransfer(hook.Instructions.First(), new_method);

					hook.MergeInto(new_method, call.Instructions.Last().Next);
				}

				if (return_variable != null)
				{
					processor.InsertBefore(ins_return, Instruction.Create(OpCodes.Ldloc, return_variable));
				}
			}

			return results;
		}
		#endregion

		#region Helpers
		public static TypeDefinition AddOrGetNestedType(this TypeDefinition type, string name, TypeAttributes attributes)
		{
			var nested_type = type.NestedTypes.SingleOrDefault(x => x.Name == name);
			if (nested_type == null)
			{
				nested_type = new TypeDefinition(String.Empty, name, attributes);
				nested_type.BaseType = type.Module.TypeSystem.Object;
				type.NestedTypes.Add(nested_type);
			}
			return nested_type;
		}

		public static TypeDefinition GetHooksType(this TypeDefinition type)
		{
			return type.AddOrGetNestedType("ModHooks",
				TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
			);
		}

		public static TypeDefinition GetHooksDelegateType(this TypeDefinition type)
		{
			var hooks_type = GetHooksType(type);
			return hooks_type.AddOrGetNestedType("ModHandlers",
				TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
			);
		}

		public static MethodDefinition Clone(this MethodDefinition method, bool add_return = false)
		{
			var clone = new MethodDefinition(method.Name, method.Attributes, method.ReturnType);

			foreach (var param in method.Parameters)
			{
				clone.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
			}

			if (add_return)
				clone.AddReturn();

			return clone;
		}

		public static IEnumerable<MethodDefinition> Clone(this IEnumerable<MethodDefinition> results)
		{
			foreach (var method in results)
			{
				yield return method.Clone();
			}
		}
		#endregion
	}
}
