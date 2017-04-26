using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mod.Framework.Extensions
{
	public static class CecilExtensions
	{
		#region Assembly
		public static TypeDefinition Type(this AssemblyDefinition assemblyDefinition, string name)
		{
			return assemblyDefinition.MainModule.Types.Single(x => x.FullName == name);
		}

		/// <summary>
		/// Enumerates all instructions in all methods across each type of the assembly
		/// </summary>
		public static void ForEachInstruction(this AssemblyDefinition assembly, Action<MethodDefinition, Mono.Cecil.Cil.Instruction> callback)
		{
			assembly.ForEachType(type =>
			{
				if (type.HasMethods)
				{
					foreach (var method in type.Methods)
					{
						if (method.HasBody)
						{
							foreach (var ins in method.Body.Instructions.ToArray())
								callback.Invoke(method, ins);
						}
					}
				}
			});
		}

		/// <summary>
		/// Enumerates over each type in the assembly, including nested types
		/// </summary>
		public static void ForEachType(this AssemblyDefinition assembly, Action<TypeDefinition> callback)
		{
			foreach (var module in assembly.Modules)
			{
				foreach (var type in module.Types)
				{
					callback(type);

					//Enumerate nested types
					type.ForEachNestedType(callback);
				}
			}
		}
		#endregion

		#region Module
		/// <summary>
		/// Enumerates all methods in the current module
		/// </summary>
		public static void ForEachMethod(this ModuleDefinition module, Action<MethodDefinition> callback)
		{
			module.ForEachType(type =>
			{
				foreach (var mth in type.Methods)
				{
					callback.Invoke(mth);
				}
			});
		}

		/// <summary>
		/// Enumerates all instructions in all methods across each type of the assembly
		/// </summary>
		public static void ForEachInstruction(this ModuleDefinition module, Action<MethodDefinition, Mono.Cecil.Cil.Instruction> callback)
		{
			module.ForEachMethod(method =>
			{
				if (method.HasBody)
				{
					foreach (var ins in method.Body.Instructions.ToArray())
						callback.Invoke(method, ins);
				}
			});
		}

		/// <summary>
		/// Enumerates over each type in the assembly, including nested types
		/// </summary>
		public static void ForEachType(this ModuleDefinition module, Action<TypeDefinition> callback)
		{
			foreach (var type in module.Types)
			{
				callback(type);

				//Enumerate nested types
				type.ForEachNestedType(callback);
			}
		}
		#endregion

		#region Method
		public static Instruction AddReturn(this MethodDefinition method)
		{
			Instruction firstInstruction = null;

			//Get the il processor instance so we can alter il
			var il = method.Body.GetILProcessor();

			//If we are working on a method with a return value
			//we will have a value to handle.
			if (method.ReturnType.FullName != "System.Void")
			{
				VariableDefinition vr1;
				method.Body.Variables.Add(vr1 = new VariableDefinition("default_result", method.ReturnType));

				//Initialise the variable
				il.Append(firstInstruction = il.Create(OpCodes.Ldloca_S, vr1));
				il.Emit(OpCodes.Initobj, method.ReturnType);
				il.Emit(OpCodes.Ldloc, vr1);
			}

			//The method is now complete.
			if (firstInstruction == null)
				il.Append(firstInstruction = il.Create(OpCodes.Ret));
			else il.Emit(OpCodes.Ret);

			return firstInstruction;
		}

		public static bool ParametersMatch(this MethodReference method, MethodReference compareTo, bool ignoreDeclaringType = true, bool ignoreParameterNames = false)
		{
			if (method.Parameters.Count != compareTo.Parameters.Count)
				return false;

			for (var x = 0; x < method.Parameters.Count; x++)
			{
				if (method.Parameters[x].ParameterType.FullName != compareTo.Parameters[x].ParameterType.FullName
					&& (ignoreDeclaringType && method.Parameters[x].ParameterType != method.DeclaringType)
					)
					return false;

				if (!ignoreParameterNames && method.Parameters[x].Name != compareTo.Parameters[x].Name)
					return false;
			}

			return true;
		}

		public static bool SignatureMatches(this MethodDefinition method, MethodDefinition compareTo, bool ignoreDeclaringType = true)
		{
			if (method.Name != compareTo.Name)
				return false;
			if (method.ReturnType.FullName != compareTo.ReturnType.FullName)
				return false;
			if (method.Overrides.Count != compareTo.Overrides.Count)
				return false;
			if (method.GenericParameters.Count != compareTo.GenericParameters.Count)
				return false;
			if (!method.DeclaringType.IsInterface && method.Attributes != compareTo.Attributes)
				return false;

			if (!method.ParametersMatch(compareTo, ignoreDeclaringType))
				return false;

			for (var x = 0; x < method.Overrides.Count; x++)
			{
				if (method.Overrides[x].Name != compareTo.Overrides[x].Name)
					return false;
			}

			for (var x = 0; x < method.GenericParameters.Count; x++)
			{
				if (method.GenericParameters[x].Name != compareTo.GenericParameters[x].Name)
					return false;
			}

			return true;
		}
		#endregion

		#region Type
		public static FieldDefinition Field(this TypeDefinition typeDefinition, string name)
		{
			return typeDefinition.Fields.Single(x => x.Name == name);
		}

		public static MethodDefinition Method(this TypeDefinition type, string name)
		{
			return type.Methods.Single(x => x.Name == name);
		}

		public static void ForEachMethod(this TypeDefinition type, Action<MethodDefinition> callback)
		{
			if (type.HasMethods)
			{
				foreach (var method in type.Methods)
				{
					callback.Invoke(method);
				}
			}
		}

		public static Type AsType(this TypeReference type)
		{
			var typeName = new StringBuilder();

			typeName.Append(type.Name);

			TypeReference node = type.DeclaringType;
			while (node != null)
			{
				typeName.Insert(0, node.Name + '\\');
				node = type.DeclaringType;
			}

			typeName.Insert(0, type.Namespace + '.');

			return System.Type.GetType(typeName.ToString());
		}

		/// <summary>
		/// Ensures all members of the type are publicly accessible
		/// </summary>
		/// <param name="type">The type to be made accessible</param>
		public static void MakePublic(this TypeDefinition type)
		{
			var state = type.IsPublic;
			if (type.IsNestedFamily)
			{
				type.IsNestedFamily = false;
				type.IsNestedPublic = true;
				state = false;
			}
			if (type.IsNestedFamilyAndAssembly)
			{
				type.IsNestedFamilyAndAssembly = false;
				type.IsNestedPublic = true;
				state = false;
			}
			if (type.IsNestedFamilyOrAssembly)
			{
				type.IsNestedFamilyOrAssembly = false;
				type.IsNestedPublic = true;
				state = false;
			}
			if (type.IsNestedPrivate)
			{
				type.IsNestedPrivate = false;
				type.IsNestedPublic = true;
				state = false;
			}

			type.IsPublic = state;

			foreach (var itm in type.Methods)
			{
				itm.IsPublic = true;
				if (itm.IsFamily) itm.IsFamily = false;
				if (itm.IsFamilyAndAssembly) itm.IsFamilyAndAssembly = false;
				if (itm.IsFamilyOrAssembly) itm.IsFamilyOrAssembly = false;
				if (itm.IsPrivate) itm.IsPrivate = false;
			}
			foreach (var itm in type.Fields)
			{
				if (itm.IsFamily) itm.IsFamily = false;
				if (itm.IsFamilyAndAssembly) itm.IsFamilyAndAssembly = false;
				if (itm.IsFamilyOrAssembly) itm.IsFamilyOrAssembly = false;
				if (itm.IsPrivate)
				{
					if (type.Events.Where(x => x.Name == itm.Name).Count() == 0)
						itm.IsPrivate = false;
					else
					{
						continue;
					}
				}

				itm.IsPublic = true;
			}
			foreach (var itm in type.Properties)
			{
				if (null != itm.GetMethod)
				{
					itm.GetMethod.IsPublic = true;
					if (itm.GetMethod.IsFamily) itm.GetMethod.IsFamily = false;
					if (itm.GetMethod.IsFamilyAndAssembly) itm.GetMethod.IsFamilyAndAssembly = false;
					if (itm.GetMethod.IsFamilyOrAssembly) itm.GetMethod.IsFamilyOrAssembly = false;
					if (itm.GetMethod.IsPrivate) itm.GetMethod.IsPrivate = false;
				}
				if (null != itm.SetMethod)
				{
					itm.SetMethod.IsPublic = true;
					if (itm.SetMethod.IsFamily) itm.SetMethod.IsFamily = false;
					if (itm.SetMethod.IsFamilyAndAssembly) itm.SetMethod.IsFamilyAndAssembly = false;
					if (itm.SetMethod.IsFamilyOrAssembly) itm.SetMethod.IsFamilyOrAssembly = false;
					if (itm.SetMethod.IsPrivate) itm.SetMethod.IsPrivate = false;
				}
			}

			foreach (var nt in type.NestedTypes)
				nt.MakePublic();
		}

		public static void MakeVirtual(this TypeDefinition type)
		{
			var methods = type.Methods.Where(m => !m.IsConstructor && !m.IsStatic).ToArray();
			foreach (var method in methods)
			{
				method.IsVirtual = true;
				method.IsNewSlot = true;
			}

			type.Module.ForEachInstruction((method, instruction) =>
			{
				if (methods.Any(x => x == instruction.Operand))
				{
					if (instruction.OpCode != OpCodes.Callvirt)
					{
						instruction.OpCode = OpCodes.Callvirt;
					}
				}
			});
		}

		public static bool SignatureMatches(this TypeDefinition type, TypeDefinition compareTo)
		{
			var typeInstanceMethods = type.Methods.Where(m => !m.IsStatic && !m.IsGetter && !m.IsSetter);
			var compareToInstanceMethods = compareTo.Methods.Where(m => !m.IsStatic && !m.IsGetter && !m.IsSetter && (type.IsInterface && !m.IsConstructor));

			var missing = compareToInstanceMethods.Where(m => !typeInstanceMethods.Any(m2 => m2.Name == m.Name));

			if (typeInstanceMethods.Count() != compareToInstanceMethods.Count())
				return false;

			for (var x = 0; x < typeInstanceMethods.Count(); x++)
			{
				var typeMethod = typeInstanceMethods.ElementAt(x);
				var compareToMethod = compareToInstanceMethods.ElementAt(x);

				if (!typeMethod.SignatureMatches(compareToMethod))
					return false;
			}

			return true;
		}

		public static void ForEachNestedType(this TypeDefinition parent, Action<TypeDefinition> callback)
		{
			foreach (var type in parent.NestedTypes)
			{
				callback(type);

				type.ForEachNestedType(callback);
			}
		}
		#endregion

		#region ILProcesor
		/// <summary>
		/// Inserts a group of instructions after the target instruction
		/// </summary>
		public static void InsertAfter(this Mono.Cecil.Cil.ILProcessor processor, Instruction target, IEnumerable<Instruction> instructions)
		{
			foreach (var instruction in instructions.Reverse())
			{
				processor.InsertAfter(target, instruction);
			}
		}

		public static IEnumerable<Instruction> ParseAnonymousInstruction(params object[] anonymous)
		{
			foreach (var anon in anonymous)
			{
				var expandable = anon as IEnumerable<object>;
				var resolver = anon as Func<IEnumerable<object>>;

				if (resolver != null)
				{
					expandable = resolver();
				}

				if (expandable != null)
				{
					foreach (var item in expandable)
					{
						foreach (var sub in ParseAnonymousInstruction(item))
						{
							yield return sub;
						}
					}
				}
				else yield return InternalAnonymousToInstruction(anon);
			}
		}

		/// <summary>
		/// Converts a anonymous type into an Instruction
		/// </summary>
		/// <param name="anon"></param>
		/// <returns></returns>
		private static Instruction InternalAnonymousToInstruction(object anon)
		{
			var reference = anon as InstructionReference;
			if (reference != null)
			{
				return reference.Reference;
			}

			var annonType = anon.GetType();
			var properties = annonType.GetProperties();

			//An instruction consists of only 1 opcode, or 1 opcode and 1 operation
			if (properties.Length == 0 || properties.Length > 2)
				throw new NotSupportedException("Anonymous instruction expected 1 or 2 properties");

			//Determine the property that contains the OpCode property
			var propOpcode = properties.SingleOrDefault(x => x.PropertyType == typeof(OpCode));
			if (propOpcode == null)
				throw new NotSupportedException("Anonymous instruction expected 1 opcode property");

			//Get the opcode value
			var opcode = (OpCode)propOpcode.GetMethod.Invoke(anon, null);

			//Now determine if we need an operand or not
			Instruction ins = null;
			if (properties.Length == 2)
			{
				//We know we already have the opcode determined, so the second property
				//must be the operand.
				var propOperand = properties.Where(x => x != propOpcode).Single();

				var operand = propOperand.GetMethod.Invoke(anon, null);
				var operandType = propOperand.PropertyType;
				reference = operand as InstructionReference;
				if (reference != null)
				{
					operand = reference.Reference;
					operandType = reference.Reference.GetType();
				}

				//Now find the Instruction.Create method that takes the same type that is 
				//specified by the operands type.
				//E.g. Instruction.Create(OpCode, FieldReference)
				var instructionMethod = typeof(Instruction).GetMethods()
					.Where(x => x.Name == "Create")
					.Select(x => new { Method = x, Parameters = x.GetParameters() })
					//.Where(x => x.Parameters.Length == 2 && x.Parameters[1].ParameterType == propOperand.PropertyType)
					.Where(x => x.Parameters.Length == 2 && x.Parameters[1].ParameterType.IsAssignableFrom(operandType))
					.SingleOrDefault();

				if (instructionMethod == null)
					throw new NotSupportedException($"Instruction.Create does not support type {operandType.FullName}");

				//Get the operand value and pass it to the Instruction.Create method to create
				//the instruction.
				ins = (Instruction)instructionMethod.Method.Invoke(anon, new[] { opcode, operand });
			}
			else
			{
				//No operand required
				ins = Instruction.Create(opcode);
			}

			return ins;
		}
		
		/// <summary>
		/// Inserts a list of anonymous instructions after the target instruction
		/// </summary>
		public static List<Instruction> InsertAfter(this Mono.Cecil.Cil.ILProcessor processor, Instruction target, params object[] instructions)
		{
			var created = new List<Instruction>();
			foreach (var anon in instructions.Reverse())
			{
				var ins = ParseAnonymousInstruction(anon);
				processor.InsertAfter(target, ins);

				created.AddRange(ins);
			}

			return created;
		}

		/// <summary>
		/// Inserts a list of anonymous instructions before the target instruction
		/// </summary>
		public static List<Instruction> InsertBefore(this Mono.Cecil.Cil.ILProcessor processor, Instruction target, params object[] instructions)
		{
			var created = new List<Instruction>();
			foreach (var anon in instructions)
			{
				var ins = ParseAnonymousInstruction(anon);
				processor.InsertBefore(target, ins);

				created.AddRange(ins);
			}

			return created;
		}
		#endregion

		#region Instructions
		public static Instruction Previous(this Instruction initial, Func<Instruction, Boolean> predicate)
		{
			while (initial.Previous != null)
			{
				if (predicate(initial)) return initial;
				initial = initial.Previous;
			}

			return null;
		}

		public static Instruction Next(this Instruction initial, Func<Instruction, Boolean> predicate)
		{
			while (initial.Next != null)
			{
				if (predicate(initial.Next)) return initial.Next;
				initial = initial.Next;
			}

			return null;
		}

		public static Instruction Previous(this Instruction initial, int count)
		{
			while (count > 0)
			{
				initial = initial.Previous;
				count--;
			}

			return initial;
		}

		public static List<Instruction> Next(this Instruction initial, int count = -1)
		{
			var instructions = new List<Instruction>();
			while (initial.Previous != null && (count == -1 || count > 0))
			{
				initial = initial.Previous;
				count--;

				instructions.Add(initial);
			}

			return instructions;
		}

		/// <summary>
		/// Replaces instruction references (ie if, try) to a new instruction target.
		/// This is useful if you are injecting new code before a section of code that is already
		/// the receiver of a try/if block.
		/// </summary>
		/// <param name="current">The original instruction</param>
		/// <param name="newTarget">The new instruction that will receive the transfer</param>
		/// <param name="originalMethod">The original method that is used to search for transfers</param>
		public static void ReplaceTransfer(this Instruction current, Instruction newTarget, MethodDefinition originalMethod)
		{
			//If a method has a body then check the instruction targets & exceptions
			if (originalMethod.HasBody)
			{
				//Replaces instruction references from the old instruction to the new instruction
				foreach (var ins in originalMethod.Body.Instructions.Where(x => x.Operand == current))
					ins.Operand = newTarget;

				//If there are exception handlers, it's possible that they will also need to be switched over
				if (originalMethod.Body.HasExceptionHandlers)
				{
					foreach (var handler in originalMethod.Body.ExceptionHandlers)
					{
						if (handler.FilterStart == current) handler.FilterStart = newTarget;
						if (handler.HandlerEnd == current) handler.HandlerEnd = newTarget;
						if (handler.HandlerStart == current) handler.HandlerStart = newTarget;
						if (handler.TryEnd == current) handler.TryEnd = newTarget;
						if (handler.TryStart == current) handler.TryStart = newTarget;
					}
				}

				//Update the new target to take the old targets place
				newTarget.Offset = current.Offset;
				newTarget.SequencePoint = current.SequencePoint;
				newTarget.Offset++; //TODO: spend some time to figure out why this is incrementing
			}
		}
		#endregion
	}
}
