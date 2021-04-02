using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BookwormFunctions.PorterLib
{
	public class QueuesHandler: IAsyncDisposable
	{
		public QueuesHandler(string queuesConnectionString, string topicsConnectionString, string? queuesAdministrationConnectionString = null, string? topicsAdministrationConnectionString = null)
		{
			QueuesClient = new (queuesConnectionString);
			QueuesAdministrationClient = new (queuesAdministrationConnectionString ?? queuesConnectionString);
			TopicsClient = new (topicsConnectionString);
			TopicsAdministrationClient = new(topicsAdministrationConnectionString ?? topicsConnectionString);

		}
		public ServiceBusClient QueuesClient { get; }
		public ServiceBusClient TopicsClient { get; }
		public ServiceBusAdministrationClient QueuesAdministrationClient { get; }
		public ServiceBusAdministrationClient TopicsAdministrationClient { get; }

		public async ValueTask DisposeAsync()
		{
			await QueuesClient.DisposeAsync();
			await TopicsClient.DisposeAsync();
		}

		public async Task CreateTopicsSubscriptions(string correlationId)
		{
			var lemmasSubscription = CreateLemmasSubscription(correlationId);
			var dictionaryArticlesSubscription = CreateDictionaryArticlesSubscription(correlationId);
			await Task.WhenAll(lemmasSubscription, dictionaryArticlesSubscription).ConfigureAwait(false);

			return;
		}

		public Task<Azure.Response<SubscriptionProperties>> CreateDictionaryArticlesSubscription(string correlationId)
		{
			var falseRuleOptions = new CreateRuleOptions("False", new FalseRuleFilter());
			var dictionaryArticlesSubscriptionOptions = new CreateSubscriptionOptions("dictionary-articles", correlationId)
			{
				// TODO: probably it should be done in some other way
				AutoDeleteOnIdle = TimeSpan.FromMinutes(10)
			};
			return TopicsAdministrationClient.CreateSubscriptionAsync(dictionaryArticlesSubscriptionOptions, falseRuleOptions);
		}

		public Task<Azure.Response<SubscriptionProperties>> CreateLemmasSubscription(string correlationId)
		{
			var lemmasSubscriptionOptions = new CreateSubscriptionOptions("lemmas", correlationId)
			{
				// TODO: probably it should be done in some other way
				AutoDeleteOnIdle = TimeSpan.FromMinutes(10)
			};
			var correlationIdRuleOptions = new CreateRuleOptions("CorrelationIdRule", new CorrelationRuleFilter(correlationId));
			return TopicsAdministrationClient.CreateSubscriptionAsync(lemmasSubscriptionOptions, correlationIdRuleOptions);
		}
	}
}
