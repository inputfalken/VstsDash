﻿(function() {
    google.charts.load("current", { 'packages': ["corechart"], 'language': "en" });
    google.charts.setOnLoadCallback(function() {
        $(function () {
            var params = getParams();

            drawCharts(params);
        });
    });

    function getParams() {
        var $activityContainer = $(".activity-container");
        if (!$activityContainer.length) {
            throw Error("Could not find activity-container-element.");
        }

        return {
            iterationId: $activityContainer.data("qsIterationId"),
            projectId: $activityContainer.data("qsProjectId"),
            teamId: $activityContainer.data("qsTeamId"),
            fromDate: parseFloat($activityContainer.data("fromDate")) || "",
            toDate: parseFloat($activityContainer.data("toDate")) || "",
            memberMaxCommits: parseInt($activityContainer.data("memberMaxCommits"), 10) || "",
            memberMaxChanges: parseInt($activityContainer.data("memberMaxChanges"), 10) || ""
        };
    }

    function drawCharts(params) {
        var teamActivitiesUrl = "/api/work/teamactivities?" +
            (params.iterationId ? "iterationId=" + params.iterationId + "&" : "") +
            (params.projectId ? "projectId=" + params.projectId + "&" : "") +
            (params.teamId ? "teamId=" + params.teamId : "");

        $.getJSON(teamActivitiesUrl,
            function(data) {
                drawTeamActivities(params, data);
            });

        var $memberActivities = $(".activity-member-activity");

        for (var i = 0; i < $memberActivities.length; i++) {
            var $memberActivity = $memberActivities.eq(i),
                memberId = $memberActivity.data("memberId");

            var memberActivityUrl = "/api/work/memberactivities/" +
                ((memberId || "") + "?") +
                (params.iterationId ? "iterationId=" + params.iterationId + "&" : "") +
                (params.projectId ? "projectId=" + params.projectId + "&" : "") +
                (params.teamId ? "teamId=" + params.teamId : "");

            getMemberActivitJson(params, memberActivityUrl, $memberActivity);
        }
    }

    function getMemberActivitJson(params, memberActivityUrl, $memberActivity) {
        $.getJSON(memberActivityUrl,
            function(data) {
                drawMembersActivity(params, data, $memberActivity);
            });
    }

    function drawMembersActivity(params, data, $memberActivity) {
        var mappedData = data.map(function(x) {
            x[0] = new Date(x[0]);
            return x;
        });

        data = [["Date", "Commits", "Changes"]].concat(mappedData);

        var options = getChartOptions();

        options.chartArea = {
            width: "100%",
            height: "100%"
        };

        if (params.memberMaxCommits) {
            options.vAxes[0].minValue = 0;
            options.vAxes[0].maxValue = params.memberMaxCommits;
        }
        if (params.memberMaxChanges) {
            options.vAxes[1].minValue = 0;
            options.vAxes[1].maxValue = params.memberMaxChanges;
        }

        drawLineChart(data, options, $memberActivity[0]);
    }

    function drawTeamActivities(params, data) {
        var mappedData = data.map(function(x) {
            x[0] = new Date(x[0]);
            return x;
        });

        data = [["Date", "Commits", "Changes"]].concat(mappedData);

        var options = getChartOptions();

        options.chartArea = {
            width: "85%",
            height: "85%"
        };

        addHAxisMinMaxValues(params, options);

        drawLineChart(data, options, document.getElementById("team-activities-chart"));
    }

    function drawLineChart(data, options, element) {
        var dataTable = google.visualization.arrayToDataTable(data);

        var chart = new google.visualization.LineChart(element);

        chart.draw(dataTable, options);
    }

    function getChartOptions() {
        var options = {
            curveType: "function",
            legend: { position: "top" },
            series: {
                0: { targetAxisIndex: 0 },
                1: { targetAxisIndex: 1 }
            },
            hAxis: {
                format: "yyyy-MM-dd"
            },
            vAxes: {
                0: { title: "Commits" },
                1: { title: "Changes" }
            }
        };
        return options;
    }

    function addHAxisMinMaxValues(params, options) {
        if (params.fromDate) {
            options.hAxis.minValue = new Date(params.fromDate);
        }
        if (params.toDate) {
            options.hAxis.maxValue = new Date(params.toDate);
        }
    }
})();