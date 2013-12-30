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

using System.Xml;
using System.Xml.Linq;

namespace NovelTheory.Components
{
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
}