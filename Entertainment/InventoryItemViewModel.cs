using System;
using System.Collections.Generic;
using System.Linq;
using Entertainment.Inventory;

namespace Entertainment;

public partial class InventoryItemViewModel
{
	private readonly InventoryItem _item;

	public string Id => _item.Id;

	public string SerialNumber => _item.SerialNumber;

	public string Status => _item.Status;

	public string? Make => _item.Make;

	public string? Type => _item.Type;

	public DateTime? LastVerifiedAt => _item.LastVerifiedAt;

	public string? LastVerifiedBy => _item.LastVerifiedBy;

	public string UPCDisplay
	{
		get
		{
			if (_item.UPC.Count <= 0)
			{
				return "None";
			}
			return string.Join(", ", _item.UPC);
		}
	}

	public List<string> UPCList => _item.UPC.ToList();

	public string LastVerifiedDisplay
	{
		get
		{
			if (LastVerifiedAt.HasValue)
			{
				return $"{LastVerifiedAt.Value:g} by {LastVerifiedBy ?? "Unknown"}";
			}
			return "";
		}
	}

	public InventoryItemViewModel(InventoryItem item)
	{
		_item = item;
	}
}
