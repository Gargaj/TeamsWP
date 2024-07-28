using System.Collections.Generic;

namespace TeamsWP.API.Commands.User
{
  // https://learn.microsoft.com/en-us/graph/api/user-get
  public class Me : ICommand
  {
    public string Endpoint => "/me";

    public class Response : IResponse
    {
      public List<string> businessPhones;
      public string displayName;
      public string givenName;
      public string jobTitle;
      public string mail;
      public string mobilePhone;
      public string officeLocation;
      public string preferredLanguage;
      public string surname;
      public string userPrincipalName;
      public string id;
    }
  }
}
