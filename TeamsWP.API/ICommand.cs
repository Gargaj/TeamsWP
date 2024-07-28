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
}
