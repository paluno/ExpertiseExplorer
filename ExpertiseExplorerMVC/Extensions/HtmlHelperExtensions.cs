namespace ExpertiseExplorer.MVC.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web.Mvc;
    using System.Web.Routing;

    using ExpertiseExplorer.MVC.Html;

    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString GetAlphabetLinks(this HtmlHelper html, string actionName, string controllerName, object routeValues, char active = 'a')
        {
            var urlHelper = new UrlHelper(html.ViewContext.RequestContext, html.RouteCollection);
            var divBuilder = new TagBuilder("div");

            var innerHtml = new StringBuilder();
            for (int i = 65; i < 90; i++)
            {
                var routeValueDictionary = new RouteValueDictionary(routeValues);
                var charValue = (char)i;
                routeValueDictionary.Add("active", charValue);

                var anchorBuilder = new TagBuilder("a");
                anchorBuilder.Attributes.Add("href", urlHelper.Action(actionName, controllerName, routeValueDictionary));

                anchorBuilder.InnerHtml = charValue.ToString().ToLower() == active.ToString().ToLower() ? string.Format("<b>{0}</b>", charValue) : charValue.ToString();

                innerHtml.Append(anchorBuilder);
            }

            divBuilder.InnerHtml = innerHtml.ToString();

            return new MvcHtmlString(divBuilder.ToString());
        }

        #region Table Stuff

        public static Table BeginTable(this HtmlHelper html, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("table");
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("border", "0", false);
            tagBuilder.MergeAttribute("cellpadding", "0", false);
            tagBuilder.MergeAttribute("cellspacing", "0", false);
            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new Table(html.ViewContext);
        }

        public static Table BeginTable(this HtmlHelper html, object htmlAttributes)
        {
            return html.BeginTable(new RouteValueDictionary(htmlAttributes));
        }

        public static Table BeginTable(this HtmlHelper html)
        {
            return html.BeginTable(null);
        }

        public static TableHead BeginTableHead(this HtmlHelper html, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("thead");
            tagBuilder.MergeAttributes(htmlAttributes);
            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new TableHead(html.ViewContext);
        }

        public static TableHead BeginTableHead(this HtmlHelper html, object htmlAttributes)
        {
            return html.BeginTableHead(new RouteValueDictionary(htmlAttributes));
        }

        public static TableHead BeginTableHead(this HtmlHelper html)
        {
            return html.BeginTableHead(null);
        }

        public static TableFoot BeginTableFoot(this HtmlHelper html, object htmlAttributes)
        {
            return html.BeginTableFoot(new RouteValueDictionary(htmlAttributes));
        }

        public static TableFoot BeginTableFoot(this HtmlHelper html)
        {
            return html.BeginTableFoot(null);
        }

        public static TableFoot BeginTableFoot(this HtmlHelper html, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("tfoot");
            tagBuilder.MergeAttributes(htmlAttributes);
            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new TableFoot(html.ViewContext);
        }

        public static TableBody BeginTableBody(this HtmlHelper html, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("tbody");
            tagBuilder.MergeAttributes(htmlAttributes);
            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new TableBody(html.ViewContext);
        }

        public static TableBody BeginTableBody(this HtmlHelper html, object htmlAttributes)
        {
            return html.BeginTableBody(new RouteValueDictionary(htmlAttributes));
        }

        public static TableBody BeginTableBody(this HtmlHelper html)
        {
            return html.BeginTableBody(null);
        }

        public static TableRow BeginTableRow(this HtmlHelper html, int itemIndex, int itemCount, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("tr");
            tagBuilder.MergeAttributes(htmlAttributes);

            var @class = AlternatingSiblingsCssClassesHelper(itemIndex, itemCount, tagBuilder.Attributes.ContainsKey("class") ? tagBuilder.Attributes["class"] : string.Empty);
            if (!string.IsNullOrEmpty(@class))
                tagBuilder.MergeAttribute("class", @class, true);

            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new TableRow(html.ViewContext);
        }

        public static TableRow BeginTableRow(this HtmlHelper html, int itemIndex, int itemCount, object htmlAttributes)
        {
            return html.BeginTableRow(itemIndex, itemCount, new RouteValueDictionary(htmlAttributes));
        }

        public static TableRow BeginTableRow(this HtmlHelper html, int itemIndex, int itemCount)
        {
            return html.BeginTableRow(itemIndex, itemCount, null);
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount, TableCellKind cellKind, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder(cellKind == TableCellKind.HeaderCell ? "th" : "td");
            tagBuilder.MergeAttributes(htmlAttributes);

            var @class = AlternatingSiblingsCssClassesHelper(itemIndex, itemCount, tagBuilder.Attributes.ContainsKey("class") ? tagBuilder.Attributes["class"] : string.Empty);
            if (!string.IsNullOrEmpty(@class))
                tagBuilder.MergeAttribute("class", @class, true);

            html.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            return new TableCell(html.ViewContext, cellKind);
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount, TableCellKind cellKind, object htmlAttributes)
        {
            return html.BeginTableCell(itemIndex, itemCount, cellKind, new RouteValueDictionary(htmlAttributes));
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount, IDictionary<string, object> htmlAttributes)
        {
            return html.BeginTableCell(itemIndex, itemCount, TableCellKind.StandardCell, htmlAttributes);
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount, object htmlAttributes)
        {
            return html.BeginTableCell(itemIndex, itemCount, new RouteValueDictionary(htmlAttributes));
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount, TableCellKind cellKind)
        {
            return html.BeginTableCell(itemIndex, itemCount, cellKind, null);
        }

        public static TableCell BeginTableCell(this HtmlHelper html, int itemIndex, int itemCount)
        {
            return html.BeginTableCell(itemIndex, itemCount, null);
        }

        #endregion

        #region Private Methods

        private static string AlternatingSiblingsCssClassesHelper(int itemIndex, int itemCount, string currentClasses)
        {
            var classes = new List<string>(currentClasses.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)));

            if (itemIndex == 0)
                classes.Add("first");

            if (itemIndex == itemCount - 1)
                classes.Add("last");

            if (itemCount > 1)
            {
                if (itemIndex < Math.Ceiling((double)itemCount / 2))
                {
                    classes.Add("first-half");

                    if (itemIndex == (int)Math.Floor((double)itemCount / 2))
                        classes.Add("last-half-floor");
                }
                else
                    classes.Add("last-half");
            }

            classes.Add(itemIndex % 2 == 0 ? "even" : "odd");

            return string.Join<string>(" ", classes.Distinct());
        }

        #endregion
    }
}
