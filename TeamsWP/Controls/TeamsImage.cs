using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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

    private static Dictionary<string, BitmapImage> _cache = new Dictionary<string, BitmapImage>();

    private async Task RenderDocument()
    {
      if (_image == null)
      {
        return;
      }

      if (string.IsNullOrEmpty(TeamsURL))
      {
        return;
      }

      BitmapImage bitmapImage = null;
      bool needsDownload = false;
      lock (_cache)
      {
        if (_cache.ContainsKey(TeamsURL))
        {
          bitmapImage = _cache[TeamsURL];
        }
        else
        {
          bitmapImage = new BitmapImage();
          needsDownload = true;
          _cache.Add(TeamsURL, bitmapImage);
        }
      }
      if (needsDownload)
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
          return;
        }

        if (responseStream == null)
        {
          return;
        }

        bitmapImage.SetSource(responseStream.AsRandomAccessStream());
      }

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
      }
    }
  }
}
