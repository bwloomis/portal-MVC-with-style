function makeInterviewerSelect(obj, mySelect, value) {
    mySelect.append('<option selected="selected"></option');

    for (i = 0; i < obj.length; i++) {
        mySelect.append('<option value="' + obj[i][value] + '">' + obj[i]["LoginID"] + ' (' + obj[i]["FirstName"] + ' ' + obj[i]["LastName"] +  ') </option>');
    }
}


function makeSelect2(obj, mySelect, value, display) {
    mySelect.append('<option selected="selected"></option');

    var dispVars = display.split(" ");

   for (i = 0; i < obj.length; i++) {
        mySelect.append('<option value="' + obj[i][value] + '">' + getDisp(obj[i], dispVars) + '</option>');
    }
}

function getDisp(obj, dispVars) {
    var disp = "";
    for (j = 0; j < dispVars.length; j++)
    {
        disp += obj[dispVars[j]];

        if (j < dispVars.length - 1) {
            disp += " ";
        }
    }
    return disp;
}
function populateLookupSelect(lookupCode, selectName, target) {
    $.ajax({
        type: "GET",
        url: "/Defws/GetLookup",
        data: param = { lkpCd: lookupCode },
        async: false,
        cache: false,
        success: function (result) {

            var obj = JSON.parse(result);

            var mySelect = $('#' + selectName);
            mySelect.append('<option selected="selected"></option');
            for (var i = 0; i < obj.def_LookupDetail.length; i++) {
                if (typeof obj.def_LookupDetail[i]["def_LookupText"][0] !== 'undefined') {
                    mySelect.append('<option value="' + obj.def_LookupDetail[i]["dataValue"] + '">' + obj.def_LookupDetail[i]["def_LookupText"][0]["displayText"] + '</option>');
                }
            }

            mySelect.val(target);
        },
        error: function (xhr, status, err) {
            alert("Error loading drop down for " + lookupCode + ".  Status: " + xhr.status + "  Message: " + err);
        }
    });
}

function populateGroupsSelect(selectName, target) {
    $.ajax({
        type: "POST",
        url: "/Search/GetGroups",
        async: false,
        cache: false,
        success: function (result) {

            //alert(result);
            var obj = JSON.parse(result);

            var mySelect = $('#' + selectName);

            makeSelect2(obj, mySelect, "GroupID", "GroupName");

            mySelect.val(target);

        },
        error: function () {
            alert("Error loading drop down for Groups.");

        }
    });
}
function populateUsersSelect(selectName, target) {
    $.ajax({
        type: "POST",
        url: "/Search/GetInterviewers",
        async: false,
        cache: false,
        success: function (result) {
            //alert(result);
            var obj = JSON.parse(result);
            
            var mySelect = $('#' + selectName);

            makeInterviewerSelect(obj, mySelect, "UserID");

            mySelect.val(target);
        },
        error: function () {
            alert("Error loading drop down for interviewer.");
        }
    });
}

function populateUserInfo(uId, name, phone, ext, addr, city, state, zip, email, title, agency, loginId ) {
    $.ajax({
        type: "POST",
        url: "/Search/GetInterviewerInfo",
        data: param = { userId: uId },
        async: false,
        cache: false,
        success: function (result) {

            var obj = JSON.parse(result);

            var nameField   = $('#' + name);
            var phoneField  = $('#' + phone);
            var extField    = $('#' + ext);
            var addrField   = $('#' + addr);
            var cityField   = $('#' + city);
            var stateField  = $('#' + state);
            var zipField    = $('#' + zip);
            var emailField  = $('#' + email);
            var titleField  = $('#' + title);
            var agencyField = $('#' + agency);
            var loginIdField = $('#' + loginId);

            var uName = obj["FirstName"] + " " + obj["LastName"];

            var vPhone = obj.uas_UserPhone;
            if (vPhone.length > 0) {

                var uPhone = vPhone[0]["PhoneNumber"];
                var uExt = vPhone[0]["Extension"];

            }

            var vAddr = obj.uas_UserAddress;

            if (vAddr.length > 0) {
                var uAddr = vAddr[0]["Address1"];
                var uCity = vAddr[0]["City"];
                var uState = vAddr[0]["StateProvince"];
                var uZip = vAddr[0]["PostalCode"];
            }

            var vEmail = obj.uas_UserEmail

            if (vEmail.length > 0) {
				var uEmail = vEmail[0]["EmailAddress"];
            }

            var uTitle = obj["Title"];
            var uAgency = obj["Area"];
            var uLoginId = obj["LoginID"];

            nameField   .val(uName);
            phoneField  .val(uPhone);
            extField    .val(uExt);
            addrField   .val(uAddr);
            cityField   .val(uCity);
            stateField  .val(uState);
            zipField    .val(uZip);
            emailField  .val(uEmail);
            titleField  .val(uTitle);
            agencyField .val(uAgency);
            loginIdField.val(uLoginId);

        },
        error: function () {
            alert("Error loading interviewer info.");
        }
    });
}

function restoreValues(name, phone, phoneTarget, ext, extTarget, addr, addrTarget, city, cityTarget, state, stateTarget, zip, zipTarget, email, emailTarget, title, titleTarget, agency, agencyTarget, loginId, loginIdTarget) {
    
            var nameField   = $('#' + name);
            var phoneField  = $('#' + phone);
            var extField    = $('#' + ext);
            var addrField   = $('#' + addr);
            var cityField   = $('#' + city);
            var stateField  = $('#' + state);
            var zipField    = $('#' + zip);
            var emailField  = $('#' + email);
            var titleField  = $('#' + title);
            var agencyField = $('#' + agency);
            var loginIdField = $('#' + loginId);

            var uName   = nameTarget;
            var uPhone  = phoneTarget;
            var uExt    = extTarget;
            var uAddr   = addrTarget;
            var uCity   = cityTarget;
            var uState  = stateTarget;
            var uZip    = zipTarget;
            var uEmail  = emailTarget;
            var uTitle  = titleTarget;
            var uAgency = agencyTarget;
            var uLoginId = loginIdTarget

            nameField   .val(uName);
            phoneField  .val(uPhone);
            extField    .val(uExt);
            addrField   .val(uAddr);
            cityField   .val(uCity);
            stateField  .val(uState);
            zipField    .val(uZip);
            emailField  .val(uEmail);
            titleField  .val(uTitle);
            agencyField .val(uAgency);
            loginIdField.val(uLoginId);
           
}

function onUserChange(uid) {
    
    populateUserInfo(uid, "sis_int_full_nm", 
        "sis_int_phone_num", 
        "sis_int_phone_num_ext", 
        "sis_int_addr_line1", 
        "sis_int_city",
        "sis_int_st",
        "sis_int_zip",
        "sis_int_email", 
        "sis_int_position_cd",
        "sis_int_agency_nm",
        "Interviewer_login_id");
}