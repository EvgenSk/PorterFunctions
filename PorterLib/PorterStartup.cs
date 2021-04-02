using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace BookwormFunctions.PorterLib
{
	public class PorterStartup : FunctionsStartup
	{
		private const string TEXT_QUEUES_CONNECTION = "TextQueuesConnection";
		private const string WORD_TOPICS_CONNECTION = "WordTopicsConnection";
		private const string DETECT_LANGUAGE_CONNECTION = "DetectLanguageConnection";
		public override void Configure(IFunctionsHostBuilder builder)
		{
			var config = builder.GetContext().Configuration;
			var textQueuesConnectionString = config.GetConnectionString(TEXT_QUEUES_CONNECTION);
			var wordTopicsConnectionString = config.GetConnectionString(WORD_TOPICS_CONNECTION);
			var languageDetectorConnectionString = config.GetConnectionString(DETECT_LANGUAGE_CONNECTION);
			var porter = new Porter(new(textQueuesConnectionString, wordTopicsConnectionString), new(new(languageDetectorConnectionString)));
			builder.Services.AddSingleton(porter);
		}

		public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
		{
			var context = builder.GetContext();
			builder.ConfigurationBuilder
				.AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
				.AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
				.AddEnvironmentVariables();
		}
	}
}
