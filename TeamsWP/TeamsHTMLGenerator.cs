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

    protected override Inline GenerateInlineForNode(AngleSharp.Dom.INode node, InlineCollection inlines)
    {
      switch (node.NodeName)
      {
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
  }
}
