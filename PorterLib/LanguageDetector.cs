using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DetectLanguage;
using System.Linq;

namespace BookwormFunctions
{
	public class LanguageDetector
	{
		DetectLanguageClient _detectLanguageClient;

		public LanguageDetector(string connectionString)
		{
			_detectLanguageClient = new DetectLanguageClient(connectionString);
		}

		public async Task<(bool, string)> TryDetectLanguageAsync(string text)
		{
			var detectedLanguages = await _detectLanguageClient.DetectAsync(text).ConfigureAwait(false);
			var theLang = detectedLanguages.Where(l => l.reliable).OrderByDescending(l => l.confidence).FirstOrDefault();
			return theLang is not null
				? (true, theLang.language)
				: (false, string.Empty);
		}
	}
}
