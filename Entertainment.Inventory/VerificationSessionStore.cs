using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Entertainment.Inventory;

public class VerificationSessionStore
{
	private static readonly string DataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Entertainment", "verification_sessions.json");

	public List<VerificationSession> Sessions { get; set; } = new List<VerificationSession>();

	public string? GetLastVerifier()
	{
		if (Sessions.Count == 0)
		{
			return null;
		}
		return Sessions.OrderByDescending((VerificationSession s) => s.CompletedAt).First().VerifierName;
	}

	public DateTime? GetLastVerificationDate()
	{
		if (Sessions.Count == 0)
		{
			return null;
		}
		return Sessions.OrderByDescending((VerificationSession s) => s.CompletedAt).First().CompletedAt;
	}

	public VerificationSession? GetLastSession()
	{
		if (Sessions.Count == 0)
		{
			return null;
		}
		return Sessions.OrderByDescending((VerificationSession s) => s.CompletedAt).First();
	}

	public List<VerificationNote> GetAllNotes()
	{
		List<VerificationNote> list = new List<VerificationNote>();
		foreach (VerificationSession item in Sessions.OrderByDescending((VerificationSession s) => s.CompletedAt))
		{
			foreach (VerificationNote note in item.Notes)
			{
				list.Add(new VerificationNote
				{
					UPC = note.UPC,
					Note = note.Note,
					UserInitials = note.UserInitials,
					Timestamp = note.Timestamp
				});
			}
		}
		return list.OrderByDescending((VerificationNote n) => n.Timestamp).ToList();
	}

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
				return JsonSerializer.Deserialize<VerificationSessionStore>(File.ReadAllText(DataFilePath)) ?? new VerificationSessionStore();
			}
		}
		catch
		{
		}
		return new VerificationSessionStore();
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
			MessageBox.Show("Error saving verification sessions: " + ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}
}
