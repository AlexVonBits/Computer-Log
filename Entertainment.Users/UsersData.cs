using System.Collections.Generic;

namespace Entertainment.Users;

public class UsersData
{
	public Associate Teamlead { get; set; } = new Associate();

	public List<Associate> Associates { get; set; } = new List<Associate>();
}
