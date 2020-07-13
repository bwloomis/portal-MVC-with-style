$(document).ready(function () {
    var regularExpressions = {};
    regularExpressions.email = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
   // regularExpressions.zipCode = /^([0-9]{5})?(?:-[0-9]{4})?$/
    regularExpressions.zipCode =/^[0-9]{5}(?:-[0-9]{4})?/
    
    regularExpressions.phone = /^[(]{0,1}[0-9]{3}[)]{0,1}[-\s\.]{0,1}[0-9]{3}[-\s\.]{0,1}[0-9]{4}$/
    regularExpressions.ssn = /^\d{3}-?\d{2}-?\d{4}$/
    //regularExpressions.Allow1to9Numbers = /^(([1-9]{1}|[0-9]{1,6})?|([0-9]{1,3})(?:,[0-9]{1,3}))$/
    regularExpressions.Allow1to9Numbers = /^(([1-9?=.*!@#$%^&*]{1}|[0-9?=.*!@#$%^&*]{1,8})?|([0-9?=.*!@#$%^&*]{1,4})(?:,[0-9?=.*!@#$%^&*]{1,3}))$/
    regularExpressions.Allow0to9Numbers = /^(([0-9?=.*!@#$%^&*]{1}|[0-9?=.*!@#$%^&*]{1,8})?|([0-9?=.*!@#$%^&*]{1,4})(?:,[0-9?=.*!@#$%^&*]{1,3}))$/
    regularExpressions.AllowtwoNumbers = /^([0-9]{1,2})?$/
    
    $("*[datavalidate='true']").on("keyup", function () {

        var val = $(this).val();
        var message = $(this).attr("datavalidationmessage");
        var dv = $(this).attr("datadv");
        var re = regularExpressions[dv];

        if (re.test(val)) {

            $(this).removeClass("errorControl").addClass("successControl");
            $(this).parent().removeClass("error").addClass("success");
            $(this).parent().find(".lbl-error").hide().text("");
            $(this).parent().find(".icon").removeClass("icon-remove").addClass("icon-ok");
        }
        else {
            $(this).parent().removeClass("success").addClass("error");
            $(this).removeClass("successControl").addClass("errorControl");
            $(this).parent().find(".lbl-error").show().text(message);
            $(this).parent().find(".icon").removeClass("icon-ok").addClass("icon-remove");
        }
    });

});

function isNumeric(e) {
    if (window.event) { var charCode = window.event.keyCode; }
    else if (e) { var charCode = e.which; }
    else { return true; }
    if (charCode != 46 && (charCode < 48 || charCode > 57) && charCode != 8 && charCode != 0)
    { return false; }
    return true;
}
function Comma(Num) { //function to add commas to textboxes

    Num += '';
    Num = Num.replace(',', ''); Num = Num.replace(',', ''); Num = Num.replace(',', '');
    Num = Num.replace(',', ''); Num = Num.replace(',', ''); Num = Num.replace(',', '');
    x = Num.split('.');
    x1 = x[0];
    x2 = x.length > 1 ? '.' + x[1] : '';
    var rgx = /(\d+)(\d{3})/;
    while (rgx.test(x1))
        x1 = x1.replace(rgx, '$1' + ',' + '$2');
    return x1 + x2;
}

function currencyTextBoxOnBlur(elementRef) {
    if (elementRef.value.length > 0) {
        elementRef.value = elementRef.value.replace('$', '');
        elementRef.value = '$' + elementRef.value;
    }
}
