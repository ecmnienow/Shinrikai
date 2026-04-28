function ShowImage(url) {
    $.get('../Scripts/bootstrap.image/Viewer.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);
        $("#image-gallery-image").prop("src", url);
    });
}
