using System;

namespace Entertainment.Inventory;

public class VerificationNote
{
	public string UPC { get; set; } = "";

	public string Note { get; set; } = "";

	public string UserInitials { get; set; } = "";

	public DateTime Timestamp { get; set; } = DateTime.Now;
}
