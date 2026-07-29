using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace EvilBrains.ApiClient.Generator;

internal sealed record LocationModel(string FilePath, int SpanStart, int SpanLength, int StartLine, int StartCharacter, int EndLine, int EndCharacter)
{
    public static LocationModel FromNode(SyntaxNode node) => FromLocation(node.GetLocation());

    public static LocationModel FromToken(in SyntaxToken token) => FromLocation(token.GetLocation());

    private static LocationModel FromLocation(Location location)
    {
        var lineSpan = location.GetLineSpan();

        return new(
            lineSpan.Path,
            location.SourceSpan.Start,
            location.SourceSpan.Length,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character);
    }

    public Location ToLocation() =>
        Location.Create(
            this.FilePath,
            new TextSpan(this.SpanStart, this.SpanLength),
            new LinePositionSpan(new LinePosition(this.StartLine, this.StartCharacter), new LinePosition(this.EndLine, this.EndCharacter)));
}
