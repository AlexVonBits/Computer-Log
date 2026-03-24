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
        public bool OnlinePick { get; set; } = false;

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
        public const string Deleted = "Deleted";
        public const string Clearance = "Clearance";

        public static readonly string[] OnHandStatuses = { InStock, Display, InClaims };
        public static readonly string[] AllStatuses = { InStock, Display, InClaims, ClaimedOut, Sold, Missing, Deleted, Clearance };

        // UPC-level statuses (for toggling clearance/deleted on UPCs)
        public const string UPCNormal = "Normal";
        public const string UPCClearance = "Clearance";
        public const string UPCDeleted = "Deleted";
        public static readonly string[] UPCStatuses = { UPCNormal, UPCClearance, UPCDeleted };
    }

    // Metadata class for UPCs (stores Make and Type for UPCs without serial numbers)
    public class UPCMetadata
    {
        public string? Make { get; set; }
        public string? Type { get; set; }
    }

    // Represents a single note from a verification session
    public class VerificationNote
    {
        public string UPC { get; set; } = "";
        public string Note { get; set; } = "";
        public string UserInitials { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Represents a completed verification session
    public class VerificationSession
    {
        public string VerifierName { get; set; } = "";
        public string VerifierInitials { get; set; } = "";
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public List<VerificationNote> Notes { get; set; } = new();
    }

    // Container for all verification sessions (persisted to JSON)
    public class VerificationSessionStore
    {
        private static readonly string DataFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entertainment",
            "verification_sessions.json"
        );

        public List<VerificationSession> Sessions { get; set; } = new();

        // Get the last user who completed a verification session fully
        public string? GetLastVerifier()
        {
            if (Sessions.Count == 0) return null;
            return Sessions.OrderByDescending(s => s.CompletedAt).First().VerifierName;
        }

        // Get the date/time of the last completed verification session
        public DateTime? GetLastVerificationDate()
        {
            if (Sessions.Count == 0) return null;
            return Sessions.OrderByDescending(s => s.CompletedAt).First().CompletedAt;
        }

        // Get all notes from all verification sessions
        public List<VerificationNote> GetAllNotes()
        {
            var allNotes = new List<VerificationNote>();
            foreach (var session in Sessions.OrderByDescending(s => s.CompletedAt))
            {
                foreach (var note in session.Notes)
                {
                    allNotes.Add(new VerificationNote
                    {
                        UPC = note.UPC,
                        Note = note.Note,
                        UserInitials = note.UserInitials,
                        Timestamp = note.Timestamp
                    });
                }
            }
            return allNotes.OrderByDescending(n => n.Timestamp).ToList();
        }

        // Add a new completed verification session
        public void AddSession(VerificationSession session)
        {
            Sessions.Add(session);
            Save();
        }

        public static VerificationSessionStore Load()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    var json = File.ReadAllText(DataFilePath);
                    return JsonSerializer.Deserialize<VerificationSessionStore>(json) ?? new VerificationSessionStore();
                }
            }
            catch
            {
                // Return empty store on error
            }
            return new VerificationSessionStore();
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
                System.Windows.MessageBox.Show($"Error saving verification sessions: {ex.Message}", "Save Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class InventoryStore
    {
        private static readonly string DataFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entertainment",
            "inventory.json"
        );

        public List<InventoryItem> InventoryItems { get; set; } = new();
        
        // Cross-references: maps a UPC to a list of related UPCs
        public Dictionary<string, List<string>> CrossReferences { get; set; } = new();
        
        // UPC-level clearance flag: maps UPC to clearance status
        public Dictionary<string, bool> UPCClearance { get; set; } = new();
        
        // UPC-level deleted flag: maps UPC to deleted status
        public Dictionary<string, bool> UPCDeleted { get; set; } = new();

        // Legacy: keep for migration - maps UPC to combined status (Normal, Clearance, Deleted)
        [Obsolete("Use UPCClearance and UPCDeleted instead")]
        public Dictionary<string, string> UPCStatuses { get; set; } = new();

        // UPC metadata: maps UPC to Make and Type info (for UPCs without serial numbers)
        public Dictionary<string, UPCMetadata> UPCMetadata { get; set; } = new();

        // Get the combined status of a specific UPC for display
        public string GetUPCStatus(string upc)
        {
            bool isDeleted = GetUPCDeleted(upc);
            bool isClearance = GetUPCClearance(upc);
            
            if (isDeleted) return InventoryStatus.UPCDeleted;
            if (isClearance) return InventoryStatus.UPCClearance;
            return InventoryStatus.UPCNormal;
        }

        // Get clearance status of a specific UPC
        public bool GetUPCClearance(string upc)
        {
            if (UPCClearance.TryGetValue(upc, out var isClearance))
                return isClearance;
            // Check legacy status for migration
            if (UPCStatuses.TryGetValue(upc, out var status) && status == InventoryStatus.UPCClearance)
                return true;
            return false;
        }

        // Get deleted status of a specific UPC
        public bool GetUPCDeleted(string upc)
        {
            if (UPCDeleted.TryGetValue(upc, out var isDeleted))
                return isDeleted;
            // Check legacy status for migration
            if (UPCStatuses.TryGetValue(upc, out var status) && status == InventoryStatus.UPCDeleted)
                return true;
            return false;
        }

        // Set clearance status of a specific UPC
        public void SetUPCClearance(string upc, bool isClearance)
        {
            if (isClearance)
                UPCClearance[upc] = true;
            else
                UPCClearance.Remove(upc);
            Save();
        }

        // Set deleted status of a specific UPC
        public void SetUPCDeleted(string upc, bool isDeleted)
        {
            if (isDeleted)
                UPCDeleted[upc] = true;
            else
                UPCDeleted.Remove(upc);
            Save();
        }

        // Toggle clearance status of a specific UPC
        public bool ToggleUPCClearance(string upc)
        {
            bool current = GetUPCClearance(upc);
            SetUPCClearance(upc, !current);
            return !current;
        }

        // Toggle deleted status of a specific UPC
        public bool ToggleUPCDeleted(string upc)
        {
            bool current = GetUPCDeleted(upc);
            SetUPCDeleted(upc, !current);
            return !current;
        }

        // Legacy: Set the status of a specific UPC (returns the new status)
        [Obsolete("Use SetUPCClearance and SetUPCDeleted instead")]
        public string SetUPCStatus(string upc, string status)
        {
            if (status == InventoryStatus.UPCNormal)
            {
                UPCClearance.Remove(upc);
                UPCDeleted.Remove(upc);
            }
            else if (status == InventoryStatus.UPCClearance)
            {
                UPCClearance[upc] = true;
                UPCDeleted.Remove(upc);
            }
            else if (status == InventoryStatus.UPCDeleted)
            {
                UPCClearance.Remove(upc);
                UPCDeleted[upc] = true;
            }
            Save();
            return status;
        }

        // Legacy: Toggle UPC status (cycles through Normal -> Clearance -> Deleted -> Normal)
        [Obsolete("Use ToggleUPCClearance and ToggleUPCDeleted instead")]
        public string ToggleUPCStatus(string upc)
        {
            var currentStatus = GetUPCStatus(upc);
            var newStatus = currentStatus switch
            {
                InventoryStatus.UPCNormal => InventoryStatus.UPCClearance,
                InventoryStatus.UPCClearance => InventoryStatus.UPCDeleted,
                InventoryStatus.UPCDeleted => InventoryStatus.UPCNormal,
                _ => InventoryStatus.UPCNormal
            };
            return SetUPCStatus(upc, newStatus);
        }

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
