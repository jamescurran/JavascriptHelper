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
using System.Web.Caching;
#endregion

namespace NovelTheory.Components
{
	static class CacheHelper
	{
		public static object Store(this Cache cache, string key, object value)
		{
			return cache.Add(key, value, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30), CacheItemPriority.Normal, null);
		}
	}
}