namespace Ocelot.Configuration.File
{
    internal static class FileSecurityOptionsExtensions
    {
        internal static bool IsFullFilled(this FileSecurityOptions fileSecurityOptions) 
            => fileSecurityOptions.IPAllowedList.Count > 0 || fileSecurityOptions.IPBlockedList.Count > 0;
    }
}
