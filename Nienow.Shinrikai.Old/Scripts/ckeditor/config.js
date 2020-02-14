/**
 * @license Copyright (c) 2003-2015, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function (config) {
    // Define changes to default configuration here. For example:
    config.language = 'pt-br';
    config.enterMode = CKEDITOR.ENTER_BR;

    config.allowedContent = true;

    // config.uiColor = '#AADC6E';
    config.removePlugins = 'divarea';
    config.extraPlugins = 'base64image,furigana,reference';
    
    config.toolbar = [
        //{ name: 'document', items: ['Source'] },
        { name: 'height', items: ['Furigana'] },
        { name: 'height', items: ['Reference'] },
        { name: 'styles', items: ['Font', 'FontSize', 'Format', 'Styles'] },
        { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike'] },
        { name: 'colors', items: ['TextColor', 'BGColor'] },
        { name: 'paragraph', groups: ['list', 'indent', 'blocks', 'align'], items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock'] },
        { name: 'links', items: ['Link', 'Unlink'] },
        { name: 'insert', items: ['base64image', 'Table', 'HorizontalRule', 'SpecialChar'] },
        { name: 'clipboard', items: ['Undo', 'Redo'] },
        { name: 'height', items: ['lineheight'] }
    ];
    
    if (window._isAdm)
        config.toolbar.push({ name: 'document', items: ['Source'] });

    // Dialog windows are also simplified.
    config.removeDialogTabs = 'link:advanced';

    //CKEDITOR.config.contentsCss =
    //    '.stretchText { text-decoration: underline; }';
};
