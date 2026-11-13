import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createContext, runInContext } from "node:vm";

const directory = join(dirname(fileURLToPath(import.meta.url)), "../docs/content-map");
const context = createContext({});
runInContext(readFileSync(join(directory, "content.js"), "utf8"), context);
runInContext(readFileSync(join(directory, "map.js"), "utf8"), context);
const map = context.AresContentMap;
const { root } = map.index(context.AresContent);
const outputs = new Map();
const check = process.argv.includes("--check");
const slug = node => node.name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
const anchor = node => "content-" + node.key.replaceAll("/", "-");
const cell = value => String(value).replaceAll("|", "\\|").replaceAll("\n", " ");
const overview = (node, link) => map.svg(node, 1000, 650, { link });
const overviewNote = "Each small cell represents one content piece. Green: **Done**; blue: **Partial**; orange: **Not implemented**; gray: **Not assessed**. Group sizes are balanced for readability. Internal names and availability still need review.\n";

outputs.set("overview.svg", overview(root, node => {
    let category = node;
    while (category.parent !== root) category = category.parent;
    return `categories/${slug(category)}.md`;
}));
const index = ["# ARES content map", "", "[Back to ARES-Server](../../README.md) · [Current server support](support.md)", "", "![ARES content overview](overview.svg)", "", overviewNote, "## Explore content", ""];
for (const category of root.children) {
    const file = slug(category);
    index.push(`- [${category.name}](categories/${file}.md)`);
    outputs.set(`categories/${file}.svg`, overview(category, node => `${file}.md#${anchor(node)}`));
    const lines = [`# ${category.name}`, "", "[All content](../README.md) · [Current server support](../support.md)", "", `![${category.name} content map](${file}.svg)`, "", overviewNote];
    function section(node, depth) {
        if (node.children) {
            if (depth) lines.push(`<a id="${anchor(node)}"></a>`, "", `${"#".repeat(Math.min(depth + 1, 6))} ${node.name}`, "");
            const pieces = node.children.filter(child => !child.children);
            if (pieces.length) {
                lines.push("| Content | Status | Details |", "| --- | --- | --- |");
                for (const piece of pieces) lines.push(`| <a id="${anchor(piece)}"></a>${cell(piece.name)} | ${map.labels[piece.status]} | ${cell(piece.note || "Availability and server behavior have not been assessed.")} |`);
                lines.push("");
            }
            for (const child of node.children) if (child.children) section(child, depth + 1);
        }
    }
    section(category, 0);
    outputs.set(`categories/${file}.md`, lines.join("\n").trimEnd() + "\n");
}
index.push("", "## Interactive view", "", "Open `index.html` in a browser from a checkout to use zoomable groups, content selection and direct links to entries. All required scripts and styles are included; no server or external downloads are needed.", "", "## Updating the map", "", "Edit the labels, notes and statuses in `content.js`, then run:", "", "```sh", "node tools/render-content-map.mjs", "node tools/render-content-map.mjs --check", "```", "", "The interactive view, README image and category pages use the same content tree.", "");
outputs.set("README.md", index.join("\n"));

for (const [file, text] of outputs) {
    const path = join(directory, file);
    if (check) {
        if (readFileSync(path, "utf8") !== text) throw new Error(`Outdated content map: ${file}`);
    } else {
        mkdirSync(dirname(path), { recursive: true });
        writeFileSync(path, text);
    }
}
console.log(`${check ? "Verified" : "Wrote"} ${outputs.size} content-map files (${root.count} content pieces).`);
