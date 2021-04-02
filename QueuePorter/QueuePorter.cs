using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BookwormFunctions.PorterLib;

namespace BookwormFunctions.Queue
{
	public class QueuePorter : IAsyncDisposable
	{
		private const string TEXT_QUEUES_CONNECTION = "TextQueuesConnection";

		private const string TEXTS = "texts";
		protected Porter _porter;

		public QueuePorter(Porter porter)
		{
			_porter = porter;
		}

		[FunctionName("QueuePorter")]
		public async Task Run(
			[ServiceBusTrigger(TEXTS, Connection = TEXT_QUEUES_CONNECTION)] Message message,
			ILogger log)
		{
			log.LogInformation($"C# ServiceBus queue trigger function processed message: {message}");
			try
			{
				await _porter.Run(message).ConfigureAwait(false);
			}
			catch (Azure.Messaging.ServiceBus.ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
			{
				log.LogWarning(ex, ex.Message);
			}
			catch (Exception ex)
			{
				log.LogError(ex, ex.Message);
			}

		}

		public ValueTask DisposeAsync() =>
			_porter.DisposeAsync();
	}
}
