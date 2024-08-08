using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TeamsWP.Inlays
{
  public partial class ChatsInlay : UserControl, INotifyPropertyChanged, IInlay
  {
    private App _app;
    private Pages.MainPage _mainPage;

    public ChatsInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      Loaded += ChatsInlay_Loaded;
      DataContext = this;
    }

    public List<Chat> Chats { get; private set; }
    private void ChatsInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
    }

    public async Task Refresh()
    {
      _mainPage?.StartLoading();

      var responseChats = await _mainPage.Get<API.Commands.Chat.ListChats.Response>(new API.Commands.Chat.ListChats
      {
        retrieveMembers = true,
        retrieveLastMessagePreview = true,
      });

      if (responseChats != null)
      {
        Chats = responseChats.value.Select(s => new Chat() {
          App = _app,
          ID = s.id,
          ChatData = s,
          CurrentUserInfo = _mainPage.CurrentUserInfo,
        }).OrderByDescending(s=>s?.ChatData?.lastMessagePreview?.createdDateTime ?? s.ChatData.lastUpdatedDateTime).ToList();
        OnPropertyChanged(nameof(Chats));
      }

      _mainPage?.EndLoading();
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
      var chat = e.ClickedItem as Chat;
      if (chat != null)
      {
        await _mainPage.ViewChat(chat.ID);
      }
    }

    public class Chat
    {
      public App App { get; set; }
      public string ID { get; set; }
      public string Name => !string.IsNullOrEmpty(ChatData.topic) ? ChatData.topic : string.Join(", ", ChatPartners.Select(m => m.displayName));
      public bool IsRead => ChatData.viewpoint == null || ChatData.lastMessagePreview == null || ChatData.viewpoint.lastMessageReadDateTime >= ChatData.lastMessagePreview.createdDateTime;
      public Windows.UI.Text.FontWeight IsReadWeight => IsRead ? Windows.UI.Text.FontWeights.Normal : Windows.UI.Text.FontWeights.Bold;
      public string ChatImageURL {
        get
        {
          if (ChatData.chatType == "oneOnOne")
          {
            return App.Client.GraphEndpointRoot + FirstChatPartner.AvatarURL;
          }
          else
          {
            return $"teams://chats/{ChatData.id}/$photo"; // fake URL for now
          }
        }
      }
      public API.Commands.Types.Chat.Member FirstChatPartner => ChatPartners.FirstOrDefault();
      public IEnumerable<API.Commands.Types.Chat.Member> ChatPartners => ChatData.members.Where(m => m.userId != CurrentUserInfo.id);
      public API.Commands.User.Me.Response CurrentUserInfo { get; set; }
      public API.Commands.Types.Chat ChatData { get; set; }
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
