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

using HtmlHelper = System.Web.Mvc.HtmlHelper;
//using HtmlHelper = System.Web.WebPages.Html.HtmlHelper;
#endregion

namespace NovelTheory.Component
{
	public class JavascriptHelper
	{
		#region Private Fields
		HttpServerUtilityBase srv;

		private XmlDocument libraryInfo = null;
		private Cache cacheProvider = null;
		private string viewPath = null;
		private const string xmlfilename = @"jslibraries.xml";
		private const string detailskeyword = @"jslibrariesDetails";
		private const string cssskeyword = @"jslibrariesCssDetails";

		private LibraryDetail[] libraryDefaults = new LibraryDetail[]
			{
				new LibraryDetail("prototype"),
				new LibraryDetail("scriptaculous", "prototype", "effects2"),
				new LibraryDetail("effectsfat", "prototype"),
				new LibraryDetail("validate", "prototype"),
				new LibraryDetail("behavior", "prototype", "behaviour")
			};
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
		public static JavascriptHelper Create(WebViewPage page)
		{
			return Create(page.ViewContext);
		}

		internal static JavascriptHelper Create(HtmlHelper html)
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
		public static JavascriptHelper Create(WebPageContext webPageContext)
		{
			return  JavascriptHelper.Create(((WebViewPage)webPageContext.Page).ViewContext);
		}

		public static JavascriptHelper Create(ViewContext vc)
		{
			object  helper = null;
			const string key = "$$javascriptHelper";

			bool reload = !vc.TempData.TryGetValue(key, out helper);

			if (helper != null &&  (helper as JavascriptHelper).viewPath != (vc.View as RazorView).ViewPath)
			{
				reload = true;
				vc.TempData.Remove(key);
			}

			if (reload)
			{
				helper = new JavascriptHelper(vc);
				vc.TempData.Add(key, helper);
			}
			return helper as JavascriptHelper;
		}

		private JavascriptHelper(ViewContext  vc)
		{
			this.srv = vc.HttpContext.Server;
			this.cacheProvider = vc.HttpContext.Cache;
			this.Segments = new JSsegments();
			this.viewPath = (vc.View as RazorView).ViewPath;
		}
		#endregion

		#region API
		/// <summary>
		/// Adds standard javascript files
		/// </summary>
		/// <param name="std"> a comma-separated list of script file ids.</param>
		/// <returns></returns>
		public string Std(string std)
		{
			string[] scripts = std.Split(' ', ',');
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
						var cssPath= Path.ChangeExtension(this.viewPath.Replace("~/Views", this.LocalCssPath), ".css");
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
			return new MvcHtmlString("");

		}

		public MvcHtmlString AddNoScriptText(string text)
		{
			var s = string.Format(@"{0}<noscript>{0}//<![CDATA[{0}{1}{0}//]]>{0}</noscript>{0}",
				Environment.NewLine, text);
			return new MvcHtmlString(s);
		}

		/// <summary>
		/// Inserts the scripts.
		/// </summary>
		/// <returns></returns>
		/// <remarks>Renders all script files & script blocks. </remarks>
		public MvcHtmlString InsertScripts()
		{
			LibraryDetail self = null;
			var sb = new StringBuilder(1024);
			foreach (LibraryDetail lib in Segments.stdFiles)
			{
					string name = lib.Name;
					if (lib.UseGoogle)
					{
						this.RenderJavascriptFile(sb, string.Format("http://ajax.googleapis.com/ajax/libs/{0}/{1}/{0}.js", name, lib.Version));
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

			return new MvcHtmlString(sb.ToString());

		}

		/// <summary>
		/// Inserts the CSS.
		/// </summary>
		/// <returns>string: text block contain htlp LINK elements to load all requested css files.</returns>
		/// <remarks>Renders all css files</remarks>
		public MvcHtmlString InsertCss()
		{
			var files = new HashSet<string>();
			StringBuilder sb = new StringBuilder(1024);
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
							RenderCssFile(sb, sheet.PathName);
					}
				}
			}
			return new MvcHtmlString(sb.ToString());
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
		internal void RenderJavascriptFile(StringBuilder sb, string file)
		{
			if (!file.StartsWith("http://"))
				file = Path.Combine(LocalJsPath, file).Replace('\\', '/');

			sb.AppendFormat(@"<script type=""text/javascript"" src=""{0}""></script>", file);
			sb.AppendLine();
		}

		internal void RenderJavascriptBlocks(StringBuilder sb)
		{
			sb.Append(Environment.NewLine);
			sb.AppendLine(@"<script type=""text/javascript"">");
			sb.AppendLine(@"//<![CDATA[");

			foreach (string script in Segments.segments.Values)
			{
				if (!String.IsNullOrEmpty(script))
					sb.AppendLine(script.Trim());
			}

			sb.AppendLine(@"//]]>");
			sb.AppendLine("</script>");
		}

		internal void RenderCssFile(StringBuilder sb,  string file)
		{
			if (!file.StartsWith("http://"))
				file = Path.Combine(LocalCssPath, file).Replace('\\', '/');

			sb.AppendFormat(@"<link rel=""stylesheet"" type=""text/css"" href=""{0}"" />", file);
			sb.AppendLine();
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
					libraryDetails = cacheProvider.Get(detailskeyword) as List<LibraryDetail>;
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
						libraryDetails = new List<LibraryDetail>(libraryDefaults);

					Cache webcache = HttpContext.Current.Cache;
					if (webcache != null)
					{
						CacheDependency depend = new CacheDependency(new string[0], new string[] { xmlfilename });
						webcache.Add(detailskeyword, libraryDetails, depend, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
					}
					else
					{
						this.cacheProvider.Store(detailskeyword, libraryDetails);
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
					this.cssDetails = cacheProvider.Get(cssskeyword) as List<CssSheetDetails>;
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
						CacheDependency depend = new CacheDependency(new string[0], new string[] { xmlfilename });
						webcache.Add(cssskeyword, this.cssDetails, depend, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
					}
					else
					{
						this.cacheProvider.Store(cssskeyword, this.cssDetails);
					}

				}
				return this.cssDetails;
			}
		}


		internal XmlDocument LibraryInfo
		{
			get
			{
				libraryInfo = cacheProvider.Get(xmlfilename) as XmlDocument;

				if (libraryInfo == null)
				{
					//                    string xmlpath = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.CodeBase), xmlfilename);
					//                    string webPath = engine.Server.MapPath(null);
					//                    string xmlpath = Path.Combine(webPath, "jslibraries.xml");


					string xmlpath = srv.MapPath(VirtualPathUtility.ToAbsolute("~/jslibraries.xml"));
					if (File.Exists(xmlpath))
					{
						libraryInfo = new XmlDocument();
						libraryInfo.Load(xmlpath);
						Cache webcache = HttpContext.Current.Cache;
						if (webcache != null)
						{
							webcache.Add(xmlfilename, libraryInfo, new CacheDependency(xmlpath), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
						}
						else
						{
							this.cacheProvider.Store(xmlfilename, libraryInfo);
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
				return VirtualPathUtility.ToAbsolute(localpath);
			}
		}
		internal string LocalCssPath
		{
			get
			{
				var localpath = "~/content";
				if (LibraryInfo != null)
				{
					XmlAttribute attr = LibraryInfo.DocumentElement.Attributes["localcsspath"];
					if (attr != null)
					{
						localpath = attr.Value;
					}
				}
				return VirtualPathUtility.ToAbsolute(localpath);
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

	internal static class XmlHelpers
	{
		public static bool xAttributeBool(this XmlElement node, string p)
		{
			string val = xAttribute(node, p);
			return val == null ? false : XmlConvert.ToBoolean(val);
		}
		public static bool xAttributeBool(this XElement node, string p)
		{
			string val = xAttribute(node, p);
			return val == null ? false : XmlConvert.ToBoolean(val);
		}
		public static string xAttribute(this XmlElement ele, string key)
		{
			XmlAttribute attr = ele.Attributes[key];
			return attr == null ? null : attr.Value;
		}
		public static string xAttribute(this XElement ele, string key)
		{
			XAttribute attr = ele.Attribute(key);
			return attr == null ? null : attr.Value;
		}

	}

	/// <summary>
	/// Used internally at run-time by the JavascriptHelper system to hold details of 
	/// scripts to include.
	/// </summary>

	[DebuggerDisplay("{Name}")]
	internal class LibraryDetail
	{
		public string Name;
		public bool UseGoogle;
		public string PathName;
		public string[] Alias;
		public string[] DependsOn;
		public string Version;
		public string Css;
		public string DebugPathName;

		public LibraryDetail(string name, bool useGoogle, string version, string pathName, string alias = "", string dependsOn = "")
		{
			this.Name = name;
			this.Alias = alias.Split(' ', ',');
			this.DependsOn = dependsOn.Split(' ', ',');
			this.PathName = pathName;
			this.UseGoogle = useGoogle;
			this.Version = version;
		}
		public LibraryDetail(string name) : this(name, false, "1", null, "", "") { }
		public LibraryDetail(string name, string dependsOn) : this(name, false, "1", null, "", dependsOn) { }
		public LibraryDetail(string name, string dependsOn, string alias) : this(name, false, "1", null, alias, dependsOn) { }
		public LibraryDetail(XmlElement node)
		{
			this.Name = node.xAttribute("name");
			this.Alias = (node.xAttribute("alias") ?? "").Split(' ', ',');
			this.DependsOn = (node.xAttribute("dependsOn") ?? "").Split(' ', ',');
			this.PathName = node.xAttribute("pathname");
			this.UseGoogle = node.xAttributeBool("useGoogle");
			this.Version = node.xAttribute("version");
			this.Css = node.xAttribute("css");
			this.DebugPathName = node.xAttribute("debugPath");
		}

		public LibraryDetail(XElement node)
		{
			this.Name = node.xAttribute("name");
			this.Alias = (node.xAttribute("alias") ?? "").Split(' ', ',');
			this.DependsOn = (node.xAttribute("dependsOn") ?? "").Split(' ', ',');
			this.PathName = node.xAttribute("pathname");
			this.UseGoogle = node.xAttributeBool("useGoogle");
			this.Version = node.xAttribute("version");
			this.Css = node.xAttribute("css");
			this.DebugPathName = node.xAttribute("debugPath");
		}


		internal static Predicate<LibraryDetail> ByNameOrAlias(string name)
		{
			name = name.Trim().ToLowerInvariant();
			return delegate(LibraryDetail ld) { return ld.Name == name || Array.IndexOf(ld.Alias, name) != -1; };
		}
	}

	internal class CssSheetDetails
	{
		public string Name;
		public string PathName;

		public CssSheetDetails(XmlElement xml)
		{
			this.Name = Attribute(xml, "name");
			this.PathName = Attribute(xml, "pathname");
		}
		public CssSheetDetails(XElement xml)
		{
			this.Name = Attribute(xml, "name");
			this.PathName = Attribute(xml, "pathname");
		}
		private static string Attribute(XmlElement ele, string key)
		{
			XmlAttribute attr = ele.Attributes[key];
			return attr == null ? null : attr.Value;
		}
		private static string Attribute(XElement ele, string key)
		{
			XAttribute attr = ele.Attribute(key);
			return attr == null ? null : attr.Value;
		}
	}

	// Extension method to make the .NET cache look like the MonoRail cache, to ease porting.
	static class CacheHelper
	{
		public static object  Store(this Cache cache, string key, object value)
		{
			return cache.Add(key, value, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30), CacheItemPriority.Normal, null);
		}
	}

}