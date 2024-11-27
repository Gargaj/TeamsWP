using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Drive
{
  // https://learn.microsoft.com/en-us/graph/api/driveitem-get
  public class GetDriveRoot : ICommand
  {
    public string Endpoint => $"/me/drive/root";

    public class Response : Types.DriveItem, IResponse
    {
    }
  }
}
