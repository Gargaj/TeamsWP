using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TeamsWP.Pages
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public partial class MainPage : Page, INotifyPropertyChanged
  {
    private App _app;
    private API.Commands.User.Me.Response _currentUserInfo;

    private uint _isLoading = 0;
    private bool _hasError = false;
    private string _errorText = string.Empty;

    public MainPage()
    {
      InitializeComponent();
      _app = (App)Windows.UI.Xaml.Application.Current;
      DataContext = this;

      Loaded += MainPage_Loaded;
    }

    public void StartLoading() { _isLoading++; OnPropertyChanged(nameof(IsLoading)); }
    public void EndLoading() { _isLoading--; OnPropertyChanged(nameof(IsLoading)); }
    public bool IsLoading { get { return _isLoading > 0; } }

    public bool HasError { get { return _hasError; } set { _hasError = value; OnPropertyChanged(nameof(HasError)); } }
    public string ErrorText { get { return _errorText; } set { _errorText = value; OnPropertyChanged(nameof(ErrorText)); } }

    public API.Commands.User.Me.Response CurrentUserInfo => _currentUserInfo;

    private async void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      StartLoading();

      await API.Emoji.Load();

      _currentUserInfo = await Get<API.Commands.User.Me.Response>(new API.Commands.User.Me());

      EndLoading();
    }

    private string _chatInlayID;
    private async void Main_PivotItemLoading(Pivot sender, PivotItemEventArgs args)
    {
      var inlay = args.Item.ContentTemplateRoot as Inlays.IInlay;
      if (inlay != null)
      {
        var chatInlay = inlay as Inlays.ChatInlay;
        if (chatInlay != null)
        {
          chatInlay.ID = _chatInlayID;
        }

        await inlay.Refresh();
      }
    }

    private void Main_PivotItemUnloading(Pivot sender, PivotItemEventArgs args)
    {
      var inlay = args.Item.ContentTemplateRoot as Inlays.IInlay;
      if (inlay != null)
      {
        inlay.Flush();
      }
    }

    public async Task ViewChat(string id)
    {
      if (MainMenu.SelectedItem != ChatPivot)
      {
        _chatInlayID = id;
        MainMenu.SelectedItem = ChatPivot;
      }
      else
      {
        var chatInlay = ChatPivot.ContentTemplateRoot as Inlays.ChatInlay;
        chatInlay.ID = id;
        await chatInlay.Refresh();
      }
    }

    public void TriggerError(string error)
    {
      HasError = true;
      ErrorText = error;
    }

    private void CloseErrorPopup_Click(object sender, RoutedEventArgs e)
    {
      HasError = false;
      ErrorText = string.Empty;
    }

    public async Task<T> Get<T>(API.ICommand input) where T : class, API.IResponse
    {
      return await Request<T>("GET", input);
    }

    public async Task<T> Post<T>(API.ICommand input) where T : class, API.IResponse
    {
      return await Request<T>("POST", input);
    }

    public async Task<T> Request<T>(string method, API.ICommand input) where T : class, API.IResponse
    {
      try
      {
        return await _app.Client.RequestAsync<T>(method, input);
      }
      catch (WebException ex)
      {
        var webResponse = ex.Response as HttpWebResponse;
        if (webResponse != null)
        {
          TriggerError($"HTTP ERROR {(int)webResponse.StatusCode}\n\n{ex.Message}");
        }
        else
        {
          TriggerError($"ERROR\n\n{ex?.InnerException?.Message ?? ex?.Message}");
        }
      }
      catch (Exception ex)
      {
        TriggerError($"ERROR\n{ex?.InnerException?.Message ?? ex?.Message}");
      }
      return null;
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
