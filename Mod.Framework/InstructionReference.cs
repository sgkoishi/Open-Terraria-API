using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mod.Framework
{
	public class InstructionReference
	{
		public Instruction Reference { get; set; }

		public InstructionReference Create(OpCode opcode)
		{
			this.Reference = Instruction.Create(opcode);
			return this;
		}

		public InstructionReference Create(OpCode opcode, string operand)
		{
			this.Reference = Instruction.Create(opcode, operand);
			return this;
		}

		public InstructionReference Create(OpCode opcode, ParameterDefinition operand)
		{
			this.Reference = Instruction.Create(opcode, operand);
			return this;
		}
		public InstructionReference Create(OpCode opcode, MethodDefinition operand)
		{
			this.Reference = Instruction.Create(opcode, operand);
			return this;
		}

		public InstructionReference Create(OpCode opcode, MethodReference operand)
		{
			this.Reference = Instruction.Create(opcode, operand);
			return this;
		}

		public InstructionReference Create(OpCode opcode, VariableDefinition operand)
		{
			this.Reference = Instruction.Create(opcode, operand);
			return this;
		}
	}
}
