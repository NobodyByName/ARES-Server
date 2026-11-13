# ARES content map

[Back to ARES-Server](../../README.md) · [Current server support](support.md)

![ARES content overview](overview.svg)

Each small cell represents one content piece. Green: **Done**; blue: **Partial**; orange: **Not implemented**; gray: **Not assessed**. Group sizes are balanced for readability. Internal names and availability still need review.

## Explore content

- [Maps](categories/maps.md)
- [ARC enemies](categories/arc-enemies.md)
- [Items & equipment](categories/items-equipment.md)
- [World objects](categories/world-objects.md)
- [World objectives](categories/world-objectives.md)
- [Character skills](categories/character-skills.md)
- [Traders & NPCs](categories/traders-npcs.md)
- [Quests](categories/quests.md)
- [Outpost furniture](categories/outpost-furniture.md)
- [Appearance](categories/appearance.md)

## Interactive view

Open `index.html` in a browser from a checkout to use zoomable groups, content selection and direct links to entries. All required scripts and styles are included; no server or external downloads are needed.

## Updating the map

Edit the labels, notes and statuses in `content.js`, then run:

```sh
node tools/render-content-map.mjs
node tools/render-content-map.mjs --check
```

The interactive view, README image and category pages use the same content tree.
