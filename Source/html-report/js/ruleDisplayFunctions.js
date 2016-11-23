ruleDisplayFunctions = [];

ruleDisplayFunctions["default"] = function(rule){
    var resultTable = $("<table>").addClass("detail-table");
	var headerRow = $("<tr>");
	var resultRow = $("<tr>");
	
	for(elem in rule.ResultData)
	{
		var headerCell = $("<td>").addClass("header").text(elem);
		headerRow.append(headerCell);
		
		var resultCell = $("<td>").addClass("cell").text(rule.ResultData[elem]);
		resultRow.append(resultCell);
	}
	
	resultTable.append(headerRow, resultRow);
	
	return resultTable;
};

ruleDisplayFunctions["Repeated Calls"] = function(rule){
	var results = rule.ResultData;
	
	if(results["Duplicates"] == 0)
	{
		return $("<div>").text("No duplicated calls found.");
	}

	var resultTable = $("<table>").addClass("detail-table");
	var headerRow = $("<tr>");
	var resultRow = $("<tr>");
	
	headerRow.append($("<td>").addClass("header").text("Total Calls"));
	headerRow.append($("<td>").addClass("header").text("Duplicates"));
	headerRow.append($("<td>").addClass("header").text("Percentage"));
	
	resultRow.append($("<td>").addClass("cell").text(results["Total Calls"]));
	resultRow.append($("<td>").addClass("warning").text(results["Duplicates"]));
	resultRow.append($("<td>").addClass("cell").text((Number(results["Percentage"]) * 100).toFixed(2) + "%"));
	
	resultTable.append(headerRow, resultRow);
	
	return resultTable;
};

ruleDisplayFunctions["Throttled Call Detection"] = function(rule){
	var graphLocation = $("<div>").css({"width": "700px", "height": "250px", "float": "center", "padding": "10px"});
	var results = rule.ResultData;
	
	if(results["Throttled Calls"] == 0)
	{
		return $("<div>").text("No throttled calls found.");
	}
	
	var resultTable = $("<table>").addClass("detail-table");
	var headerRow = $("<tr>");
	var resultRow = $("<tr>");
	
	headerRow.append($("<td>").addClass("header").text("Total Calls"));
	headerRow.append($("<td>").addClass("header").text("Throttled Calls"));
	headerRow.append($("<td>").addClass("header").text("Percentage"));
	
	resultRow.append($("<td>").addClass("cell").text(results["Total Calls"]));
	if(rule.Result == "Warning")
	{
		resultRow.append($("<td>").addClass("warning").text(results["Throttled Calls"]).css("background-color", "yellow"));
	}
	else if(rule.Result == "Error")
	{
		resultRow.append($("<td>").addClass("error").text(results["Throttled Calls"]).css("background-color", "red"));
	}
	resultRow.append($("<td>").addClass("cell").text((Number(results["Percentage"]) * 100).toFixed(2) + "%"));
	
	resultTable.append(headerRow, resultRow);
	
	return resultTable;
};

ruleDisplayFunctions["Polling Detection"] = function(rule){
	var count = rule.ResultData["Polling Sequences Found"];
	if(count == 0)
	{
		return $("<div>").text("No polling sequences found.");
	}
	return $("<div>").html("<b>Polling Sequences Found:</b> " + rule.ResultData["Polling Sequences Found"]);
}

ruleDisplayFunctions["Call Frequency"] = function(rule){
	var sustainedExceeded = rule.ResultData["Times Sustained Exceeded"];
	var burstExceeded = rule.ResultData["Times Sustained Exceeded"];
	
	if(sustainedExceeded == 0 && burstExceeded == 0)
	{
		return $("<div>").text("All calls within allowed limits.");
	}
	
	var result = $("<table>").attr("align","center").addClass("detail-table");
	
	result.append($("<tr>").append($("<td>").addClass("header"), $("<td>").addClass("header").text("Sustained"), $("<td>").addClass("header").text("Burst")));
	result.append($("<tr>").append($("<td>").addClass("header").text("Call Period in Seconds"), $("<td>").addClass("cell").text(rule.ResultData["Sustained Call Period"]), $("<td>").addClass("cell").text(rule.ResultData["Burst Call Period"])));
	result.append($("<tr>").append($("<td>").addClass("header").text("Max Calls in Period"),    $("<td>").addClass("cell").text(rule.ResultData["Sustained Call Limit"]),  $("<td>").addClass("cell").text(rule.ResultData["Burst Call Limit"])));

	if(Number(sustainedExceeded) > 0)
	{
	    sustainedExceeded = $("<td>").addClass("error").text(rule.ResultData["Times Sustained Exceeded"]);
	}
	else
	{
	    sustainedExceeded = $("<td>").addClass("cell").text(rule.ResultData["Times Sustained Exceeded"]);
	}
	
	if(Number(burstExceeded) > 0)
	{
	    burstExceeded = $("<td>").addClass("error").text(rule.ResultData["Times Burst Exceeded"]);
	}
	else
	{
	    burstExceeded = $("<td>").addClass("cell").text(rule.ResultData["Times Burst Exceeded"]);
	}
	
	result.append($("<tr>").append($("<td>").addClass("header").text("Times Exceeded"), sustainedExceeded, burstExceeded));
	
	return result;
}

ruleDisplayFunctions["Burst Detection"] = function(rule){
	
	if(rule.ResultData["Total Bursts"] == 0)
	{
		return $("<div>").text("No bursts of calls detected.");
	}
	
	var resultTable = $("<table>").addClass("detail-table");
	var headerRow = $("<tr>");
	var resultRow = $("<tr>");
	
	headerRow.append($("<td>").addClass("header").text("Average Calls Per Second"));
	headerRow.append($("<td>").addClass("header").text("Std. Deviation"));
	headerRow.append($("<td>").addClass("header").text("Min. Calls in Burst"));
	headerRow.append($("<td>").addClass("header").text("Burst Time Window in Seconds"));
	headerRow.append($("<td>").addClass("header").text("Total Bursts Found"));
	
	resultRow.append($("<td>").addClass("cell").text(Number(rule.ResultData["Avg. Calls Per Sec."]).toFixed(3)));
	resultRow.append($("<td>").addClass("cell").text(Number(rule.ResultData["Std. Deviation"]).toFixed(4)));
	resultRow.append($("<td>").addClass("cell").text(rule.ResultData["Burst Size"]));
	resultRow.append($("<td>").addClass("cell").text(rule.ResultData["Burst Window"]));
	
	var totalBursts = $("<td>").addClass("warning").text(rule.ResultData["Total Bursts"]);
	
	resultRow.append(totalBursts);
	
	resultTable.append(headerRow, resultRow);
	
	return resultTable;
};

ruleDisplayFunctions["Small-Batch Detection"] = function(rule){
	if(rule.ResultData["Calls Below Count"] == 0)
	{
		return $("<div>").text("No calls with small batch counts detected.");
	}
	var resultTable = $("<table>").addClass("detail-table");
	var headerRow = $("<tr>");
	var resultRow = $("<tr>");
	
	headerRow.append($("<td>").addClass("header").text("Total Batch Calls"));
	headerRow.append($("<td>").addClass("header").text("Min. Users Allowed"));
	headerRow.append($("<td>").addClass("header").text("Calls Below Count"));
	headerRow.append($("<td>").addClass("header").text("Percent Below Count"));
	
	resultRow.append($("<td>").addClass("cell").text(Number(rule.ResultData["Total Batch Calls"])));
	resultRow.append($("<td>").addClass("cell").text(Number(rule.ResultData["Min. Users Allowed"])));
	resultRow.append($("<td>").addClass("warning").text(rule.ResultData["Calls Below Count"]));
	resultRow.append($("<td>").addClass("cell").text((Number(rule.ResultData["% Below Count"]).toFixed(4) * 100) + "%"));
	
	resultTable.append(headerRow, resultRow);
	
	return resultTable;
};

ruleDisplayFunctions["Batch Frequency"] = function(rule){
	if(rule.ResultData["Times Exceeded"] == 0)
	{
		return $("<div>").text("No batch calls to potentially combine.");
	}
	
	var table = ruleDisplayFunctions["default"](rule);
	$($($(table.children()[0]).children()[1]).children()[2]).addClass("warning");
	
	return table;
};


















