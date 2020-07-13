//Functinality for people picker V2
var RetrieveEnWrkrsOnSiteSelection = function (site) {

    var postdata = {
        'site': site,
    }

    $.ajax({
        type: 'POST',
        url: '/AdapCa/RetrieveEnWorkersByGroup',
        data: postdata,
        async: false,
        cache: false,
        success: function (result) {

            var combobox = $('#cointainerEnWorker');
            var $element = combobox.find('input[type=text]');
            var $button = combobox.find('.dropdown-toggle');


            if (result != "") {

                $('#ComboBoxSelect').append(result);
                $("#ComboBoxSelect").show();

                $element.removeAttr('disabled');
                $element.removeAttr('readonly');

                $('.combobox').combobox({ templateSelector: "#cointainerEnWorker" });


                $element.attr('placeholder', 'Please Select your Enrollment Worker');
                $("#ComboBoxSelect").hide();

                $button.attr('disabled', false);

            }
            else {

                combobox.disabled = true;
                combobox.addClass('combobox-disabled');
                $("#ComboBoxSelect").hide();

                $("#C1_InsuranceAssistEnrollPerson").attr('readonly', 'readonly');
                $("#C1_InsuranceAssistEnrollPerson").attr('placeholder', 'No Enrollment Worker Available at this site');
            }


        },
        error: function () {
            alert('Fail');
        }
    });
}
var ClearPreviousComboHtml = function () {

    var ele = $('.combobox-container').clone(false);
    $('.combobox-container').remove();
    $("#divComboParent").append(ele);

    //$('#ComboBoxSelect').find('option').remove().end();

    $('#ComboBoxSelect').find("option:gt(0)").remove();



    var parent = $("#EnWorkerDrpParent");

    if (parent.find('.typeahead')) {

        parent.find('.typeahead').remove();
    }

    $("#ComboBoxSelect").show();
    $("#ComboBoxSelect").removeData('combobox');

    var combobox = $('#cointainerEnWorker');
    var $element = combobox.find('input[type=text]');
    var $button = combobox.find('.dropdown-toggle');
    $button.attr('disabled', false);

    $element.removeClass('combobox');


}
var CheckDisableComboBoxControl = function (comboBoxId) {

    var value = $("#C1_InsuranceAssistEnrollSite option:selected").val();

    var combobox = $(comboBoxId);
    var $element = combobox.find('input[type=text]');
    var $button = combobox.find('.dropdown-toggle');

    ClearPreviousComboHtml();

    if (value == "") {

        $("#ComboBoxSelect").hide();

        $("#C1_InsuranceAssistEnrollPerson").val("");
        $("#C1_InsuranceAssistEnrollPerson").attr('disabled', 'disabled');
        $("#C1_InsuranceAssistEnrollPerson").attr('readonly', 'readonly');

        $button.attr('disabled', true);
        $button.css("pointer-events", "none");

        combobox.disabled = true;
        combobox.addClass('combobox-disabled');

        $(comboBoxId).prop('disabled', true);
    }
    else {
        RetrieveEnWrkrsOnSiteSelection(value);

    }
}

var ComboBoxActivities = function () {

    CheckDisableComboBoxControl('#cointainerEnWorker');

    $('#C1_InsuranceAssistEnrollSite').on('change', function (e) {

        var combobox = $('#cointainerEnWorker');
        var $element = combobox.find('input[type=text]');
        var $button = combobox.find('.dropdown-toggle');

        $element.prop('value', "");
        $element.prop('disabled', false);
        combobox.removeClass('combobox-selected');
        combobox.removeClass('combobox-disabled');

        CheckDisableComboBoxControl('#cointainerEnWorker');

    });

}