using System.Collections.Generic;

namespace Mod.Framework
{
	public class QueryResult : List<TypeMeta>
	{
		public Query Query { get; internal set; }
	}
}
