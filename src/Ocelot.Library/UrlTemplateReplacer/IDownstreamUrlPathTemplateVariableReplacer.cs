namespace Ocelot.Library.UrlTemplateReplacer
{
    using DownstreamRouteFinder;
    using Responses;

    public interface IDownstreamUrlTemplateVariableReplacer
    {
        Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute);   
    }
}