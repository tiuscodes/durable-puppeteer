namespace DurablePuppeteer.CustomTransforms
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;

    class ExtractPropertyGuruLandSizeTransformation : OpenScraping.Transformations.ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            if (node != null)
            {
                var text = node.InnerText;
                if (text.Contains("m"))
                {
                    text = text.Substring(0, text.IndexOf("m"));
                    string noHTML = Regex.Replace(text, @"<[^>]+>|&nbsp;", string.Empty).Trim();
                    return noHTML;
                }
            }

            return null;
        }
    }
}
