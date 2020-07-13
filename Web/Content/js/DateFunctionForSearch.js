function lastMonth(idPrefix) {
    var fromDate = moment().startOf('month').subtract('month', 1).format('MM/DD/YYYY');
    var toDate = moment().endOf('month').subtract('month', 1).format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function lastQuarter(idPrefix) {
    var curDate = moment().format('MM');
    console.log(curDate);

    if (curDate === '01' || curDate === '02' || curDate === '03') {
        var fromDate = moment().month("October").startOf('month').subtract('year', 1).format('MM/DD/YYYY');
        var toDate = moment().month("December").startOf('month').subtract('year', 1).format('MM/DD/YYYY');
    }
    else if (curDate === '04' || curDate === '05' || curDate === '06') {
        var fromDate = moment().month("January").startOf('month').format('MM/DD/YYYY');
        var toDate = moment().month("March").startOf('month').format('MM/DD/YYYY');
    }
    else if (curDate === '07' || curDate === '08' || curDate === '09') {
        var fromDate = moment().month("April").startOf('month').format('MM/DD/YYYY');
        var toDate = moment().month("June").endOf('month').format('MM/DD/YYYY');
    }
    else {
        var fromDate = moment().month("July").startOf('month').format('MM/DD/YYYY');
        var toDate = moment().month("September").startOf('month').format('MM/DD/YYYY');
    }

    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function yearToDate(idPrefix) {
    var fromDate = moment().startOf('year').format('MM/DD/YYYY');
    var toDate = moment().format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function lastYear(idPrefix) {
    var fromDate = moment().startOf('year').subtract('year', 1).format('MM/DD/YYYY');
    var toDate = moment().endOf('year').subtract('year', 1).format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endMonth(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var toDate = moment().endOf('month').format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endNextMonth(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var toDate = moment().endOf('month').add('month', 1).format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endQuarter(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var theDate = moment().format('MM');

    if (theDate === '01' || theDate === '02' || theDate === '03') {
        var toDate = moment().month("March").endOf('month').format('MM/DD/YYYY');
    }
    else if (theDate === '04' || theDate === '05' || theDate === '06') {
        var toDate = moment().month("June").endOf('month').format('MM/DD/YYYY');
    }
    else if (theDate === '07' || theDate === '08' || theDate === '09') {
        var toDate = moment().month("September").endOf('month').format('MM/DD/YYYY');
    }
    else {
        var toDate = moment().month("December").endOf('month').format('MM/DD/YYYY');
    }

    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endNextQuarter(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var theDate = moment().format('MM');

    if (theDate === '01' || theDate === '02' || theDate === '03') {
        var toDate = moment().month("June").endOf('month').format('MM/DD/YYYY');
    }
    else if (theDate === '04' || theDate === '05' || theDate === '06') {
        var toDate = moment().month("September").endOf('month').format('MM/DD/YYYY');
    }
    else if (theDate === '07' || theDate === '08' || theDate === '09') {
        var toDate = moment().month("December").endOf('month').format('MM/DD/YYYY');
    }
    else {
        var toDate = moment().month("March").endOf('month').add('y', 1).format('MM/DD/YYYY');
    }

    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endYear(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var toDate = moment().endOf('year').format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}

function endNextYear(idPrefix) {
    var fromDate = moment().format('MM/DD/YYYY');
    var toDate = moment().endOf('year').add('y', 1).format('MM/DD/YYYY');
    $('#' + idPrefix + 'From').val(fromDate);
    $('#' + idPrefix + 'To').val(toDate);
}