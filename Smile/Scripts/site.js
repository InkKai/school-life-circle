/* 全站共用：分享按鈕（手機開系統分享選單，電腦複製連結） */
(function () {
    document.addEventListener('click', function (e) {
        var b = e.target.closest ? e.target.closest('[data-share-url]') : null;
        if (!b) return;
        e.preventDefault();
        var data = { title: b.getAttribute('data-share-title') || document.title, text: b.getAttribute('data-share-text') || '', url: b.getAttribute('data-share-url') };
        var label = b.querySelector('.share-label') || b;
        if (navigator.share) { navigator.share(data).catch(function () { }); return; }
        var t = (data.text ? data.text + ' ' : '') + data.url;
        (navigator.clipboard ? navigator.clipboard.writeText(t) : Promise.reject()).then(function () {
            var old = label.textContent;
            label.textContent = '已複製連結';
            setTimeout(function () { label.textContent = old; }, 2000);
        }).catch(function () { prompt('複製連結傳給朋友：', data.url); });
    });
})();
