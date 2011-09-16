﻿<%@ Page Title="" Language="C#" MasterPageFile="~/signup/SignUp.master" AutoEventWireup="True" Inherits="BVCommerce.signup_PayPalOffer" Codebehind="PayPalOffer.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
    <script type="text/javascript" src="https://www.paypal-marketing.com/paypal/html/hosted/offer_date.js"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeroPlaceHolder" Runat="Server">
    <div class="superh1">
        <h1>
            Special PayPal Offer</h1>
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" Runat="Server">
<div class="col-2w-a">
    <a href="/signup/register/starter"><img src="/content/images/system/PayPalAd2.png" alt="Sign Up for a Free Store" /></a>    
    <div style="height:20px;"></div>
</div>
<div class="col-2w-b">
    <a href="/signup"><img src="/content/images/system/PayPalOfferAd.png" alt="Special Limited-Time PayPal Offer" /></a>
            <div id="expires">
                <table>
                <tr><td align="right">Offer Expires: </td><td align="left"><div id="date"></div></td></tr></table>
            </div>                        
</div>
<div class="clear"></div>  
    <div class="col-3-a">
        <div class="block" style="height: 300px;">
            <h2><a href="#" onclick="javascript:window.open('https://www.paypal.com/cgi-bin/webscr?cmd=xpt/Marketing/popup/OLCWhatIsPayPal-outside','olcwhatispaypal','toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=400, height=350');">
            <img  src="https://www.paypal.com/en_US/i/logo/PayPal_mark_60x38.gif" border="0" alt="Acceptance Mark"></a>&nbsp;
                <span class="hide">PayPal</span> Benefits</h2>
            <ul class="showbullets">
                <li>Small-to-medium-sized businesses get an average sales lift of 14% by accepting PayPal. (1)</li>
                <li>PayPal is one of the world's largest online payment services with 36% of online retailers offering the brand. (2)</li>
                <li>PayPal represents more than 1/2 the total online spend of active PayPal buyers (3)</li>
                <li>43% of North American online shoppers have used PayPal.(4)</li>
                <li>24% of North American online buyers consider PayPal their favorite way to pay online.(5)</li>
                <li>More than 140 million accounts in 190 countries </li>
            </ul>
        </div>
    </div>
    <div class="col-3-b">
        <div class="block" style="height: 300px;">
            <h3>
                BV Software Customers</h3>
            <p>
                Our professionals have worked with some of the best online retailers using our toolkit
                software and hosted services. Here are a few samples:</p>
            <img class="awards" src="/content/images/system/ClientLogos.png" alt="BV Software Client Sample"
                border="0" />
        </div>
    </div>
      <div class="col-3-c">
        <div class="block" style="height: 300px;">
            <h2>
                Grow a Store With BV Commerce</h2>
            <p>
                BV Software offers a complete range of shopping cart solutions from all-in-one hosted
                ecommerce to licensed software with source code. From hosted to custom carts, BV
                grows with your business.</p>
            <h2>
                How to Get Started</h2>
            <p>
                It only takes a minute to create a store with BV Commerce. Upload your logo, pick
                your colors and populate your products. You can try out your store for Free! There
                are no setup fees, no long term contracts, cancel at any time.</p>
            <a href="/signup">
                <img alt="Sign Up for Basic Plan" src="/content/images/system/SignUpSmall.png"></a><br />
            &nbsp;
        </div>
    </div>
    <div class="clear">
    </div>
    <div class="block">
        <div style="font-size: 10px;">
(1) Q1 2006 PayPal phone survey of 110 small and medium sized businesses doing a minimum of $120,000.00 USD in annual online sales.<br>
(2) Cybersource survey, as referenced in the Internet Retailer, October 9, 2006<br>
(3) AC Nielsen Q3Customer Relationship Assessment (Buyers); sample frame: used PayPal once in the past 3 months, twice in the past year.<br>
(4) Forrester Research, Inc. The Technographics Survey, April 2005 (sample: 5,051, +/- 1.4% margin of error).<br>
(5) AC Nielsen Q3 Customer Relationship Assessment (Buyers); sample frame: used PayPal once in past 3 months, twice in past year.&#8221;<br>
</div>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="EndOfForm" Runat="Server">
    <script>    offer_date('date');</script>
</asp:Content>
