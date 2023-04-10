toggleChildren = function(elem){
    var e = $(elem);
    e.children("div:last-child").slideToggle(100);
    var header = e.children("h4");
    var headerChild = header.children("a");
    if(headerChild.children().text() == "-"){
        headerChild.html("<div style=\"width: 15px; float: left\">+</div>");
    }
    else {
        headerChild.html("<div style=\"width: 15px; float: left\">-</div>");
    }
};

showChildren = function(elem){
    var e = $(elem);
    e.children("div:last-child").show();
    var header = e.children("h4");
    var headerChild = header.children("a");
    headerChild.html("<div style=\"width: 15px; float: left\">-</div>");
};

toggleNext = function(elem){
    var e = $(elem);
    e.next().slideToggle(100);
    var expander = e.children("a");
    if(expander.children().text() == "-"){
        expander.html("<div style=\"width: 15px; float: left\">+</div>");
    }
    else {
        expander.html("<div style=\"width: 15px; float: left\">-</div>");
    }
};

toggleId = function(id){
    var location = $('#' + id);
    toggleChildren(location);
}

jumpToId = function(id) {
    var location = $('#' + id);
    console.log(location);
    showChildren(location);
    $(window).scrollTop(location.offset().top - 120);
};

jumpToCall = function(id) {
    var location = $('#' + id);
    $(window).scrollTop(location.offset().top - 120);
};

jumpToTop = function() {
    $(window).scrollTop(0);
}
