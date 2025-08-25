using Microsoft.EntityFrameworkCore;
using NeoSmart.Unicode;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule;

internal static class Utils
{
    private const string _commandPrefixes = "!$?@#%^&`~><¡¿*-+_=;:'\"\\|/,.？！[]{}()";

    private static readonly List<Codepoint> _emojiSpecialCharacters =
        [Emoji.ZeroWidthJoiner, Emoji.ObjectReplacementCharacter, Emoji.Keycap, Emoji.VariationSelector];

    internal static bool ContainsPrefix(ReadOnlySpan<Codepoint> input, out ReadOnlySpan<Codepoint> prefixTrimmedInput)
    {
        int count = 0;
        foreach (Codepoint codepoint in input)
        {
            if (Emoji.IsEmoji(codepoint.AsString()) ||
                Emoji.SkinTones.All.Any(x => x == codepoint) ||
                _emojiSpecialCharacters.Any(x => x == codepoint) ||
                _commandPrefixes.Any(x => x == codepoint)
               )
            {
                count++;
                continue;
            }

            if (codepoint.Equals(' ')) count++;
            break;
        }

        if (count == 0)
        {
            prefixTrimmedInput = input;
            return false;
        }

        prefixTrimmedInput = input[count..];
        return true;
    }

    internal static string Filter(string input)
    {
        string result = input;
        foreach (PermittedTerm term in BotCore.DBContext.PermittedTerms.AsNoTracking().ToList())
        {
            string tt = term.Term;
            if (!term.Noreplace) tt = tt.Replace("_", " ");
            result = result.Replace(tt, term.Alternative,
                term.Anycase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        return result;
    }

    internal static class Responses
    {
        internal static CommandResult Fail => new("NOIDONTTHINKSO ", false);
        internal static CommandResult Ok => new("ok ", false);
        internal static CommandResult Surprise => new("oh ", false);
    }
}
