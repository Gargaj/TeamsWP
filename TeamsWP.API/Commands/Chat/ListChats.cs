using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Chat
{
  // https://learn.microsoft.com/en-us/graph/api/chat-list
  public class ListChats : ICommand
  {
    public string Endpoint
    {
      get
      {
        var url = $"/chats";
        var paramList = new List<string>();

        var expandList = new List<string>();
        if (retrieveMembers)
        {
          expandList.Add("members");
        }
        if (retrieveLastMessagePreview)
        {
          expandList.Add("lastMessagePreview");
        }
        if (expandList.Count > 0)
        {
          paramList.Add($"$expand={string.Join(",", expandList)}");
        }

        if (paramList.Count > 0)
        {
          url += "?" + string.Join("&", paramList);
        }
        return url;
      }
    }

    [JsonIgnore]
    public bool retrieveMembers;
    [JsonIgnore]
    public bool retrieveLastMessagePreview;

    public class Response : IResponse
    {
      [JsonProperty("@odata.nextLink")]
      public string _odataNextLink;

      public List<Types.Chat> value;
   }
  }
}