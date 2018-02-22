using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ocelot.Configuration;
using Ocelot.Configuration.Repository;

namespace ApiGateway.Web.Pages
{
    public class IndexModel : PageModel
    {
        public IOcelotConfiguration OcelotConfiguration { get; set; }

        public IndexModel(IOcelotConfigurationRepository repository)
        {
            GetOcelotConfigurationAsync(repository).Wait();
        }

        private async Task GetOcelotConfigurationAsync(IOcelotConfigurationRepository repository)
        {
            var resposne = await repository?.Get();
            OcelotConfiguration = resposne?.Data;
        }
        public void OnGet()
        {

        }
    }
}
