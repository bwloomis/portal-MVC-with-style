
//To Submit the form
function SubmitForm(targetPartId, targetSectionId) {
    document.getElementById('navPartId').setAttribute('value', targetPartId);
    document.getElementById('navSectionId').setAttribute('value', targetSectionId);
    $("#btnSubmit").click();
}

function Initializeevents() {

    //For attchments adding pace holder
    $('.file-caption-name').attr('contentEditable', true);
    $('.file-caption-name').attr('data-placeholder', 'Receipt for claim');

    //trigger notify popups on save
    if (Notify == "Saved") {
        console.log("notify start");
        if (NotifyMessage != '')
            $.growl.notice({ title: "Save", message: NotifyMessage });
        else
            $.growl.notice({ title: "Save", message: "Application Save successfully" });

    }
    else if (Notify == "Cancel") {
        if (NotifyMessage != '')
            $.growl.error({ title: "Error", message: NotifyMessage });
        else
            $.growl.error({ title: "Error", message: "Internal server error:Review the logs" });
    }

    //Custom event for triggering the date validation
    $('#C1_ClaimDateOfService').on("ClaimDateOfService:blur", function (event) {

        CheckForDate();
    });

    $('#C1_ClaimDateOfService').blur(function () {

        CheckForDate();

    });

    $('#C1_ClaimDateOfService')
       .focus(function () { $(this).select(); })
       .mouseup(function (e) { e.preventDefault(); });

    //triggering validation on blur of amount
    $("#C1_ClaimAmount").blur(function () {

        CheckForAmount();

    });

    $('.cacurrency').maskMoney({ thousands: "", allowZero: true });

    //initialize the tooltips
    $('[data-toggle="tooltip"]').tooltip();


    //To remove input date attached in layout and initialise a new one with End date not greater than todays date
    $('.input-group.date').datepicker('remove');

    $('.input-group.date').datepicker({
        autoclose: true,
        startDate: new Date(2016,06,01),
        endDate: '+0d',
        showOnFocus: false,
        format: 'mm/dd/yyyy',


    });

    $('div.date>input').inputmask({
        "mask": ["99/99/9999"],
        "placeholder": " ",
        "showMaskOnHover": false,
        jitMasking: true
    });

    
    //hide all the layout Buttons
    $('.class_prev').click(function () {
        window.location = "/AdapCa/ClientProfile";
    });
    $('.class_nextscreen').hide();
    $('#certify').hide();
    $('#checkErrors').hide();
    $('.class_prevscreen').hide();
    $('.class_submit').hide();

   
    //Event handlers for buttons in form -- buttons available in medicalClaim.cshtml

    $('#btnsubmitChanges').click(function () {

        var isValidationSuccess = validateMOOPForm();

        //errors present
        if (!isValidationSuccess)
        {

            return false;
        }

        bootbox.confirm("By submitting the request you certify that the information provided is accurate. Click OK to submit the request.", function (result) {

            if (result) {

                $('form').attr('action', '/AdapCa/SubmitMedicalClaim');

                SubmitForm('44', '753');
            }

        });



    });

    $("#btnsave").click(function () {

        SubmitForm('44', '753');
    });


    $("#btncancel").click(function () {

        var formResultId = $("#informResultId").val();

        bootbox.confirm("Are you sure you want to cancel the request?", function (result) {
            
            if (result) {

                window.location = '/AdapCa/CancelSVF?formResultId=' + formResultId;
            }

        });

       

        return false;
    });




}


function RenderFormelementsOnStatus(status) {
   

    if (status === 0) {
        savedState();

    }
    else if (status === 1) {
        submittedState();

    }
    else if (status === 2) {

        cancelledState();
    }
}

function savedState() {

    //showInfoMessage("Click on Sa. Click on Submit to send this to benefits management for processing.Click on Cancel if you do not want to submit this claim yet and keep itfor a future date.");
}

function cancelledState() {

    hideButton('#btnsave');
    hideButton('#btnsubmitChanges');
    hideButton('#btncancel');
    showErrorMessage("This form has been cancelled.");
    makeAllelementsreadOnly();
}

function submittedState() {

    hideButton('#btnsave');
    hideButton('#btnsubmitChanges');
    hideButton('#btncancel');
    showSuccessMessage("This form has been submitted to benefits management for processing.");
    makeAllelementsreadOnly();

}

function hideButton(btnID) {

    $(btnID).hide();
}

function showErrorMessage(message) {

    $("#divMsg").text(message);
    $("#divMsg").removeAttr('class').addClass('alert alert-danger');
}

function showSuccessMessage(message) {

    $("#divMsg").text(message);
    $("#divMsg").removeAttr('class').addClass('alert alert-success');
}

function showInfoMessage(message) {

    $("#divMsg").text(message);
    $("#divMsg").removeAttr('class').addClass('alert alert-info');
}

function makeAllelementsreadOnly() {

    $("#C1_ClaimDateOfService").attr('readonly', true);
    $("#C1_ClaimAmount").attr("disabled", "disabled");
    $("#C1_ClaimOptionalComments").attr("disabled", "disabled");
    $("#C1_ClaimAttachDoc_delete").attr("disabled", "disabled");

    $("#C1_ClaimDateOfService").attr("disabled", "disabled");
    $(".input-group-addon").attr("disabled", "disabled");
    $(".input-group-addon").addClass("disablelement");
    $(".file-input-new").addClass("disablelement");
    $(".file-input-new").attr("disabled", "disabled");

    $(".kv-fileinput-caption").css("background-color", "EFEFEF");
}

//For validations


function SetError(id, msg) {


    $(id).removeClass("hide").addClass("text-danger");
    $(id).find("#msg").text(msg);

    return false;
}

function SetSuccess(id, msg) {

    $(id).removeClass("text-danger").addClass("hide");


    return true;
}

function CheckAttachment() {

    var message;

   
    var AttachdeleteButtoonLen = $("#C1_ClaimAttachDoc_group").length;

    if ((AttachdeleteButtoonLen == 1) && ($("#C1_ClaimAttachDoc_group").css('display') != 'none'))
    {
        message = '';
        return SetSuccess("#helpClaimAttachDoc", message);
    }


    $("#helpClaimAttachDoc").removeClass("text-danger");

    var attachTitle = $(".file-caption-name").attr("title");
 

    if (attachTitle == undefined || attachTitle == '') {
        message = 'Claim Attach document is required.';

        return SetError("#helpClaimAttachDoc", message);
    }
    else {
        return SetSuccess("#helpClaimAttachDoc", message);

    }
}

function CheckForAmount() {

    var message;

    $("#helpClaimAmount").removeClass("text-danger");

    var amount = $("#C1_ClaimAmount").val().trim();
   
    if (amount == '') {
        message = 'Claim Amount is a required field.';

        return SetError("#helpClaimAmount", message);
    }
    else if (!($.isNumeric(amount))) {

        message = 'please enter amount in dollars';

        return SetError("#helpClaimAmount", message);
    }
    else if (amount <= 0) {

        message = 'please enter an amount greater than 0';

        return SetError("#helpClaimAmount", message);
    }
    else {
        return SetSuccess("#helpClaimAmount", message);

    }

}

function CheckForDate() {

    var message;

    $("#helpClaimDateOfService").removeClass("text-danger");

    var date = $("#C1_ClaimDateOfService").val().trim();

    if (date == '') {
        message = 'Date of Service is a required field.';

        return SetError("#helpClaimDateOfService", message);
    }

    var start = moment("2016-07-01", "YYYY-MM-DD");
    var end = new Date();
    var range = moment.range(start, end);
    var mdate = moment(date, "MM-DD-YYYY");

    if (!range.contains(mdate))
    {
        message = 'Must be between July 1, 2016 and today';

        return SetError("#helpClaimDateOfService", message);

    }
    else {
        return SetSuccess("#helpClaimDateOfService", message);

    }



}


function validateMOOPForm() {

    var isAmtSuccess = CheckForAmount();

    var isdateSuccess = CheckForDate();

    var isAttachmentSuccess = CheckAttachment();

    if (isAmtSuccess && isdateSuccess && isAttachmentSuccess) {
        return true;
    }
    else {
        return false;
    }


}