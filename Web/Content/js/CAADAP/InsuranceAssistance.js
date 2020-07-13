function getPreviousValue(previous) {
	if (typeof previous !== 'boolean') {
		return false;
	}
	return previous;
}
function getPreviousSelector(selector, previous) {
	previous = getPreviousValue(previous);
	if (!previous) {
		if (selector.startsWith('.')) {
			selector = ':not(.previous-response):not(.previous-response *)' + selector;
		}
	} else {
		if (selector.startsWith('.')) {
			selector = ".previous-response " + selector;
		} else if (selector.startsWith('#')) {
			selector = "#PREVIOUS_" + selector.slice(1);
		}
	}
	return selector;
}
function showHideElements(selector, condition, previousCallbackParameter, showCallback, hideCallback) {
	previous = getPreviousValue(previousCallbackParameter);
	var elements = $(selector);
	if (!elements) {
		return;
	}
	if (condition) {
		elements.show();
		if (typeof showCallback === "function") {
			showCallback(previousCallbackParameter);
		}
	} else {
		elements.hide();
	}
}

function enableDisableElements(selector, condition, previousCallbackParameter, enableCallback, disableCallback) {
	previous = getPreviousValue(previousCallbackParameter);
	var elements = $(selector);
	if (!elements) {
		return;
	}
	if (condition) {
		elements.prop("disabled", false);
		if (typeof enableCallback === "function") {
			enableCallback(previousCallbackParameter);
		}
	} else {
		if (typeof disableCallback === "function") {
			disableCallback(previousCallbackParameter);
		}
		elements.prop("disabled", true);
	}
}

function hippDetailsShowHide(previous) {
	previous = getPreviousValue(previous);
	var medicalChecked = $(getPreviousSelector('#C1_MemberHIPPEnrollPgm1', previous)).prop('checked') === true;
	
	enableDisableElements(getPreviousSelector('#C1_MemberHIPPEnrollPgm2', previous), medicalChecked, previous, null, function() {
		$(getPreviousSelector('#C1_MemberHIPPEnrollPgm2', previous)).prop('checked', false);
	});
	enableDisableElements(getPreviousSelector('#C1_MemberHIPPEnrollPgm3', previous), medicalChecked, previous, null, function() {
		$(getPreviousSelector('#C1_MemberHIPPEnrollPgm3', previous)).prop('checked', false);
	});

	var dentalChecked = $(getPreviousSelector('#C1_MemberHIPPEnrollPgm2', previous)).prop('checked') === true;
	var visionChecked = $(getPreviousSelector('#C1_MemberHIPPEnrollPgm3', previous)).prop('checked') === true;

	//Medical
	showHideElements(getPreviousSelector('.hipp.medical', previous), medicalChecked, previous, function() {
		var medicalPlanType = $(getPreviousSelector('#C1_MemberMedicalPlanType', previous)).val();
		showHideElements(getPreviousSelector('.hipp.medical.medical-plan-type-other', previous), medicalPlanType === "4"); //4 = Other
		showHideElements(getPreviousSelector('.hipp.medical.medical-plan-type-covered-ca', previous), medicalPlanType === "0"); // 0 = Covered CA
		showHideElements(getPreviousSelector('.hipp.medical.medical-plan-type-non-covered-ca', previous), medicalPlanType !== "0");
		var yesAccountNumber = $(getPreviousSelector('#C1_MemberMedicalPlanAccountIDYesNo_YesOption', previous)).prop('checked') === true;
		showHideElements(getPreviousSelector('.hipp.medical.account-number-yes', previous), yesAccountNumber);
	});

	//Dental
	showHideElements(getPreviousSelector('.hipp.dental', previous), dentalChecked, previous, function() {
		var dentalIncluded = $(getPreviousSelector('#C1_MemberDentalPlanIncluded', previous)).val(); //0 = separate, 1 = included, 2 = don't know
		showHideElements(getPreviousSelector('.hipp.dental.dental-included', previous), dentalIncluded === "1", previous, function() {
			var accountNumberSameNo = $(getPreviousSelector('#C1_MemberIncludedDentalAccountSame_NoOption', previous)).prop('checked') === true;
			showHideElements(getPreviousSelector('.hipp.dental.dental-included.account-number-same-no', previous), accountNumberSameNo);
			var policyNumberSameNo = $(getPreviousSelector('#C1_MemberIncludedDentalPolicySame_NoOption', previous)).prop('checked') === true;
			showHideElements(getPreviousSelector('.hipp.dental.dental-included.policy-number-same-no', previous), policyNumberSameNo);
		});
		showHideElements(getPreviousSelector('.hipp.dental.dental-separate', previous), dentalIncluded === "0");
		var dentalPlanType = $(getPreviousSelector('#C1_MemberDentalPlanType', previous)).val(); //4 = other
		showHideElements(getPreviousSelector('.hipp.dental.dental-plan-type-other', previous), dentalPlanType === "4");
	});

	//Medical & Dental shared elements
	showHideElements(getPreviousSelector('.hipp.medical-dental', previous), medicalChecked || dentalChecked, previous, function() {
		var medicalCoverageType = $(getPreviousSelector('#C1_MemberMedicalPlanCoverage', previous)).val();
		showHideElements(getPreviousSelector('.hipp.medical-dental.plan-coverage-family', previous), medicalCoverageType === "2"); //2 = Family
		var enrolledWithEmployer = $(getPreviousSelector('#C1_MemberEnrolledWithEmployer_YesOption', previous)).prop('checked') === true;
		showHideElements(getPreviousSelector('.hipp.medical-dental.enrolled-with-employer-yes', previous), enrolledWithEmployer);
		var highPayments = $(getPreviousSelector('#C1_MedicareDHighPayments_YesOption', previous)).prop('checked') === true;
		showHideElements(getPreviousSelector('.hipp.medical-dental.high-payments-attachment', previous), highPayments);
	});

	//Vision
	showHideElements(getPreviousSelector('.hipp.vision', previous), visionChecked, previous, function() {
		var visionIncluded = $(getPreviousSelector('#C1_MemberVisionPlanIncluded', previous)).val(); // 0 = yes, medical, 1 = yes, dental, 2 = no
		showHideElements(getPreviousSelector('.hipp.vision.vision-included-yes', previous), visionIncluded === "0" || visionIncluded === "1");
		showHideElements(getPreviousSelector('.hipp.vision.vision-included-no', previous), visionIncluded === "2");
	});
}

function medicarePartDDetailsShowHide(previous) {
	previous = getPreviousValue(previous);
	var medicarePartDAccountNumberYes = $(getPreviousSelector('#C1_MemberMedicareDAccountID_YesOption', previous)).prop('checked') === true;
	showHideElements(getPreviousSelector('.medicare-part-d.account-number-yes', previous), medicarePartDAccountNumberYes);
}

function enrollmentAssistanceDetailsShowHide(previous) {
	previous = getPreviousValue(previous);
	var enrollmentAssistanceYes = $(getPreviousSelector('#C1_InsuranceAssistEnrollSiteFlag_YesOption', previous)).prop('checked') === true;
	showHideElements(getPreviousSelector('.enrollment-assistance.enrollment-assistance-yes', previous), enrollmentAssistanceYes);
}

function sectionsShowHide(previous) {
	previous = getPreviousValue(previous);
	var yesHippChecked = $(getPreviousSelector('#C1_MemberRequestsHIPP31', previous)).prop('checked') === true;
	var yesMedicarePartDChecked = $(getPreviousSelector('#C1_MemberRequestsHIPP32', previous)).prop('checked') === true;

	//HIPP
	showHideElements(getPreviousSelector('.hipp', previous), yesHippChecked, previous, hippDetailsShowHide);

	//Medicare Part D
	showHideElements(getPreviousSelector('.medicare-part-d', previous), yesMedicarePartDChecked, previous, medicarePartDDetailsShowHide);

	//HIPP & Medicare Part D shared elements
	showHideElements(getPreviousSelector('.hipp-medicare-part-d', previous), yesHippChecked || yesMedicarePartDChecked);

	//Enrollment assistance
	showHideElements(getPreviousSelector('.enrollment-assistance', previous), yesHippChecked || yesMedicarePartDChecked, previous, enrollmentAssistanceDetailsShowHide);
	
	//Reset previous-response displays
	$('.previous-response').hide();
}

$(document).ready(function () {
    if (!String.prototype.startsWith) {
        String.prototype.startsWith = function (searchString, position) {
            position = position || 0;
            return this.indexOf(searchString, position) === position;
        };
    }

    $('.cacurrency').maskMoney({ thousands: "", allowZero: true });

	$('.class_phone_input').mask("999-999-9999");

	$(".navbar-nav").find("li").each(function () {
		$(this).click(function () {
			$.ajax({
				url: '@Url.Action("PageLoadEvents","Results")',
			});
		});
	});

	var Notify = '@ViewBag.Notify';
	var NotifyMessage = '@ViewBag.NotifyMessage';
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

	//Wire up show/hide function
	var sectionsShowHideSenders = [
		'#C1_MemberRequestsHIPP31', //Yes, HIPP
		'#C1_MemberRequestsHIPP32', //Yes, Medicare Part D
		'#C1_MemberRequestsHIPP33' //No
	];
	$(sectionsShowHideSenders.join(", ")).on('change', sectionsShowHide);

	var hippDetailsShowHideSenders = [
		'#C1_MemberHIPPEnrollPgm1', 
		'#C1_MemberHIPPEnrollPgm2', 
		'#C1_MemberHIPPEnrollPgm3', 
		'#C1_MemberMedicalPlanType', 
		'#C1_MemberMedicalPlanCoverage',
		'#C1_MemberMedicalPlanAccountIDYesNo_YesOption', //Fix this
		'#C1_MemberMedicalPlanAccountIDYesNo_NoOption', //Fix this
		'#C1_MemberDentalPlanIncluded',
		'#C1_MemberDentalPlanType',
		'#C1_MemberIncludedDentalAccountSame_YesOption',
		'#C1_MemberIncludedDentalAccountSame_NoOption',
		'#C1_MemberIncludedDentalPolicySame_YesOption',
		'#C1_MemberIncludedDentalPolicySame_NoOption',
		'#C1_MemberVisionPlanIncluded',
		'#C1_MemberEnrolledWithEmployer_YesOption',
		'#C1_MemberEnrolledWithEmployer_NoOption',
		'#C1_MedicareDHighPayments_YesOption',
		'#C1_MedicareDHighPayments_NoOption'
	];
	$(hippDetailsShowHideSenders.join(", ")).on('change', hippDetailsShowHide);

	var medicarePartDDetailsShowHideSenders = [
		'#C1_MemberMedicareDAccountID_YesOption',
		'#C1_MemberMedicareDAccountID_NoOption'
	];
	$(medicarePartDDetailsShowHideSenders.join(", ")).on('change', medicarePartDDetailsShowHide);

	var enrollmentAssistanceSenders = [
		'#C1_InsuranceAssistEnrollSiteFlag_YesOption',
		'#C1_InsuranceAssistEnrollSiteFlag_NoOption'
	];
	$(enrollmentAssistanceSenders.join(", ")).on('change', enrollmentAssistanceDetailsShowHide);

	//Any call to initialize the UI needs to happen after the drop lists are populated
});