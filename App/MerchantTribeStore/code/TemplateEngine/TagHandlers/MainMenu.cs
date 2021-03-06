﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MerchantTribe.Commerce;
using System.Text;

namespace MerchantTribeStore.code.TemplateEngine.TagHandlers
{
    public class MainMenu : ITagHandler
    {
        public class MainMenuViewModelLink
        {
            public string DisplayName { get; set; }
            public string Url { get; set; }
            public string AltText { get; set; }
            public int TabIndex { get; set; }
            public string Target {get;set;}
            public bool IsActive {get;set;}
        }

        public class MainMenuViewModel
        {
            public int LinksPerRow { get; set; }
            public int MaxLinks { get; set; }
            public List<MainMenuViewModelLink> Links { get; set; }

            public MainMenuViewModel()
            {
                LinksPerRow = 9;
                MaxLinks = 9;
                Links = new List<MainMenuViewModelLink>();
            }
        }

        public string TagName
        {
            get { return "sys:mainmenu"; }
        }

        public void Process(StringBuilder output, 
                            MerchantTribe.Commerce.MerchantTribeApplication app, 
                            dynamic viewBag,
                            ITagProvider tagProvider, 
                            ParsedTag tag, 
                            string innerContents)
        {
            
            int linksPerRow = 9;
            string tryLinksPerRow = tag.GetSafeAttribute("linksperrow");
            int temp1 = -1;
            if (int.TryParse(tryLinksPerRow, out temp1)) linksPerRow = temp1;
            if (linksPerRow < 1) linksPerRow = 1;

            int maxLinks = 9;
            int temp2 = -1;
            string tryMaxLinks = tag.GetSafeAttribute("maxlinks");
            if (int.TryParse(tryMaxLinks, out temp2)) maxLinks = temp2;
            if (maxLinks < 1) maxLinks = 1;

            int tabIndex = 0;
            string tryTabIndex = tag.GetSafeAttribute("tabindex");
            int.TryParse(tryTabIndex, out tabIndex);
            if (tabIndex < 0) tabIndex = 0;

            

            MainMenuViewModel model = new MainMenuViewModel();
            model.LinksPerRow = linksPerRow;
            model.MaxLinks = maxLinks;            

            //Find Categories to Display in Menu
            List<MerchantTribe.Commerce.Catalog.CategorySnapshot> categories = app.CatalogServices.Categories.FindForMainMenu();

            int tempTabIndex = 0;
            foreach (var c in categories)
            {
                var l = new MainMenuViewModelLink();
                l.AltText = c.MetaTitle;
                l.DisplayName = c.Name;
                l.TabIndex = tempTabIndex;
                l.Target = string.Empty;
                l.IsActive = false;
                l.Url = MerchantTribe.Commerce.Utilities.UrlRewriter.BuildUrlForCategory(c, app.CurrentRequestContext.RoutingContext);

                if (c.Bvin == SessionManager.CategoryLastId) l.IsActive = true;
                if (c.SourceType == MerchantTribe.Commerce.Catalog.CategorySourceType.CustomLink ||
                    c.SourceType == MerchantTribe.Commerce.Catalog.CategorySourceType.CustomPage)
                {
                    if (c.CustomPageOpenInNewWindow) l.Target = "_blank";
                }

                model.Links.Add(l);
                tempTabIndex += 1;
            }

            Render(output, model);            
        }

        private string Render(StringBuilder sb, MainMenuViewModel model)
        {           
            // Limit number of links
            int stopCount = model.Links.Count - 1;
            if ((model.MaxLinks > 0) && ((model.MaxLinks - 1) < stopCount))
            {
                stopCount = (model.MaxLinks - 1);
            }

            if (model.Links.Count > 0)
            {
                sb.Append("<ul>");
                 for (int i = 0; i <= stopCount; i++)
                 {
                     var link = model.Links[i];
                     sb.Append("<li><a href=\"" + link.Url + "\" title=\"" + HttpUtility.HtmlEncode(link.AltText) + "\"");
                     if(link.IsActive) { sb.Append(" class=\"activemainmenuitem\""); }
                     if (link.Target.Length > 0) { sb.Append(" target=\"" + link.Target + "\""); }
                     sb.Append("><span>" + HttpUtility.HtmlEncode(link.DisplayName) + "</span></a></li>");
                     
                     // Move to Next Row if Not Last Item
                     int endOfRowCount = (i + 1) % model.LinksPerRow;
                     if ((endOfRowCount == 0) && (i < stopCount))
                     {
                         sb.Append("</ul><ul>");
                     }
                 }
                sb.Append("</ul>");
            }
            return sb.ToString();
        }
    }
}