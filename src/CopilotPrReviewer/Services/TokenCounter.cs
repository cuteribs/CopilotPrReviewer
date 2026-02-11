using SharpToken;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class TokenCounter
{
    private readonly GptEncoding _encoding;

    public TokenCounter()
    {
        _encoding = GptEncoding.GetEncoding("cl100k_base");
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return _encoding.Encode(text).Count;
    }
}
