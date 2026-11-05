(() => {
    "use strict";
    const map = globalThis.AresContentMap;
    const { root, nodes } = map.index(globalThis.AresContent);
    const chart = document.getElementById("chart");
    const breadcrumbs = document.getElementById("breadcrumbs");
    const groups = document.getElementById("groups");
    const selection = document.getElementById("selection");
    let current = root;
    let selected = null;

    function navigate(key) {
        const target = nodes.get(key) || root;
        selected = target.children ? null : target;
        current = target.children ? target : target.parent;
        draw();
    }

    function button(node, parent) {
        const element = document.createElement("button");
        element.type = "button";
        element.textContent = node.name;
        if (node === current) element.setAttribute("aria-current", "location");
        element.addEventListener("click", () => { location.hash = node.key; });
        parent.append(element);
    }

    function draw() {
        const width = Math.max(280, Math.floor(chart.getBoundingClientRect().width));
        chart.innerHTML = map.svg(current, width, width < 440 ? 640 : 650);
        chart.dataset.group = current.name;
        chart.dataset.contentCount = String(current.count);
        breadcrumbs.replaceChildren();
        const path = [];
        for (let node = current; node; node = node.parent) path.unshift(node);
        path.forEach((node, i) => {
            if (i) { const separator = document.createElement("span"); separator.textContent = "›"; separator.setAttribute("aria-hidden", "true"); breadcrumbs.append(separator); }
            button(node, breadcrumbs);
        });
        groups.replaceChildren();
        for (const node of current.children || []) if (node.children) button(node, groups);
        selection.textContent = selected ? `${selected.name} · ${map.labels[selected.status]} — ${selected.note || "Availability and server behavior have not been assessed."}` : "Select a cell for its status. Open a group to explore its content.";
        if (selected) {
            const mark = chart.querySelector(`[data-node="${selected.key}"]`);
            if (mark) mark.setAttribute("aria-current", "true");
        }
    }

    // Cell selection keeps the current group in view; group links open their children.
    chart.addEventListener("click", event => {
        const link = event.target.closest("a");
        if (!link) return;
        event.preventDefault();
        const key = link.dataset.group || link.dataset.node;
        const target = nodes.get(key);
        if (target.children) { location.hash = key; return; }
        selected = target;
        draw();
        history.replaceState(null, "", `#${key}`);
    });
    window.addEventListener("hashchange", () => navigate(location.hash.slice(1)));
    let lastWidth = 0;
    new ResizeObserver(entries => {
        const width = Math.floor(entries[0].contentRect.width);
        if (width !== lastWidth) { lastWidth = width; draw(); }
    }).observe(chart);
    navigate(location.hash.slice(1));
})();
