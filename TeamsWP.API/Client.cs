using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

// https://learn.microsoft.com/en-us/graph/auth-v2-user?tabs=http

namespace TeamsWP.API
{
  public class Client
  {
    private readonly string AuthEndpoint = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize";
    private readonly string TokenEndpoint = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
    private readonly string GraphEndpointRoot = "https://graph.microsoft.com/v1.0";
    private readonly string RedirectURI = "https://login.microsoftonline.com/common/oauth2/nativeclient";

    private readonly string Tenant = "organizations";
    private readonly IEnumerable<string> Scopes = new[] {
      "offline_access",

      "Channel.ReadBasic.All",
      "ChannelMessage.Edit",
      "ChannelMessage.Send",
      "Chat.Create",
      "Chat.Read",
      "Chat.ReadBasic",
      "Chat.ReadWrite",
      "ChatMessage.Read",
      "ChatMessage.Send",
      "Team.Create",
      "Team.ReadBasic.All",
      "User.Read",
    };

    private Settings _settings = new Settings();
    private Newtonsoft.Json.JsonSerializerSettings _deserializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
    {
      MetadataPropertyHandling = Newtonsoft.Json.MetadataPropertyHandling.ReadAhead,
      NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
      TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
    };
    private string _state;
    private static Random _random = new Random();

    public Client()
    {
    }

    public Settings Settings => _settings;
    public Settings.AccountSettingsData CurrentAccountSettings => Settings.CurrentAccountSettings;

    private static string GenerateRandomString(int length)
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
      return new string(Enumerable.Repeat(chars, length) .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public string GetAuthenticationURL()
    {
      _state = GenerateRandomString(16);

      var authEndpoint = AuthEndpoint;
      authEndpoint = authEndpoint.Replace("{tenant}", Tenant);
      var collection = new Dictionary<string, string>()
      {
        { "client_id", Environment.ClientID },
        { "response_type", "code" },
        { "redirect_uri", RedirectURI },
        { "response_mode", "query" },
        { "scope", string.Join(" ", Scopes ) },
        { "state", _state },
      };

      return authEndpoint + "?" + string.Join("&", collection.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
    }

    public async Task<bool> ProcessAuthURL(Uri uri)
    {
      if (!uri.OriginalString.StartsWith(RedirectURI))
      {
        return false;
      }

      var queryString = new Windows.Foundation.WwwFormUrlDecoder(uri.Query[0] == '?' ? uri.Query.Substring(1) : uri.Query);
      if (_state != queryString.GetFirstValueByName("state"))
      {
        return false;
      }
      _state = string.Empty;
      var code = string.Empty;
      try
      {
        code = queryString.GetFirstValueByName("code");
      }
      catch
      {
        return false;
      }

      var endpoint = TokenEndpoint;
      endpoint = endpoint.Replace("{tenant}", Tenant);
      var collection = new Dictionary<string, string>()
      {
        { "client_id", Environment.ClientID },
        { "scope", string.Join(" ", Scopes ) },
        { "code", code },
        { "redirect_uri", RedirectURI },
        { "grant_type", "authorization_code" },
      };

      var data = string.Join("&", collection.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));

      var http = new HTTP();

      var headers = new NameValueCollection();
      headers["Content-Type"] = "application/x-www-form-urlencoded";

      var response = await http.DoPOSTRequestAsync(endpoint, data, headers);
      if (response == null)
      {
        return false;
      }

      var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(response) as Newtonsoft.Json.Linq.JObject;
      if (jsonObj == null || jsonObj.GetValue("access_token") == null)
      {
        return false;
      }

      var credentials = new Settings.AccountSettingsData()
      {
        Credentials = new Settings.CredentialsData()
        {
          UserPrincipalName = "user",
          AccessToken = jsonObj.GetValue("access_token").ToString(),
          RefreshToken = jsonObj.GetValue("refresh_token").ToString(),
        }
      };

      _settings.SelectedUserPrincipalName = credentials?.Credentials.UserPrincipalName;
      _settings.AccountSettings.Add(credentials);

      var responseMe = await GetAsync<Commands.User.Me.Response>(new Commands.User.Me());
      if (responseMe == null)
      {
        return false;
      }
      _settings.SelectedUserPrincipalName = credentials.Credentials.UserPrincipalName = responseMe.userPrincipalName;

      return await _settings.WriteSettings();
    }

    public bool IsAuthenticated { get { return CurrentAccountSettings?.Credentials.AccessToken != null; } }

    public async Task<T> GetAsync<T>(ICommand input) where T : IResponse
    {
      return await RequestAsync<T>("GET", input);
    }

    public async Task<T> PostAsync<T>(ICommand input) where T : IResponse
    {
      return await RequestAsync<T>("POST", input);
    }

    public async Task<T> RequestAsync<T>(string method, ICommand input) where T : IResponse
    {
      var http = new HTTP();

      var headers = new NameValueCollection();
      headers["Content-Type"] = "application/json";
      headers["Authorization"] = $"Bearer {CurrentAccountSettings.Credentials.AccessToken}";

      var url = $"{GraphEndpointRoot}/{input.Endpoint}";
      string responseJson = null;
      string bodyJson = string.Empty;
      try
      {
        switch (method)
        {
          case "GET":
            {
              url += SerializeInputToQueryString(input);
              responseJson = await http.DoGETRequestAsync(url, null, headers);
            }
            break;
          case "POST":
            {
              var inputType = input.GetType();
              var fields = inputType.GetFields();
              bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(input, _deserializerSettings);
              if (bodyJson == "{}" || fields.Length == 0)
              {
                bodyJson = string.Empty;
              }
              responseJson = await http.DoPOSTRequestAsync(url, bodyJson, headers);
            }
            break;
        }
      }
      catch (WebException ex)
      {
        var webResponse = ex.Response as HttpWebResponse;
        var error = ex.Response != null ? await new StreamReader(ex.Response.GetResponseStream()).ReadToEndAsync() : ex.ToString();
        if (ex?.Response?.Headers != null && ex.Response.Headers["content-type"].ToLower().StartsWith("application/json"))
        {
          var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(error) as Newtonsoft.Json.Linq.JObject;
          if (jsonObj != null && jsonObj.GetValue("error") != null && jsonObj.GetValue("error") != null)
          {
            var errorObj = jsonObj.GetValue("error") as Newtonsoft.Json.Linq.JObject;
            if (errorObj != null && errorObj.GetValue("code")?.ToString() == "InvalidAuthenticationToken")
            {
              if (await RefreshCredentials())
              {
                // Re-fire request
                headers["Authorization"] = $"Bearer {CurrentAccountSettings.Credentials.AccessToken}";
                switch (method)
                {
                  case "GET":
                    {
                      responseJson = await http.DoGETRequestAsync(url, null, headers);
                    }
                    break;
                  case "POST":
                    {
                      responseJson = await http.DoPOSTRequestAsync(url, bodyJson, headers);
                    }
                    break;
                }
              }
            }
            else
            {
              throw ex;
            }
          }
          else
          {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[HTTP ERROR {webResponse.StatusCode}] {error}");
#endif
            throw new WebException(error, ex, ex.Status, ex.Response);
          }
        }
        else
        {
          throw new WebException(error, ex, ex.Status, ex.Response);
        }
      }

      return responseJson != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseJson, _deserializerSettings) : default(T);
    }

    private async Task<bool> RefreshCredentials()
    {
      var endpoint = TokenEndpoint;
      endpoint = endpoint.Replace("{tenant}", Tenant);
      var collection = new Dictionary<string, string>()
      {
        { "client_id", Environment.ClientID },
        { "scope", string.Join(" ", Scopes ) },
        { "refresh_token", CurrentAccountSettings.Credentials.RefreshToken },
        { "grant_type", "refresh_token" },
      };

      var data = string.Join("&", collection.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));

      var http = new HTTP();

      var headers = new NameValueCollection();
      headers["Content-Type"] = "application/x-www-form-urlencoded";

      var response = await http.DoPOSTRequestAsync(endpoint, data, headers);
      if (response == null)
      {
        return false;
      }

      var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(response) as Newtonsoft.Json.Linq.JObject;
      if (jsonObj == null || jsonObj.GetValue("access_token") == null)
      {
        return false;
      }

      CurrentAccountSettings.Credentials.AccessToken = jsonObj.GetValue("access_token").ToString();
      CurrentAccountSettings.Credentials.RefreshToken = jsonObj.GetValue("refresh_token").ToString();

      return true;
    }

    private string SerializeInputToQueryString(ICommand input)
    {
      var inputType = input.GetType();
      bool first = true;
      var queryString = string.Empty;
      foreach (var field in inputType.GetFields())
      {
        if (field.GetCustomAttribute(typeof(Newtonsoft.Json.JsonIgnoreAttribute)) != null)
        {
          continue;
        }
        var value = inputType.GetField(field.Name).GetValue(input);
        if (value != null)
        {
          if (value is IEnumerable<object>)
          {
            var a = value as IEnumerable<object>;
            foreach (var i in a)
            {
              queryString += first ? "?" : "&";
              queryString += $"{field.Name}[]={WebUtility.UrlEncode(i.ToString())}";
              first = false;
            }
            continue;
          }
          queryString += first ? "?" : "&";
          queryString += $"{field.Name}={WebUtility.UrlEncode(value.ToString())}";
          first = false;
        }
      }
      return queryString;
    }
  }
}
