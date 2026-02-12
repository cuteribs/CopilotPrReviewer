using Cuteribs.CopilotPrReviewer.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class FindingParser
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly ILogger _logger;

	public FindingParser(ILogger logger)
	{
		_logger = logger;
	}

	public List<ReviewFinding> Parse(string response)
	{
		if (string.IsNullOrWhiteSpace(response))
			return [];

		// Extract JSON from the response - it may be wrapped in markdown code blocks
		var json = ExtractJson(response);

		if (string.IsNullOrWhiteSpace(json))
			return [];

		try
		{
			return JsonSerializer.Deserialize<List<ReviewFinding>>(json, JsonOptions) ?? [];
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "Failed to parse JSON");
			_logger.LogWarning(response);
			_logger.LogWarning("=======================");
			_logger.LogWarning(json);
			return [];
		}
	}

	internal static string? ExtractJson(string text)
	{
		// Try to find JSON array in markdown code block first
		var codeBlockStart = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
		if (codeBlockStart >= 0)
		{
			var jsonStart = text.IndexOf('[', codeBlockStart);
			if (jsonStart >= 0)
			{
				var jsonEnd = text.LastIndexOf(']');
				if (jsonEnd > jsonStart)
					return text[jsonStart..(jsonEnd + 1)];
			}
		}

		// Try to find a bare code block
		codeBlockStart = text.IndexOf("```", StringComparison.Ordinal);
		if (codeBlockStart >= 0)
		{
			var jsonStart = text.IndexOf('[', codeBlockStart);
			if (jsonStart >= 0)
			{
				var jsonEnd = text.LastIndexOf(']');
				if (jsonEnd > jsonStart)
					return text[jsonStart..(jsonEnd + 1)];
			}
		}

		// Try to find raw JSON array
		var rawStart = text.IndexOf('[');
		if (rawStart >= 0)
		{
			var rawEnd = text.LastIndexOf(']');
			if (rawEnd > rawStart)
				return text[rawStart..(rawEnd + 1)];
		}

		return null;
	}
}
