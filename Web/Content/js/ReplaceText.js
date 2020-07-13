function ReplaceText(elementId, code) {
    var newText = GetLookupText(code);
    if (!(newText == "")) {
        $(elementId).text(newText);
    }
}

function GetLookupText(code) {
    var text = "";
    $.ajax({
        type: "GET",
        url: "/Defws/GetLookupTextByCode",
        data: param = { lkpCd: code },
        async: false,
        cache: false,
        success: function (result) {

            text = result;
        },
        error: function (xhr, status, err) {
            alert("Error loading drop down for " + lookupCode + ".  Status: " + xhr.status + "  Message: " + err);
        }
    });

    return text;
}
