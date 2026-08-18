window.pdfPreview = {
    // Points an <iframe> at the PDF bytes via a blob: URL — the browser's own PDF viewer
    // renders it, so the preview is the real, final document, not a facsimile.
    show: function (bytes, iframeId) {
        const iframe = document.getElementById(iframeId);
        if (!iframe) return;

        const blob = new Blob([new Uint8Array(bytes)], { type: "application/pdf" });
        const url = URL.createObjectURL(blob);
        if (iframe.dataset.blobUrl) URL.revokeObjectURL(iframe.dataset.blobUrl);
        iframe.dataset.blobUrl = url;
        iframe.src = url;
    },

    // Same bytes as the preview, saved straight to disk — no OS print dialog involved.
    download: function (bytes, fileName) {
        const blob = new Blob([new Uint8Array(bytes)], { type: "application/pdf" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },

    revoke: function (iframeId) {
        const iframe = document.getElementById(iframeId);
        if (iframe && iframe.dataset.blobUrl) {
            URL.revokeObjectURL(iframe.dataset.blobUrl);
            delete iframe.dataset.blobUrl;
        }
    },
};
