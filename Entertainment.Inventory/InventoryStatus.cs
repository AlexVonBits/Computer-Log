namespace Entertainment.Inventory;

public static class InventoryStatus
{
	public const string InStock = "In Stock";

	public const string Display = "Display";

	public const string InClaims = "In Claims";

	public const string ClaimedOut = "Claimed Out";

	public const string Sold = "Sold";

	public const string Missing = "Missing";

	public const string Deleted = "Deleted";

	public const string Clearance = "Clearance";

	public static readonly string[] OnHandStatuses = new string[3] { "In Stock", "Display", "In Claims" };

	public static readonly string[] AllStatuses = new string[8] { "In Stock", "Display", "In Claims", "Claimed Out", "Sold", "Missing", "Deleted", "Clearance" };

	public const string UPCNormal = "Normal";

	public const string UPCClearance = "Clearance";

	public const string UPCDeleted = "Deleted";

	public static readonly string[] UPCStatuses = new string[3] { "Normal", "Clearance", "Deleted" };
}
