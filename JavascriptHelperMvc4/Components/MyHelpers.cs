using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using NovelTheory.Component;

namespace JSHelperDemo.Components
{
	public static class MyHelpers
	{
		public static MvcHtmlString MessageBox(this HtmlHelper html, string id, 
								string Text, string title = "Error", string icon = "info")
		{
			var text = string.Format(@"<div id=""{0}"" title=""{1}"">	" +
									 @"<p><span class=""ui-icon ui-icon-{2}"" style=""float:left; margin:0 7px 20px 0;"">" +
									 @"</span>{3}</p></div>",
				id, title, icon, Text);

			var script = JavascriptHelper.Create(WebPageContext.Current);

			script.Std("dialog");
			script.AddOnReadyScript(@"$('#" + id + "').dialog({ autoOpen: false });");

			script.AddScript("MessageBox", "function OpenDiag(id){$('#'+id).dialog('open');}");

			return new MvcHtmlString(text);
		}
	}
}