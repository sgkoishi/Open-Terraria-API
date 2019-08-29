namespace OTAPI
{
    public static partial class Hooks
    {
        public static partial class Console
        {
            #region Handlers
            public delegate HookResult WriteHandler<ConsoleHookArgs>(ConsoleHookArgs value);
            public delegate HookResult WriteArgsHandler(object format, object arg0, object arg1);
            #endregion

            /// <summary>
            /// Occurs each time vanilla calls Console.WriteLine
            /// </summary>
            public static WriteHandler<ConsoleHookArgs> Write;
        }
    }
}
