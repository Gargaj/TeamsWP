using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsWP.API.Commands
{
  public class Types
  {
    public class User
    {
      public string id;
      public string displayName;
      public string userId;
      public string email;
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
      public User user;
    }
    public class Reaction
    {
      public string reactionType;
      public DateTime createdDateTime;
      public User user;
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
  }
}
