using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookwormFunctions.PorterLib
{
	public class Porter : IAsyncDisposable
	{
		private static bool _isNewEnvironment = true;
		private const string LEMMAS = "lemmas";
		private const string PARAGRAPHS_FORMAT = "paragraphs-{0}";

		private const string WARMUP_MESSAGE_ID = "warmup-message";
		private const string WARMUP_MESSAGE_LABEL = "lemma";
		private const string WARMUP_MESSAGE_BODY = "warmup message";

		private readonly QueuesHandler queuesHandler;
		private readonly ServiceBusSender topicsSender;
		private readonly ServiceBusMessage warmUpMessage = new(WARMUP_MESSAGE_BODY) { MessageId = WARMUP_MESSAGE_ID, Subject = WARMUP_MESSAGE_LABEL };
		private ServiceBusSender? paragraphsSender;
		private readonly TextPreprocessor textPreprocessor;

		public Porter(QueuesHandler queuesHandler, TextPreprocessor textPreprocessor)
		{
			this.queuesHandler = queuesHandler;
			topicsSender = queuesHandler.TopicsClient.CreateSender(LEMMAS);
			this.textPreprocessor = textPreprocessor;
		}

		public async Task Run<T>(T source, string corellationId)
		{
			var warmUpTopicsTask = _isNewEnvironment ? WarmUpTopics() : Task.CompletedTask;
			var createSubscriptionsTask = queuesHandler.CreateTopicsSubscriptions(corellationId);

			var textForAnalysis = await textPreprocessor.FetchTextForAnalysis(source);
			paragraphsSender = await CreateParagraphsSender(string.Format(PARAGRAPHS_FORMAT, textForAnalysis.FromLang!)).ConfigureAwait(false);
			var warmUpParagraphsTask = _isNewEnvironment ? WarmUpParagraphs() : Task.CompletedTask;

			_isNewEnvironment = false;

			await SendMessages(TextPreprocessor.CreateMessages(textForAnalysis, corellationId)).ConfigureAwait(false);
			await createSubscriptionsTask.ConfigureAwait(false);
			await warmUpTopicsTask.ConfigureAwait(false);
			await warmUpParagraphsTask.ConfigureAwait(false);
		}

		private Task WarmUpParagraphs() => paragraphsSender!.SendMessageAsync(warmUpMessage);

		private Task WarmUpTopics() => topicsSender.SendMessageAsync(warmUpMessage);

		private Task SendMessages(IEnumerable<ServiceBusMessage> messages) =>
			paragraphsSender!.SendMessagesAsync(messages);

		private async Task<ServiceBusSender> CreateParagraphsSender(string paragraphsQueueName) =>
			await queuesHandler.QueuesAdministrationClient.QueueExistsAsync(paragraphsQueueName).ConfigureAwait(false)
			? queuesHandler.QueuesClient.CreateSender(paragraphsQueueName)
			: throw new ApplicationException($"Queue '{paragraphsQueueName}' doesn't exist.");

		public async ValueTask DisposeAsync()
		{
			if (paragraphsSender != null)
			{
				await paragraphsSender.DisposeAsync();
			}
			await topicsSender.DisposeAsync();
			await queuesHandler.DisposeAsync();
		}
	}
}
