namespace Crossdeck.Devices.Moza;

internal static class MozaIds
{
    public const int VendorId = 0x346E;
    public const int Ma3FProductId = 0x1305;
    public const int Mb7FProductId = 0x1306;

    private static readonly int[] FcdDisplayProductIds = [Ma3FProductId, Mb7FProductId];

    public static bool IsFcdDisplay(int vendorId, int productId)
    {
        return vendorId == VendorId && FcdDisplayProductIds.Contains(productId);
    }
}