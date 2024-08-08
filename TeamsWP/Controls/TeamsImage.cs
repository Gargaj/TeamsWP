using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace TeamsWP.Controls
{
  public class TeamsImage : Control
  {
    private Image _image;
    private Canvas _renderCanvas;

    /// <summary>
    /// Gets or sets the URL for the (protected) image from Teams
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
    /// Gets or sets the fallback text to generate a temporary thumbnail from if the image cannot be found
    /// </summary>
    public string FallbackText
    {
      get { return (string)GetValue(FallbackTextProperty); }
      set { SetValue(FallbackTextProperty, value); }
    }

    /// <summary>
    /// Gets the dependency property for <see cref="FallbackText"/>.
    /// </summary>
    public static readonly DependencyProperty FallbackTextProperty = DependencyProperty.Register(
        nameof(FallbackText),
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

    internal uint _updateCount = 0;
    /// <summary>
    /// Re-renders the HTML document when the property changes
    /// </summary>
    private static async void OnRenderingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = d as TeamsImage;
      if (control == null)
        return;

      control._updateCount++;

      if (control._updateCount >= 2)
      {
        await control.RenderDocument();
      }
    }

    public TeamsImage()
    {
    }

    protected override async void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      _image = GetTemplateChild("Image") as Image;
      _renderCanvas = GetTemplateChild("RenderCanvas") as Canvas;

      await RenderDocument();
    }

    private async Task<IRandomAccessStream> DownloadImage(string url)
    {
      if (!url.StartsWith("https://"))
      {
        return null;
      }
      var app = (App)Application.Current;

      var http = new API.HTTP();

      var headers = new NameValueCollection();
      headers["Authorization"] = $"Bearer {app.Client.CurrentAccountSettings.Credentials.AccessToken}";

      MemoryStream responseStream = null;
      try
      {
        responseStream = await http.DoHTTPRequestStreamAsync(url, new byte[] { }, headers, "GET");
      }
      catch (WebException ex)
      {
        var error = ex.Response != null ? await new StreamReader(ex.Response.GetResponseStream()).ReadToEndAsync() : ex.ToString();
        System.Diagnostics.Debug.WriteLine($"[TeamsImage Error]\nURL: {url}\nERROR: {ex.Status} ({(int)ex.Status}): {error}");
        return null;
      }

      if (responseStream == null)
      {
        return null;
      }

      return responseStream.AsRandomAccessStream();
    }

    private static Windows.UI.Xaml.Media.SolidColorBrush HSLBrush(float hue, float saturation, float lightness)
    {
      float r = lightness;   // default to gray
      float g = lightness;
      float b = lightness;
      float v = (lightness <= 0.5f) ? (lightness * (1.0f + saturation)) : (lightness + saturation - lightness * saturation);
      if (v > 0.0f)
      {
        float m = lightness + lightness - v;
        float sv = (v - m) / v;
        hue *= 6.0f;
        int sextant = (int)hue;
        float fract = hue - sextant;
        float vsf = v * sv * fract;
        float mid1 = m + vsf;
        float mid2 = v - vsf;
        switch (sextant)
        {
          case 0:
            r = v;
            g = mid1;
            b = m;
            break;
          case 1:
            r = mid2;
            g = v;
            b = m;
            break;
          case 2:
            r = m;
            g = v;
            b = mid1;
            break;
          case 3:
            r = m;
            g = mid2;
            b = v;
            break;
          case 4:
            r = mid1;
            g = m;
            b = v;
            break;
          case 5:
            r = v;
            g = m;
            b = mid2;
            break;
        }
      }
      byte r8 = Convert.ToByte(r * 255.0f);
      byte g8 = Convert.ToByte(g * 255.0f);
      byte b8 = Convert.ToByte(b * 255.0f);
      return new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, r8, g8, b8));
    }

    private async Task<IRandomAccessStream> GenerateTempImage(string fallbackText)
    {
      var hue = (uint)(fallbackText.GetHashCode()) / (float)uint.MaxValue;
      var firstLetters = string.Join("", fallbackText.Split(' ').Select(s => s.Substring(0, 1).ToUpper()).Where(s=>char.IsLetter(s[0]))).Substring(0, 2);
      
      var border = new Border()
      {
        Width = _renderCanvas.Width,
        Height = _renderCanvas.Height,
        VerticalAlignment = VerticalAlignment.Center,
      };
      border.Child = new TextBlock()
      {
        Text = firstLetters,
        Width = _renderCanvas.Width,
        Height = _renderCanvas.Height,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontWeight = Windows.UI.Text.FontWeights.Bold,
        FontSize = 50,
        Margin = new Thickness(0, 25, 0, 0),
        Foreground = HSLBrush(hue, 0.5f, 0.2f),
      };
      _renderCanvas.Background = HSLBrush(hue, 0.5f, 0.8f);
      _renderCanvas.Children.Add(border);

      var renderbmp = new RenderTargetBitmap();
      await renderbmp.RenderAsync(_renderCanvas, (int)_renderCanvas.Width, (int)_renderCanvas.Height);
      _renderCanvas.Children.Clear();

      var stream = new InMemoryRandomAccessStream();
      var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
      var pixels = await renderbmp.GetPixelsAsync();
      var reader = DataReader.FromBuffer(pixels);
      if (reader.UnconsumedBufferLength == 0)
      {
        return null;
      }
      byte[] bytes = new byte[reader.UnconsumedBufferLength];
      reader.ReadBytes(bytes);
      encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)renderbmp.PixelWidth, (uint)renderbmp.PixelHeight, 0, 0, bytes);
      await encoder.FlushAsync();

      return stream;
    }

    private static Dictionary<string, BitmapImageStatus> _cache = new Dictionary<string, BitmapImageStatus>();
    private string _lastSubbedURL = string.Empty;

    private async Task RenderDocument()
    {
      if (_image == null)
      {
        return;
      }

      var teamsURL = TeamsURL; // Store locally to avoid raciness
      var fallbackText = FallbackText; // Store locally to avoid raciness
      if (string.IsNullOrEmpty(teamsURL))
      {
        _image.Source = null;
        return;
      }

      _image.Tag = teamsURL;

      BitmapImage bitmapImage = null;
      bool needsDownload = false;
      lock (_cache)
      {
        // It's possible that the image gets "reused", so if it's already waiting on something, cancel that
        if (_cache.ContainsKey(_lastSubbedURL))
        {
          _cache[_lastSubbedURL].DownloadFinished -= OnUpdateImage;
          _lastSubbedURL = string.Empty;
        }

        if (_cache.ContainsKey(teamsURL))
        {
          // If image not downloaded, sign up for "download finished" event
          if (!_cache[teamsURL].IsDownloadFinished)
          {
            _image.Source = null;
            _cache[teamsURL].DownloadFinished += OnUpdateImage;
            _lastSubbedURL = teamsURL;
            return;
          }
          bitmapImage = _cache[teamsURL].BitmapImage;
        }
        else
        {
          // We're the first here, kick off a download (use a flag since we can't await in a lock)
          needsDownload = true;
          bitmapImage = new BitmapImage()
          {
            CreateOptions = BitmapCreateOptions.IgnoreImageCache
          };
          _cache.Add(teamsURL, new BitmapImageStatus()
          {
            URL = teamsURL,
            BitmapImage = bitmapImage,
            IsDownloadFinished = false
          });
        }
      }

      if (needsDownload)
      {
        // This should only happen on one request at per URL, since the lock() above takes care of that
        var stream = await DownloadImage(teamsURL);

        if (stream == null && !string.IsNullOrWhiteSpace(fallbackText))
        {
          stream = await GenerateTempImage(fallbackText);
        }

        lock (_cache)
        {
          // Download finished, notify images that we're good
          _cache[teamsURL].FinalizeDownload(stream);
        }
      }

      UpdateImage(bitmapImage);
    }

    private void OnUpdateImage(object s, BitmapImageStatusArgs e)
    {
      UpdateImage(e.BitmapImage);
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

    private class BitmapImageStatusArgs
    {
      public BitmapImage BitmapImage;
    }

    private class BitmapImageStatus
    {
      public string URL;
      public BitmapImage BitmapImage;
      public bool IsDownloadFinished;
      public event EventHandler<BitmapImageStatusArgs> DownloadFinished;
      public void FinalizeDownload(IRandomAccessStream stream)
      {
        IsDownloadFinished = true;

        if (stream != null)
        {
          BitmapImage.SetSource(stream);
          DownloadFinished?.Invoke(BitmapImage, new BitmapImageStatusArgs()
          {
            BitmapImage = BitmapImage
          });
        }
      }
    }
  }
}
