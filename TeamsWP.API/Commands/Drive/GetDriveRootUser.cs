using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Drive
{
  // https://learn.microsoft.com/en-us/graph/api/driveitem-get
  public class GetDriveRootUser : ICommand
  {
    public string Endpoint => $"/users/{userID}/drive/root";

    public string userID;

    public class Response : Types.DriveItem, IResponse
    {
    }
  }
}
