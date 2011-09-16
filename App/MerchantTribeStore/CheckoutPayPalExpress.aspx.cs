﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BVSoftware.Commerce;
using BVSoftware.Commerce.Content;
using BVSoftware.Commerce.Orders;
using BVSoftware.Commerce.Utilities;
using BVSoftware.Commerce.Shipping;
using BVSoftware.PaypalWebServices;
using MerchantTribe.Web.Geography;
using com.paypal.soap.api;
using BVSoftware.Commerce.Catalog;

namespace BVCommerce
{

    public partial class CheckoutPayPalExpress : BaseStorePage
    {

        public override bool RequiresSSL
        {
            get { return true; }
        }

        protected override void OnInit(System.EventArgs e)
        {
            base.OnInit(e);
            this.Title = "Checkout";
        }

        protected void CheckoutImageButton_Click(object sender, System.Web.UI.ImageClickEventArgs e)
        {
            if (Page.IsValid)
            {
                if (!SiteTermsAgreement1.IsValid)
                {
                    MessageBox1.ShowError(SiteTerms.GetTerm(SiteTermIds.SiteTermsAgreementError));
                }
                else
                {
                    Order Basket = SessionManager.CurrentShoppingCart(BVApp.OrderServices);

                    // Save Shipping Selection
                    BVSoftware.Commerce.Shipping.ShippingRateDisplay r = FindSelectedRate(this.ShippingRatesList.SelectedValue, Basket);
                    BVApp.OrderServices.OrdersRequestShippingMethod(r, Basket);
                    BVApp.CalculateOrder(Basket);
                    SessionManager.SaveOrderCookies(Basket);
                    
                    // Save Payment Information
                    SavePaymentInfo(Basket);

                    BVApp.OrderServices.Orders.Update(Basket);

                    // Save as Order
                    BVSoftware.Commerce.BusinessRules.OrderTaskContext c 
                        = new BVSoftware.Commerce.BusinessRules.OrderTaskContext(BVApp);
                    c.UserId = SessionManager.GetCurrentUserId();
                    c.Order = Basket;

                    if (BVSoftware.Commerce.BusinessRules.Workflow.RunByName(c, BVSoftware.Commerce.BusinessRules.WorkflowNames.ProcessNewOrder))
                    {                                               
                        // Clear Cart ID because we're now an order
                        SessionManager.CurrentCartID = string.Empty;

                        // Process Payment
                        if (BVSoftware.Commerce.BusinessRules.Workflow.RunByName(c, BVSoftware.Commerce.BusinessRules.WorkflowNames.ProcessNewOrderPayments))
                        {                            
                            BVSoftware.Commerce.BusinessRules.Workflow.RunByName(c, BVSoftware.Commerce.BusinessRules.WorkflowNames.ProcessNewOrderAfterPayments);
                            Order tempOrder = BVApp.OrderServices.Orders.FindForCurrentStore(Basket.bvin);
                            BVSoftware.Commerce.Integration.Current().OrderReceived(tempOrder, BVApp);                            
                            Response.Redirect("~/Receipt.aspx?id=" + Basket.bvin);
                        }
                        else
                        {
                            // Redirect to Payment Error
                            SessionManager.CurrentPaymentPendingCartId = Basket.bvin;
                            Response.Redirect("~/CheckoutPaymentError.aspx");
                        }                        
                    }
                    else
                    {
                        // Show Errors                
                        foreach (BVSoftware.Commerce.BusinessRules.WorkflowMessage item in c.GetCustomerVisibleErrors())
                        {
                            MessageBox1.ShowError(item.Description);
                        }
                    }
                }
            }
        }

        private void DisplayPaypalExpressMode()
        {
            if ((Request.QueryString["token"] != null) && (Request.QueryString["token"] != string.Empty))
            {
                PayPalAPI ppAPI = BVSoftware.Commerce.Utilities.PaypalExpressUtilities.GetPaypalAPI();
                bool failed = false;
                GetExpressCheckoutDetailsResponseType ppResponse = null;
                try
                {
                    if (!GetExpressCheckoutDetails(ppAPI, ref ppResponse))
                    {
                        if (!GetExpressCheckoutDetails(ppAPI, ref ppResponse))
                        {
                            failed = true;
                            EventLog.LogEvent("Paypal Express Checkout", "GetExpressCheckoutDetails call failed. Detailed Errors will follow. ", BVSoftware.Commerce.Metrics.EventLogSeverity.Error);
                            foreach (ErrorType ppError in ppResponse.Errors)
                            {
                                EventLog.LogEvent("Paypal error number: " + ppError.ErrorCode, "Paypal Error: '" + ppError.ShortMessage + "' Message: '" + ppError.LongMessage + "' " + " Values passed to GetExpressCheckoutDetails: Token: " + Request.QueryString["token"], BVSoftware.Commerce.Metrics.EventLogSeverity.Error);
                            }
                            MessageBox1.ShowError("An error occurred during the Paypal Express checkout. No charges have been made. Please try again.");
                            CheckoutImageButton.Visible = false;
                        }
                    }
                }
                finally
                {
                    Order o = SessionManager.CurrentShoppingCart(BVApp.OrderServices);
                    EditAddressLinkButton.Visible = true;
                    if (o.CustomProperties["PaypalAddressOverride"] != null)
                    {
                        if (o.CustomProperties["PaypalAddressOverride"].Value == "1")
                        {
                            EditAddressLinkButton.Visible = false;
                        }
                    }

                    o.CustomProperties.Add("bvsoftware", "PayerID", Request.QueryString["PayerID"]);
                    if (!failed)
                    {
                        if (ppResponse != null && ppResponse.GetExpressCheckoutDetailsResponseDetails != null && ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo != null)
                        {
                            o.UserEmail = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Payer;
                            if (string.IsNullOrEmpty(o.ShippingAddress.Phone))
                            {
                                o.ShippingAddress.Phone = ppResponse.GetExpressCheckoutDetailsResponseDetails.ContactPhone;
                            }
                        }
                    }
                    BVApp.OrderServices.Orders.Update(o);
                    ppAPI = null;
                }
            }
            else
            {
                Response.Redirect(BVApp.CurrentStore.RootUrl());
            }

        }

        protected bool GetExpressCheckoutDetails(PayPalAPI ppAPI, ref GetExpressCheckoutDetailsResponseType ppResponse)
        {
            ppResponse = ppAPI.GetExpressCheckoutDetails(Request.QueryString["token"]);
            if (ppResponse.Ack == AckCodeType.Success || ppResponse.Ack == AckCodeType.SuccessWithWarning)
            {
                EmailLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Payer;
                FirstNameLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.PayerName.FirstName;
                if (ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.PayerName.MiddleName.Length > 0)
                {
                    MiddleInitialLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.PayerName.MiddleName.Substring(0, 1);
                }
                else
                {
                    MiddleInitialLabel.Text = string.Empty;
                }
                LastNameLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.PayerName.LastName;
                CompanyLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.PayerBusiness;
                StreetAddress1Label.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.Street1;
                StreetAddress2Label.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.Street2;
                CountryLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.CountryName;
                ViewState["CountryCode"] = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.Country.ToString();
                CityLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.CityName;
                StateLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.StateOrProvince;
                ZipLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.PostalCode;
                PhoneNumberLabel.Text = ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.ContactPhone;

                if (ppResponse.GetExpressCheckoutDetailsResponseDetails.PayerInfo.Address.AddressStatus == AddressStatusCodeType.Confirmed)
                {
                    AddressStatusLabel.Text = "Confirmed";
                }
                else
                {
                    AddressStatusLabel.Text = "Unconfirmed";
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            BVSoftware.Commerce.Utilities.WebForms.MakePageNonCacheable(this);
            this.CheckoutImageButton.Visible = true;
            if (!Page.IsPostBack)
            {
                ThemeManager themes = BVApp.ThemeManager();
                CheckoutImageButton.ImageUrl = themes.ButtonUrl("PlaceOrder", Request.IsSecureConnection);
                this.btnKeepShopping.ImageUrl = themes.ButtonUrl("keepshopping", Request.IsSecureConnection);
                DisplayPaypalExpressMode();
                LoadShippingMethodsForOrder();
            }
        }

        private void LoadShippingMethodsForOrder()
        {
            Order o = SessionManager.CurrentShoppingCart(BVApp.OrderServices);

            BVSoftware.Commerce.Contacts.Address address = GetAddress();
            if ((address != null))
            {
                o.ShippingAddress = address;
                o.BillingAddress = address;
                BVApp.CalculateOrderAndSave(o);
                SessionManager.SaveOrderCookies(o);
            }

            LoadShippingMethodsForOrder(o);
        }
        private void LoadShippingMethodsForOrder(Order o)
        {

            SortableCollection<ShippingRateDisplay> Rates = new SortableCollection<ShippingRateDisplay>();

            if (o.HasShippingItems == false)
            {
                ShippingRateDisplay r = new ShippingRateDisplay();
                r.DisplayName = SiteTerms.GetTerm(SiteTermIds.NoShippingRequired);
                r.ProviderId = "";
                r.ProviderServiceCode = "";
                r.Rate = 0;
                r.ShippingMethodId = "NOSHIPPING";
                Rates.Add(r);
            }
            else
            {
                // Shipping Methods

                Rates = BVApp.OrderServices.FindAvailableShippingRates(o);

                if ((Rates.Count < 1))
                {
                    ShippingRateDisplay result = new ShippingRateDisplay();
                    result.DisplayName = "Shipping can not be calculated at this time. We will contact you after receiving your order with the exact shipping charges.";
                    result.ShippingMethodId = "TOBEDETERMINED";
                    result.Rate = 0;
                    Rates.Add(result);
                }

            }

            // Shipping Methods
            SessionManager.LastShippingRates = Rates;
            this.ShippingRatesList.DataTextField = "RateAndNameForDisplay";
            this.ShippingRatesList.DataValueField = "UniqueKey";
            this.ShippingRatesList.DataSource = Rates;
            this.ShippingRatesList.DataBind();
            //this.litMain.Text = BVSoftware.Commerce.Utilities.HtmlRendering.ShippingRatesToRadioButtons(Rates, this.TabIndex, o.ShippingMethodUniqueKey);
        }

        private BVSoftware.Commerce.Contacts.Address GetAddress()
        {
            BVSoftware.Commerce.Contacts.Address a = new BVSoftware.Commerce.Contacts.Address();
            Country country = Country.FindByISOCode((string)ViewState["CountryCode"]);
            if (country.Bvin == string.Empty)
            {
                MessageBox1.ShowError("Could not retreive address properly, country could not be found.");
                CheckoutImageButton.Enabled = false;
            }
            else
            {
                CheckoutImageButton.Enabled = true;
            }

            //if (!country.Active) {
            //    MessageBox1.ShowError("This country is not active for this store.");
            //    CheckoutImageButton.Enabled = false;
            //}
            //else {
            CheckoutImageButton.Enabled = true;
            //}

            if (country.Bvin != string.Empty)
            {
                a.CountryBvin = country.Bvin;
                a.CountryName = country.DisplayName;
                a.RegionName = StateLabel.Text;
                foreach (Region region in Country.FindByBvin(country.Bvin).Regions)
                {
                    if ((string.Compare(region.Abbreviation, a.RegionName, true) == 0) || (string.Compare(region.Name, a.RegionName, true) == 0))
                    {
                        a.RegionBvin = region.Abbreviation;
                        a.RegionName = region.Name;
                    }
                }
                a.FirstName = FirstNameLabel.Text;
                a.MiddleInitial = MiddleInitialLabel.Text;
                a.LastName = LastNameLabel.Text;
                a.Company = CompanyLabel.Text;
                a.Line1 = StreetAddress1Label.Text;
                a.Line2 = StreetAddress2Label.Text;
                a.City = CityLabel.Text;
                a.PostalCode = ZipLabel.Text;
                a.Phone = PhoneNumberLabel.Text;
                a.Fax = "";
                a.WebSiteUrl = "";
                return a;
            }
            else
            {
                return null;
            }
        }

        private BVSoftware.Commerce.Shipping.ShippingRateDisplay FindSelectedRate(string uniqueKey, Order o)
        {
            BVSoftware.Commerce.Shipping.ShippingRateDisplay result = null;

            BVSoftware.Commerce.Utilities.SortableCollection<BVSoftware.Commerce.Shipping.ShippingRateDisplay> rates = SessionManager.LastShippingRates;
            if ((rates == null) | (rates.Count < 1))
            {
                rates = BVApp.OrderServices.FindAvailableShippingRates(o);
            }

            foreach (BVSoftware.Commerce.Shipping.ShippingRateDisplay r in rates)
            {
                if (r.UniqueKey == uniqueKey)
                {
                    result = r;
                    break;
                }
            }

            return result;
        }

        private void SavePaymentInfo(Order o)
        {
            OrderPaymentManager payManager = new OrderPaymentManager(o, BVApp);
            payManager.ClearAllTransactions();

            string token = Request.QueryString["Token"];
            string payerId = Request.QueryString["PayerId"];
            if (!string.IsNullOrEmpty(payerId))
            {
                // This is to fix a bug with paypal returning multiple payerId's
                payerId = payerId.Split(',')[0];
            }

            payManager.PayPalExpressAddInfo(o.TotalGrand, token, payerId);
        }

        protected void CustomValidator1_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
        {
            if (!BVApp.CurrentStore.Settings.PayPal.AllowUnconfirmedAddresses)
            {
                if (string.Compare(AddressStatusLabel.Text, "Unconfirmed", true) == 0)
                {
                    args.IsValid = false;
                }
                else
                {
                    args.IsValid = true;
                }
            }
        }

        protected void btnKeepShopping_Click(object sender, System.Web.UI.ImageClickEventArgs e)
        {
            string destination = "~";

            if (SessionManager.CategoryLastId != string.Empty)
            {
                BVSoftware.Commerce.Catalog.Category c = BVApp.CatalogServices.Categories.Find(SessionManager.CategoryLastId);
                if (c != null)
                {
                    if (c.Bvin != string.Empty)
                    {
                        destination = BVSoftware.Commerce.Utilities.UrlRewriter.BuildUrlForCategory(new CategorySnapshot(c), BVApp.CurrentRequestContext.RoutingContext);
                    }
                }
            }

            Response.Redirect(destination);
        }

        protected void EditAddressLinkButton_Click(object sender, System.EventArgs e)
        {
            Order o = SessionManager.CurrentShoppingCart(BVApp.OrderServices);

            PayPalAPI ppAPI = BVSoftware.Commerce.Utilities.PaypalExpressUtilities.GetPaypalAPI();
            try
            {
                string cartReturnUrl = BVApp.CurrentStore.RootUrlSecure() + "paypalexpresscheckout";
                string cartCancelUrl = BVApp.CurrentStore.RootUrlSecure() + "checkout";

                SetExpressCheckoutResponseType expressResponse;
                if (BVApp.CurrentStore.Settings.PayPal.ExpressAuthorizeOnly)
                {
                    expressResponse = ppAPI.SetExpressCheckout(string.Format("{0:N}", o.TotalOrderBeforeDiscounts), cartReturnUrl, cartCancelUrl, PaymentActionCodeType.Order, PayPalAPI.GetCurrencyCodeType(BVApp.CurrentStore.Settings.PayPal.Currency), o.OrderNumber);
                }
                else
                {
                    expressResponse = ppAPI.SetExpressCheckout(string.Format("{0:N}", o.TotalOrderBeforeDiscounts), cartReturnUrl, cartCancelUrl, PaymentActionCodeType.Sale, PayPalAPI.GetCurrencyCodeType(BVApp.CurrentStore.Settings.PayPal.Currency), o.OrderNumber);
                }

                if (expressResponse.Ack == AckCodeType.Success || expressResponse.Ack == AckCodeType.SuccessWithWarning)
                {
                    o.ThirdPartyOrderId = expressResponse.Token;
                    if (BVApp.OrderServices.Orders.Update(o))
                    {
                        if (string.Compare(BVApp.CurrentStore.Settings.PayPal.Mode, "Live", true) == 0)
                        {
                            System.Web.HttpContext.Current.Response.Redirect("https://www.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=" + expressResponse.Token, false);
                        }
                        else
                        {
                            System.Web.HttpContext.Current.Response.Redirect("https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=" + expressResponse.Token, false);
                        }

                    }
                }
                else
                {
                    foreach (ErrorType ppError in expressResponse.Errors)
                    {
                        EventLog.LogEvent("Paypal error number: " + ppError.ErrorCode, "Paypal Error: '" + ppError.ShortMessage + "' Message: '" + ppError.LongMessage + "' " + " Values passed to SetExpressCheckout: Total=" + string.Format("{0:c}", o.TotalOrderBeforeDiscounts) + " Cart Return Url: " + cartReturnUrl + " Cart Cancel Url: " + cartCancelUrl, BVSoftware.Commerce.Metrics.EventLogSeverity.Error);
                    }
                }
            }
            finally
            {
                ppAPI = null;
            }
        }

        protected void BVRequiredFieldValidator1_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
        {
            if ((this.ShippingRatesList.SelectedIndex == -1))
            {
                args.IsValid = false;
            }
            else
            {
                args.IsValid = true;
            }
        }
    }
}