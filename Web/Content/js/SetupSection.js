function SetupSection(toolTipWidths, defaultWidth, controlPrefix) {
    var toolTipWidth = 600;
    if (defaultWidth) {
        toolTipWidth = defaultWidth;
    }

    $(".toolTip").live("mouseout", hideddrivetip);
    
    // bind the mouse over for the tooltips
    $(".toolTip").each(function(i, item) {
        var id = $(this)[0].id;
        var ttWidth = toolTipWidth;
        var prefix = controlPrefix ? controlPrefix : "ctl00_ctl00_cntMain_cntSection_";
        var resourceId = id.replace(prefix, "");
        var tipText = '';

        // have a custom tool tip width for the control?
        if (toolTipWidths != null && toolTipWidths[resourceId] != null) {
            ttWidth = toolTipWidths[resourceId];
        }

        // add an empty paragraph tag for the tooltip
        var pId = id.substring(0, 1).toUpperCase() + id.substring(1, id.length);
        $("#" + id).after('<p id="p' + pId + '" class="hidden"></p>');

        PageMethods.GetResourceText(resourceId,
                 function(result) {
                     // have a tool tip?
                     if (result != "") {
                         $("#" + id).live("mouseover", function() { ddrivetip(result, ttWidth); });
                         $("#" + id).bind("focus", function() {
                             // set the text of the tooltip
                             $("#" + id + " ~ p").html(result);
                         });

                         $(".toolTip").bind("blur", function() {
                             // clear the text of teh tooltip
                             $("#" + id + " ~ p").html("");
                         });
                     }
                 }, null, null);
    });
}
function LoadCirclesForQuestions(min, max) {
    //  Load circles for question values
    $("input.sectionTextBox").each(function(index, item) {
        SetCircleChange(min, max, $(this).attr("id"), '/Images/', '');
    });
}
function BindCircleChangeForQuestions(min, max) {
    // set the change event for each question
    $("input.sectionTextBox").change(function() {
        SetCircleChange(min, max, $(this).attr("id"), '/Images/', '');
    });
    $("input.sectionTextBox").keypress(function(e) {
        return OnlyAllowNumbers(this, e);
    });
}
