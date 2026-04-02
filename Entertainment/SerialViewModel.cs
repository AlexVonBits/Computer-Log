using Entertainment.Inventory;

namespace Entertainment;

public partial class SerialViewModel
{
	public string ItemId { get; }

	public string SerialNumber { get; }

	public string Status { get; }

	public string[] StatusOptions { get; }

	public string SelectedStatus { get; set; }

	public SerialViewModel(InventoryItem item)
	{
		ItemId = item.Id;
		SerialNumber = item.SerialNumber;
		Status = item.Status;
		StatusOptions = new string[6] { "In Stock", "Display", "In Claims", "Claimed Out", "Sold", "Missing" };
		SelectedStatus = item.Status;
	}
}
