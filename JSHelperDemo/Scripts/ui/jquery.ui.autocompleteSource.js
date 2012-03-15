function AutoCompleteSource(request, response)
{
    var _response;
    this.options = jQuery.extend({}, { type: "json" }, this.options);
    var funcItem0 = function (item) { return item[0]; };
    var formatItem = this.options.formatItem || funcItem0;
    var formatResult = this.options.formatResult || formatItem;
    var valueFld = this.options.valueFld;
    switch (this.options.type.toLowerCase())
    {
        case "json":
            var getID = this.options.getID || function(item) { return item[valueFld]; };
            if (this.options.formatItem)
            {

                _response = function(content)
                {
                    if (content == null)
                        return "";
                    return response(jQuery.map(content, function(that, nInx)
                    {
                        var item =
                            {
                                label: formatItem(that, nInx, 0, request),
                                value: formatResult(that, request),
                                id: getID(that),
                                item: that
                            };  // end object 
                       return item;
                    }   // end function()
                )   // end map()
                );  // end return response()
                } // end _response = function()
            }
            else
                _response = response;
            break;

        case "text":
        	var getID = this.options.getID || funcItem0;
            _response = function(content)
            {
                return response(jQuery.map(content.split("\n"), function(that, nInx)
                {
                    if (that.length === 0)
                        return;
                    var rows = that.split('|');
                    var item =
                            {
                                label: formatItem(rows, nInx, 0, request),
                                value: formatResult(rows, request),
                                id: getID(rows),
                                item: rows
                            };  // end object 
                    return item;

                }));
            };
            break;
    }
    jQuery.get(this.options.url, jQuery.extend(request, this.options.extraParams), _response, this.options.type);
   }

   function ACformatItem(row, pos, num, term) {
   	return row.label;
   }
   function ACformatResult(data, value) {
   	return data.label;
   }
