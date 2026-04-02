namespace Entertainment;

public partial class UPCWithStatus
{
	public string UPC { get; set; } = "";

	public string Make { get; set; } = "";

	public string Type { get; set; } = "";

	public bool IsClearance { get; set; }

	public bool IsDeleted { get; set; }

	public int OnHand { get; set; }

	public string Status
	{
		get
		{
			if (IsDeleted)
			{
				return "Deleted";
			}
			if (IsClearance)
			{
				return "Clearance";
			}
			return "";
		}
	}
}
