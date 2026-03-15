using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Entertainment.Inventory
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;//computer or tablet
        public string Make { get; set; } = string.Empty;//brand like hp, dell, lenovo
        public string SerialNumber { get; set; } = string.Empty;//serial number of the computer
        public List<string> UPC { get; set; } = new();//upc associated with the computer
        public DateTime CreatedAt { get; set; } = DateTime.Now;//date and time the computer serial number was created
        public string CreatedBy { get; set; } = string.Empty;//user who created the computer serial number
        public DateTime SoldAt { get; set; } = DateTime.Now;//date and time the computer was sold
        public string SoldBy { get; set; } = string.Empty;//user who sold the computer
        public string Status { get; set; } = "Unknown";//status of the computer like available, sold, returned
    }
    public class InventoryStore
    {
        public List<InventoryItem> InventoryItems { get; set; } = new();

        public static string SavePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Entertainment",
                "Inventory",
                "inventory.json"
            );
        public void Save()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(InventoryItems, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? string.Empty);
            File.WriteAllText(SavePath, json);
        }
        public static InventoryStore Load()
        {
            if (!File.Exists(SavePath))
            {
                return new InventoryStore();
            }
            var json = File.ReadAllText(SavePath);
            var inventoryItems = System.Text.Json.JsonSerializer.Deserialize<List<InventoryItem>>(json) ?? new List<InventoryItem>();
            return new InventoryStore { InventoryItems = inventoryItems };
        }
    }
}
