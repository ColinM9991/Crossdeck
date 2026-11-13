namespace Crossdeck.Devices.Moza;

internal static class MozaIds
{
    private const int VendorId = 0x346E;
    private const int Ma3FProductId = 0x1305;
    private const int Mb7FProductId = 0x1306;

    private static readonly int[] FcdDisplayProductIds = [Ma3FProductId, Mb7FProductId];

    public static bool IsFcdDisplay(int vendorId, int productId) => vendorId == VendorId && FcdDisplayProductIds.Contains(productId);
}