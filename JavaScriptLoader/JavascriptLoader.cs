#region Apache License
/*
 * Copyright 2012, James M. Curran
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
	   http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */  
#endregion
#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Web.Caching;
using System.Text;

using System.Web.Optimization;

using HtmlHelper = System.Web.Mvc.HtmlHelper;
#endregion

namespace NovelTheory.Components
{
	public class JavascriptLoader
	{
		#region Private Fields
		HttpServerUtilityBase srv;
		readonly HttpContextBase httpcontext;

		private XmlDocument libraryInfo = null;
		private readonly Cache cacheProvider = null;
		private readonly string viewPath = null;
		private const string Xmlfilename = @"jslibraries.xml";
		private const string Detailskeyword = @"jslibrariesDetails";
		private const string Csskeyword = @"jslibrariesCssDetails";

		private readonly string bundleFile;

		// Originally, when I wrote this for Castle Monorail, the component would
		// work without an jslibraries.xml file, using just these js libraries,
		// which were the standard used by Monorail.  I'd planned to update this
		// to the standard libraries used by ASP.NET MVC (jQuery, Modernizr, Knockout etc)
		// but they all embed the version number into the filename (e.g. "jquery-1.6.1.js"),
		// and I absolutely REFUSE to hard-code a version number into my code, so we'll 
		// just remove this, and make the lack of the library file an error.
		// JMC/11-Oct-2013
		//private readonly LibraryDetail[] libraryDefaults = new LibraryDetail[]
		//{
		//	new LibraryDetail("prototype"),
		//	new LibraryDetail("scriptaculous", "prototype", "effects2"),
		//	new LibraryDetail("effectsfat", "prototype"),
		//	new LibraryDetail("validate", "prototype"),
		//	new LibraryDetail("behavior", "prototype", "behaviour")
		//};

		#endregion

		#region Construction
		/// <summary>
		/// Creates the specified page.
		/// </summary>
		/// <returns>JavascriptHelper</returns>
		/// <remarks>
		/// Used in view pages functions:
		/// 	var script = StateTheaterMvc.Component.JavascriptHelper.Create(this);
		/// </remarks>
		public static JavascriptLoader Create(WebViewPage page)
		{
			return Create(page.ViewContext);
		}

		public static JavascriptLoader Create(HtmlHelper html)
		{
			return Create(html.ViewContext);
		}

		/// <summary>
		/// Creates the specified web page context.
		/// </summary>
		/// <param name="webPageContext">The web page context.</param>
		/// <returns>JavascriptHelper</returns>
		/// <remarks>
		/// Used in @helper functions:
		/// 	var script = StateTheaterMvc.Component.JavascriptHelper.Create(WebPageContext.Current);
		/// </remarks>
		public static JavascriptLoader Create(WebPageContext webPageContext)
		{
			return  JavascriptLoader.Create(((WebViewPage)webPageContext.Page).ViewContext);
		}

		public static JavascriptLoader Create(ViewContext vc)
		{
			object  helper = null;
			const string key = "$$javascriptHelper";

			bool reload = !vc.TempData.TryGetValue(key, out helper);

			if (helper != null &&  (helper as JavascriptLoader).viewPath != (vc.View as RazorView).ViewPath)
			{
				reload = true;
				vc.TempData.Remove(key);
			}

			if (reload)
			{
				helper = new JavascriptLoader(vc);
				vc.TempData.Add(key, helper);
			}
			return helper as JavascriptLoader;
		}

		private JavascriptLoader(ViewContext  vc)
		{
			this.httpcontext = vc.HttpContext;

			this.srv = this.httpcontext.Server;
			this.cacheProvider = this.httpcontext.Cache;
			this.Segments = new JSsegments();
			this.viewPath = (vc.View as RazorView).ViewPath;
			var bb = this.viewPath.Split('/', '\\', '.');
			bundleFile = String.Join("_", bb.Skip(bb.Length - 3).Take(2));
			Include("base");
		}
		#endregion

		#region API
		/// <summary>
		/// Adds standard javascript files
		/// </summary>
		/// <param name="std"> a comma-separated list of script file ids.</param>
		/// <returns></returns>
		[Obsolete("Std() has been depreciated.  Use 'Include(string)' instead.")]
		public string Std(string std) { return Include(std); }


		/// <summary>
		/// Includes standard javascript files
		/// </summary>
		/// <param name="fileids"> a comma-separated list of script file ids.</param>
		/// <returns></returns>
		public string Include(string fileids)
		{
			string[] scripts = fileids.Split(' ', ',');
			foreach (string script in scripts)
			{
				if (script.Length > 0)
				{
					if (script == "self")
					{
						var selfscript = Path.ChangeExtension(this.viewPath.Replace("~/Views", this.SelfJsPath), ".js");
						if (!File.Exists(HttpContext.Current.Server.MapPath(selfscript)))
							selfscript = null;

						var details = new LibraryDetail(this.viewPath,  false, "", selfscript, "self", "*");
						var cssPath= Path.ChangeExtension(this.viewPath.Replace("~/Views", this.LocalCssSelfPath), ".css");
						if (File.Exists(HttpContext.Current.Server.MapPath(cssPath)))
							details.Css = cssPath;
						Segments.stdFiles.Add(details);
					}
					else if (this.LibraryDetails.Exists(LibraryDetail.ByNameOrAlias(script)))
						InsertDependancy(script, Segments.stdFiles.Count);
				}
			}

			return "";
		}

		/// <summary>
		/// Adds the script.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="text">The text.</param>
		/// <returns>empty string</returns>
		/// <remarks>accepts a block of script as text. Multiple calls with the same id are rendered only once.
		///   All script from either AddScript methods is rendered in a block at the script insertion point.</remarks>
		public string AddScript(string id, string text)
		{
			if (!Segments.segments.ContainsKey(id))
			{
				Segments.segments[id] = text;
			}
			return "";
		}

		/// <summary>
		/// Adds the script.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>Empty String</returns>
		/// <remarks>accepts a block of script as text.  Each call is rendered.   All script from either AddScript methods is rendered in a block at the script insertion point.</remarks>
		public string AddScript(string text)
		{
			return this.AddScript(Guid.NewGuid().ToString(), text);
		}

		/// <summary>
		/// Adds the on ready script.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>Empty String</returns>
		/// <remarks>accepts a block of script as text.  Appended to script run on page ready.  
		/// All on rendered, wrapped in a jQuery document ready event function, at the point of InsertOnReady()</remarks>
		public string AddOnReadyScript(string text)
		{
			this.Segments.onreadyscript.AppendLine(text);
			return "";
		}

		/// <summary>
		/// Adds the inline script.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		public MvcHtmlString AddInlineScript(string text)
		{
			var s = string.Format(@"{0}<script>{0}//<![CDATA[{0}{1}{0}//]]>{0}</script>{0}",Environment.NewLine, text);
			return new MvcHtmlString(s);
		}

		public MvcHtmlString AddInlineFile(string text)
		{
			if (!text.StartsWith("http"))
				text = UrlHelper.GenerateContentUrl(CombineUrlPath(LocalJsPath, text), httpcontext);

			var s = string.Format(@"{0}<script type=""text/javascript"" src=""{1}""></script>{0}",Environment.NewLine, text);
			return new MvcHtmlString(s);

		}

		public MvcHtmlString AddFile(string filename)
		{
			var details = new LibraryDetail(filename, false, "", filename);
			Segments.stdFiles.Add(details);
			return MvcHtmlString.Empty;
		}

		public MvcHtmlString AddNoScriptText(string text)
		{
			var s = string.Format(@"{0}<noscript>{0}//<![CDATA[{0}{1}{0}//]]>{0}</noscript>{0}",
				Environment.NewLine, text);
			return new MvcHtmlString(s);
		}

		public MvcHtmlString InsertScripts()
		{
			var ttype = this.Transform;
			switch(ttype)
			{
				case transformType.BundleOnly:
				case transformType.Compress:
					return InsertScripts_Bundled(ttype);
				case transformType.None:
				default:
					return InsertScripts_None();

			}
		}
		private MvcHtmlString InsertScripts_Bundled(transformType ttype)
		{
			var files = new HashSet<string>();
//			BundleTable.Bundles.Clear();

			var vpath = CombineUrlPath(LocalJsPath, bundleFile);
//			IBundleTransform transform = ttype == transformType.Compress ? new JsMinify() : new NoTransform("text/javascript") as IBundleTransform;
//			var bundle = new Bundle(vpath, transform);
			IBundleTransform[] transform = ttype == transformType.Compress ? new IBundleTransform[1] { new JsMinify() } : new IBundleTransform[0];
			var bundle = new Bundle(vpath, transform);
			BundleTable.Bundles.Add(bundle);
			int cnt = 0;  //Bundles offer no practical way to know how many file it contains, so we have to count them.
			int segment = 0;   // bundling is just broken.   Let's try to workaround it.

			LibraryDetail self = null;
			StringWriter sb = new StringWriter();
			foreach (LibraryDetail lib in Segments.stdFiles)
			{
				string name = lib.Name;
				if (lib.UseCds)
				{
					if(cnt > 0)
					{
						this.RenderJavascriptFile(sb, BundleTable.Bundles.ResolveBundleUrl(vpath));
//						BundleTable.Bundles.Clear();
						vpath = CombineUrlPath(LocalJsPath, bundleFile) + ++segment;
						bundle = new Bundle(vpath, transform);
						BundleTable.Bundles.Add(bundle);
						cnt = 0;
					}
					this.RenderJavascriptFile(sb, string.Format(@"http://ajax.googleapis.com/ajax/libs/{0}/{1}/{0}.js", name, lib.Version));
					continue;
				}

				if (lib.Alias.Contains("self"))
					self = lib;
				else
				{
					var pathname = GetFilePath(lib);

					if (pathname != null)
					{
						bundle.Include(CombineUrlPath(LocalJsPath, pathname));
						++cnt;
					}
				}
			}
			if (self != null && self.PathName != null)
			{
				bundle.Include(CombineUrlPath(LocalJsPath, self.PathName));
				++cnt;
			}
			foreach (string file in Segments.files)
			{
				bundle.Include(file);
				++cnt;
			}
			if (cnt >0)
				this.RenderJavascriptFile(sb, BundleTable.Bundles.ResolveBundleUrl(vpath));

			if (Segments.segments.Any())
			{
				this.RenderJavascriptBlocks(sb);
			}
			Segments.wasRendered = true;

			return new MvcHtmlString(sb.GetStringBuilder().ToString());

		}

		private string GetFilePath(IFileDetails lib)
		{
			var pathname = lib.PathName;
			if (lib.DebugPathName != null && (this.UseDebugScripts || Debugger.IsAttached) && File.Exists(CombineUrlPath(LocalJsPath, lib.DebugPathName)))
				pathname = lib.DebugPathName;
			return pathname;
		}
		/// <summary>
		/// Inserts the scripts.
		/// </summary>
		/// <returns></returns>
		/// <remarks>Renders all script files & script blocks. </remarks>
		private MvcHtmlString InsertScripts_None()
		{
			LibraryDetail self = null;
			var sb = new StringWriter();
			foreach (LibraryDetail lib in Segments.stdFiles)
			{
					string name = lib.Name;
					if (lib.UseCds)
					{
						this.RenderJavascriptFile(sb, string.Format(@"http://ajax.googleapis.com/ajax/libs/{0}/{1}/{0}.js", name, lib.Version));
						continue;
					}

					if (lib.Alias.Contains("self"))
						self = lib;
					else
					{
						var pathname = lib.PathName;
						if (this.UseDebugScripts || Debugger.IsAttached)
							pathname = lib.DebugPathName ?? lib.PathName;
						if (pathname != null)
							this.RenderJavascriptFile(sb, pathname);
					}
			}
			if (self != null && self.PathName !=null)
			{
				this.RenderJavascriptFile(sb, self.PathName);
			}
			foreach (string file in Segments.files)
			{
				this.RenderJavascriptFile(sb, file);
			}

			if (Segments.segments.Any())
			{
				this.RenderJavascriptBlocks(sb);
			}
			Segments.wasRendered = true;

			return new MvcHtmlString(sb.GetStringBuilder().ToString());

		}

		/// <summary>
		/// Inserts the CSS.
		/// </summary>
		/// <returns>string: text block contain htlp LINK elements to load all requested css files.</returns>
		/// <remarks>Renders all css files</remarks>
		public MvcHtmlString InsertCss()
		{
			var ttype = this.Transform;
			switch(ttype)
			{
				case transformType.BundleOnly:
				case transformType.Compress:
					return InsertCss_Bundled(ttype);
				case transformType.None:
				default:
					return InsertCss_None();

			}
		}
  
		private MvcHtmlString InsertCss_Bundled(transformType ttype)
		{
			var files = new HashSet<string>();
			var bundles = BundleTable.Bundles;
			IBundleTransform[] transform = ttype == transformType.Compress ? new IBundleTransform[1] { new CssMinify() } : new IBundleTransform[0];

			var vpath = CombineUrlPath(LocalCssPath, bundleFile);
			var bundle = new Bundle(vpath, transform);
			foreach (var lib in Segments.stdFiles)
			{
				string name = lib.Css ?? lib.Name;
				if (!files.Contains(name))
				{
					files.Add(name);
					if (name[0] == '/')
					{
						bundle.Include(name);
					}
					else
					{
						var sheet = CssDetails.FirstOrDefault(css => css.Name == name);
						if (sheet != null)
						{
							var pathname = GetFilePath(sheet);
							bundle.Include(CombineUrlPath(LocalCssPath, pathname));
						}
					}
				}
			}
			bundles.Add(bundle);
//			var resp = bundle.GenerateBundleResponse(new BundleContext(this.httpcontext, bundles, this.LocalCssPath));
			return new MvcHtmlString(
							String.Format(@"<link href=""{0}"" rel=""stylesheet"" type=""text/css"" />", bundles.ResolveBundleUrl(vpath)));

	
		
		}

		string CombineUrlPath(string path1, string path2)
		{
			return Path.Combine(path1, path2).Replace('\\', '/');
		}

		private MvcHtmlString InsertCss_None()
		{
			var files = new HashSet<string>();
			StringWriter sb = new StringWriter();
			foreach (var lib in Segments.stdFiles)
			{
				string name = lib.Css ?? lib.Name;
				if (!files.Contains(name))
				{
					files.Add(name);
					if (name[0] == '/')
					{
						RenderCssFile(sb, name);
					}
					else
					{
						var sheet = CssDetails.FirstOrDefault(css => css.Name == name);
						if (sheet != null)
						{
							var pathname = GetFilePath(sheet);
							RenderCssFile(sb, pathname);
						}
					}
				}
			}
			return new MvcHtmlString(sb.GetStringBuilder().ToString());
		}

		public MvcHtmlString InsertOnReady()
		{
			if (Segments.onreadyscript.Length == 0)
				return new MvcHtmlString("");

			var sb = new StringBuilder(1024);
			sb.AppendLine();
			sb.AppendLine(@"<script type=""text/javascript"">");
			sb.AppendLine(@"//<![CDATA[");

#if true
			sb.AppendLine("jQuery(function($) {");
#else
			var  rm = new ResourceManager("Castle.MonoRail.ViewComponents.InsertJavascript",  Assembly.GetExecutingAssembly());
			RenderText(rm.GetString(this.helper.PreferredLibrary.ToLowerInvariant()));
			RenderText(Environment.NewLine);
#endif
			sb.AppendLine(Segments.onreadyscript.ToString());
			sb.AppendLine("});");

			sb.AppendLine(@"//]]>");
			sb.AppendLine("</script>");
			return new MvcHtmlString(sb.ToString());

		}
		#endregion

		#region Internal

		internal void RenderJavascriptFile(TextWriter sb, string file)
		{
			if (!file.StartsWith("http://"))
			{
				if (file.IndexOf('~') == -1)
					file = Path.Combine(LocalJsPath, file);
				file = file.Replace('\\', '/');
				file = VirtualPathUtility.ToAbsolute(file);
			}
			sb.WriteLine(@"<script type=""text/javascript"" src=""{0}""></script>", file);
		}

		internal void RenderJavascriptBlocks(TextWriter sb)
		{
			sb.Write(Environment.NewLine);
			sb.Write(@"<script type=""text/javascript"">");
			sb.WriteLine(@"//<![CDATA[");

			foreach (string script in Segments.segments.Values)
			{
				if (!String.IsNullOrEmpty(script))
					sb.WriteLine(script.Trim());
			}

			sb.WriteLine(@"//]]>");
			sb.WriteLine("</script>");
		}

		//internal void RenderCssFile(StringBuilder sb,  string file)
		//{
		//    if (!file.StartsWith("http://"))
		//        file = Path.Combine(LocalCssPath, file).Replace('\\', '/');

		//    sb.AppendFormat(@"<link rel=""stylesheet"" type=""text/css"" href=""{0}"" />", file);
		//    sb.AppendLine();
		//}

		internal void RenderCssFile(TextWriter tw, string file)
		{
			if (!file.StartsWith("http://"))
				file = Path.Combine(LocalCssPath, file).Replace('\\', '/');
			file = VirtualPathUtility.ToAbsolute(file);

			tw.WriteLine(@"<link rel=""stylesheet"" type=""text/css"" href=""{0}"" />", file);
		}


		private static string CombineUrl(string baseUrl, params string[] relativeUrls)
		{
			var viewpath = VirtualPathUtility.AppendTrailingSlash(VirtualPathUtility.ToAbsolute(baseUrl));
			foreach (var segment in relativeUrls)
			{
				viewpath = VirtualPathUtility.AppendTrailingSlash(viewpath);
				viewpath = VirtualPathUtility.Combine(viewpath, segment);
			}
			return viewpath;
		}



		/// <summary>
		/// Inserts the details of a required javascript file into the list.
		/// </summary>
		/// <remarks>
		/// Places the details of the js script file known by <paramref name="name"/> onto the list
		/// of files that will be included on this page.  Designed to place new files at the end, and
		/// files they depend on just before them.
		/// </remarks>
		/// <param name="name">The name.</param>
		/// <param name="pos">The position in stdFile list to add items.</param>
		private int InsertDependancy(string name, int pos)
		{
			if (string.IsNullOrEmpty(name))
				return 0;

			int index = 0;

			if (Segments.stdFiles.Find(LibraryDetail.ByNameOrAlias(name)) == null)
			{
				LibraryDetail details = LibraryDetails.Find(LibraryDetail.ByNameOrAlias(name));
				foreach (string fname in details.DependsOn)
					index += InsertDependancy(fname, pos + index);
				Segments.stdFiles.Insert(pos + index, details);
				index++;
			}
			return index;
		}
		#endregion

		#region Internal Properties
		private List<LibraryDetail> libraryDetails = null;
		internal List<LibraryDetail> LibraryDetails
		{
			get
			{
				if (libraryDetails == null)
				{
					libraryDetails = cacheProvider.Get(Detailskeyword) as List<LibraryDetail>;
				}

				if (libraryDetails == null)
				{
					if (LibraryInfo != null)
					{
						List<LibraryDetail> details = new List<LibraryDetail>();
						var libs = LibraryInfo.DocumentElement.ChildNodes.OfType<XmlElement>().Where(xe => xe.Name == "library");
						foreach (XmlElement lib in libs)
						{
							details.Add(new LibraryDetail(lib));
						}
						libraryDetails = details;
					}
					else
						//						libraryDetails = new List<LibraryDetail>(libraryDefaults);
						throw new FileNotFoundException("JavascriptLoader requires the file jslibraries.xml to be in the website root.");

					Cache webcache = HttpContext.Current.Cache;
					if (webcache != null)
					{
						CacheDependency depend = new CacheDependency(new string[0], new string[] { Xmlfilename });
						webcache.Add(Detailskeyword, libraryDetails, depend, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
					}
					else
					{
						this.cacheProvider.Store(Detailskeyword, libraryDetails);
					}

				}
				return libraryDetails;
			}
		}
		private List<CssSheetDetails> cssDetails = null;
		internal List<CssSheetDetails> CssDetails
		{
			get
			{
				if (this.cssDetails == null)
				{
					this.cssDetails = cacheProvider.Get(Csskeyword) as List<CssSheetDetails>;
				}

				if (this.cssDetails == null)
				{
					if (LibraryInfo != null)
					{
						List<CssSheetDetails> details = new List<CssSheetDetails>();
						var libs = CssInfo.ChildNodes.OfType<XmlElement>().Where(xe => xe.Name == "sheet");
						foreach (XmlElement lib in libs)
						{
							details.Add(new CssSheetDetails(lib));
						}
						this.cssDetails = details;
					}
					else
						libraryDetails = new List<LibraryDetail>();

					Cache webcache = HttpContext.Current.Cache;
					if (webcache != null)
					{
						CacheDependency depend = new CacheDependency(new string[0], new string[] { Xmlfilename });
						webcache.Add(Csskeyword, this.cssDetails, depend, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
					}
					else
					{
						this.cacheProvider.Store(Csskeyword, this.cssDetails);
					}

				}
				return this.cssDetails;
			}
		}


		internal XmlDocument LibraryInfo
		{
			get
			{
				libraryInfo = cacheProvider.Get(Xmlfilename) as XmlDocument;

				if (libraryInfo == null)
				{
					string xmlpath = srv.MapPath(VirtualPathUtility.ToAbsolute("~/jslibraries.xml"));
					if (File.Exists(xmlpath))
					{
						libraryInfo = new XmlDocument();
						libraryInfo.Load(xmlpath);
						Cache webcache = HttpContext.Current.Cache;
						if (webcache != null)
						{
							webcache.Add(Xmlfilename, libraryInfo, new CacheDependency(xmlpath), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
						}
						else
						{
							this.cacheProvider.Store(Xmlfilename, libraryInfo);
						}
					}
				}
				return libraryInfo;
			}
		}
		internal XmlElement CssInfo
		{
			get
			{
				return LibraryInfo.DocumentElement.ChildNodes.OfType<XmlElement>().First(xe => xe.Name == "css");
			}
		}
		/// <summary>
		/// Gets the preferred library. The name of the javascript framework chosen as the default.  If none is specified, "prototype" is used.
		/// </summary>
		/// <value>The preferred library.</value>
		public string PreferredLibrary
		{
			get
			{
				if (LibraryInfo != null)
				{
					//                    XmlAttribute attr = LibraryInfo.DocumentElement.SelectSingleNode("/libraries/@preferredLibrary") as XmlAttribute;
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["preferredLibrary"];
					if (attr != null)
					{
						return attr.Value;
					}
				}
				return "prototype";
			}
		}
		public string SelfJsPath
		{
			get
			{
				if (LibraryInfo != null)
				{
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["selfJsPath"];
					if (attr != null)
					{
						return attr.Value;
					}
				}
				return "~/Views"; // "~/" + mrconfig.ViewEngineConfig.VirtualPathRoot;
			}
		}


		public bool UseDebugScripts
		{
			get
			{
				if (LibraryInfo != null)
				{
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["useDebugScripts"];
					if (attr != null)
					{
						return false;
					}
				}
				return true;
			}
		}

		enum transformType {None, BundleOnly, Compress};
		private  transformType Transform 
		{
			get
			{
					var result = transformType.None;	
				if (LibraryInfo != null)
				{
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["transform"];
					if (attr != null)
						Enum.TryParse<transformType>(attr.Value, true, out result);
				}
				return result;
			}
		}



		internal JSsegments Segments {get; private set;}

		internal bool SkipRender {get; set;}

		internal string LocalJsPath
		{
			get
			{
				var localpath = "~/Scripts";
				if (LibraryInfo != null)
				{
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["localjspath"];
					if (attr != null)
					{
						localpath = attr.Value;
					}
				}
				return localpath; //  VirtualPathUtility.ToAbsolute(localpath);
			}
		}
		internal string LocalCssSelfPath
		{
			get
			{
				var localpath = "~/content";
				if (CssInfo != null)
				{
					XmlAttribute attr = CssInfo.Attributes["selfcsspath"];
					if (attr != null)
					{
						localpath = attr.Value;
					}
				}
				return VirtualPathUtility.ToAbsolute(localpath);
			}
		}
		internal string LocalCssPath
		{
			get
			{
				var localpath = "~/content";
				if (CssInfo != null)
				{
					XmlAttribute attr = CssInfo.Attributes["localcsspath"];
					if (attr != null)
					{
						localpath = attr.Value;
					}
				}
				return localpath;
			}
		}

		
		#endregion
		#region Internal class JSsegments
		internal class JSsegments
	{
		/// <summary>
		/// Initializes a new instance of the JSsegments class.
		/// </summary>
		public JSsegments()
		{
			segments = new Dictionary<string, string>();
			files = new HashSet<string>();
			stdFiles = new List<LibraryDetail>();
			wasRendered = false;
			onreadyscript = new StringBuilder();
			//			engine.Trace.Warn("New JSsegment created");
		}

		internal bool wasRendered;

		internal Dictionary<string, string> segments;
		internal HashSet<string> files;
		internal List<LibraryDetail> stdFiles;
		internal StringBuilder onreadyscript;
	}
		#endregion
	}
}