(function ($)
{
	$.widget("ui.combobox", {
		_create: function ()
		{
			var self = this;
			var width = self.element.width();
			var select = this.element.hide();
			var input = $("<input>").insertAfter(select).autocomplete({
				source: function (request, response)
				{
					var matcher = new RegExp(request.term, "i");
					response(select.children("option").map(function ()
					{
						var text = $(this).text();
						if (!request.term || matcher.test(text))
							return {
								id: $(this).val(),
								label: request.term ?text.replace(new RegExp("(?![^&;]+;)(?!<[^<>]*)(" + request.term.replace(/([\^\$\(\)\[\]\{\}\*\.\+\?\|\\])/gi, "\\$1") + ")(?![^<>]*>)(?![^&;]+;)", "gi"), "<strong>$1</strong>") : text,
								value: text
							};
					}));
				},
				delay: 0,
				select: function (e, ui)
				{
					if (!ui.item)
					{
						// remove invalid value, as it didn't match anything
						$(this).val("");
						return false;
					}
					$(this).focus();
					select.val(ui.item.id);
					self._trigger("selected", null, {
						item: select.children().find("[value='" + ui.item.id + "']")
					});
				},
				minLength: 0
			}).removeClass("ui-corner-all").addClass("ui-corner-left");
			if (width > 0)
			{
				input.attr("width", width)
				.css("min-width", width)
				.attr("size", width / 8);
			}
			$("<button type='button'>&nbsp;</button>").insertAfter(input).button({
				icons: {
					primary: "ui-icon-triangle-1-s"
				},
				text: false
			}).removeClass("ui-corner-all").addClass("ui-corner-right ui-button-icon").position({
				my: "left center",
				at: "right center",
				of: input,
				offset: "-1 0"
			}).css("top", "")
			.click(function ()
			{
				// close if already visible
				if (input.autocomplete("widget").is(":visible"))
				{
					input.autocomplete("close");
					return;
				}
				// pass empty string as value to search for, displaying all results
				input.autocomplete("search", "");
				input.focus();
				return;
			});
			//			var initialSelect = $("option:selected", select);
			//			if (initialSelect)
			//				input.val(initialSelect.text());
			input.val($("option:selected", select).text());
		},
		show: function ()
		{
			this.element.next("input").show().next("button").show();
		},
		hide: function ()
		{
			this.element.next("input").hide().next("button").hide();
		}
	});
})(jQuery);
