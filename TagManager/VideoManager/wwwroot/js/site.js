window.openLink = (url, app) => {
    if (app && app.length > 0) {
        window.location.href = `${app}:${url}`;
    } else {
        window.open(url, "_blank");
    }
};