using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeamsWP.API.Commands.Drive
{
  // https://learn.microsoft.com/en-us/graph/api/drive-list
  public class ListDrives : ICommand
  {
    public string Endpoint => $"/me/drives";

    public class Response : IResponse
    {
      public List<Types.Drive> value = null;
    }
  }
}
