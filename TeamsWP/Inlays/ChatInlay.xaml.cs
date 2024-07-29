using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TeamsWP.Inlays
{
  public partial class ChatInlay : UserControl, INotifyPropertyChanged, IInlay
  {
    private App _app;
    private Pages.MainPage _mainPage;
    private DispatcherTimer _updateTimer = new DispatcherTimer();

    public ChatInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      Loaded += ChatInlay_Loaded;
      DataContext = this;

      _updateTimer.Interval = TimeSpan.FromSeconds(10);
      _updateTimer.Tick += async (s, e) => { await Update(); };
    }

    public string ID { get; set; }
    public List<Message> Messages { get; private set; } = new List<Message>();
    public string MessageText { get; set; }

    private void ChatInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
      Messages.Clear();
      _updateTimer.Stop();
    }

    public async Task Refresh()
    {
      _mainPage?.StartLoading();
      await Update();
      _mainPage?.EndLoading();

      _updateTimer.Start();
    }

    public async Task Update()
    {
      if (string.IsNullOrEmpty(ID))
      {
        return;
      }

      var responseMessages = await _mainPage.Get<API.Commands.Chat.ListChatMessages.Response>(new API.Commands.Chat.ListChatMessages
      {
        id = ID
      });

      if (responseMessages != null)
      {
        AddNewMessages(responseMessages.value);
      }
    }

    private void AddNewMessages(IEnumerable<API.Commands.Types.Message> newMessages)
    {
      Messages.AddRange(
        newMessages
        .Where(s => s.deletedDateTime == null && !Messages.Any(m => m.ID == s.id))
        .Select(s => new Message()
        {
          ID = s?.id,
          Sender = s?.from?.user?.displayName ?? "[unknown]",
          SenderImageURL = _app.Client.GraphEndpointRoot + s?.from?.user?.AvatarURL,
          Text = s?.body?.content ?? "[unknown]",
          Timestamp = s?.createdDateTime
        })
      );
      Messages = Messages.OrderBy(s => s.Timestamp).ToList();

      OnPropertyChanged(nameof(Messages));
      listView.UpdateLayout();
      listView.ScrollIntoView(Messages.LastOrDefault());
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
      var response = await _mainPage.Post<API.Commands.Chat.SendChatMessage.Response>(new API.Commands.Chat.SendChatMessage
      {
        id = ID,
        body = new API.Commands.Types.Body()
        {
          contentType = "text",
          content = MessageText,
        }
      });

      if (response != null)
      {
        MessageText = string.Empty;
        AddNewMessages(new List<API.Commands.Types.Message>() { response });
      }
    }

    public class Message
    {
      public string ID { get; set; }
      public string Sender { get; set; }
      public string SenderImageURL { get; set; }
      public string TimestampString => Timestamp.HasValue ? (Timestamp.Value.Date == DateTime.Now.Date ? Timestamp.Value.ToString("HH:mm") : Timestamp.Value.ToString("yyyy-MM-dd HH:mm")) : string.Empty;
      public DateTime? Timestamp { get; set; }
      public string Text { get; set; }
      public RichTextControls.Generators.IHtmlXamlGenerator HTMLGenerator { get { return new TeamsHTMLGenerator(Text); } }
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
