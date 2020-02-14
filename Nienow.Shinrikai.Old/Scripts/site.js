$(function () {
    $(document).ajaxError(function (event, jqXHR, settings, thrownError) {
        ErrorDialog("Ocorreu um erro inesperado.", "Erro", "Ver", function (){
            //open a new window note:this is a popup so it may be blocked by your browser
            var newWindow = window.open("", "_newWindow");

            //write the data to the document of the newWindow
            newWindow.document.write(jqXHR.responseText);
        });
    });
});

function ShuffleElements(parent) {
    var elements = parent.children();
    while (elements.length) {
        parent.append(elements.splice(Math.floor(Math.random() * elements.length), 1)[0]);
    }
}

function validateEmail(email) {
    var re = /^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$/i;
    return re.test(email);
}
