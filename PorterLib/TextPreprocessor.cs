using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookwormFunctions.PorterLib
{
	public class TextPreprocessor
	{
		private const string FROM_LANG = "fromLang";
		private const string TO_LANG = "toLang";

		private readonly LanguageDetector languageDetector;

		public TextPreprocessor(LanguageDetector languageDetector)
		{
			this.languageDetector = languageDetector;
		}
		public async Task<TextForAnalysis> FetchTextForAnalysis(Message message)
		{
			var fullText = System.Text.Encoding.UTF8.GetString(message.Body);
			var (detected, fromLang) =
				message.UserProperties.TryGetValue(FROM_LANG, out var fromLangObj)
				? (true, fromLangObj.ToString())
				: await languageDetector.TryDetectLanguageAsync(fullText).ConfigureAwait(false);

			if (!detected)
				throw new Exception("Couldn't detect a language");

			var toLang =
				message.UserProperties.TryGetValue(TO_LANG, out var toLangObj)
				? toLangObj.ToString()
				: fromLang;

			return new()
			{
				Text = fullText,
				FromLang = fromLang,
				ToLang = toLang
			};
		}

		public static IEnumerable<ServiceBusMessage> CreateMessages(TextForAnalysis textForAnalysis, string corellationId, IDictionary<string, object>? properties = null) =>
			textForAnalysis.Text.Split('\n')
			.Where(m => !string.IsNullOrWhiteSpace(m))
			.Zip(GetIndices(1), (p, i)
				=> CreateNewMessage(p, i, corellationId, textForAnalysis.FromLang!, textForAnalysis.ToLang!, properties));

		public static ServiceBusMessage CreateNewMessage(string paragraph, long paragraphIndex, string correlationId, string fromLang, string toLang, IDictionary<string, object>? userProperties = null)
		{
			var message = new ServiceBusMessage
			{
				Body = new BinaryData(System.Text.Encoding.UTF8.GetBytes(paragraph!)),
				CorrelationId = correlationId!
			};
			if (userProperties is not null)
			{
				foreach (var property in userProperties)
				{
					message.ApplicationProperties.Add(property.Key, property.Value);
				}
			}
			message.ApplicationProperties.Add("ParagraphNumber", paragraphIndex);
			if (!message.ApplicationProperties.ContainsKey(FROM_LANG))
			{
				message.ApplicationProperties.Add(FROM_LANG, fromLang!);
			}
			if (!message.ApplicationProperties.ContainsKey(TO_LANG))
			{
				message.ApplicationProperties.Add(TO_LANG, toLang!);
			}
			return message;
		}

		static IEnumerable<int> GetIndices(int startIndex = 0)
		{
			while (true)
				yield return startIndex++;
		}

	}
}
