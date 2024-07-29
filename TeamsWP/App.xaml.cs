using System;
using TeamsWP.API;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TeamsWP
{
  public sealed partial class App : Application
  {
    private API.Client _client = new API.Client();
    private Frame _rootFrame;

    public App()
    {
      InitializeComponent();
      Suspending += OnSuspending;
    }

    public Client Client => _client;

    protected async override void OnLaunched(LaunchActivatedEventArgs e)
    {
      _rootFrame = Window.Current.Content as Frame;

      // Do not repeat app initialization when the Window already has content,
      // just ensure that the window is active
      if (_rootFrame == null)
      {
        // Create a Frame to act as the navigation context and navigate to the first page
        _rootFrame = new Frame();

        _rootFrame.NavigationFailed += OnNavigationFailed;

        if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
        {
          //TODO: Load state from previously suspended application
        }

        // Place the frame in the current Window
        Window.Current.Content = _rootFrame;
      }

      if (e.PrelaunchActivated == false)
      {
        if (_rootFrame.Content == null)
        {
          await _client.Settings.ReadSettings();
          if (_client.IsAuthenticated)
          {
            _rootFrame.Navigate(typeof(Pages.MainPage), e.Arguments);
          }
          else
          {
            _rootFrame.Navigate(typeof(Pages.LoginPage), e.Arguments);
          }
        }
        // Ensure the current window is active
        Window.Current.Activate();
      }
    }

    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
      throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
      var deferral = e.SuspendingOperation.GetDeferral();
      deferral.Complete();
    }

    public T GetCurrentFrame<T>() where T : Page
    {
      return _rootFrame.Content as T;
    }

    public void NavigateToMainScreen(string arguments)
    {
      if (_rootFrame == null)
      {
        return;
      }

      _rootFrame.Navigate(typeof(Pages.MainPage), arguments);
    }

    public void NavigateToLoginScreen(string arguments)
    {
      if (_rootFrame == null)
      {
        return;
      }

      _rootFrame.Navigate(typeof(Pages.LoginPage), arguments);
    }
  }
}
