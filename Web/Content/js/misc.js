//
// OnlyAllowPhoneNumbers - Called from onkeypress event, rejects non-integers
//
function OnlyAllowPhoneNumbers(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter},-,.,(,)}
	var reKeyboardChars = /[\x00\x08\x0D\x2D\x2E\x28\x29]/;

	// Numeric characters
	var reValidChars = /\d/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

//
// OnlyAllowNumbers - Called from onkeypress event, rejects non-integers
//
function OnlyAllowNumbers(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter}, (Tab)}
    var reKeyboardChars = /[\x00\x08\x09\x0D0]/;

	// Numeric characters
	var reValidChars = /\d/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

//
// OnlyAllowMoney - Called from onkeypress event, rejects non-integers but allow . and $
//
function OnlyAllowMoney(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter}}
	var reKeyboardChars = /[\x00\x08\x0D0\x24\x2E]/;

	// Numeric characters
	var reValidChars = /\d/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

//
// OnlyAllowDateNumbers - Called from onkeypress event, rejects non-integers
//
function OnlyAllowDateNumbers(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter},-,/,.}
	var reKeyboardChars = /[\x00\x08\x0D0\x2D\x2F\x2E]/;

	// Numeric characters
	var reValidChars = /\d/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}
function OnlyAllowZip(field, e)
{ 
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter}, {-}}
	var reKeyboardChars = /[\x00\x08\x0D0\x2D]/;

	// Numeric characters
	var reValidChars = /\w/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar) && (keycode != 96))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

//
// OnlyAllowSSN - Called from onkeypress event, rejects non-integers but allows -
//
function OnlyAllowSSN(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter}}
	var reKeyboardChars = /[\x00\x08\x0D]/;

	// Numeric characters
	var reValidChars = /\d/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);

	// Block non-numeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

function OnlyAllowAlphanumerics(field, e)
{
	// Special keyboard keys - Netscape 6 passes these to onkeypress
	// {{Home, End, Delete, Arrow Keys}, {Backspace}, {Enter}}
	var reKeyboardChars = /[\x00\x08\x0D]/;

	// Alphanumeric characters
	var reValidChars = /[0-9,A-Z,a-z]/;

	// Grab event key code and character (method depends on browser)
	var keycode = window.Event ? e.which : e.keyCode;
	var strChar = String.fromCharCode (keycode);
	
	// Block non-alphanumeric characters and non-special keys
	if (!reValidChars.test (strChar) && !reKeyboardChars.test (strChar))
	{
		// Invalid character, reject key press
		return false;
	}  

	// Valid character or special key, allow key press
	return true;
}

function textCounter(field, countfield, maxlimit) 
{
    // if too long trim it otherwise, update 'characters left' counter
    if (field.value.length > maxlimit) 
        field.value = field.value.substring(0, maxlimit);
    else 
        countfield.value = maxlimit - field.value.length;
}

function textCounterNoLabel(field, maxlimit)
{
    if (field.value.length > maxlimit) 
        field.value = field.value.substring(0, maxlimit);
}

function TextCounterOnLoad() {
    var theForm = document.forms[0];

    for (i = 0; i < theForm.elements.length; i++) {
        if (theForm.elements[i].type == "textarea") {
            // have a next form element which contains the counter display?
            if (theForm.elements.length > i + 1 && theForm.elements[i + 1].nodeName == "INPUT") {
                var notesTextArea = theForm.elements[i];
                var counterDisplay = theForm.elements[i + 1];
                var limit = theForm.elements[i + 1].value;

                textCounter(notesTextArea, counterDisplay, limit);
            }
        }
    }
}

