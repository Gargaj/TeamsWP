using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsWP.API
{
  public class Emoji
  {
    private static readonly string EmojiListURL = "https://statics.teams.cdn.office.net/evergreen-assets/personal-expressions/v1/metadata/a098bcb732fd7dd80ce11c12ad15767f/en-us.json";
    private static Dictionary<string, Emoticon> _emoticons;

    public static async Task Load()
    {
      HTTP http = new HTTP();

      var dataJson = await http.DoGETRequestAsync(EmojiListURL);

      var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Response>(dataJson);

      _emoticons = data?.categories?.SelectMany(s => s.emoticons).GroupBy(s => s.id).Select(s => s.First()).ToDictionary(s => s.id, s => s);
    }

    public static Dictionary<string, Emoticon> Emoticons => _emoticons;

    private class Response
    {
      public IEnumerable<Category> categories;
    }

    public class Category
    {
      public string id;
      public string title;
      public string description;
      public IEnumerable<Emoticon> emoticons;
    }

    public class Emoticon
    {
      public string id;
      public string description;
      public IEnumerable<string> shortcuts;
      public string unicode;
      public string etag;
      public bool diverse;
      public IEnumerable<string> keywords;
    }

    public class Animation
    {
      public uint fps;
      public uint framesCount;
      public uint firstFrame;
    }
  }
}
