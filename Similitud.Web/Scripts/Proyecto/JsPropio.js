$(document).ready(function () {
});
$(function () {
    $("form").submit(function () { return false; });
});

function enviarURL() {
    if ($('#urlButton').is(':checked')) {
        console.log("1");
        var contenido = '<div class="panel panel-default"><div class="panel-heading"><h3 class="panel-title">Track Original</h3></div><div class="panel-body"><audio controls><source src="' + $("#url").val() + '" type="audio/mpeg"></audio></div></div>';
        $("#miAudio").html(contenido);

        $.ajax({
            type: "POST",
            data: { URL: JSON.stringify($("#url").val()) },
            async: false,
            url: path + '/Principal/ObtenerSimilares',
            success: function (ViewSimilares) {
                $("#PlayList").html(ViewSimilares);
            },
            error: function (xhr, status, error) {
                parent.ShowError(xhr.responseText);
            }
        });
    }
    if ($('#titleButton').is(':checked')) {
        console.log("2");
        $.ajax({
            type: "POST",
            data: { titulo: JSON.stringify($("#titulo").val()), artista: JSON.stringify($("#artista").val()) },
            async: false,
            url: path + '/Principal/ObtenerSimilaresxTitulo',
            success: function (ViewSimilares) {
                $("#PlayList").html(ViewSimilares);
            },
            error: function (xhr, status, error) {
                parent.ShowError(xhr.responseText);
            }
        });
    }
    
}