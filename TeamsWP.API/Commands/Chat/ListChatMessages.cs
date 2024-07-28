using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Chat
{
  // https://learn.microsoft.com/en-us/graph/api/chat-list-messages
  public class ListChatMessages : ICommand
  {
    public string Endpoint => $"/chats/{id}/messages";

    public string id;

    public class Response : IResponse
    {
      public List<Types.Message> value;
    }
  }
}
