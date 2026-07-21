using System.Text;

namespace Content.Shared._Respiral.Skill;

/// <summary>
///     Подмена букв в тексте бумаги в зависимости от уровня грамотности читателя (0..1).
///     Доля подменённых букв ≈ (1 - progress): чем выше грамотность, тем меньше «каракулей».
///
///     Свойства алгоритма:
///     - <b>Детерминированность:</b> результат зависит только от (текст, progress) → стабилен между
///       обновлениями UI, не «мерцает».
///     - <b>Монотонность:</b> рост грамотности только открывает новые буквы, ранее открытые не прячет.
///     - <b>Подмена, а не скрытие:</b> буква заменяется случайной буквой того же алфавита и регистра,
///       пробелы/пунктуация/цифры сохраняются → форма и длина слов остаются (выглядит как
///       «не могу прочитать», а не «пусто»).
///
///     Работает со строкой разметки (markup): участки внутри <c>[...]</c> (теги цвета/жирности и т.п.)
///     пропускаются без изменений, чтобы не сломать рич-текст.
/// </summary>
public static class LiteracyScramble
{
    public static string Apply(string markup, float progress)
    {
        if (progress >= 1f || string.IsNullOrEmpty(markup))
            return markup;

        progress = Math.Clamp(progress, 0f, 1f);

        var seed = StableSeed(markup);
        var sb = new StringBuilder(markup.Length);
        var insideTag = false;
        var letterPos = 0;

        foreach (var c in markup)
        {
            // Теги разметки оставляем как есть.
            if (c == '[')
            {
                insideTag = true;
                sb.Append(c);
                continue;
            }
            if (c == ']')
            {
                insideTag = false;
                sb.Append(c);
                continue;
            }
            if (insideTag || !char.IsLetter(c))
            {
                sb.Append(c);
                continue;
            }

            // Позиция буквы читается, если её «бросок» ниже уровня грамотности.
            var roll = Hash01(seed, letterPos);
            if (roll < progress)
                sb.Append(c);
            else
                sb.Append(Substitute(c, seed, letterPos));

            letterPos++;
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Заменяет букву случайной буквой того же алфавита и регистра. Неизвестные алфавиты — без замены.
    /// </summary>
    private static char Substitute(char c, uint seed, int pos)
    {
        var h = Mix(seed ^ (uint)(pos * 2654435761u) ^ c);

        // Кириллица а-я / А-Я (32 буквы), ё/Ё кладём в тот же диапазон.
        if (c is >= 'а' and <= 'я' or 'ё')
            return (char)('а' + h % 32);
        if (c is >= 'А' and <= 'Я' or 'Ё')
            return (char)('А' + h % 32);
        if (c is >= 'a' and <= 'z')
            return (char)('a' + h % 26);
        if (c is >= 'A' and <= 'Z')
            return (char)('A' + h % 26);

        return c;
    }

    /// <summary>FNV-1a по тексту — стабильный сид (одинаков на сервере и клиенте).</summary>
    private static uint StableSeed(string s)
    {
        var hash = 2166136261u;
        foreach (var ch in s)
        {
            hash ^= ch;
            hash *= 16777619u;
        }
        return hash;
    }

    /// <summary>Псевдослучайное [0,1) из (сид, позиция).</summary>
    private static float Hash01(uint seed, int pos)
    {
        var h = Mix(seed ^ (uint)(pos * 2654435761u));
        return (h & 0xFFFFFF) / (float)0x1000000; // 24 бита → [0,1)
    }

    private static uint Mix(uint h)
    {
        h ^= h >> 16;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h;
    }
}
