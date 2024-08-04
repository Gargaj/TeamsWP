using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Channel
{
  // https://learn.microsoft.com/en-us/graph/api/channel-list-messages
  public class ListChannelMessages : ICommand
  {
    public string Endpoint => $"/teams/{teamId}/channels/{channelId}/messages";

    public string teamId;
    public string channelId;

    public class Response : IResponse
    {
      public List<Types.Message> value;
    }
  }
}
