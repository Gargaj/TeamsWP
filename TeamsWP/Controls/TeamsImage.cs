using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace TeamsWP.Controls
{
  public class TeamsImage : Control
  {
    private Image _image;

    /// <summary>
    /// Gets or sets the style for the blockquote Border
    /// </summary>
    public string TeamsURL
    {
      get { return (string)GetValue(TeamsURLProperty); }
      set { SetValue(TeamsURLProperty, value); }
    }

    /// <summary>
    /// Gets the dependency property for <see cref="TeamsURL"/>.
    /// </summary>
    public static readonly DependencyProperty TeamsURLProperty = DependencyProperty.Register(
        nameof(TeamsURL),
        typeof(string),
        typeof(TeamsImage),
        new PropertyMetadata(null, OnRenderingPropertyChanged)
    );

    /// <summary>
    /// The root child containing the parsed and rendered HTML.
    /// </summary>
    public UIElement Child
    {
      get
      {
        return _image;
      }
    }

    /// <summary>
    /// Re-renders the HTML document when the property changes
    /// </summary>
    private static async void OnRenderingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = d as TeamsImage;
      if (control == null)
        return;

      await control.RenderDocument();
    }

    public TeamsImage()
    {
    }

    protected override async void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      _image = GetTemplateChild("Image") as Image;

      await RenderDocument();
    }

    private async Task<IRandomAccessStream> DownloadImage()
    {
      var app = (App)Application.Current;

      var http = new API.HTTP();

      var headers = new NameValueCollection();
      headers["Authorization"] = $"Bearer {app.Client.CurrentAccountSettings.Credentials.AccessToken}";

      MemoryStream responseStream = null;
      try
      {
        responseStream = await http.DoHTTPRequestStreamAsync(TeamsURL, new byte[] { }, headers, "GET");
      }
      catch (WebException ex)
      {
        //var error = ex.Response != null ? await new StreamReader(ex.Response.GetResponseStream()).ReadToEndAsync() : ex.ToString();
        return null;
      }

      if (responseStream == null)
      {
        return null;
      }

      return responseStream.AsRandomAccessStream();
    }

    private static Dictionary<string, BitmapImageStatus> _cache = new Dictionary<string, BitmapImageStatus>();

    private async Task RenderDocument()
    {
      if (_image == null)
      {
        return;
      }

      var teamsURL = TeamsURL; // Store locally to avoid raciness
      if (string.IsNullOrEmpty(teamsURL))
      {
        return;
      }

      BitmapImage bitmapImage = null;
      bool needsDownload = false;
      lock (_cache)
      {
        if (_cache.ContainsKey(teamsURL))
        {
          // If image not downloaded, sign up for "download finished" event
          bitmapImage = _cache[teamsURL].BitmapImage;
          if (!_cache[teamsURL].IsDownloadFinished)
          {
            _cache[teamsURL].DownloadFinished += OnUpdateImage;
          }
        }
        else
        {
          // We're the first here, kick off a download (use a flag since we can't await in a lock)
          needsDownload = true;
          bitmapImage = new BitmapImage();
          _cache.Add(teamsURL, new BitmapImageStatus() {
            BitmapImage = bitmapImage,
            IsDownloadFinished = false
          });
        }
      }

      if (needsDownload)
      {
        // This should only happen on one request at per url, since the lock() above takes care of that
        var stream = await DownloadImage();
        if (stream != null)
        {
          await bitmapImage.SetSourceAsync(stream);
        }

        lock (_cache)
        {
          // Download finished, notify images that we're good
          _cache[teamsURL].MarkDownloadAsFinished(bitmapImage);
        }
      }

      UpdateImage(bitmapImage);
    }

    private void OnUpdateImage(object s, EventArgs e)
    {
      UpdateImage(s as BitmapImage);
    }

    private void UpdateImage(BitmapImage bitmapImage)
    {
      _image.Source = bitmapImage;

      if (bitmapImage.PixelWidth != 0 && bitmapImage.PixelHeight != 0)
      {
        if (Width == 0 && Height != 0)
        {
          _image.Width = Width = bitmapImage.PixelWidth / (bitmapImage.PixelHeight / Height);
          _image.Height = Height;
        }
        else if (Width != 0 && Height == 0)
        {
          _image.Width = Width;
          _image.Height = Height = bitmapImage.PixelHeight / (bitmapImage.PixelWidth / Width);
        }
        else if (Width != 0 && Height != 0)
        {
          _image.Width = Width;
          _image.Height = Height;
        }
      }
    }

    private class BitmapImageStatus
    {
      public BitmapImage BitmapImage;
      public bool IsDownloadFinished;
      public event EventHandler DownloadFinished;
      public void MarkDownloadAsFinished(BitmapImage bitmapImage)
      {
        IsDownloadFinished = true;
        DownloadFinished?.Invoke(bitmapImage, EventArgs.Empty);
      }
    }
  }
}
