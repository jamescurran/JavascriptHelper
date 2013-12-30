JavascriptLoader
  
Quick Start:
At the top of your layout, add:
@{
    var JScript = NovelTheory.Components.JavascriptLoader.Create(this);
    JScript.Include("jquery, moderizr");

}

Then, where you want the CSS files, remove the line:
	@Styles.Render("~/Content/css")
and add:
	@JScript.InsertCss()

Where you want the JS files, remove the lines
	@Scripts.Render("~/bundles/jquery")
	@RenderSection("scripts", required: false)
add:
	@JScript.InsertScripts()

Then in you views, add (for example)
@{
    var JScript = NovelTheory.Components.JavascriptLoader.Create(this);
    JScript.Std("menu, slider, dialog, self");
}

"self" refers to a JS file following the same naming conventions as the view template, under the Scripts folder
e.g., /Scripts/View/Home/Index.js

The other names ("menu", "slider" etc) refer to JS files defined in the jslibraries.xml in the website root.

The ~/jslibraries.xml file need to be manually updated for the JS files you are using.

Full documentation available at :
http://www.codeproject.com/Articles/395143/JavascriptHelper-Managing-JS-files-for-ASP-NET-MVC






