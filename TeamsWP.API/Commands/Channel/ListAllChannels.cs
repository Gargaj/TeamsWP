using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Channel
{
  // https://learn.microsoft.com/en-us/graph/api/team-list-allchannels
  public class ListAllChannels : ICommand
  {
    public string Endpoint => $"/teams/{id}/allChannels";

    public string id;

    public class Response : IResponse
    {
      public List<Channel> value;

      public class Channel
      {
        public string id;
        public DateTime createdDateTime;
        public string displayName;
        public string description;
        public string membershipType;
        public string tenantId;
        public bool isArchived;
      }
    }
  }
}
