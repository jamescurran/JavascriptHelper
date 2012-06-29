using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace JavascriptHelperMvc4
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			// The next two lines (and the /App_Start/BundleConfig.cs file)
			// are only needed for the standard Wizard-generated bundling.
			BundleTable.EnableOptimizations = true;
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}
	}
}