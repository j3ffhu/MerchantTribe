using System;
using System.Collections.ObjectModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using BVSoftware.Commerce;
using BVSoftware.Commerce.Catalog;
using BVSoftware.Commerce.Orders;
using BVSoftware.Commerce.Utilities;
using System.Collections.Generic;

namespace BVCommerce
{

    partial class Receipt : BaseStorePage
    {

        public override bool RequiresSSL
        {
            get { return true; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.AddBodyClass("store-receipt-page");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            WebForms.MakePageNonCacheable(this);
            if (!Page.IsPostBack)
            {
                this.Page.Title = "Order Receipt";
                BindGrids();
                LoadOrder();
            }
        }

        private void LoadOrder()
        {
            if (Request.Params["id"] != null)
            {
                Order o = BVApp.OrderServices.Orders.FindForCurrentStore(Request.Params["id"]);
                if (o != null)
                {
                    this.ViewOrder1.OrderBvin = o.bvin;
                    this.ViewOrder1.LoadOrder();

                    RenderAnalytics(o);
                }
            }
            else
            {
                this.lblOrderNumber.Text = "Order Number missing. Please contact an administrator.";
            }
        }

        private void RenderAnalytics(Order o)
        {

            // Reset Analytics for receipt page
            this.ViewData["analyticstop"] = string.Empty;

            // Add Tracker and Maybe Ecommerce Tracker to Top
            if (BVApp.CurrentStore.Settings.Analytics.UseGoogleTracker)
            {
                if (BVApp.CurrentStore.Settings.Analytics.UseGoogleEcommerce)
                {
                    // Ecommerce + Page Tracker
                    this.ViewData["analyticstop"] = BVSoftware.Commerce.Metrics.GoogleAnalytics.RenderLatestTrackerAndTransaction(
                        BVApp.CurrentStore.Settings.Analytics.GoogleTrackerId,
                        o,
                        BVApp.CurrentStore.Settings.Analytics.GoogleEcommerceStoreName,
                        BVApp.CurrentStore.Settings.Analytics.GoogleEcommerceCategory);
                }
                else
                {
                    // Page Tracker Only
                    this.ViewData["analyticstop"] = BVSoftware.Commerce.Metrics.GoogleAnalytics.RenderLatestTracker(BVApp.CurrentStore.Settings.Analytics.GoogleTrackerId);
                }
            }


            // Clear Bottom Analytics Tags
            this.ViewData["analyticsbottom"] = string.Empty;

            // Adwords Tracker at bottom if needed
            if (BVApp.CurrentStore.Settings.Analytics.UseGoogleAdWords)
            {
                this.ViewData["analyticsbottom"] = BVSoftware.Commerce.Metrics.GoogleAnalytics.RenderGoogleAdwordTracker(
                                                        o.TotalGrand,
                                                        BVApp.CurrentStore.Settings.Analytics.GoogleAdWordsId,
                                                        BVApp.CurrentStore.Settings.Analytics.GoogleAdWordsLabel,
                                                        BVApp.CurrentStore.Settings.Analytics.GoogleAdWordsBgColor,
                                                        Request.IsSecureConnection);
            }

            // Add Yahoo Tracker to Bottom if Needed
            if (BVApp.CurrentStore.Settings.Analytics.UseYahooTracker)
            {
                this.ViewData["analyticsbottom"] += BVSoftware.Commerce.Metrics.YahooAnalytics.RenderYahooTracker(
                    o, BVApp.CurrentStore.Settings.Analytics.YahooAccountId);
            }
        }

        private void BindGrids()
        {

            Order o;
            o = BVApp.OrderServices.Orders.FindForCurrentStore(Request.QueryString["id"]);

            if ((o.PaymentStatus == OrderPaymentStatus.Paid) && (o.StatusCode != OrderStatusCode.OnHold))
            {
                List<ProductFile> files = new List<ProductFile>();
                foreach (LineItem item in o.Items)
                {
                    if (item.ProductId != string.Empty)
                    {
                        List<ProductFile> productFiles = BVApp.CatalogServices.ProductFiles.FindByProductId(item.ProductId);
                        foreach (ProductFile file in productFiles)
                        {
                            files.Add(file);
                        }
                    }
                }

                if (files.Count > 0)
                {
                    DownloadsPanel.Visible = true;
                    FilesGridView.DataSource = files;
                    FilesGridView.DataBind();
                }
                else
                {
                    DownloadsPanel.Visible = false;
                }
            }
            else
            {
                DownloadsPanel.Visible = false;
            }
        }

        protected void FilesGridView_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                ImageButton DownloadImageButton = (ImageButton)e.Row.FindControl("DownloadImageButton");
                DownloadImageButton.ImageUrl = this.BVApp.ThemeManager().ButtonUrl("Download", Request.IsSecureConnection);
                DownloadImageButton.CommandArgument = e.Row.RowIndex.ToString();
            }
        }

        protected void FilesGridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            Order o;
            o = BVApp.OrderServices.Orders.FindForCurrentStore(Request.QueryString["id"]);

            string primaryKey = (string)((GridView)sender).DataKeys[e.NewEditIndex].Value;
            ProductFile file = BVApp.CatalogServices.ProductFiles.Find(primaryKey);

            int count = 0;
            string propertyKey = "file" + file.Bvin;
            if (o != null)
            {
                if (o.CustomProperties[propertyKey] != null)
                {
                    if (int.TryParse(o.CustomProperties[propertyKey].Value, out count))
                    {
                        count += 1;
                    }
                    else
                    {
                        count = 1;
                    }
                    o.CustomProperties[propertyKey].Value = count.ToString();
                }
                else
                {
                    count = 1;
                    o.CustomProperties.Add("bvsoftware", propertyKey, count.ToString());
                }
            }
            BVApp.OrderServices.Orders.Update(o);

            // Hack
            if ((file.MaxDownloads == 0))
            {
                file.MaxDownloads = 32000;
            }

            if (count > file.MaxDownloads)
            {
                MessageBox1.ShowError("File has been downloaded the maximum number of " + file.MaxDownloads + " times.");
                return;
            }

            if (file.AvailableMinutes != 0)
            {
                if (DateTime.UtcNow.AddMinutes(file.AvailableMinutes * -1) > o.TimeOfOrderUtc)
                {
                    MessageBox1.ShowError("File can no longer be downloaded. Its available time period has elapsed.");
                    return;
                }
            }

            if (!ViewUtilities.DownloadFile(file))
            {
                MessageBox1.ShowError("The file to download could not be found.");
            }

        }



    }
}