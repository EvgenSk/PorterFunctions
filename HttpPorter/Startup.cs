using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(BookwormFunctions.Http.Startup))]

namespace BookwormFunctions.Http
{
	public class Startup : PorterLib.PorterStartup
	{
	}
}
