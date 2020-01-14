CKEDITOR.plugins.add('furigana',
{
    init: function (editor) {
        //var pluginName = 'furigana';
        editor.ui.addButton('Furigana',
            {
                label: 'Inserir Furigana',
                command: 'OpenFuriganaDialog',
                icon: CKEDITOR.plugins.getPath('furigana') + 'furigana_icon.png'
            });

        editor.addCommand('OpenFuriganaDialog', { exec: showFuriganaDialog });

        //editor.on('contentDom', function () {
        //    var removeEmptyRuby = function (callback) {
        //        console.log("teste");
        //        var text = editor.getData();
                
        //        var match1 = text.match(/<ruby><rb>[ ]{0,}<\/rb><rt>.*?<\/rt><\/ruby>{0,}/igm);
        //        var match2 = text.match(/<ruby><rt>.*?<\/rt><\/ruby>{0,}/igm);
                
        //        if (match1 != null && match1.length > 0) {
        //            var replaced_text = text.replace(/<ruby><rb>[ ]{0,}<\/rb><rt>.*?<\/rt><\/ruby>{0,}/igm, "");
        //            editor.setData(replaced_text);
        //            //alert(replaced_text);
        //            //callback();
        //        }

        //        if (match2 != null && match2.length > 0) {
        //            var replaced_text = text.replace(/<ruby><rt>.*?<\/rt><\/ruby>{0,}/igm, "");
        //            editor.setData(replaced_text);
        //            //alert(replaced_text);
        //            //callback();
        //        }
        //    };

        //    editor.document.on('keydown', function (e) {
        //        //var key = e.data.$.keyCode ? e.data.$.keyCode : e.data.$.charCode;
        //        //if (key == 8 || key == 46)
        //        //removeEmptyRuby(function () { e.data.$.preventDefault(); });
        //        removeEmptyRuby();
        //    });

        //    // Firefox OK
        //    editor.document.on('drop', removeEmptyRuby);
        //    // IE OK
        //    editor.document.getBody().on('drop', removeEmptyRuby);
        //});
    }
});

function showFuriganaDialog(e) {
    var editor = CKEDITOR.instances["editor"];
    var oldText = editor.getSelection().getSelectedText();

    if (oldText.length == 0)
        return;

    $.get(CKEDITOR.plugins.getPath('furigana') + 'furigana.html?a=' + (new Date()).getTime(), function (data) {
        $("html > body:not(.cke_editable)").append(data);
        $("#furiganaDialog").show();
        $("#txtFurigana").select();
    });
}

function addFurigana(furigana) {
    if (furigana.length > 0) {

        var editor = CKEDITOR.instances["editor"];
        var oldText = editor.getSelection().getSelectedText();

        editor.insertHtml(
            "<ruby><rb>" + oldText + "</rb><rt>" + furigana + "</rt></ruby>"
        );
    }

    $('#furiganaDialog').remove();
}