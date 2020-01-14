function AlertDialog(message, title) {
    if (typeof message !== "string" || message.length == 0)
        return;

    $.get('../Scripts/bootstrap.dialogs/AlertDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (typeof title === "string" && title.length > 0)
            $("#alertDialog .modal-title").text(title);
        else
            $("#alertDialog .modal-title").text("Shinrikai");

        $("#alertDialog .modal-body .modal-message").html(message);

        $("#alertDialog").show();
    });
}

function ErrorDialog(message, title, extraButton, callback) {
    if (typeof message !== "string" || message.length == 0)
        return;

    $.get('../Scripts/bootstrap.dialogs/ErrorDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (typeof title === "string" && title.length > 0)
            $("#errorDialog .modal-title").text(title);
        else
            $("#errorDialog .modal-title").text("Shinrikai");

        if (typeof extraButton === "string" && title.length > 0) {
            $("#errorDialog .modal-footer").append(
                '<button type="button" class="btn btn-default ' + extraButton + '">' + extraButton + '</button>');
        }

        if (typeof callback === "function")
            $("#errorDialog .modal-footer ." + extraButton).click(callback);

        $("#errorDialog .modal-body .modal-message").text(message);

        $("#errorDialog").show();
    });
}

function InformationDialog(message, title, callback) {
    if (typeof message !== "string" || message.length == 0)
        return;

    $.get('../Scripts/bootstrap.dialogs/InformationDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (typeof title === "string" && title.length > 0)
            $("#informationDialog .modal-title").text(title);
        else
            $("#informationDialog .modal-title").text("Shinrikai");

        $("#informationDialog .modal-body .modal-message").text(message);

        $("#informationDialog button.btn").click(function () {
            $('#informationDialog').remove();
            if (typeof callback === "function")
                callback();
        });

        $("#informationDialog").show();
    });
}

function NotificationDialog(id, force, callback) {
    if (typeof id !== "number" || id == 0)
        return;

    force = typeof force === "boolean" ? force : false;

    var understanding = function () {
        $.ajax({
            type: "POST",
            url: window._url_DeclareUnderstanding,
            data: { id: id }
        });
    };

    $.get('../Scripts/bootstrap.dialogs/NotificationDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (!force) //Modo forçado não requer que o usuário declare entendimento.
            $("#notificationDialog button.btn-success").click(function () {
                $('#notificationDialog').remove();
                understanding();
                if (typeof callback === "function")
                    callback();
            });
        else
            $("#notificationDialog button.btn-success").hide();

        $.ajax({
            type: "POST",
            url: window._url_GetNotification,
            data: { id: id, force: force }
        })
        .done(function (message) {
            if (message == "")
                $('#notificationDialog').remove();
            else {
                $("#notificationDialog .modal-body .modal-message").html(message);
                $("#notificationDialog").show();
            }
        });
    });
}

function QuestionDialog(message, title) {
    if (typeof message !== "string" || message.length == 0)
        return;

    $.get('../Scripts/bootstrap.dialogs/QuestionDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (typeof title === "string" && title.length > 0)
            $("#questionDialog .modal-title").text(title);
        else
            $("#questionDialog .modal-title").text("Shinrikai");

        $("#questionDialog .modal-body .modal-message").text(message);

        $("#questionDialog").show();
    });
}

function SuccessDialog(message, title, callback) {
    if (typeof message !== "string" || message.length == 0)
        return;

    $.get('../Scripts/bootstrap.dialogs/SuccessDialog.html?a=' + (new Date()).getTime(), function (data) {
        $("article").append(data);

        if (typeof title === "string" && title.length > 0)
            $("#successDialog .modal-title").text(title);
        else
            $("#successDialog .modal-title").text("Shinrikai");

        $("#successDialog .modal-body .modal-message").text(message);

        $("#successDialog button.btn").click(function () {
            $('#successDialog').remove();
            if (typeof callback === "function")
                callback();
        });

        $("#successDialog").show();
    });
}
