using System.Resources;

namespace D3B.Bis.App.Tests.Pages;

/// <summary>The texts of <see cref="TestPage"/> in English and French, as a module's own resources hold them.</summary>
internal static class TestPageTexts
{
    public static ResourceManager Resources { get; } = new("D3B.Bis.App.Tests.Pages.TestPageTexts", typeof(TestPageTexts).Assembly);
}
