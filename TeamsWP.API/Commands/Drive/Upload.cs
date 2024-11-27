using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Drive
{
  // https://learn.microsoft.com/en-us/graph/api/driveitem-put-content
  public class Upload : ICommand, IRawPost
  {
    public string Endpoint => $"/me/drive/items/root:/TeamsWP/{filename}:/content";

    [JsonIgnore]
    public string filename;

    [JsonIgnore]
    public byte[] PostData { get; set; }
    [JsonIgnore]
    public string MimeType { get; set; }

    public class Response : Types.DriveItem, IResponse
    {
    }
  }
}
