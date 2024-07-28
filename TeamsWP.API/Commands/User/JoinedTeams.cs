using System.Collections.Generic;

namespace TeamsWP.API.Commands.User
{
  // https://learn.microsoft.com/en-us/graph/api/user-list-joinedteams
  public class JoinedTeams : ICommand
  {
    public string Endpoint => "/me/joinedTeams";

    public class Response : IResponse
    {
      public List<Team> value;

      public class Team
      {
        public string id;
        public string displayName;
        public string description;
      }
    }
  }
}
