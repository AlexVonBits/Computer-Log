using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Entertainment.Inventory
{
    public class InventoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SerialNumber { get; set; } = string.Empty;
        public List<string> UPC { get; set; } = new();
        public string? Make { get; set; }
        public string? Type { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? SoldBy { get; set; }
        public DateTime? SoldAt { get; set; }
        public string Status { get; set; } = "In Stock";

        // Verification tracking
        public DateTime? LastVerifiedAt { get; set; }
        public string? LastVerifiedBy { get; set; }
        public string? VerificationNote { get; set; }
    }

    // Status constants for inventory items
    public static class InventoryStatus
    {
        public const string InStock = "In Stock";
        public const string Display = "Display";
        public const string InClaims = "In Claims";
        public const string ClaimedOut = "Claimed Out";
        public const string Sold = "Sold";
        public const string Missing = "Missing";

        public static readonly string[] OnHandStatuses = { InStock, Display, InClaims };
        public static readonly string[] AllStatuses = { InStock, Display, InClaims, ClaimedOut, Sold, Missing };
    }

    public class InventoryStore
    {
        private static readonly string DataFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entertainment",
            "inventory.json"
        );

        public List<InventoryItem> InventoryItems { get; set; } = new();

        public static InventoryStore Load()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    var json = File.ReadAllText(DataFilePath);
                    return JsonSerializer.Deserialize<InventoryStore>(json) ?? new InventoryStore();
                }
            }
            catch
            {
                // Return empty store on error
            }
            return new InventoryStore();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(DataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(DataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving inventory: {ex.Message}", "Save Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public bool SerialNumberExists(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return false;
            
            return InventoryItems.Any(i => 
                i.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public List<InventoryItem> FindByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return new List<InventoryItem>();

            barcode = barcode.Trim();
            
            // Find items matching the barcode (serial number or UPC)
            // Only return items with on-hand statuses
            return InventoryItems.Where(i =>
                (i.SerialNumber.Equals(barcode, StringComparison.OrdinalIgnoreCase) ||
                 i.UPC.Any(u => u.Equals(barcode, StringComparison.OrdinalIgnoreCase))) &&
                InventoryStatus.OnHandStatuses.Contains(i.Status)
            ).ToList();
        }

        public List<InventoryItem> FindBySerialOrUPC(string serialNumber, string upc)
        {
            var results = new List<InventoryItem>();
            
            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                var snMatches = InventoryItems.Where(i => 
                    i.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    InventoryStatus.OnHandStatuses.Contains(i.Status)).ToList();
                results.AddRange(snMatches);
            }
            
            if (!string.IsNullOrWhiteSpace(upc))
            {
                var upcMatches = InventoryItems.Where(i => 
                    i.UPC.Any(u => u.Equals(upc.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                    InventoryStatus.OnHandStatuses.Contains(i.Status)).ToList();
                
                // Add UPC matches that aren't already in results
                foreach (var item in upcMatches)
                {
                    if (!results.Any(r => r.Id == item.Id))
                    {
                        results.Add(item);
                    }
                }
            }
            
            return results.Distinct().ToList();
        }

        public void UpdateItemStatus(string itemId, string newStatus, string? verifiedBy, string? note = null)
        {
            var item = InventoryItems.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.Status = newStatus;
                item.LastVerifiedAt = DateTime.Now;
                item.LastVerifiedBy = verifiedBy;
                item.VerificationNote = note;
                Save();
            }
        }

        public void MarkVerified(string itemId, string verifiedBy)
        {
            var item = InventoryItems.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.LastVerifiedAt = DateTime.Now;
                item.LastVerifiedBy = verifiedBy;
                Save();
            }
        }

        /// <summary>
        /// Migrates Type values from UPCs to all serial numbers.
        /// For each unique UPC, determines the canonical Type and applies it to all items with that UPC.
        /// Uses the most common Type for each UPC, or the first one if there's a tie.
        /// </summary>
        public void MigrateTypesFromUPC()
        {
            // Group items by UPC
            var upcGroups = InventoryItems
                .Where(i => i.UPC != null && i.UPC.Count > 0)
                .SelectMany(i => i.UPC.Select(u => new { Item = i, UPC = u }))
                .GroupBy(x => x.UPC)
                .ToList();

            foreach (var upcGroup in upcGroups)
            {
                string upc = upcGroup.Key;
                var itemsWithThisUPC = upcGroup.Select(x => x.Item).ToList();

                // Get all non-null types for this UPC
                var typesWithCount = itemsWithThisUPC
                    .Where(i => !string.IsNullOrEmpty(i.Type))
                    .GroupBy(i => i.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList();

                if (typesWithCount.Count == 0)
                    continue; // No Type information for this UPC

                // Get the most common Type (if tie, first one wins)
                string? canonicalType = typesWithCount
                    .OrderByDescending(t => t.Count)
                    .ThenBy(t => t.Type)
                    .First()
                    .Type;

                // Update all items with this UPC to use the canonical Type
                foreach (var item in itemsWithThisUPC)
                {
                    if (item.Type != canonicalType)
                    {
                        item.Type = canonicalType;
                    }
                }
            }

            Save();
        }
    }
}
