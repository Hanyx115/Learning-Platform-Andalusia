using System.Text;
using System.Text.RegularExpressions;

namespace AndalusiaAcademy.Application;

public static partial class SlugGenerator
{
    public static string Generate(string value)
    {
        var builder = new StringBuilder(value.Trim().ToLowerInvariant());
        for (var i = 0; i < builder.Length; i++)
        {
            if (!char.IsLetterOrDigit(builder[i]))
            {
                builder[i] = '-';
            }
        }

        return CollapseHyphensRegex().Replace(builder.ToString(), "-").Trim('-');
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseHyphensRegex();
}
