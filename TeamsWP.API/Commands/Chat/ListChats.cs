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

      public List<Chat> value;

      public class Chat
      {
        public string id;
        public string topic;
        public DateTime createdDateTime;
        public DateTime lastUpdatedDateTime;
        public string chatType;
        public Viewpoint viewpoint;
        public OnlineMeetingInfo onlineMeetingInfo;
        public string tenantID;
        public string webUrl;
        public List<Member> members;
        public Types.Message lastMessagePreview;

        public class Viewpoint
        {
          public bool isHidden;
          public DateTime lastMessageReadDateTime;
        }

        public class OnlineMeetingInfo
        {
          public object calendarEventId;
          public string joinWebUrl;
          public Types.UserBasic organizer;
        }

        public class Member
        {
          public string id;
          public List<string> roles;
          public string displayName;
          public string userId;
          public string email;

          public string AvatarURL => $"/users/{userId}/photo/$value";
        }
      }
    }
  }
}