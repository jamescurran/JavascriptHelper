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
using System.Xml;
using System.Xml.Linq;
#endregion

namespace NovelTheory.Components
{
	internal class CssSheetDetails : IFileDetails
	{
		public string Name { get; set; }
		public string PathName { get; set; }
		public string DebugPathName { get; set; }

		public CssSheetDetails(XmlElement xml)
		{
			this.Name = Attribute(xml, "name");
			this.PathName = Attribute(xml, "pathname");
			this.DebugPathName = Attribute(xml, "debugpath");
		}

		public CssSheetDetails(XElement xml)
		{
			this.Name = Attribute(xml, "name");
			this.PathName = Attribute(xml, "pathname");
			this.DebugPathName = Attribute(xml, "debugpath");
		}

		private static string Attribute(XmlElement ele, string key)
		{
			XmlAttribute attr = ele.Attributes[key];
			return attr == null ? null : attr.Value.ToLowerInvariant();
		}

		private static string Attribute(XElement ele, string key)
		{
			XAttribute attr = ele.Attribute(key);
			return attr == null ? null : attr.Value.ToLowerInvariant();
		}
	}
}