using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Entertainment.Inventory;

public class InventoryStore
{
	private static readonly string DataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Entertainment", "inventory.json");

	public List<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

	public Dictionary<string, List<string>> CrossReferences { get; set; } = new Dictionary<string, List<string>>();

	public Dictionary<string, bool> UPCClearance { get; set; } = new Dictionary<string, bool>();

	public Dictionary<string, bool> UPCDeleted { get; set; } = new Dictionary<string, bool>();

	[Obsolete("Use UPCClearance and UPCDeleted instead")]
	public Dictionary<string, string> UPCStatuses { get; set; } = new Dictionary<string, string>();

	public Dictionary<string, UPCMetadata> UPCMetadata { get; set; } = new Dictionary<string, UPCMetadata>();

	public string GetUPCStatus(string upc)
	{
		bool uPCDeleted = GetUPCDeleted(upc);
		bool uPCClearance = GetUPCClearance(upc);
		if (uPCDeleted)
		{
			return "Deleted";
		}
		if (uPCClearance)
		{
			return "Clearance";
		}
		return "Normal";
	}

	public bool GetUPCClearance(string upc)
	{
		if (UPCClearance.TryGetValue(upc, out var value))
		{
			return value;
		}
		if (UPCStatuses.TryGetValue(upc, out string value2) && value2 == "Clearance")
		{
			return true;
		}
		return false;
	}

	public bool GetUPCDeleted(string upc)
	{
		if (UPCDeleted.TryGetValue(upc, out var value))
		{
			return value;
		}
		if (UPCStatuses.TryGetValue(upc, out string value2) && value2 == "Deleted")
		{
			return true;
		}
		return false;
	}

	public void SetUPCClearance(string upc, bool isClearance)
	{
		if (isClearance)
		{
			UPCClearance[upc] = true;
		}
		else
		{
			UPCClearance.Remove(upc);
		}
		Save();
	}

	public void SetUPCDeleted(string upc, bool isDeleted)
	{
		if (isDeleted)
		{
			UPCDeleted[upc] = true;
		}
		else
		{
			UPCDeleted.Remove(upc);
		}
		Save();
	}

	public bool ToggleUPCClearance(string upc)
	{
		bool uPCClearance = GetUPCClearance(upc);
		SetUPCClearance(upc, !uPCClearance);
		return !uPCClearance;
	}

	public bool ToggleUPCDeleted(string upc)
	{
		bool uPCDeleted = GetUPCDeleted(upc);
		SetUPCDeleted(upc, !uPCDeleted);
		return !uPCDeleted;
	}

	[Obsolete("Use SetUPCClearance and SetUPCDeleted instead")]
	public string SetUPCStatus(string upc, string status)
	{
		switch (status)
		{
		case "Normal":
			UPCClearance.Remove(upc);
			UPCDeleted.Remove(upc);
			break;
		case "Clearance":
			UPCClearance[upc] = true;
			UPCDeleted.Remove(upc);
			break;
		case "Deleted":
			UPCClearance.Remove(upc);
			UPCDeleted[upc] = true;
			break;
		}
		Save();
		return status;
	}

	[Obsolete("Use ToggleUPCClearance and ToggleUPCDeleted instead")]
	public string ToggleUPCStatus(string upc)
	{
		return SetUPCStatus(upc, GetUPCStatus(upc) switch
		{
			"Normal" => "Clearance", 
			"Clearance" => "Deleted", 
			"Deleted" => "Normal", 
			_ => "Normal", 
		});
	}

	public static InventoryStore Load()
	{
		try
		{
			if (File.Exists(DataFilePath))
			{
				return JsonSerializer.Deserialize<InventoryStore>(File.ReadAllText(DataFilePath)) ?? new InventoryStore();
			}
		}
		catch
		{
		}
		return new InventoryStore();
	}

	public void Save()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(DataFilePath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			string contents = JsonSerializer.Serialize(this, options);
			File.WriteAllText(DataFilePath, contents);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error saving inventory: " + ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	public bool SerialNumberExists(string serialNumber)
	{
		if (string.IsNullOrWhiteSpace(serialNumber))
		{
			return false;
		}
		return InventoryItems.Any((InventoryItem i) => i.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	public List<InventoryItem> FindByBarcode(string barcode)
	{
		if (string.IsNullOrWhiteSpace(barcode))
		{
			return new List<InventoryItem>();
		}
		barcode = barcode.Trim();
		return InventoryItems.Where((InventoryItem i) => (i.SerialNumber.Equals(barcode, StringComparison.OrdinalIgnoreCase) || i.UPC.Any((string u) => u.Equals(barcode, StringComparison.OrdinalIgnoreCase))) && InventoryStatus.OnHandStatuses.Contains(i.Status)).ToList();
	}

	public List<InventoryItem> FindBySerialOrUPC(string serialNumber, string upc)
	{
		List<InventoryItem> list = new List<InventoryItem>();
		if (!string.IsNullOrWhiteSpace(serialNumber))
		{
			List<InventoryItem> collection = InventoryItems.Where((InventoryItem i) => i.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase) && InventoryStatus.OnHandStatuses.Contains(i.Status)).ToList();
			list.AddRange(collection);
		}
		if (!string.IsNullOrWhiteSpace(upc))
		{
			foreach (InventoryItem item in InventoryItems.Where((InventoryItem i) => i.UPC.Any((string u) => u.Equals(upc.Trim(), StringComparison.OrdinalIgnoreCase)) && InventoryStatus.OnHandStatuses.Contains(i.Status)).ToList())
			{
				if (!list.Any((InventoryItem r) => r.Id == item.Id))
				{
					list.Add(item);
				}
			}
		}
		return list.Distinct().ToList();
	}

	public void UpdateItemStatus(string itemId, string newStatus, string? verifiedBy, string? note = null)
	{
		InventoryItem inventoryItem = InventoryItems.FirstOrDefault((InventoryItem i) => i.Id == itemId);
		if (inventoryItem != null)
		{
			inventoryItem.Status = newStatus;
			inventoryItem.LastVerifiedAt = DateTime.Now;
			inventoryItem.LastVerifiedBy = verifiedBy;
			inventoryItem.VerificationNote = note;
			Save();
		}
	}

	public void MarkVerified(string itemId, string verifiedBy)
	{
		InventoryItem inventoryItem = InventoryItems.FirstOrDefault((InventoryItem i) => i.Id == itemId);
		if (inventoryItem != null)
		{
			inventoryItem.LastVerifiedAt = DateTime.Now;
			inventoryItem.LastVerifiedBy = verifiedBy;
			Save();
		}
	}

	public void MigrateTypesFromUPC()
	{
		foreach (var item in (from x in InventoryItems.Where((InventoryItem i) => i.UPC != null && i.UPC.Count > 0).SelectMany((InventoryItem i) => i.UPC.Select((string u) => new
			{
				Item = i,
				UPC = u
			}))
			group x by x.UPC).ToList())
		{
			_ = item.Key;
			List<InventoryItem> list = item.Select(x => x.Item).ToList();
			var list2 = (from i in list
				where !string.IsNullOrEmpty(i.Type)
				group i by i.Type into g
				select new
				{
					Type = g.Key,
					Count = g.Count()
				}).ToList();
			if (list2.Count == 0)
			{
				continue;
			}
			string type = (from t in list2
				orderby t.Count descending, t.Type
				select t).First().Type;
			foreach (InventoryItem item2 in list)
			{
				if (item2.Type != type)
				{
					item2.Type = type;
				}
			}
		}
		Save();
	}
}
