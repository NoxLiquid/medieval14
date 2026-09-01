namespace Content.Server._Corvax.TTS;

public sealed partial class TTSSystem
{
    // Respiral: собираем SSML с per-voice pitch/rate. Шёпот принудительно понижает тон.
    private string ToSsmlText(string text, string pitch, string rate, bool isWhisper)
    {
        var p = isWhisper ? "x-low" : pitch;                         // шёпот — низкий тон поверх голоса
        var r = isWhisper ? rate : (string.IsNullOrEmpty(rate) ? "fast" : rate); // обычная речь — быстрее по умолчанию

        var result = text;
        if (!string.IsNullOrEmpty(r))
            result = $"<prosody rate=\"{r}\">{result}</prosody>";
        if (!string.IsNullOrEmpty(p))
            result = $"<prosody pitch=\"{p}\">{result}</prosody>";
        return $"<speak>{result}</speak>";
    }
}
