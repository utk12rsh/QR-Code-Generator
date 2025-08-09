$(function () {
    $('#qrForm').on('submit', function (e) {
        e.preventDefault();

        var url = $(this).find('input[name="url"]').val().trim();

        if (!url) {
            alert('Please enter a URL');
            return;
        }

        var modelData = {
            URL: url
        };

        $.ajax({
            url: '/QRCode/Generate',
            type: 'POST',
            data: modelData, 
            success: function (response) {
                $('#qrResult').html(response);
            },
            error: function () {
                alert('Error generating QR code');
            }
        });
    });

    $(document).on('click', '#downloadPdfBtn', function () {
        var qrCodeImage = $('.qr-code-result img').attr('src');
        var urlText = $('.qr-code-result .url-text span').text();

        var modelData = {
            QrCodeImage: qrCodeImage,
            URL: urlText
        };

        $.ajax({
            url: '/QRCode/DownloadQRCode',
            type: 'POST',
            data: modelData,
            xhrFields: { responseType: 'blob' },
            success: function (blob) {
                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(blob);
                link.download = "QRCode.pdf";
                link.click();
            },
            error: function () {
                alert('Error downloading PDF');
            }
        });
    });
});
