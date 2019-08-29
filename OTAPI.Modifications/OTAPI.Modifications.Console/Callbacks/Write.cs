namespace OTAPI.Callbacks.Terraria
{
    internal partial class Console
    {
        internal static void Write(object value)
        {
            if (Hooks.Console.Write?.Invoke(new ConsoleHookArgs()
            {
                Format = null,
                Arg1 = value,
                Arg2 = null
            }) == HookResult.Cancel)
                return;

            System.Console.Write(value);
        }
        
        internal static void Write(string format, object arg0, object arg1)
        {
            if (Hooks.Console.Write?.Invoke(new ConsoleHookArgs()
            {
                Format = format,
                Arg1 = arg0,
                Arg2 = arg1
            }) == HookResult.Cancel)
                return;

            System.Console.Write(format, arg0, arg1);
        }
    }
}
