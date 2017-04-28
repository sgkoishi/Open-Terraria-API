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
		const String DefaultHooksTypeName = "ModHooks";
		const String DefaultHandlersTypeName = "ModHandlers";

		#region Hooking
		/// <summary>
		/// Generates a hook that is called before any native code
		/// </summary>
		/// <param name="method">Method to generate the hook in</param>
		/// <param name="options">Configurable hook options</param>
		/// <returns>A <see cref="MergableMethod"/> instance</returns>
		public static MergableMethod GenerateBeginHook(this MethodDefinition method, HookOptions options = HookOptions.Default)
		{
			// emit call at offset 0

			// add or get the types where we are storing out auto generated hooks & their delegates
			var hooks_type = method.DeclaringType.GetHooksType();
			var hooks_delegates_type = method.DeclaringType.GetHooksDelegateType();

			// generate the hook handler delegate
			var hook_delegate_emitter = new HookDelegateEmitter("OnPre", method, options);
			var hook_handler = hook_delegate_emitter.Emit();
			hooks_delegates_type.NestedTypes.Add(hook_handler);

			// generate the api hook external modules can attach to
			var hook_field = new FieldDefinition("Pre" + method.Name, FieldAttributes.Public | FieldAttributes.Static, hook_handler);
			hooks_type.Fields.Add(hook_field);

			// generate the call to the delegate
			var hook_emitter = new HookEmitter(hook_field, method,
				(options & HookOptions.Cancellable) != 0,
				(options & HookOptions.ReferenceParameters) != 0
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

		/// <summary>
		/// Generates a hook that is called after native code
		/// </summary>
		/// <param name="method">Method to generate the hook in</param>
		/// <param name="options">Configurable hook options</param>
		/// <returns>A <see cref="MergableMethod"/> instance</returns>
		public static MergableMethod GenerateEndHook(this MethodDefinition method, HookOptions options = HookOptions.Default)
		{
			// emit call at each ret instruction

			// add or get the types where we are storing out auto generated hooks & their delegates
			var hooks_type = method.DeclaringType.GetHooksType();
			var hooks_delegates_type = method.DeclaringType.GetHooksDelegateType();

			// generate the hook handler delegate
			var hook_delegate_emitter = new HookDelegateEmitter("OnPost", method, options & ~(
				HookOptions.ReferenceParameters |
				HookOptions.Cancellable
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

		/// <summary>
		/// Adds configurable hooks into each method of the query
		/// </summary>
		/// <param name="results">Methods to be hooked</param>
		/// <param name="options">Hook options</param>
		/// <returns>The existing <see cref="QueryResult"/> instance</returns>
		public static QueryResult Hook(this QueryResult results, HookOptions options = HookOptions.Default)
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

				var call_emitter = new CallEmitter(method, new_method.Body.Instructions.First());

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

				if ((options & HookOptions.Pre) != 0)
				{
					var hook = new_method.GenerateBeginHook(options);

					if ((options & HookOptions.Cancellable) != 0
						&& (options & HookOptions.AlterResult) == 0
						&& method.ReturnType.FullName != "System.Void")
					{
						// TODO: this functionality will be desired - idea: just generate the "default(T)"
						// what to do if you get here: add HookFlags.AlterResult to your flags or don't try cancelling
						throw new NotSupportedException("Attempt to cancel a non-void method without allowing the callback to alter the result.");
					}

					hook.MergeInto(new_method, 0);
				}

				if ((options & HookOptions.Post) != 0)
				{
					var hook = new_method.GenerateEndHook(options);

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
		/// <summary>
		/// Adds or gets an existing nested type
		/// </summary>
		/// <param name="parentType">The parent type</param>
		/// <param name="nestedTypeName">The name of the nested type</param>
		/// <param name="attributes">Attributes for the nested type</param>
		/// <returns></returns>
		public static TypeDefinition AddOrGetNestedType
		(
			this TypeDefinition parentType,
			string nestedTypeName,
			TypeAttributes attributes
		)
		{
			var nested_type = parentType.NestedTypes.SingleOrDefault(x => x.Name == nestedTypeName);
			if (nested_type == null)
			{
				nested_type = new TypeDefinition(String.Empty, nestedTypeName, attributes);
				nested_type.BaseType = parentType.Module.TypeSystem.Object;
				parentType.NestedTypes.Add(nested_type);
			}
			return nested_type;
		}

		/// <summary>
		/// Adds the standard hooks type into the given type
		/// </summary>
		/// <param name="type">Parent type</param>
		/// <returns>The requested hook type</returns>
		public static TypeDefinition GetHooksType(this TypeDefinition type)
		{
			return type.AddOrGetNestedType(DefaultHooksTypeName,
				TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
			);
		}

		/// <summary>
		/// Adds the standard handlers type into the given type
		/// </summary>
		/// <param name="type">Parent type</param>
		/// <returns>The requested handler type</returns>
		public static TypeDefinition GetHooksDelegateType(this TypeDefinition type)
		{
			var hooks_type = GetHooksType(type);
			return hooks_type.AddOrGetNestedType(DefaultHandlersTypeName,
				TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
			);
		}

		/// <summary>
		/// Clones the signatures of a method into a new empty method.
		/// This is used to replace native methods.
		/// </summary>
		/// <param name="method">The method to clone</param>
		/// <returns>The new cloned method</returns>
		public static MethodDefinition Clone(this MethodDefinition method)
		{
			var clone = new MethodDefinition(method.Name, method.Attributes, method.ReturnType);

			foreach (var param in method.Parameters)
			{
				clone.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
			}

			return clone;
		}

		/// <summary>
		/// Clones the signatures of a method into a new empty method.
		/// This is used to replace native methods.
		/// </summary>
		/// <param name="methods">The methods to clone</param>
		/// <returns>The new cloned methods</returns>
		public static IEnumerable<MethodDefinition> Clone(this IEnumerable<MethodDefinition> methods)
		{
			foreach (var method in methods)
			{
				yield return method.Clone();
			}
		}
		#endregion
	}
}
