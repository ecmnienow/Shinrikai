CKEDITOR.plugins.add('reference',
{
    init: function (editor) {
        editor.ui.addButton('Reference',
            {
                label: 'Inserir referência',
                command: 'OpenReferenceDialog',
                icon: CKEDITOR.plugins.getPath('reference') + 'reference_icon.png'
            });

        editor.addCommand('OpenReferenceDialog', { exec: showReferenceDialog });
    }
});

function showReferenceDialog(e) {
    var editor = CKEDITOR.instances["editor"];
    var oldText = editor.getSelection().getSelectedText();

    if (oldText.length == 0)
        return;

    $.get(CKEDITOR.plugins.getPath('reference') + 'reference.html?a=' + (new Date()).getTime(), function (data) {
        $("html > body:not(.cke_editable)").append(data);
        $("#referenceDialog").show();
        $("#txtReference").select();

        $("#txtReference").autocomplete({
            delay: 500,
            source: function (request, response) {
                $.ajax({
                    type: "POST",
                    url: window._url_SearchKanjiWord,
                    dataType: "json",
                    data: {
                        term: request.term
                    }
                })
                .done(function (search) {
                    response(search);
                });
            },
            select: function (event, ui) {
                if (ui.item) {
                    var kanji = "";
                    if (ui.item.KanjiWord1) kanji += ui.item.KanjiWord1;
                    if (ui.item.KanjiWord2 && ui.item.KanjiWord2 != ui.item.KanjiWord1) kanji += ", " + ui.item.KanjiWord2;
                    if (ui.item.KanjiWord3 && ui.item.KanjiWord3 != ui.item.KanjiWord1 && ui.item.KanjiWord3 != ui.item.KanjiWord2) kanji += ", " + ui.item.KanjiWord3;

                    $("#txtReference").val(kanji);
                    $("#txtReferenceID").val(ui.item.ID);
                }
                else {
                    $("#txtReference").val("");
                    $("#txtReferenceID").val("");
                }

                return false;
            }
        })
        .autocomplete("instance")._renderItem = function (ul, item) {
            var kanji = "";
            if (item.KanjiWord1) kanji += item.KanjiWord1;
            if (item.KanjiWord2 && item.KanjiWord2 != item.KanjiWord1) kanji += ", " + item.KanjiWord2;
            if (item.KanjiWord3 && item.KanjiWord3 != item.KanjiWord1 && item.KanjiWord3 != item.KanjiWord2) kanji += ", " + item.KanjiWord3;

            var word = "";
            if (item.KanjiWord1 && item.Word1) word += item.Word1;
            if (item.KanjiWord2 && item.Word2) word += ", " + item.Word2;
            if (item.KanjiWord3 && item.Word3) word += ", " + item.Word3;

            return $('<li>')
                .append(
                    '<a data-id="' + item.ID + '">' +
                        '<strong style="font-size: 1.2em;">' + kanji + '</strong> [' + item.Kana + ']' +
                        '<br>' +
                        '<span style="font-size: 0.8em;">' + word + '</span>' +
                    '</a>'
                ).appendTo(ul);
        };
    });
}

function addReference(wordId) {
    var editor = CKEDITOR.instances["editor"];

    var selection = editor.getSelection();
    var ranges = selection.getRanges();
    var range = ranges[0];
    
    //var el = editor.document.createElement('div');

    var clone = range.cloneContents();
    var childNodes = clone.$.childNodes;

    var text = "";
    $.each(childNodes, function(i, n) {
        text += n.outerHTML;
    });
    
    alert(text);

    editor.insertHtml(
        '<span class="stretchText" data-id="' + wordId + '">' + text + "</span>"
    );

    $('#referenceDialog').remove();
}
