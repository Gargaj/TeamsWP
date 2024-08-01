using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsWP.API.Commands
{
  public class Types
  {
    public class UserBasic
    {
      public string id;
      public string displayName;
      public string userIdentityType;

      public string AvatarURL => $"/users/{id}/photo/$value";
    }
    public class Body
    {
      public string contentType;
      public string content;
    }
    public class From
    {
      public string application;
      public string device;
      public UserBasic user;
    }
    public class Reaction
    {
      public string reactionType;
      public DateTime createdDateTime;
      public UserBasic user;
    }
    public class Attachment
    {
      public string id;
      public string contentType;
      public string contentUrl;
      public string content;
      public string name;
      public string thumbnailUrl;
      public string teamsAppId;
    }
    public class Mention
    {
      public uint id;
      public string mentionText;
      public From mentioned;
    }
    public class Message
    {
      public string id;
      public string replyToId;
      public string etag;
      public string messageType;
      public DateTime createdDateTime;
      public DateTime? lastModifiedDateTime;
      public DateTime? lastEditedDateTime;
      public DateTime? deletedDateTime;
      public string subject;
      public string summary;
      public string chatId;
      public string importance;
      public string locale;
      public string webUrl;
      public string channelIdentity;
      public string policyViolation;
      public object eventDetail;
      public From from;
      public Body body;

      public List<Attachment> attachments;
      public List<Reaction> reactions;
      public List<Mention> mentions;

      public bool isDeleted;
    }

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
      public Message lastMessagePreview;

      public class Viewpoint
      {
        public bool isHidden;
        public DateTime lastMessageReadDateTime;
      }

      public class OnlineMeetingInfo
      {
        public object calendarEventId;
        public string joinWebUrl;
        public UserBasic organizer;
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
