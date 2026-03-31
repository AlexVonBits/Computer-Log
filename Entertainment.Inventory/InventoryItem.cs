using System;
using System.Collections.Generic;

namespace Entertainment.Inventory;

public class InventoryItem
{
	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string SerialNumber { get; set; } = string.Empty;

	public List<string> UPC { get; set; } = new List<string>();

	public string? Make { get; set; }

	public string? Type { get; set; }

	public string? CreatedBy { get; set; }

	public DateTime? CreatedAt { get; set; }

	public string? SoldBy { get; set; }

	public DateTime? SoldAt { get; set; }

	public string Status { get; set; } = "In Stock";

	public bool OnlinePick { get; set; }

	public DateTime? LastVerifiedAt { get; set; }

	public string? LastVerifiedBy { get; set; }

	public string? VerificationNote { get; set; }
}
