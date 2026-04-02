using System;
using Entertainment.Inventory;

namespace Entertainment;

public partial class SerialWithUPCStatus
{
	public InventoryItem Item { get; }

	public string UPCStatus { get; }

	public string SerialNumber => Item.SerialNumber;

	public string? Type => Item.Type;

	public string Status => Item.Status;

	public string? CreatedBy => Item.CreatedBy;

	public DateTime? CreatedAt => Item.CreatedAt;

	public string? SoldBy => Item.SoldBy;

	public DateTime? SoldAt => Item.SoldAt;

	public string Id => Item.Id;

	public SerialWithUPCStatus(InventoryItem item, string upcStatus)
	{
		Item = item;
		UPCStatus = upcStatus;
	}
}
