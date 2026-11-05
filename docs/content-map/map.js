/* Content treemap layout and rendering, shared by the browser and documentation renderer. */
(() => {
    "use strict";

    const labels = { partial: "Partial", pending: "Not implemented", unassessed: "Not assessed" };
    const escape = value => String(value).replace(/[&<>"']/g, character => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);

    function index(data) {
        const nodes = new Map();
        function visit(node, parent, key) {
            const result = { ...node, parent, key };
            nodes.set(key, result);
            if (node.children) {
                result.children = node.children.map((child, i) => visit(child, result, `${key}/${i}`));
                result.count = result.children.reduce((sum, child) => sum + child.count, 0);
            } else {
                result.count = 1;
            }
            return result;
        }
        return { root: visit(data, null, "root"), nodes };
    }

    function worst(row, side) {
        const total = row.reduce((sum, item) => sum + item.area, 0);
        const minimum = Math.min(...row.map(item => item.area));
        const maximum = Math.max(...row.map(item => item.area));
        return Math.max(side * side * maximum / (total * total), total * total / (side * side * minimum));
    }

    function tiles(items, bounds) {
        const output = [];
        let remaining = { ...bounds };
        let row = [];
        function place() {
            const area = row.reduce((sum, item) => sum + item.area, 0);
            const vertical = remaining.w >= remaining.h;
            const side = vertical ? remaining.h : remaining.w;
            const thickness = area / side;
            let offset = 0;
            for (const item of row) {
                const length = item.area / thickness;
                output.push({ node: item.node, x: remaining.x + (vertical ? 0 : offset), y: remaining.y + (vertical ? offset : 0), w: vertical ? thickness : length, h: vertical ? length : thickness });
                offset += length;
            }
            if (vertical) { remaining.x += thickness; remaining.w -= thickness; }
            else { remaining.y += thickness; remaining.h -= thickness; }
            row = [];
        }
        for (const item of items) {
            const side = Math.min(remaining.w, remaining.h);
            if (row.length && worst([...row, item], side) > worst(row, side)) place();
            row.push(item);
        }
        if (row.length) place();
        return output;
    }

    function layout(root, width, height) {
        const result = [];
        function visit(node, box, depth) {
            result.push({ node, ...box, depth });
            if (!node.children || box.w < 2 || box.h < 2) return;
            const inset = 2;
            const heading = depth === 1 ? 27 : 0;
            const inner = { x: box.x + inset, y: box.y + inset + heading, w: Math.max(0.01, box.w - inset * 2), h: Math.max(0.01, box.h - inset * 2 - heading) };
            const weights = node.children.map(child => node === root && !root.parent ? 1 : Math.sqrt(child.count));
            const total = weights.reduce((sum, value) => sum + value, 0);
            const items = node.children.map((child, i) => ({ node: child, area: inner.w * inner.h * weights[i] / total }));
            for (const item of tiles(items, inner)) {
                visit(item.node, { x: item.x + 1, y: item.y + 1, w: Math.max(0.01, item.w - 2), h: Math.max(0.01, item.h - 2) }, depth + 1);
            }
        }
        visit(root, { x: 0, y: 0, w: width, h: height }, 0);
        return result;
    }

    function shorten(text, width) {
        const capacity = Math.max(0, Math.floor(width / 7));
        return text.length <= capacity ? text : capacity > 1 ? text.slice(0, capacity - 1) + "…" : "";
    }

    function lines(text, width, height) {
        const capacity = Math.max(3, Math.floor((width - 10) / 7));
        const limit = Math.min(3, Math.floor((height - 8) / 16));
        const words = text.split(/\s+/);
        const output = [];
        let line = "";
        while (words.length && output.length < limit) {
            let word = words.shift();
            if (word.length > capacity) { words.unshift(word.slice(capacity)); word = word.slice(0, capacity); }
            if (line && `${line} ${word}`.length > capacity) { output.push(line); line = ""; words.unshift(word); }
            else line += (line ? " " : "") + word;
        }
        if (line && output.length < limit) output.push(line);
        if (words.length && output.length) output[output.length - 1] = output[output.length - 1].slice(0, Math.max(1, capacity - 1)) + "…";
        return output;
    }

    const svgStyle = `
        svg{--background:#ffffff;--foreground:#24292f;--heading:#eaeef2;--border:#d0d7de;--partial:#b6d9ff;--pending:#ffead8;--unassessed:#eaedef;font-family:system-ui,sans-serif}
        @media(prefers-color-scheme:dark){svg{--background:#0d1117;--foreground:#e6edf3;--heading:#21262d;--border:#30363d;--partial:#173d66;--pending:#49301d;--unassessed:#242a32}}
        text{fill:var(--foreground);font-size:12px}a{cursor:pointer}.frame{fill:none;stroke:var(--border);stroke-width:1}.heading{fill:var(--heading)}.cell{fill:var(--unassessed)}.partial .cell{fill:var(--partial)}.pending .cell{fill:var(--pending)}a:hover .cell,a[aria-current=true] .cell{stroke:var(--foreground);stroke-width:1.5}.group-label{font-weight:600}
    `;

    function svg(root, width, height, options = {}) {
        const positions = layout(root, width, height);
        const parts = [`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" role="img" aria-labelledby="map-title map-description">`, `<title id="map-title">${escape(root.name)} content map</title>`, `<desc id="map-description">Each small cell is an individual content piece. Blue: partial; orange: not implemented; gray: not assessed. Groups are balanced for readability.</desc>`, `<style>${svgStyle}</style>`, `<rect width="100%" height="100%" fill="var(--background)"/>`];
        const f = number => number.toFixed(2);
        for (const box of positions.filter(box => !box.node.children)) {
            if (box.w < 1 || box.h < 1) continue;
            const n = box.node;
            const href = options.link ? options.link(n) : `#${n.key}`;
            parts.push(`<a href="${escape(href)}" class="${escape(n.status)}" data-node="${n.key}" aria-label="${escape(n.name)}; ${labels[n.status]}"><title>${escape(n.name)} · ${labels[n.status]} · ${escape(n.technical)}</title><rect class="cell" x="${f(box.x)}" y="${f(box.y)}" width="${f(box.w)}" height="${f(box.h)}"/>`);
            if (box.w > 43 && box.h > 22) {
                parts.push(`<text x="${f(box.x + 5)}" y="${f(box.y + 17)}">`);
                lines(n.name, box.w, box.h).forEach((line, i) => parts.push(`<tspan x="${f(box.x + 5)}" dy="${i ? 16 : 0}">${escape(line)}</tspan>`));
                parts.push("</text>");
            }
            parts.push("</a>");
        }
        for (const box of positions.filter(box => box.node.children && box.depth === 1)) {
            const n = box.node;
            const href = options.link ? options.link(n) : `#${n.key}`;
            parts.push(`<rect class="frame" x="${f(box.x)}" y="${f(box.y)}" width="${f(box.w)}" height="${f(box.h)}"/><a href="${escape(href)}" data-group="${n.key}" aria-label="Open ${escape(n.name)}"><title>${escape(n.name)} · ${n.count} content pieces</title><rect class="heading" x="${f(box.x)}" y="${f(box.y)}" width="${f(box.w)}" height="25"/><text class="group-label" x="${f(box.x + 6)}" y="${f(box.y + 17)}">${escape(shorten(n.name + " ›", box.w - 12))}</text></a>`);
        }
        parts.push("</svg>");
        return parts.join("");
    }

    globalThis.AresContentMap = { index, layout, svg, labels };
})();
