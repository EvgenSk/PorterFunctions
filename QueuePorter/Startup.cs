using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(BookwormFunctions.Queue.Startup))]

namespace BookwormFunctions.Queue
{
	public class Startup : PorterLib.PorterStartup
	{
	}
}
