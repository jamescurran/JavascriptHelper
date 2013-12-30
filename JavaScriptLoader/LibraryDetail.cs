#region Apache License
/*
 * Copyright 2012-2013, James M. Curran
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
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
#endregion

namespace NovelTheory.Components
{
	/// <summary>
	/// Used internally at run-time by the JavascriptLoader system to hold details of 
	/// scripts to include.
	/// </summary>
	[DebuggerDisplay("{Name}")]
	internal class LibraryDetail : IFileDetails
	{
		public string Name { get; set; }
		public bool UseCds { get; set; }
		public string PathName { get; set; }
		public string[] Alias { get; set; }
		public string[] DependsOn { get; set; }
		public string Version { get; set; }
		public string Css { get; set; }
		public string DebugPathName { get; set; }

		public LibraryDetail(string name, bool useCds, string version, string pathName, string alias = "", string dependsOn = "")
		{
			this.Name = name;
			this.Alias = alias.Split(' ', ',');
			this.DependsOn = dependsOn.Split(' ', ',');
			this.PathName = pathName;
			this.UseCds = useCds;
			this.Version = version;
		}

		public LibraryDetail(string name) : this(name, false, "1", null, "", "")
		{
		}

		public LibraryDetail(string name, string dependsOn) : this(name, false, "1", null, "", dependsOn)
		{
		}

		public LibraryDetail(string name, string dependsOn, string alias) : this(name, false, "1", null, alias, dependsOn)
		{
		}

		public LibraryDetail(XmlElement node)
		{
			this.Name = node.xAttribute("name").ToLowerInvariant();
			this.Alias = (node.xAttribute("alias") ?? "").ToLowerInvariant().Split(' ', ',');
			this.DependsOn = (node.xAttribute("dependsOn") ?? "").ToLowerInvariant().Split(' ', ',');
			this.PathName = node.xAttribute("pathname");
			this.UseCds = node.xAttributeBool("useCDS");
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
			this.UseCds = node.xAttributeBool("useCDS");
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
}