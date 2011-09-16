using System.Web.UI;
using BVSoftware.Commerce.Catalog;
using System.Linq;

namespace BVCommerce
{

    partial class BVAdmin_Catalog_OptionItems_Delete : BaseAdminJsonPage
    {

        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);


            if (!Page.IsPostBack)
            {
                string ids = Request.Form["id"];
                string optionId = Request.Form["optionid"];
                Remove(ids, optionId);
            }

        }

        private void Remove(string ids, string optionId)
        {
            Option opt = BVApp.CatalogServices.ProductOptions.Find(optionId);
            OptionItem item = opt.Items.Where(y => y.Bvin == ids).FirstOrDefault();
            if (item != null)
            {
                opt.Items.Remove(item);
                BVApp.CatalogServices.ProductOptions.Update(opt);
                this.litOutput.Text = "{\"result\":true}";
            }
            this.litOutput.Text = "{\"result\":false}";
        }

    }

}