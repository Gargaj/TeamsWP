using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace TeamsWP
{
  public class TeamsHTMLGenerator : RichTextControls.Generators.HtmlXamlGenerator
  {
    private API.Commands.Types.Message _messageData;

    public TeamsHTMLGenerator(API.Commands.Types.Message messageData) 
      : base(messageData?.body?.content)
    {
      _messageData = messageData;
    }

    public TeamsHTMLGenerator(AngleSharp.Dom.Html.IHtmlDocument document) 
      : base(document)
    {
    }

    public override UIElement Generate()
    {
      var result = base.Generate();

      Prune(result);

      return result;
    }

    private void Prune(UIElement element)
    {
      var panel = element as Panel;
      if (panel != null)
      {
        foreach(var item in panel.Children)
        {
          Prune(item);
        }
      }

      var richTextBlock = element as RichTextBlock;
      if (richTextBlock != null)
      {
        var toDelete = new List<Block>();
        foreach (var item in richTextBlock.Blocks)
        {
          if (PruneBlock(item))
          {
            toDelete.Add(item);
          }
        }
        foreach (var i in toDelete)
        {
          richTextBlock.Blocks.Remove(i);
        }
      }
    }

    private bool PruneBlock(Block block)
    {
      var paragraph = block as Paragraph;
      if (paragraph != null)
      {
        PruneInlines(paragraph.Inlines);
        if (paragraph.Inlines.Count == 0)
        {
          return true;
        }
      }
      return false;
    }

    private void PruneInlines(InlineCollection inlines)
    {
      var toDelete = new List<Inline>();
      foreach (var item in inlines)
      {
        if (PruneInline(item))
        {
          toDelete.Add(item);
        }
      }
      foreach (var i in toDelete)
      {
        inlines.Remove(i);
      }
    }

    private bool PruneInline(Inline inline)
    {
      var span = inline as Span;
      if (span != null)
      {
        PruneInlines(span.Inlines);
      }

      var run = inline as Run;
      if (run != null)
      {
        run.Text = run.Text.Trim();
        return string.IsNullOrEmpty(run.Text);
      }

      return false;
    }

    public override string PrepareRawHtml(string rawHtml)
    {
      var str = base.PrepareRawHtml(rawHtml);

      str = str.Replace("<p></p>\n", "");
      str = str.Replace("<p></p>", "");

      return str;
    }

    protected override UIElement GenerateImage(AngleSharp.Dom.Html.IHtmlImageElement node)
    {
      if (node.Source.StartsWith("https://graph.microsoft.com"))
      {
        return new Controls.TeamsImage()
        {
          TeamsURL = node.Source,
          Width = node.DisplayWidth,
          Height = node.DisplayHeight,
        };
      }

      return base.GenerateImage(node);
    }

    protected override Inline GenerateInlineForNode(AngleSharp.Dom.INode node, InlineCollection inlines)
    {
      switch (node.NodeName)
      {
        case "AT":
          {
            // TODO
          }
          break;
        case "EMOJI":
          {
            var element = node as AngleSharp.Dom.IElement;
            if (element != null)
            {
              var run = new Run();
              run.Text = element.Attributes["alt"].Value;
              return run;
            }
          }
          break;
      }
      return base.GenerateInlineForNode(node, inlines);
    }

    protected override UIElement GenerateUIElementForNode(AngleSharp.Dom.INode node, UIElementCollection elements)
    {
      switch (node.NodeName)
      {
        case "SYSTEMEVENTMESSAGE":
          {
          }
          break;
        case "ATTACHMENT":
          {
            var element = node as AngleSharp.Dom.IElement;
            if (element != null)
            {
              var attachment = _messageData.attachments.FirstOrDefault(s => s.id == element.Id);
              switch (attachment?.contentType)
              {
                case "messageReference":
                  {
                    var messageObject = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageReference>(attachment.content);

                    var brush = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["ButtonBorderThemeBrush"];
                    var background = Application.Current.Resources["TextBoxButtonForegroundThemeBrush"] as Windows.UI.Xaml.Media.SolidColorBrush;
                    background.Color = new Windows.UI.Color() { R = background.Color.R, G = background.Color.G, B = background.Color.B, A = 10 };
                    var border = new Border()
                    {
                      BorderBrush = brush,
                      BorderThickness = new Thickness(3, 1, 1, 1),
                      Background = background,
                      Padding = new Thickness(3),
                    };

                    var stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
                    border.Child = stackPanel;

                    stackPanel.Children.Add(new TextBlock() { Text = messageObject.messageSender.user.displayName, FontSize = 12 });
                    stackPanel.Children.Add(new TextBlock() { Text = messageObject.messagePreview });

                    return border;
                  }
                  break;
                case "application/vnd.microsoft.card.adaptive":
                  {
                  }
                  break;
              }
            }
          }
          break;
      }
      return base.GenerateUIElementForNode(node, elements);
    }

    public class MessageReference
    {
      public string messageId;
      public string messagePreview;
      public API.Commands.Types.From messageSender;
    }
  }
}
