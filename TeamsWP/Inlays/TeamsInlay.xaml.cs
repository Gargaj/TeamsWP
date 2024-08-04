using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TeamsWP.Inlays
{
  public partial class TeamsInlay : UserControl, INotifyPropertyChanged, IInlay
  {
    private App _app;
    private Pages.MainPage _mainPage;

    public TeamsInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      Loaded += TeamsInlay_Loaded;
      DataContext = this;
    }

    public List<Team> Teams { get; private set; }
    private void TeamsInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
    }

    public async Task Refresh()
    {
      TeamsInlay_Loaded(null, null);//

      _mainPage?.StartLoading();

      var responseTeams = await _mainPage.Get<API.Commands.User.JoinedTeams.Response>(new API.Commands.User.JoinedTeams());
      if (responseTeams != null)
      {
        Teams = new List<Team>();
        
        foreach (var teamData in responseTeams.value)
        {
          var team = new Team() {
            ID = teamData.id,
            Name = teamData.displayName
          };

          var responseTeam = await _mainPage.Get<API.Commands.Channel.ListAllChannels.Response>(new API.Commands.Channel.ListAllChannels
          {
            id = teamData.id
          });
          if (responseTeam != null)
          {
            team.Channels = responseTeam.value.Select(s => new Channel() {
              ID = s.id,
              TeamID = teamData.id,
              Name = s.displayName
            }).ToList();
          }
          Teams.Add(team);
        }

        OnPropertyChanged(nameof(Teams));
      }

      _mainPage?.EndLoading();
    }

    private async void TeamChannel_ItemClick(object sender, ItemClickEventArgs e)
    {
      var channel = e.ClickedItem as Channel;
      if (channel != null)
      {
        /*
         Missing scope permissions on the request. API requires one of 'ChannelMessage.Read.All'

        var responseMessages = await _mainPage.Get<API.Commands.Channel.ListChannelMessages.Response>(new API.Commands.Channel.ListChannelMessages
        {
          channelId = channel.ID,
          teamId = channel.TeamID
        });
        */
      }
    }

    public class Team
    {
      public string ID { get; set; }
      public string Name { get; set; }
      public List<Channel> Channels { get; set; }
    }

    public class Channel
    {
      public string ID { get; set; }
      public string TeamID { get; set; }
      public string Name { get; set; }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises this object's PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The property that has a new value.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
