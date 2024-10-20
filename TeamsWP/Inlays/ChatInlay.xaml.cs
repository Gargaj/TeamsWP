﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public ObservableCollection<Message> Messages { get; private set; } = new ObservableCollection<Message>();
    public string MessageText { get; set; }
    public string ChatName { get; set; }

    private void ChatInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
      ChatName = string.Empty;
      Messages.Clear();
      _updateTimer.Stop();
    }

    public async Task Refresh()
    {
      _mainPage?.StartLoading();
      await Update();

      var responseChatInfo = await _mainPage.Get<API.Commands.Chat.GetChat.Response>(new API.Commands.Chat.GetChat
      {
        id = ID
      });
      if (responseChatInfo != null)
      {
        if (string.IsNullOrEmpty(responseChatInfo.topic))
        {
          ChatName = responseChatInfo.topic;
        }
        else if (responseChatInfo.members != null)
        {
          ChatName = string.Join(", ", responseChatInfo.members.Where(m => m.userId != _mainPage.CurrentUserInfo.id).Select(m => m.displayName));
        }
        OnPropertyChanged(nameof(ChatName));
      }

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

    private void AddNewMessages(IEnumerable<API.Commands.Types.Message> messageBurst)
    {
      var newMessages = messageBurst.Where(s => s.deletedDateTime == null && !Messages.Any(m => m.ID == s.id));
      if (!newMessages.Any())
      {
        return;
      }

      foreach (var message in newMessages)
      {
        var insertionItem = Messages.FirstOrDefault(s => s.Timestamp > message?.createdDateTime.ToLocalTime());
        var idx = Messages.IndexOf(insertionItem);
        Messages.Insert(idx < 0 ? Messages.Count : idx, new Message()
        {
          ID = message?.id,
          Sender = message?.from?.user?.displayName ?? "[unknown]",
          SenderImageURL = message?.from?.user?.AvatarURL == null ? string.Empty : _app.Client.GraphEndpointRoot + message?.from?.user?.AvatarURL,
          MessageData = message
        });
      }

      OnPropertyChanged(nameof(Messages));
      listView.UpdateLayout();
      listView.ScrollIntoView(Messages.LastOrDefault());
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(MessageText))
      {
        return;
      }
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
        OnPropertyChanged(nameof(MessageText));
        AddNewMessages(new List<API.Commands.Types.Message>() { response });
      }
    }

    public class Message
    {
      public string ID { get; set; }
      public string Sender { get; set; }
      public string SenderImageURL { get; set; }
      public string TimestampString => Timestamp.HasValue ? (Timestamp.Value.Date == DateTime.Now.Date ? Timestamp.Value.ToString("HH:mm") : Timestamp.Value.ToString("yyyy-MM-dd HH:mm")) : string.Empty;
      public DateTime? Timestamp => MessageData?.createdDateTime.ToLocalTime();
      public string Text => MessageData?.body?.content ?? "[unknown]";
      public RichTextControls.Generators.IHtmlXamlGenerator HTMLGenerator { get { return new TeamsHTMLGenerator(MessageData); } }
      public IEnumerable<GroupedReaction> Reactions
      {
        get
        {
          var groupedReactions = MessageData.reactions.GroupBy(s => s.reactionType);
          return groupedReactions.Select(s => new GroupedReaction() {
            Reaction = API.Emoji.Emoticons.ContainsKey(s.Key) ? API.Emoji.Emoticons[s.Key].unicode : s.Key,
            Count = s.Count()
          });
        }
      }

      public API.Commands.Types.Message MessageData { get; internal set; }
    }

    public class GroupedReaction
    {
      public string Reaction { get; set; }
      public int Count { get; set; }
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
