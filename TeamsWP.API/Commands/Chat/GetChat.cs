using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Chat
{
  // https://learn.microsoft.com/en-us/graph/api/chat-get
  public class GetChat : ICommand
  {
    public string Endpoint => $"/chats/{id}";

    public string id;

    public class Response : Types.Chat, IResponse
    {
    }
  }
}
