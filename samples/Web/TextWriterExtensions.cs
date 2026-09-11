using System.Text;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Ocelot.Samples;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class TextWriterExtensions
{
    public static TextWriter Welcome(this TextWriter tw, out int pageWidth, string welcoming = null, int indent = 3, char symbol = '#')
    {
        welcoming ??= "Welcome!";
        var sb = new StringBuilder()
            // Line 1
            .AppendJoin(string.Empty, Enumerable.Repeat(symbol, welcoming.Length + 2 * indent)).AppendLine()
            // Line 2
            .Append(symbol).AppendJoin(string.Empty, Enumerable.Repeat(" ", indent - 1))
            .Append(welcoming)
            .AppendJoin(string.Empty, Enumerable.Repeat(" ", indent - 1)).Append(symbol).AppendLine()
            // Line 3
            .AppendJoin(string.Empty, Enumerable.Repeat(symbol, welcoming.Length + 2 * indent)).AppendLine();
        tw.Write(sb);
        string[] lines = sb.ToString().Split(Environment.NewLine);
        pageWidth = lines.Max(l => l.Length);
        return tw;
    }

    public static TextWriter Print(this TextWriter tw, string message, int indent = 1, char symbol = '#', ConsoleColor? color = null, bool resetColor = false)
    {
        if (resetColor) Console.ResetColor();
        tw.Write(symbol);
        tw.Write(string.Join(string.Empty, Enumerable.Repeat(" ", indent)));
        if (color.HasValue) Console.ForegroundColor = color.Value;
        tw.WriteLine(message);
        if (resetColor) Console.ResetColor();
        return tw;
    }

    public static TextWriter PageBreak(this TextWriter tw, int indent = 3, char delimiter = '-', char symbol = '#')
    {
        tw.Write(symbol);
        tw.WriteLine(string.Join(string.Empty, Enumerable.Repeat(delimiter, indent - 1)));
        return tw;
    }
}
