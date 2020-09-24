namespace ClassicUO.Game.Scripting
{
    public static class Aliases
    {
        public static void Register()
        {
            Interpreter.RegisterAliasHandler("ground", Ground);
        }

        private static uint Ground(string alias)
        {
            return 0;
        }
    }
}