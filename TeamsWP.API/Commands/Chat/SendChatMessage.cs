using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Chat
{
  // https://learn.microsoft.com/en-us/graph/api/chatmessage-post
  public class SendChatMessage : ICommand
  {
    public string Endpoint => $"/chats/{id}/messages";

    [JsonIgnore]
    public string id;

    public Types.Body body;

    public class Response : Types.Message, IResponse
    {
    }
  }
}
