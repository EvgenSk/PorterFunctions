using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookwormFunctions.PorterLib
{
	public class TextPreprocessor
	{
		private const string FROM_LANG = "fromLang";
		private const string TO_LANG = "toLang";
		private const string COULD_NOT_DETECT_LANGUAGE_MESSAGE = "Couldn't detect a language";

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
				throw new Exception(COULD_NOT_DETECT_LANGUAGE_MESSAGE);

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

		private (bool, string) TryGetFromQuery(IQueryCollection query, string key) =>
			query.TryGetValue(key, out var vals)
			? (true, vals[0])
			: (false, string.Empty);

		public async Task<TextForAnalysis> FetchTextForAnalysis(HttpRequest req)
		{
			using var streamReader = new StreamReader(req.Body);
			var bodyString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
			return req.ContentType switch
			{
				"application/json" => await FetchTextForAnalysisFromAppJson(bodyString).ConfigureAwait(false),
				"text/plain" => await FetchTextForAnalysisFromPlainText(bodyString, req.Query).ConfigureAwait(false),
				var contentType => throw new ArgumentException($"ContentType '{contentType}'")
			};
		}

		private async Task<TextForAnalysis> FetchTextForAnalysisFromAppJson(string json)
		{
			var textForAnalysys = JsonConvert.DeserializeObject<TextForAnalysis>(json);

			var (detected, fromLang) =
				!string.IsNullOrWhiteSpace(textForAnalysys?.FromLang)
				? (true, textForAnalysys.FromLang)
				: await languageDetector.TryDetectLanguageAsync(textForAnalysys!.Text).ConfigureAwait(false);

			if (!detected)
				throw new Exception(COULD_NOT_DETECT_LANGUAGE_MESSAGE);

			var toLang = !string.IsNullOrWhiteSpace(textForAnalysys?.ToLang) ? textForAnalysys?.ToLang : fromLang;

			return new()
			{
				FromLang = fromLang,
				ToLang = toLang,
				Text = textForAnalysys!.Text
			};
		}

		private async Task<TextForAnalysis> FetchTextForAnalysisFromPlainText(string text, IQueryCollection query)
		{
			var(detected, fromLang) =
				TryGetFromQuery(query, FROM_LANG) switch
				{
					(false, _) => await languageDetector.TryDetectLanguageAsync(text).ConfigureAwait(false),
					var result => result
				};

			if (!detected)
				throw new Exception(COULD_NOT_DETECT_LANGUAGE_MESSAGE);

			var toLang =
				TryGetFromQuery(query, TO_LANG) switch
				{
					(true, var l) => l,
					_ => fromLang
				};

			return new()
			{
				FromLang = fromLang,
				ToLang = toLang,
				Text = text
			};
		}

		public Task<TextForAnalysis> FetchTextForAnalysis<T>(T source) => source switch
		{
			Message message => FetchTextForAnalysis(message),
			HttpRequest httpRequest => FetchTextForAnalysis(httpRequest),
			_ => throw new ArgumentException($"unsupported source type: '{source!.GetType().FullName}'")
		};

		public static IEnumerable<ServiceBusMessage> CreateMessages(TextForAnalysis textForAnalysis, string corellationId) =>
			textForAnalysis.Text.Split('\n')
			.Where(m => !string.IsNullOrWhiteSpace(m))
			.Zip(GetIndices(1), (p, i)
				=> CreateNewMessage(p, i, corellationId, textForAnalysis.FromLang!, textForAnalysis.ToLang!));

		public static ServiceBusMessage CreateNewMessage(string paragraph, long paragraphIndex, string correlationId, string fromLang, string toLang)
		{
			var message = new ServiceBusMessage
			{
				Body = new BinaryData(System.Text.Encoding.UTF8.GetBytes(paragraph!)),
				CorrelationId = correlationId!
			};
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
