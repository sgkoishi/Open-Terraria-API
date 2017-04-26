namespace Mod.Framework.Emitters
{
	public interface IEmitter<TOutput>
	{
		TOutput Emit();
	}
}
