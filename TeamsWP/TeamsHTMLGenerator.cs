using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace TeamsWP
{
  public class TeamsHTMLGenerator : RichTextControls.Generators.HtmlXamlGenerator
  {
    public TeamsHTMLGenerator(string html) 
      : base(html)
    {
    }

    public TeamsHTMLGenerator(AngleSharp.Dom.Html.IHtmlDocument document) 
      : base(document)
    {
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
        case "ATTACHMENT":
          {
            // TODO
          }
          break;
      }
      return base.GenerateUIElementForNode(node, elements);
    }
  }
}
