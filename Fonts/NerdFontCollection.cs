using Avalonia.Media.Fonts;
using neisep.NerdFonts;

namespace GitOverview.Fonts;

/// <summary>Registers the Symbols Nerd Font from neisep.NerdFonts as <c>fonts:NerdFont</c>.</summary>
public sealed class NerdFontCollection : FontCollectionBase
{
    public NerdFontCollection()
    {
        if (!TryAddGlyphTypeface(NerdFont.OpenStream(), out _))
            throw new InvalidOperationException($"Avalonia could not load the {NerdFont.FamilyName} font.");
    }

    public override Uri Key { get; } = new("fonts:NerdFont");
}
