// Write your Javascript code.
var gethc = function (url) {
    var state = 'offline';
    $.ajax({
        url: url,
        async: false,
        type: 'GET',
        success: function (result, status) {
            //console.log(status);
            //if (status == 'success') {
                
            //}

            state = 'online';
            //console.log(state);
        }
        });
    return state;
};

$(document).ready(function () {
    $(".hc").each(function () {
        url = $(this).attr('url');
        val = gethc(url);
        console.log(val);
        var css = 'hc-' + val;
        $(this).text(val);
        $(this).addClass(css);
    });

});
