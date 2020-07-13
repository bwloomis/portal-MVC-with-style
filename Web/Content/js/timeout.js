$(document).ready(function () {
    $.sessionTimeout({ warnAfter: convertToMilliseconds(timeout - 1), redirAfter: convertToMilliseconds(timeout), logoutUrl: '/Account/LogoutUAS', redirUrl: '/Account/LogoutUAS', keepAliveUrl: '/Search/keepAlive' });
});

function convertToMilliseconds(minutes) {
    return minutes * 60 * 1000;
}