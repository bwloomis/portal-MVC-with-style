
function populateLookupSelect(lookupCode, selectId, target) {
    $.ajax({
        type: "GET",
        url: "/Defws/GetLookupADAP",
        data: param = { lkpCd: lookupCode },
        async: false,
        cache: false,
        success: function (result) {

            var obj = JSON.parse(result);
            var foundTarget = false;

            var mySelect = $('#' + selectId);
            for (var i = 0; i < obj.def_LookupDetail.length; i++) {
                if (typeof obj.def_LookupDetail[i]["def_LookupText"][0] !== 'undefined') {
                    if (target == obj.def_LookupDetail[i]["dataValue"])
                        foundTarget = true;
                    mySelect.append('<option value="' + obj.def_LookupDetail[i]["dataValue"] + '">' + obj.def_LookupDetail[i]["def_LookupText"][0]["displayText"] + '</option>');
                }
            }
            if (foundTarget)
                mySelect.val(target);
        },
        error: function (xhr, status, err) {
            alert("Error loading drop down for " + lookupCode + ".  Status: " + xhr.status + "  Message: " + err);
        }
    });
}

function populateLookupRadio(lookupCode, containerId, target, optionWidth1to12 ) {
    $.ajax({
        type: "GET",
        url: "/Defws/GetLookupADAP",
        data: param = { lkpCd: lookupCode },
        async: false,
        cache: false,
        success: function (result) {

            var obj = JSON.parse(result);

            var myContainer = $('#' + containerId);

            for (var i = 0; i < obj.def_LookupDetail.length; i++) {
                if (typeof obj.def_LookupDetail[i]["def_LookupText"][0] !== 'undefined') {
                    var checked = (obj.def_LookupDetail[i]["dataValue"] == target ? 'checked' : '');
                    myContainer.append(' \
                    <div class="col-md-' + optionWidth1to12 + '"> \
                        <div class="radio radio-inline"> \
                            <input type="radio" name="' + containerId + '" style="margin-left:1px" id="' + containerId + i + '" value="' + obj.def_LookupDetail[i]["dataValue"] + '" ' + checked + '> \
                            <label class="small" for="' + containerId + i + '"><span></span>' + obj.def_LookupDetail[i]["def_LookupText"][0]["displayText"] + '</label> \
                        </div> \
                    </div>');
                }
            }
            myContainer.val(target);
        },
        error: function (xhr, status, err) {
            alert("Error loading drop down for " + lookupCode + ".  Status: " + xhr.status + "  Message: " + err);
        }
    });
}

