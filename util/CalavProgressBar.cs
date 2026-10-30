using Spectre.Console;
using Spectre.Console.Rendering;
namespace CalavHashScanner.CustomSpectre;
public sealed class CalavProgressColumn : ProgressColumn
{
    public override IRenderable Render(
        RenderOptions options,
        ProgressTask task,
        TimeSpan deltaTime)
    {
        double pct = task.Percentage / 100.0;

        const int width = 30;
        int filled = (int)(pct * width);

        string bar;

        if (filled == 0)
        {
            bar = ">" + new string('.', width - 1);
        }
        else if (filled >= width)
        {
            bar = new string('=', width);
        }
        else
        {
            bar =
                new string('=', filled) +
                ">" +
                new string('.', width - filled - 1);
        }

        return new Markup(
            $"([green]{bar}[/])");
    }
}