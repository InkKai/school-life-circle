/* 新手導覽：畫面變暗，只把要按的地方圈起來變亮，一步一句話 */
(function () {
    var steps = [
        { sel: '[data-tour="location"]', text: '先選你在哪間學校附近' },
        { sel: '[data-tour="hello"]', text: '我是小圈！不知道吃什麼就按這裡' },
        { sel: '[data-tour="deals"]', text: '今日優惠，左右滑看看，喜歡就按「領取」' },
        { sel: '[data-tour="cats"]', text: '想買什麼？點分類直接找店' },
        { sel: '[data-tour="nav-map"]', text: '用地圖看附近有哪些店' },
        { sel: '[data-tour="nav-coupons"]', text: '領到的券在這，到店打開給店員掃' },
        { sel: '[data-tour="nav-me"]', text: '會員卡、集點、載具都在「我的」' },
        { sel: '[data-tour="textsize"]', text: '字太小？按這裡放大' }
    ];

    function visible(el) {
        if (!el) return false;
        var r = el.getBoundingClientRect();
        return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== 'hidden';
    }
    function find(sel) {
        var list = document.querySelectorAll(sel);
        for (var i = 0; i < list.length; i++) if (visible(list[i])) return list[i];
        return null;
    }

    var idx = 0, active = [], ring, tip, lastFocus, opts = {};
    var base = (document.currentScript && document.currentScript.src || '').replace(/Scripts\/tour\.js.*$/, '');

    function build() {
        ring = document.createElement('div');
        ring.className = 'tour-ring';
        ring.setAttribute('aria-hidden', 'true');
        tip = document.createElement('div');
        tip.className = 'tour-tip';
        tip.setAttribute('role', 'dialog');
        tip.setAttribute('aria-modal', 'true');
        tip.setAttribute('aria-labelledby', 'tourText');
        tip.innerHTML = '<div class="tour-head"><img src="' + base + 'Content/img/mascot-point.svg" alt="" width="54" height="57" />' +
            '<div><p class="tour-count" id="tourCount"></p><p class="tour-text" id="tourText"></p></div></div>' +
            '<div class="tour-actions"><button type="button" class="btn btn-link tour-skip">略過</button>' +
            '<button type="button" class="btn btn-brand tour-next">下一步</button></div>';
        var shade = document.createElement('div');
        shade.className = 'tour-shade';
        shade.addEventListener('click', function (e) { e.stopPropagation(); });
        document.body.appendChild(shade);
        document.body.appendChild(ring);
        document.body.appendChild(tip);
        tip.querySelector('.tour-skip').addEventListener('click', end);
        tip.querySelector('.tour-next').addEventListener('click', next);
        document.addEventListener('keydown', onKey, true);
        window.addEventListener('resize', place);
        window.addEventListener('scroll', place, { passive: true });
        document.body.classList.add('tour-on');
    }

    function onKey(e) {
        if (e.key === 'Escape') { e.preventDefault(); end(); }
        else if (e.key === 'Tab') {
            // 焦點只在導覽框內循環
            var btns = tip.querySelectorAll('button');
            var first = btns[0], last = btns[btns.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    }

    function place() {
        var el = active[idx] && active[idx].el;
        if (!el) return;
        var r = el.getBoundingClientRect(), pad = 8;
        ring.style.top = (r.top - pad) + 'px';
        ring.style.left = (r.left - pad) + 'px';
        ring.style.width = (r.width + pad * 2) + 'px';
        ring.style.height = (r.height + pad * 2) + 'px';
        var tw = Math.min(320, window.innerWidth - 24);
        tip.style.width = tw + 'px';
        var left = Math.min(Math.max(12, r.left + r.width / 2 - tw / 2), window.innerWidth - tw - 12);
        tip.style.left = left + 'px';
        var below = r.bottom + pad + 14;
        var th = tip.offsetHeight || 150;
        if (below + th > window.innerHeight - 8) {
            tip.style.top = Math.max(8, r.top - pad - 14 - th) + 'px';
        } else {
            tip.style.top = below + 'px';
        }
    }

    function show() {
        var step = active[idx];
        var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var r = step.el.getBoundingClientRect();
        if (r.top < 70 || r.bottom > window.innerHeight - 90) step.el.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
        tip.querySelector('#tourCount').textContent = (idx + 1) + ' / ' + active.length;
        tip.querySelector('#tourText').textContent = step.text;
        tip.querySelector('.tour-next').textContent = idx === active.length - 1 ? (opts.endLabel || '開始使用') : '下一步';
        place();
        setTimeout(place, reduce ? 0 : 350);
        tip.querySelector('.tour-next').focus();
    }

    function next() { if (idx < active.length - 1) { idx++; show(); } else end(true); }

    function end(finished) {
        document.removeEventListener('keydown', onKey, true);
        window.removeEventListener('resize', place);
        window.removeEventListener('scroll', place);
        document.querySelectorAll('.tour-shade,.tour-ring,.tour-tip').forEach(function (n) { n.remove(); });
        document.body.classList.remove('tour-on');
        if (lastFocus && lastFocus.focus) lastFocus.focus();
        // 走完整個導覽（不是按略過）才帶去下一頁，例如去領第一張券
        if (finished === true && opts.endUrl) window.location.href = opts.endUrl;
    }

    window.SmileTour = {
        start: function (o) {
            opts = o || {};
            active = steps.map(function (s) { return { el: find(s.sel), text: s.text }; }).filter(function (s) { return s.el; });
            if (!active.length) return;
            idx = 0;
            lastFocus = document.activeElement;
            build();
            show();
        }
    };
})();
