using System;

namespace Mod.Framework
{
	[Flags]
	public enum HookFlags : byte
	{
		Default = Pre | Post | Cancellable | PreReferenceParameters | AlterResult,

		Pre = 1,
		Post = 2,

		PreReferenceParameters = 4,
		AlterResult = 8,

		Cancellable = 16 // only applie to Pre hooks
	}
}
