using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace TeamsWP.Pages
{
  public sealed partial class LoginPage : Page
  {
    private App _app;

    public LoginPage()
    {
      InitializeComponent();
      _app = (App)Windows.UI.Xaml.Application.Current;
      DataContext = this;

      Loaded += LoginPage_Loaded;
    }

    private void LoginPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      webView.Navigate(new System.Uri(_app.Client.GetAuthenticationURL()));
      webView.NavigationStarting += WebView_NavigationStarting;
    }

    private async void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
    {
      try
      {
        var uri = args.Uri;
        if (await _app.Client.ProcessAuthURL(uri))
        {
          _app.NavigateToMainScreen(string.Empty);
        }
      }
      catch
      {
      }
    }

    private async void SaveAuth_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      var url = _app.Client.GetAuthenticationURL();

      var picker = new Windows.Storage.Pickers.FileSavePicker();
      picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
      picker.FileTypeChoices.Add("Plain text", new List<string>() { ".txt" });
      var file = await picker.PickSaveFileAsync();
      if (file != null)
      {
        await Windows.Storage.FileIO.WriteTextAsync(file, url);
      }
    }

    private async void LoadAuth_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      var picker = new Windows.Storage.Pickers.FileOpenPicker();
      picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
      picker.FileTypeFilter.Add(".txt");
      var file = await picker.PickSingleFileAsync();
      if (file != null)
      {
        var str = await Windows.Storage.FileIO.ReadTextAsync(file);
        if (await _app.Client.ProcessAuthURL(new Uri(str)))
        {
          _app.NavigateToMainScreen(string.Empty);
        }
      }
    }
  }
}
