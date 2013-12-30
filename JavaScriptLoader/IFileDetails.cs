using System;
namespace NovelTheory.Components
{
	interface IFileDetails
	{
		string DebugPathName { get; set; }
		string Name { get; set; }
		string PathName { get; set; }
	}
}
