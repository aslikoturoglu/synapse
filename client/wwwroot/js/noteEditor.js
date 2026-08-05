window.noteEditor = {
    exec: function (command, value) {
        document.execCommand(command, false, value || null);
    },
    getHtml: function (element) {
        return element ? element.innerHTML : "";
    },
};
