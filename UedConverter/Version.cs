namespace UedConverter
{
    public static class Version
    {
        public static string GetVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return "v" + version.Major + "." + version.Minor + "." + version.Revision;
        }
    }
}
