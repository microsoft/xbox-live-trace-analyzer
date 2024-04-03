API = "C";
var rules = ["Call Frequency", "Burst Detection", "Throttled Call Detection", "Small-Batch Detection", "Batch Frequency", "Repeated Calls", "Polling Detection"];


function Heatmap() {
}
Heatmap.prototype = {
	build: function(data) {
		this.data = data;
		this.table = $("<table>").addClass("atg-heatmap");
		var header = $("<thead>");
		var headerRow = $("<tr>");
		var serviceCell = $("<td>");

		var table = this.table;

		header.append(headerRow.append(serviceCell));

		$.each(rules, function(index, r){
			var cell = $("<div>").text(r);

			var ruleCell = $("<td>").addClass("col-header")
									.css({"text-align": "center"})
									.append(cell);

			headerRow.append(ruleCell);
		});
		
		var body = $("<tbody>");
		$.each(data.Results, function(endpointIndex, endpoint){
			var getRuleResults = this._getRuleResults;
			var getRuleCounts = this._getRuleCounts;
			
			var serviceCol = $("<td>").attr("id", "0" + endpointIndex)
									  .text(endpoint[API + "Service"])
									  .addClass("service-col");

			var endpointRow = $("<tr>").append(serviceCol);

			$.each(rules,function(ruleIndex, rule){
				var result = Heatmap._getRuleResults(rules[ruleIndex], endpoint.Rules);
				var ruleCol = $("<td>").addClass("result-cell")
									   .addClass(result)
									   .attr("align","center")
									   .text(Heatmap._getRuleCounts(rules[ruleIndex], endpoint.Rules));

				endpointRow.append(ruleCol);
			});
			body.append(endpointRow);
		});
		
		this.table.append(header, body);
	},
	
	show: function(element) {
		element.append(this.table);
		this.table.find(".service-col").click(function () {
			var scrollbox = $(".result-container");
			var index = $(this).parent().index();
			var endpoint = $("#e0" + index);
			scrollbox.scrollTop(0);
			scrollbox.scrollTop(endpoint.offset().top - scrollbox.offset().top + 1);
		});
		this.table.find(".result-cell:not(.Pass)").click(function () {
			var scrollbox = $(".result-container");
			var epIndex = $(this).parent().index();
			var ruleIndex = $(this).index() - 1;
			var endpoint = $("#r" + ruleIndex + "" + epIndex);
			scrollbox.scrollTop(0);
			scrollbox.scrollTop(endpoint.offset().top - scrollbox.offset().top - 34);
		});
		this.table.delegate('.result-cell', 'mouseover mouseleave', function (e) {
			if (e.type == 'mouseover') {
				$(".col-header").eq($(this).index() - 1).addClass("hover");
			}
			else {
				$(".col-header").eq($(this).index() - 1).removeClass("hover");
			}
		});
	},
	cleanUp: function() {

	},
	changeAPI: function(API) {
		var data = this.data;
		var body = this.table.children("tbody");
		var rows = body.children();
		
		$.each(rows, function (index, row) {
			var serviceCell = $(row).children("td:first-child");
			endpoint = data.Results[index];
			serviceCell.text(endpoint[API + "Service"]);
		});
	},
};

Heatmap._getRuleResults = function(ruleName, rules) {
	var rule = getRule(ruleName, rules);
	
	if(rule != null)
	{
		return rule.Result;
	}
	return "None";
};

Heatmap._getRuleCounts = function(ruleName, rules) {
	for(var i = 0; i < rules.length; ++i){
		if(rules[i].Name === ruleName)
		{
			if(rules[i].Result == "Pass")
			{
				return "";
			}
			else if(rules[i].Result == "Error")
			{
				return rules[i].Error;
			}
			else if(rules[i].Result == "Warning")
			{
				return rules[i].Warning;
			}
			return "";
		}
	} 
	return "-";
};

function ViolationDetails() {
	this.expanded = false;
};
ViolationDetails.prototype = {
	build: function(violation) {
		this.violation = violation;
		this.violationDiv = $("<div>");

		this.expander = $("<div>").addClass("atg-expander");
		var violationDescription = $("<div>").addClass("summary-line");
		this.expanderData = [this, this.expander, violationDescription];
		var image = $("<img>").attr("src", "img/" + violation.Level + ".png").addClass("result-icon");
		violationDescription.append(image, violation.Summary);

		var callList = $("<ul>").css({ "padding-left": "44px" });

		$.each(violation.Calls, function (callIndex, call) {
			var listItem = $("<li>");
			var callLine = $("<div>").addClass("api-call")
									 .attr("onclick","javascript:showCalls(" + call["Call Id"] + ")")
									 .text(call["Call Id"] + " " + call[API + "Method"]);
			var copyIcon = $("<div>").addClass("atg-copy");

			listItem.click(function () {
				showCalls(call["Call Id"]);
			});

			callList.append(listItem.append(callLine));
		});

		this.expandElement = callList.hide();
		this.violationDiv.append(this.expander, violationDescription, callList);
	},
	show: function(element) {
		element.append(this.violationDiv);
		this.fix();
	},
	fix: function() {
		this.expander.click(this.expanderData, toggleExpanderElemement);

		this.violationDiv.find("api-call")
	},
	changeAPI: function(API){
		this.expandElement.empty();
		var callList = this.expandElement;
		$.each(this.violation.Calls, function (callIndex, call) {
			var listItem = $("<li>");
			var callLine = $("<div>").addClass("api-call")
									 .attr("onclick", "javascript:showCalls(" + call["Call Id"] + ")")
									 .text(call["Call Id"] + " " + call[API + "Method"]);
			var copyIcon = $("<div>").addClass("atg-copy");

			listItem.click(function () {
				showCalls(call["Call Id"]);
			});

			callList.append(listItem.append(callLine));
		});
	}
};

function RuleDetails() {
	this.violationData = [];
	this.expanded = false;
};
RuleDetails.prototype = {
	build: function(rule, endpointIdx, ruleIdx) {
		this.rule = rule;
		this.listItem = $("<li>");
		var rowDiv = $("<div>").addClass("row-div");
		var ruleNameDiv = $("<div>").addClass("rule-name").attr("id", "r" + ruleIdx + "" + endpointIdx);
		this.expander = $(`<div role='button' tabindex='0' aria-expanded='false' aria-label='${rule.Name}'>`).addClass("atg-expander");
		this.expanderData = [this, this.expander, ruleNameDiv];
		this.expander.click(this.expanderData, toggleExpanderElemement);
		var image = $("<img>").attr("src", "img/" + rule.Result + ".png").addClass("result-icon");
		ruleNameDiv.append(image, rule.Name);

		rowDiv.append(this.expander, ruleNameDiv)
		if (rule.Result == "Pass") {
			var cell = $("<div>").addClass("counter-cell").addClass("pass").text(0);
			rowDiv.append(cell);
		}
		else {
			if (rule.Warning > 0) {
				var cell = $("<div>").addClass("warning").addClass("counter-cell").text(rule.Warning);
				rowDiv.append(cell);
			}
			if (rule.Error > 0) {
				var cell = $("<div>").addClass("error").addClass("counter-cell").text(rule.Error);
				rowDiv.append(cell);
			}
		}

		var detailDiv = $("<div>").addClass("rule-detail-container");
		var description = $("<div>").text(rule.Description).css("margin-bottom","5px");
		var resultView = this._displayRuleResults(rule);
		detailDiv.append(description, resultView);

		var violations = $("<div>").css({"margin-top":"20px", "margin-bottom": "20px"});

		var violationData = this.violationData;
		$.each(rule.Violations, function (index, violation) {
			var violatonDetails = new ViolationDetails();
			violatonDetails.build(violation);
			violatonDetails.show(violations);
			violationData.push(violatonDetails);
		});

		this.expandElement = detailDiv.hide();
		this.listItem.append(rowDiv, detailDiv.append(violations));
	},
	
	show: function(element) {
		element.append(this.listItem);
	},
	fix: function () {
		this.expander.click(this.expanderData, toggleExpanderElemement);
		this.listItem.find(".rule-name").click(this.expanderData, function (event) {
			var scrollbox = $(".result-container");
			scrollbox.scrollTop(0);
			scrollbox.scrollTop(event.data[2].offset().top - scrollbox.offset().top - 34);
		
			toggleExpanderElemement(event);
		});

		$.each(this.violationData, function (index, violation) {
			violation.fix();
		});
	},
	
	changeAPI: function(API) {
		$.each(this.violationData, function (index, violation) {
			violation.changeAPI(API);
		});
	},
	_displayRuleResults: function(rule) {
		var displayFunc = ruleDisplayFunctions[rule.Name];
		if(displayFunc == null)
		{
			displayFunc = ruleDisplayFunctions["default"];
		}
		return displayFunc(rule);
	}
};

function EndpointDetails() {
	this.rules = [];
};
EndpointDetails.prototype = {
	build: function(results) {
		this.data = results;
		var ruleData = this.rules;
		this.endpointContainer = $("<div>").addClass("result-container");
		var endpointList = $("<ul>").addClass("endpoint-results");

		$.each(results, function (index, endpoint) {
			var listItem = $("<li>").addClass("endpoint-details");
			var header = $("<div>").addClass("endpoint-header").text(endpoint[API + "Service"]).attr("id","e0" + index);

			listItem.append(header);

			$.each(rules, function (ruleIndex, ruleName) {
				var rule = getRule(ruleName, endpoint.Rules);

				if (rule == null) {
					return;
				}

				var ruleList = $("<ul>");

				var ruleDetail = new RuleDetails();
				ruleDetail.build(rule, index, ruleIndex);
				ruleDetail.show(ruleList);
				ruleData.push(ruleDetail);

				listItem.append(ruleList);
			});

			endpointList.append(listItem);
		});

		this.endpointContainer.append(endpointList.append($("<div>").css({ "height": "435px", "border-left": "1px solid black" })));
	},
	
	show: function(element) {
		element.append(this.endpointContainer);

		$.each(this.rules, function (index, rule) {
			rule.fix();
		});

		$(".endpoint-header-fixed").remove();
		$(".endpoint-header").fixThis();
		this.clean = false;
	},
	cleanUp: function () {
		if (this.clean == false || this.clean == "undefined") {
			this.clean = true;
			$(".endpoint-header-fixed").remove();
			$(".endpoint-header").unwrap();
		}
	},
	changeAPI: function (API) {
		var results = this.data;
		$.each(this.endpointContainer.find(".endpoint-header"), function (index, header) {
			var idx = Number(index / 2);
			$(header).text(results[Math.floor(idx)][API + "Service"]);
		});
		$.each(this.rules, function(index, rule){
			rule.changeAPI(API);
		});
	}
};

function ReportPage() {
	this.endpointDetails = new EndpointDetails();
	this.heatmap = new Heatmap();
}

ReportPage.prototype = {
	build: function(data) {
		this.data = data;
		this.heatmap.build(data);
		this.endpointDetails.build(data.Results);
	},
	show: function(element) {
		this.heatmap.show(element.empty());
		this.endpointDetails.show(element);
	},
	cleanUp: function () {
		//this.heatmap.cleanUp();
		this.endpointDetails.cleanUp();
	},
	changeAPI: function(API) {
		this.heatmap.changeAPI(API);
		
		this.endpointDetails.changeAPI(API);
	}
}

function StatsPage() {
	this.timelineGraphData = [];
	this.timelineGraphOptions = {
			lines: {
				show: true
			},
			zoom: {
				interactive: true
			},
			pan: {
				interactive: true
			},
			xaxis: {
				min: 0,
				tickDecimals: 0,
				tickFormatter: function(number, obj) {
					var seconds = number % 60;
					var minutes = (number / 60).toFixed(0);
					
					if(seconds < 10) {
						return (minutes + ":0" + seconds);
					}
					else {
						return (minutes + ":" + seconds);
					}
				}
				
			},
			yaxis: {
				min: 0,
				tickDecimals: 0,
				minTickSize: 1,
			},
			grid: {
				backgroundColor: "#3C3C3C",
				borderColor: "black",
				borderWidth: 1
			}
		};
		
	this.callCountGraphData = [];
	this.callCountGraphOptions = {
			series: {
				bars: {
						show: true,
						barWidth: .6,
						align: "center",
						horizontal: true,
					lineWidth: 1
				},
				
			},
			colors: ["#00BB00"],
			yaxis: {
				mode: "categories",
				tickLength: 0,
				
			},
			grid: {
				backgroundColor: "#3C3C3C",
				borderColor: "black",
				borderWidth: 1,
			}
		};
}

StatsPage.prototype = {
	build: function(stats, calls) {
		this.stats = stats;
		this.calls = calls;
		this.countTitle = $("<div>").addClass("graph-header").text("Calls Per Endpoint");
		this.callCountgraph = $("<div>").css({ "width": "780px", "height": "300px", "margin": "auto"});
		this._buildCountsGraph(this.stats);
		
		
		this.statDetails = $("<table summary='Details of endpoint, total calls and average time between calls'>").addClass("stats-table");
		var header = $("<tr>");
		var endpoint = $("<td>").text("Endpoint").addClass("endpoint");
		var totalCalls = $("<td>").text("Total Calls").addClass("center-text");
		var avgTime = $("<td>").text("Average Time Between Calls");
		this.statDetails.append(header.append(endpoint, totalCalls, avgTime));
		var details = this.statDetails;
		$.each(this.stats, function(index, stat) {
			var endpointRow = $("<tr>");
			var endpointName = stat[API]? stat[API] : stat["Uri"];
			var endpoint = $("<td>").addClass("endpoint").html("<b>" + endpointName + "</b>");
			var totalCalls = $("<td>").addClass("center-text").text(stat["Call Count"]);
			var avgTime = $("<td>").text(Number(stat["Average Time Between Calls"] / 1000).toFixed(3) + " seconds");

			endpointRow.append(endpoint, totalCalls, avgTime);
			details.append(endpointRow);
		});	
		
		var callCountGraphData = this.callCountGraphData;
		
		$.each(stats, function(index, stat) {
			callCountGraphData.push([ stat["Call Count"],stat[API] ]);
		});
		
		this.timelineTitle = $("<div>").addClass("graph-header").text("Calls Per Second");
		this.timelineGraph = $("<div>").css({ "width": "780px", "height": "300px", "margin": "auto"});
		var timelineGraphData = this.timelineGraphData;
		var startTime = calls["Start Time"];
		var endTimeRel = Number((calls["End Time"] - startTime) / 1000).toFixed(0);
		var maxHeight = 0;
		
		$.each(this.calls["Call List"], function(index, endpoint) {
			var endpointData = {
				label: endpoint[API],
				data: [],
				endpoint: endpoint
			};
			var callsPerSecond = {};
			$.each(endpoint.Calls, function(index, call) {
				var relTime = ((call.ReqTime - startTime) / 1000).toFixed(0);
				if(callsPerSecond[relTime.toString()] === undefined)
				{
					callsPerSecond[relTime.toString()] = 0;
				}
				callsPerSecond[relTime.toString()]++;
			});
			
			if(callsPerSecond["0"] === undefined)
			{
				endpointData.data.push([0,0]);
			}
			
			for(second in callsPerSecond)
			{
				var secondNum = Number(second);
				
				if(callsPerSecond[(secondNum - 1).toString()] === undefined)
				{
					endpointData.data.push([ secondNum - 1, 0 ]);
				}
				
				endpointData.data.push([ secondNum, callsPerSecond[second] ]);
				
				if(callsPerSecond[second] > maxHeight) {
					maxHeight = callsPerSecond[second];
				}
				
				if(callsPerSecond[(secondNum + 1).toString()] === undefined)
				{
					endpointData.data.push([ secondNum + 1, 0 ]);
				}
			}
			
			timelineGraphData.push(endpointData)
		});
		
		this.timelineGraphOptions.xaxis.zoomRange = [5,endTimeRel];
		this.timelineGraphOptions.xaxis.panRange = [0,endTimeRel];
		this.timelineGraphOptions.xaxis.max = endTimeRel;
		
		this.timelineGraphOptions.yaxis.zoomRange = [5,maxHeight * 1.1];
		this.timelineGraphOptions.yaxis.panRange = [0,maxHeight * 1.1];
		this.timelineGraphOptions.yaxis.max = maxHeight * 1.1;

	},
	show: function(element) {
		element.append(this.countTitle, this.callCountgraph, this.timelineTitle, this.timelineGraph, this.statDetails);
	},
	changeAPI: function (API) {
		var callCountGraphData = [];

		$.each(stats, function (index, stat) {
			callCountGraphData.push([stat["Call Count"], stat[API]]);
		});

		this.callCountGraphData = callCountGraphData;
		this._buildCountsGraph();
		
		this.statDetails.empty();
		var header = $("<tr>").addClass("header-row");
		var endpoint = $("<th>").text("Endpoint").addClass("endpoint");
		var totalCalls = $("<th>").text("Total Calls").addClass("center-text");
		var avgTime = $("<th>").text("Average Time Between Calls").addClass("center-text");
		this.statDetails.append(header.append(endpoint, totalCalls, avgTime));
		var details = this.statDetails;
		$.each(this.stats, function (index, stat) {
			var endpointRow = $("<tr>");
			var endpointName = stat[API]? stat[API] : stat["Uri"];
			var endpoint = $("<td>").addClass("endpoint").html(endpointName);
			var totalCalls = $("<td>").addClass("center-text").text(stat["Call Count"]);
			var avgTime = $("<td>").addClass("time").text(Number(stat["Average Time Between Calls"] / 1000).toFixed(3) + " seconds");

			endpointRow.append(endpoint, totalCalls, avgTime);
			details.append(endpointRow);
		});
		
		$.each(this.timelineGraphData, function(index, endpoint) {
			endpoint.label = endpoint.endpoint[API];
		});
		
		this._buildCountsGraph();
		this._buildTimelineGraph();
	},
	_buildCountsGraph: function() {
		this.callCountgraph.empty();
		$.plot(this.callCountgraph, [ this.callCountGraphData ], this.callCountGraphOptions);
	},
	_buildTimelineGraph: function() {
		this.timelineGraph.empty();
		$.plot(this.timelineGraph, this.timelineGraphData, this.timelineGraphOptions);
	}
}

function CallPage() {
}

CallPage.prototype = {
	build: function(calls) {
		this.calls = calls;
		this.processedCalls = [];
		var processedCalls = this.processedCalls;
		var callDetails = $("<div>").addClass("result-container").css({
			"width": "100%",
			"height": "700px",
			"border-left": "1px solid black"
		});
		
		var endpointList = $("<ul>").addClass("endpoint-results");;

		$.each(this.calls["Call List"], function(index, endpoint) {
			var listItem = $("<li>").addClass("endpoint-details");
			var totalCalls = $("<div>").css({
				"float": "right",
				"font-size": "12px"
			}).text("Total Calls: " + endpoint.Calls.length);
			var header = $("<div>").addClass("endpoint-header").css({
				"width": "100%",
				"margin-left": "-1px",
				"text-align": "left"
			}).text(endpoint[API]).append(totalCalls);
			
			var callList = $("<ul>").css({"list-style-type": "none"});
			$.each(endpoint.Calls, function(index, call) {
				var callLI = $("<li>").css({"padding": "5px", "border-bottom": "1px solid black"});
				
				callLI.append("<div style='float: left; width: 40px; text-align: right; padding-right: 5px; color: #DCDCDC' id='call" + call.Id + "'><b>" + call.Id + "</b></div>");
				
				var callDetailDiv = $("<div>").css({"margin-left":"40px", "padding-left": "5px"});
				
				callDetailDiv.append("<div>" + call.Uri + "</div>");
				
				var apiDiv = $("<div>");
				
				if(API != "URI") {
					apiDiv = $("<div><b>" + API + " Method:</b> " + call[API] + "</div>");
					callDetailDiv.append(apiDiv);
				}
				
				if(call["Request Body"] != "")
				{
					var body = "";
					try{
						body = JSON.stringify(JSON.parse(call["Request Body"]), null, 2);
					} catch(e){
						body = call["Request Body"];
					}
						
					callDetailDiv.append("<div><b>Request Body:</b><br /><pre> " + body + "</pre></div>");
				}
				
				var processedCall = {};
				processedCall.call = call;
				processedCall.div = apiDiv;
				processedCalls.push(processedCall);
				
				callLI.append(callDetailDiv);
				callList.append(callLI);
			});
			
			listItem.append(header, callList);
			
			endpointList.append(listItem);
		});
		
		
		this.callDetails = callDetails.append(endpointList.append($("<div>").css("height","600px")));
	},
	show: function(element, callId) {
		element.append(this.callDetails);
		this.callDetails.find(".endpoint-header").fixThis();
		this.clean = false;
		if(typeof callId !== "undefined") {
			var scrollbox = $(".result-container");
			var endpoint = $("#call" + callId);
			scrollbox.scrollTop(0);
			scrollbox.scrollTop(endpoint.offset().top - scrollbox.offset().top - 40);
		}
	},
	changeAPI: function(API) {
		var processedCalls = this.processedCalls;
		$.each(processedCalls, function(index, processedCall) {
			if(API == "Uri") {
				processedCall.div.empty();
			}
			else {
				processedCall.div.html("<b>" + API + " Method:</b> " + processedCall.call[API]);
			}
		});

		var callList = this.calls["Call List"];
		
		$.each(this.callDetails.find(".endpoint-header").not(".endpoint-header-fixed"), function(index, header) {
			var endpoint = callList[index];
			var totalCalls = $("<div>").css({
					"float": "right",
					"font-size": "12px"
			}).text("Total Calls: " + endpoint.Calls.length);
			
			if(endpoint[API])
			{
				$(header).text(endpoint[API]);
			}
			else
			{
				$(header).empty();
			}
			$(header).append(totalCalls);
		});
	},
	cleanUp: function() {
		if (this.clean == false || this.clean == "undefined") {
			this.clean = true;
			this.callDetails.find(".endpoint-header-fixed").remove();
			this.callDetails.find(".endpoint-header").unwrap();
		}
	},
}

function CertWarningPage(){
}

CertWarningPage.prototype = {
	build: function(warnings) {
	    this.warnings = warnings;
	    this.expanders = [];
	    
	    var expanders = this.expanders;

		var warningDiv = $("<div>").addClass("result-container").css({
			"width": "100%",
			"height": "700px",
		});
		
		var warningList = $("<ul>").addClass("endpoint-results");

				
		$.each(warnings, function (index, warning) {
		    var listItem = $("<li>").addClass("endpoint-details");
		    var rowDiv = $("<div>").addClass("row-div");

		    
		    var expander = $("<div>").addClass("atg-expander");
		    var ruleNameDiv = $("<div>").addClass("rule-name");
		    var image = $("<img>").attr("src", "img/Error.png").addClass("result-icon");
		    rowDiv.append(expander, ruleNameDiv)
		    ruleNameDiv.append(image, $("<span>").text(warning.XRName));

		    var detailDiv = $("<div>").addClass("rule-detail-container");

		    var warningRequirement = $("<span>").html("<b>Requirement:</b> " + warning.Requirement + "<br />");
		    var warningRemark = $("<span>").html("<b>Remark:</b> " + warning.Remark + "<br />");
		    var warningIntent = $("<span>").html("<b>Intent:</b> " + warning.Intent);

		    var expanderProperties = {
		        expanded: false,
		        expandElement: detailDiv.append(warningRequirement, warningRemark, warningIntent).hide(),
		        fix: function () {
		            this.expander.click([this, this.expander, ruleNameDiv], toggleExpanderElemement);
		        }
		    };

		    expanderProperties.expander = expander;

		    expanders.push(expanderProperties);

		    warningList.append(listItem.append(rowDiv,detailDiv));
		});

		this.warningDiv = warningDiv.append(warningList,$("<div>").css({ "height": "700px", "border-left": "1px solid black" }));

	},
	show: function(element) {
	    element.append(this.warningDiv);
	    this.fix();
	},
	fix: function () {
	    $.each(this.expanders, function (index, expanderProperties) {
	        expanderProperties.fix();
	    });
    }
}


var getRule = function(ruleName, rules){
	for(var i = 0; i < rules.length; ++i){
		if(rules[i].Name === ruleName)
		{
			return rules[i];
		}
	} 
	return null;
}

var getUrlParameter = function getUrlParameter(sParam) {
	var sPageURL = decodeURIComponent(window.location.search.substring(1)),
		sURLVariables = sPageURL.split('&'),
		sParameterName,
		i;

	for (i = 0; i < sURLVariables.length; i++) {
		sParameterName = sURLVariables[i].split('=');

		if (sParameterName[0] === sParam) {
			return sParameterName[1] === undefined ? true : sParameterName[1];
		}
	}
};

function toggleExpanderElemement(obj) {
    var currentExpanded = $(this).attr("aria-expanded");
    $(this).attr("aria-expanded", currentExpanded === "true" ? "false" : "true");
	if (obj.data[0].expanded === true) {
		obj.data[0].expandElement.hide();
		obj.data[0].expanded = false;

		obj.data[1].removeClass("atg-expander-expanded");

		if (obj.data[2] != undefined) {
			obj.data[2].removeClass("expanded");

		}
	}
	else {
		obj.data[0].expandElement.show();
		obj.data[0].expanded = true;

		obj.data[1].addClass("atg-expander-expanded");

		if (obj.data[2] != undefined) {
			obj.data[2].addClass("expanded");

		}
	}
};

$.fn.fixThis = function () {
	return this.each(function (index) {
		var $this = $(this), $t_fixed;
		function init() {
			$this.wrap('<div />');
			$t_fixed = $this.clone();
			$t_fixed.removeAttr("id");
			$t_fixed.addClass("endpoint-header-fixed").insertBefore($this).width($this.width());
			var containerPosition = $(".result-container").offset().top;
			$t_fixed.css("top", containerPosition + "px");
			$t_fixed.hide();
		}
		function scrollFixed() {
			var offset = $(this).offset().top,
			tableOffsetTop = $this.parent().parent().offset().top,
			tableOffsetBottom = tableOffsetTop + $this.parent().parent().parent().height();

			if ($t_fixed.is(":hidden") == false && (offset < tableOffsetTop || offset > tableOffsetBottom)) {
				$t_fixed.hide();
			}
			else if (offset >= tableOffsetTop && offset <= tableOffsetBottom && $t_fixed.is(":hidden")) {
				$t_fixed.show();
			}
		}
		function scrollFixedWindow() {
			var offset = $(".result-container").offset().top;
			$t_fixed.css("top", offset - $(window).scrollTop());
		}
		var container = $(".result-container");
		container.scroll(scrollFixed);
		$(window).scroll(scrollFixedWindow);
		init();
	});
};