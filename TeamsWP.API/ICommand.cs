using Newtonsoft.Json;

namespace TeamsWP.API
{
  public interface ICommand
  {
    [JsonIgnore]
    string Endpoint { get; }
  }

  public interface IResponse
  {
  }

  public interface IRawPost
  {
    [JsonIgnore]
    byte[] PostData { get; set; }
    [JsonIgnore]
    string MimeType { get; set; }
  }
}
