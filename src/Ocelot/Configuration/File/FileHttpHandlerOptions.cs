namespace Ocelot.Configuration.File
{
    public class FileHttpHandlerOptions
    {
        public FileHttpHandlerOptions()
        {
            AllowAutoRedirect = true;
            UseCookieContainer = true;
        }

        public bool AllowAutoRedirect { get; set; }

        public bool UseCookieContainer { get; set; }
    }
}
