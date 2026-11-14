using AutoFixture;
using HidSharp.Reports;

namespace Crossdeck.Testing;

/// <summary>
/// Resolves <see cref="ReportDescriptor"/> from a fixed, valid descriptor so AutoFixture never feeds
/// random bytes to the HidSharp parser.
/// </summary>
public sealed class ReportDescriptorCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register(() => new ReportDescriptor(TestReportDescriptor.Fcd));
    }
}
