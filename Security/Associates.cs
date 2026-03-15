using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Entertainment.Security;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Entertainment.Users
{
    public class Associate
    {
        //add users/associates to the system
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public bool IsTeamLead { get; set; } = false;
        public string FirstName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
    public class User
    {

        public Associate TeamLead { get; set; } = new Associate
        {
            Id = 0,
            IsAdmin = true,
            IsTeamLead = true,
            Username = "",
            PasswordHash = "",
            FirstName = "",
            LastName = "",
            Initials = ""
        };
        public List<Associate> Associates { get; set; } = new List<Associate>();

        private static string SavePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EntertainmentInventory",
                "users.json"
             );

        public bool TeamLeadNeedsSetup()
        {
            return string.IsNullOrWhiteSpace(TeamLead.Username)
                || string.IsNullOrWhiteSpace(TeamLead.PasswordHash);
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            var data = new UsersData
            {
                Teamlead = TeamLead,
                Associates = Associates
            };
            var options = new JsonSerializerOptions { WriteIndented = true};
            File.WriteAllText(SavePath, JsonSerializer.Serialize(data, options));
        }
        public void Load()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            if (!File.Exists(SavePath))
                return;
            var data = JsonSerializer.Deserialize<UsersData>(File.ReadAllText(SavePath));
            if (data == null) return;

            TeamLead = data.Teamlead ?? TeamLead;
            TeamLead.IsTeamLead = true;
            TeamLead.IsAdmin = true;

            Associates = data.Associates ?? new List<Associate>();
        }
    }
    public class UsersData
    {
        public Associate Teamlead { get; set; } = new Associate();
        public List<Associate> Associates { get; set; } = new List<Associate>();
    }
}
