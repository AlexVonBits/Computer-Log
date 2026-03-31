using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Entertainment.Users;

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

	private static string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EntertainmentInventory", "users.json");

	public bool TeamLeadNeedsSetup()
	{
		if (!string.IsNullOrWhiteSpace(TeamLead.Username))
		{
			return string.IsNullOrWhiteSpace(TeamLead.PasswordHash);
		}
		return true;
	}

	public void Save()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
		UsersData value = new UsersData
		{
			Teamlead = TeamLead,
			Associates = Associates
		};
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		File.WriteAllText(SavePath, JsonSerializer.Serialize(value, options));
	}

	public void Load()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
		if (File.Exists(SavePath))
		{
			UsersData usersData = JsonSerializer.Deserialize<UsersData>(File.ReadAllText(SavePath));
			if (usersData != null)
			{
				TeamLead = usersData.Teamlead ?? TeamLead;
				TeamLead.IsTeamLead = true;
				TeamLead.IsAdmin = true;
				Associates = usersData.Associates ?? new List<Associate>();
			}
		}
	}
}
