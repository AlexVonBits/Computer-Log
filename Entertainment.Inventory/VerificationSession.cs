using System;
using System.Collections.Generic;

namespace Entertainment.Inventory;

public class VerificationSession
{
	public string VerifierName { get; set; } = "";

	public string VerifierInitials { get; set; } = "";

	public DateTime CompletedAt { get; set; } = DateTime.Now;

	public List<VerificationNote> Notes { get; set; } = new List<VerificationNote>();

	public int IssuesFixedCount { get; set; }

	public int IssuesNotFixedCount { get; set; }

	public int NoIssuesCount { get; set; }
}
