(function (ssn) {
    (function (candy, $, undefined) {

        candy.dataValueName = 'ssn-candy-value';
        candy.cssClassName = 'ssn-candy-mask';
        candy.isMaskAttributeName = 'ssn-candy-masked';
        candy.inputmaskPattern = '999-99-9999';
        candy.inputmaskPlaceholder = ' ';
        candy.inputmaskJitMasking = true;
        candy.displayMaskRegex = '';
        candy.displayMaskReplace = 'XXX-XX';

        candy.ssnFocus = function () {
            var value = $(this).data(candy.dataValueName);
            if (value) {
                $(this).val(value);
            }
            //Mask
            $(this).inputmask({
                "mask": candy.inputmaskPattern,
                "placeholder": candy.inputmaskPlaceholder,
                jitMasking: candy.inputmaskJitMasking
            });
        }

        candy.ssnBlur = function ssnBlur() {
            var ssnWrapper = $(this).parent();
            if (ssnWrapper) {
                //Unmask
                var maskedValue = $(this).val();
                var newNode = $(this).clone();
                $(this).inputmask('remove');
                $(this).remove();
                ssnWrapper.append(newNode);
                //Obscure value
                newNode.data(candy.dataValueName, maskedValue);
                var obscuredValue = maskedValue.replace(/^\d{3}-\d{2}/, candy.displayMaskReplace);
                newNode.val(obscuredValue);
                newNode.each(candy.ssnMask);
            }
        }

        candy.ssnValue = function () {
            var value = $(this).data(candy.dataValueName);
            return value ? value : '';
        }

        candy.ssnClear = function () {
            $(this).data(candy.dataValueName, '');
            $(this).val('');
        }

        candy.ssnMask = function () {
            $(this).focus(candy.ssnFocus);
            $(this).blur(candy.ssnBlur);
            $(this).attr(candy.isMaskAttributeName, true);
            $(this).init.prototype.ssnValue = candy.ssnValue;
            $(this).init.prototype.ssnClear = candy.ssnClear;
        }

        $(document).ready(function () {
            $('.' + candy.cssClassName).each(candy.ssnMask);
        });

    }(window.ssn.candy = window.ssn.candy || {}, jQuery));
}(window.ssn = window.ssn || {}));