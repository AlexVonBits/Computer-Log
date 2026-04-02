namespace Entertainment.Users;

public class Associate
{
	public int Id { get; set; }

	public string Username { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public bool IsAdmin { get; set; }

	public bool IsTeamLead { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string Initials { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;
}
