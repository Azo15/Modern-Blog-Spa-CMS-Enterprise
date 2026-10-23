(() => {
    'use strict';
    const panels = [...document.querySelectorAll('[data-notifications]')];
    if (!panels.length) return;
    let version = 0, refreshScheduled = false;
    const scheduleRefresh = () => { if(refreshScheduled) return; refreshScheduled = true; setTimeout(() => { refreshScheduled = false; void refresh(); },120); };
    const stamp = value => {
        const raw = String(value); const date = new Date(/[zZ]|[+-]\d\d:\d\d$/.test(raw) ? raw : raw + 'Z');
        return Number.isNaN(date.getTime()) ? '' : new Intl.DateTimeFormat('tr-TR',{timeZone:'Europe/Istanbul',dateStyle:'short',timeStyle:'short'}).format(date) + ' · Türkiye saati';
    };
    const badge = value => {
        const count = Math.max(0,Number(value) || 0);
        document.querySelectorAll('[data-notification-count]').forEach(node => { node.hidden = count === 0; node.textContent = count > 99 ? '99+' : String(count); });
        const toggle = document.getElementById('notification-bell'); if (toggle) toggle.setAttribute('aria-label',`Bildirimler: ${count} okunmamış`);
    };
    const element = (tag,text,css) => { const node = document.createElement(tag); if (text != null) node.textContent = text; if(css) node.className = css; return node; };
    const safeLink = value => { try { const url = new URL(value,window.location.origin); return url.origin === window.location.origin && ['http:','https:'].includes(url.protocol) ? url.pathname + url.search : '/AdminDashboard'; } catch { return '/AdminDashboard'; } };
    function render(panel,data) {
        panel.dataset.latestId = String(data.latestId);
        const list = panel.querySelector('[data-notification-list]'); const rows = [];
        for (const item of data.items) {
            const row = element('li',null,'notification-item ' + (item.isRead ? 'is-read' : 'is-unread'));
            const link = element('a'); link.href = safeLink(item.url); link.dataset.notificationLink = String(item.id); link.dataset.read = String(item.isRead);
            link.append(element('strong',item.title),element('span',item.content),element('time',stamp(item.createdAtUtc))); row.append(link);
            if (!item.isRead) { const button = element('button','Okundu','btn btn-sm btn-outline-secondary'); button.type='button'; button.dataset.notificationRead=String(item.id); button.setAttribute('aria-label','Bildirimi okundu işaretle'); row.append(button); }
            rows.push(row);
        }
        if (!rows.length) rows.push(element('li','Henüz bildiriminiz yok.','notification-empty'));
        list.replaceChildren(...rows); badge(data.unreadCount);
    }
    async function refresh() {
        const current = ++version;
        await Promise.all(panels.map(async panel => {
            try {
                const response = await fetch(panel.dataset.listUrl,{credentials:'same-origin',headers:{Accept:'application/json'}});
                if (!response.ok) throw new Error('Bildirimler yüklenemedi. Lütfen yeniden deneyin.');
                const data = await response.json(); if (current !== version) return;
                render(panel,data); panel.querySelector('[data-notification-status]').textContent='';
            } catch (error) { if (current === version) panel.querySelector('[data-notification-status]').textContent=error.message || 'Bildirimlere erişilemiyor.'; }
        }));
    }
    async function mutate(panel,action,fields,button) {
        const form = document.createElement('form'); form.action='/admin/notifications/' + action;
        const body = new FormData(); Object.entries(fields).forEach(([key,value]) => body.append(key,String(value)));
        if(button) button.disabled=true;
        try { const data = await window.XspaAjax.postForm(form,body); badge(data.unreadCount); await refresh(); return true; }
        catch(error) { panel.querySelector('[data-notification-status]').textContent=error.message || 'İşlem tamamlanamadı.'; return false; }
        finally { if(button) button.disabled=false; }
    }
    panels.forEach(panel => panel.addEventListener('click',async event => {
        const read = event.target.closest('[data-notification-read]');
        const all = event.target.closest('[data-notification-all]');
        const link = event.target.closest('[data-notification-link]');
        if(read) { event.preventDefault(); if(!read.disabled) await mutate(panel,'read',{id:read.dataset.notificationRead},read); }
        else if(all) { event.preventDefault(); if(!all.disabled) await mutate(panel,'read-all',{throughId:panel.dataset.latestId || 0},all); }
        else if(link && link.dataset.read === 'false' && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
            event.preventDefault(); if(link.dataset.busy) return; link.dataset.busy='true';
            if(await mutate(panel,'read',{id:link.dataset.notificationLink})) window.location.assign(link.href); else delete link.dataset.busy;
        }
    }));
    window.addEventListener('xspa:notifications-changed',event => { if(event.detail?.unreadCount != null) badge(event.detail.unreadCount); const bell = document.querySelector('.notification-bell'); bell?.classList.add('is-notifying'); setTimeout(() => bell?.classList.remove('is-notifying'),500); scheduleRefresh(); });
    window.addEventListener('xspa:notifications-refresh',scheduleRefresh);
    document.addEventListener('visibilitychange',() => { if(!document.hidden) void refresh(); });
    document.getElementById('notification-bell')?.addEventListener('click',() => void refresh());
    setInterval(() => { if(!document.hidden) void refresh(); },30000);
    void refresh();
})();
