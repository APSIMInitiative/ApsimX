window.onload = function() {
  var headHeight  = $("nav").height();
  var mainHeight = $("main").height();
  var sideHeight = $("#sidebar").height();
  var footHeight  = $("footer").height();
  var totalHeight   = headHeight + mainHeight + footHeight;
  var w = $(window);

  if (   w.width() > $("main").width() + $("#sidebar").width()
      && sideHeight > 0
      && sideHeight < mainHeight ) {
    $(".doc-sidebar").css("height", mainHeight);
    var sideNode = $("#sidebar");
    sideNode.css({"position": "fixed"});
    var scrollStart = 0;
    var scrollStop  = headHeight + mainHeight - sideHeight;

    w.scroll(function() {
      if (w.scrollTop() <= scrollStart) {
        sideNode.css({"position": "fixed"});
      } else if (scrollStart < w.scrollTop() && w.scrollTop() < scrollStop) {
        sideNode.css({"position": "fixed", "top": headHeight + 20 + "px"});
      } else if (w.scrollTop() >= scrollStop) {
        var topNext
          = headHeight - (headHeight + sideHeight + footHeight)
            * (w.scrollTop() - scrollStop) / (totalHeight - scrollStop);
        sideNode.css({
          "position": "fixed", "top": topNext + "px", "bottom": footHeight + "px"
        });
      }
    });
  }
}

function resetSidebarPos() {
  var sideNode = $("#sidebar");
  if ( $(window).width() > $("main").width() + $("#sidebar").width() ) {
    sideNode.css({"position": "fixed", "top": "", "bottom": ""});
  }
}