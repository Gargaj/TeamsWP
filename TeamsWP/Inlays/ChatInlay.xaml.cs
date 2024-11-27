using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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

      ImageAttachments = new ObservableCollection<ImageAttachment>();

      _updateTimer.Interval = TimeSpan.FromSeconds(10);
      _updateTimer.Tick += async (s, e) => { await Update(); };
    }

    public string ID { get; set; }
    public ObservableCollection<Message> Messages { get; private set; } = new ObservableCollection<Message>();
    public string MessageText { get; set; }
    public string ChatName { get; set; }
    public string ImageAttachmentsHeight { get { return ImageAttachments.Any() ? "120" : "0"; } }

    public ObservableCollection<ImageAttachment> ImageAttachments { get; set; }
    public App App { get => _app; set => _app = value; }

    private void ChatInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
      ChatName = string.Empty;
      Messages.Clear();
      _updateTimer.Stop();

      ImageAttachments = new ObservableCollection<ImageAttachment>();
      OnPropertyChanged(nameof(ImageAttachmentsHeight));
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
          Parent = this,
          MessageData = message
        });
      }

      OnPropertyChanged(nameof(Messages));
      listView.UpdateLayout();
      listView.ScrollIntoView(Messages.LastOrDefault());
    }

    private async void AttachFile_Click(object sender, RoutedEventArgs e)
    {
      var picker = new Windows.Storage.Pickers.FileOpenPicker
      {
        SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
      };
      picker.FileTypeFilter.Add(".gif");
      picker.FileTypeFilter.Add(".jpg");
      picker.FileTypeFilter.Add(".jpeg");
      picker.FileTypeFilter.Add(".png");

      var file = await picker.PickSingleFileAsync();
      if (file != null)
      {
        using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
        {
          const uint FileSizeLimit = 1000000;
          if (fileStream.Size > FileSizeLimit)
          {
            var dialog = new ContentDialog
            {
              Content = new TextBlock { Text = $"The image is too big! Do you want to resize it?", TextWrapping = TextWrapping.WrapWholeWords },
              Title = $"Image too big!",
              IsSecondaryButtonEnabled = true,
              PrimaryButtonText = "Yes",
              SecondaryButtonText = "No"
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
              var resizedByteData = await ResizedImage(file, 1920, 1920);

              var bitmapImage = new BitmapImage();
              bitmapImage.SetSource(fileStream);

              ImageAttachments.Add(new ImageAttachment()
              {
                BitmapImage = bitmapImage,
                MimeType = "image/jpeg",
                ByteData = resizedByteData,
                Filename = file.Name,
              });
            }
            else
            {
              return;
            }
          }
          else
          {
            var buffer = await FileIO.ReadBufferAsync(file);
            byte[] byteData = new byte[buffer.Length];
            using (var dataReader = DataReader.FromBuffer(buffer))
            {
              dataReader.ReadBytes(byteData);
            }

            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(fileStream);

            ImageAttachments.Add(new ImageAttachment()
            {
              BitmapImage = bitmapImage,
              MimeType = file.ContentType,
              ByteData = byteData,
              Filename = file.Name,
            });
          }
        }
      }
      OnPropertyChanged(nameof(ImageAttachments));
      OnPropertyChanged(nameof(ImageAttachmentsHeight));
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(MessageText))
      {
        return;
      }

      var attachments = new List<API.Commands.Types.Attachment>();
      if (ImageAttachments.Any())
      {
        //var responseRoot = await _mainPage.Get<API.Commands.Drive.ListDrives.Response>(new API.Commands.Drive.ListDrives());
        var responseRoot = await _mainPage.Get<API.Commands.Drive.GetDriveRootUser.Response>(new API.Commands.Drive.GetDriveRootUser() {
          userID = _mainPage.CurrentUserInfo.id
        });
        foreach (var attachment in ImageAttachments)
        {
          var responseFile = await _mainPage.Request<API.Commands.Drive.Upload.Response>("PUT", new API.Commands.Drive.Upload
          {
            filename = attachment.Filename,

            MimeType = attachment.MimeType,
            PostData = attachment.ByteData,
          });

          attachments.Add(new API.Commands.Types.Attachment()
          {
            id = responseFile.id,
            contentType = "reference",
            contentUrl = responseFile.webUrl,
            name = attachment.Filename,
          });
        }
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
      public ChatInlay Parent = null;
      public string ID => MessageData?.id;
      public string Sender => MessageData?.from?.user?.displayName ?? "[unknown]";
      public string SenderImageURL => MessageData?.from?.user?.AvatarURL == null ? string.Empty : Parent.App.Client.GraphEndpointRoot + MessageData?.from?.user?.AvatarURL;
      public string TimestampString => Timestamp.HasValue ? (Timestamp.Value.Date == DateTime.Now.Date ? Timestamp.Value.ToString("HH:mm") : Timestamp.Value.ToString("yyyy-MM-dd HH:mm")) : string.Empty;
      public DateTime? Timestamp => MessageData?.createdDateTime.ToLocalTime();
      public string Text
      {
        get
        {
          if (MessageData.from != null && MessageData?.body?.content != null)
          {
            return MessageData?.body?.content;
          }
          if (MessageData.eventDetail != null)
          {
            return $"<p>{MessageData.eventDetail.ToString()}</p>";
          }
          return "[unknown]";
        }
      }
      public RichTextControls.Generators.IHtmlXamlGenerator HTMLGenerator { get { return new TeamsHTMLGenerator(MessageData, Text); } }
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

    public class ImageAttachment : INotifyPropertyChanged
    {
      public BitmapImage BitmapImage { get; set; }
      public byte[] ByteData { get; set; }
      public string MimeType { get; set; }
      public string Filename { get; set; }
      public bool IsLoading { get; set; } = false;

      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Raises this object's PropertyChanged event.
      /// </summary>
      /// <param name="propertyName">The property that has a new value.</param>
      public virtual void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
      var b = sender as Button;
      var i = b?.DataContext as ImageAttachment;
      ImageAttachments.Remove(i);
      OnPropertyChanged(nameof(ImageAttachments));
      OnPropertyChanged(nameof(ImageAttachmentsHeight));
    }

    public static async Task<byte[]> ResizedImage(StorageFile imageFile, int maxWidth, int maxHeight)
    {
      using (var stream = await imageFile.OpenAsync(FileAccessMode.Read))
      {
        // Create the decoder from the stream
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var ratioX = maxWidth / (float)softwareBitmap.PixelWidth;
        var ratioY = maxHeight / (float)softwareBitmap.PixelHeight;
        var ratio = Math.Min(ratioX, ratioY);
        var newWidth = (uint)(softwareBitmap.PixelWidth * ratio);
        var newHeight = (uint)(softwareBitmap.PixelHeight * ratio);

        var writeStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, writeStream);

        encoder.SetSoftwareBitmap(softwareBitmap);
        encoder.BitmapTransform.ScaledWidth = newWidth;
        encoder.BitmapTransform.ScaledHeight = newHeight;
        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;

        await encoder.FlushAsync();

        writeStream.Seek(0);
        var readStream = writeStream.AsStreamForRead();
        byte[] byteData = new byte[readStream.Length];
        await readStream.ReadAsync(byteData, 0, (int)readStream.Length);

        return byteData;
      }
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
