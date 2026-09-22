# Freito (SFQS) — UI / Design Plan

ต่อจาก [design-system-spec.md](design-system-spec.md) และ [technical-plan.md](technical-plan.md)
ครอบคลุม: ผลตรวจ design system ที่มีอยู่, ช่องที่ต้องเติม, โครงหน้าจอ, และกติกาการเขียน UI

## 1. ผลตรวจ design system ปัจจุบัน (วัดจริงจาก tokens.json)

**โครงสร้างผ่าน**: three-layer ครบ (primitive 80+ / semantic 48 / component 43 / dark) — ไม่มี hardcode ในชั้น semantic

**ปัญหา contrast ที่ต้องแก้ (WCAG AA)** — badge ใช้ `fontSize.xs` = 12px ซึ่งไม่เข้าเกณฑ์ large text จึงต้องผ่าน 4.5:1 แต่ปัจจุบัน:

| คู่สี | ตอนนี้ | หลังเปลี่ยนเป็น shade 700 |
|---|---|---|
| success / success-bg | 3.15 ❌ | `#15803D` → 4.79 ✅ |
| warning / warning-bg (= status-delayed) | 3.07 ❌ | `#B45309` → 4.84 ✅ |
| destructive / destructive-bg | 4.41 ❌ | `#B91C1C` → 5.91 ✅ |
| export / export-bg (= status-pending-do) | 3.35 ❌ | `#C2410C` → 4.88 ✅ |
| mode-lcl / bg | 3.59 ❌ | `#0F766E` → 5.25 ✅ |
| mode-air / bg | 3.84 ❌ | `#0369A1` → 5.57 ✅ |
| status-arrived / bg | 3.15 ❌ | `#15803D` → 4.79 ✅ |

ที่ผ่านอยู่แล้ว: primary/white 10.87, foreground/background 17.06, muted-foreground 4.55, info & import & fcl & in-transit 7.31, table header 6.92

> หมายเหตุ: `destructive` ยังใช้ `#DC2626` เป็นสีปุ่ม/พื้นแดงได้ตามเดิม — เปลี่ยนเฉพาะ **สีตัวอักษรบนพื้นอ่อน**

## 2. Component tokens ที่ยังขาด (เติมก่อนเริ่มเขียน UI)

design system ปัจจุบันครอบคลุมเฉพาะ component ที่อยู่ใน style guide แต่แอปจริงต้องใช้มากกว่านั้น:

- **Form**: `select`, `checkbox`, `radio`, `segmented-control` (toggle FCL/LCL/Air), `field-error`, `field-help`, `disabled` state, `focus-ring` (width/offset ชัดเจน)
- **Overlay**: `dialog` (ใช้กับ Reject/ลบ rate), `toast`, `tooltip`, `dropdown-menu`
- **Data**: `table.sticky-header`, `pagination`, `skeleton`, `empty-state`
- **Layout**: `sidebar`, `topbar`, **z-index scale** (คงที่ ห้ามใส่เลขสุ่ม), breakpoint tokens (375 / 768 / 1024 / 1440)
- **Dark mode**: ตรวจ contrast ชุดเดียวกันซ้ำในโหมดมืด (ยังไม่ได้ตรวจ)

## 3. ทิศทางภาพ (จุดยืนของหน้าจอนี้)

สิ่งที่ลูกค้าและ Sale มองหาในวินาทีแรกคือ **ตัวเลขราคา** ไม่ใช่กราฟฟิก ดังนั้น:

- **ตัวเลขคือพระเอก** — ราคารวมใช้ `font.numeric` ขนาดใหญ่กว่าหัวข้อรอบตัว, ทุกคอลัมน์ตัวเลขใช้ `tabular-nums` เรียงหลักตรงกัน
- **อ่านแบบใบราคา ไม่ใช่การ์ด SaaS** — แยกส่วนด้วยเส้นและตาราง ไม่ใช่การ์ดมุมมนซ้ำกันทุกกล่อง เงาใช้น้อยที่สุด (ตรงกับ decision เดิมในสเปก)
- **accent เดียวต่อหน้า** — หน้า quote ให้ความเด่นกับการ์ดที่ breakeven ชนะเท่านั้น, หน้า approval ให้ความเด่นกับปุ่ม Approve
- **ไม่มี motion ตกแต่ง** — มีเฉพาะ motion ที่ตอบการกระทำ (เปิด dialog, ขยายรายละเอียดราคา) ≤ 200ms, เคารพ `prefers-reduced-motion`
- **ไม่ใช้ gradient / glow** และไม่ใช้อีโมจิแทนไอคอน (ใช้ SVG ชุดเดียว — Lucide)

*cross-check กับฐานข้อมูล UI pattern: แนวที่ตรงกับงานนี้คือ Swiss/minimal + dense dashboard + status color เขียว/เหลือง/แดง ซึ่งตรงกับระบบที่มีอยู่แล้ว จึงไม่เปลี่ยน palette navy/slate และ Inter + IBM Plex Mono*

## 4. หน้าจอที่ต้องมี (Information architecture)

| # | หน้า | ผู้ใช้ | องค์ประกอบหลัก |
|---|---|---|---|
| 1 | Instant Quote (public) | Guest / Sale | ฟอร์ม + segmented mode + ผลคำนวณ + FCL vs LCL compare |
| 2 | Quote result / ส่งคำขอ | Guest | สรุปราคา + ฟอร์มติดต่อ + ข้อความยืนยันว่าจะมี Sale ติดต่อกลับ |
| 3 | Quotation inbox | Sale | ตารางใบเสนอราคา + filter ตามสถานะ + badge |
| 4 | Quotation detail + Approval | Sale | รายการราคาแยกบรรทัด, rate delta, final price, note, Approve/Reject |
| 5 | Rate management | Operation | ตาราง rate + filter เส้นทาง + CRUD + import CSV + audit log |
| 6 | Local charge management | Operation | เหมือนข้อ 5 |
| 7 | Master data | Admin | ports / carriers / currencies + FX rate / users |
| 8 | Login | Internal | — |
| 9 | Shipment tracking (เฟสถัดไป) | Sale / ลูกค้า | ตาม Tracking Card ที่ออกแบบไว้แล้ว |

**หน้าที่ยังไม่มีดีไซน์เลย**: 3, 5, 6, 7, 8 — ต้องออกแบบเพิ่ม (style guide เดิมครอบเฉพาะ 1, 4 และ 9)

## 5. กติกาการเขียน UI (บังคับทุก PR)

**Stack ฝั่ง UI**: React + Vite + TypeScript, Tailwind (map ตัวแปรจาก `tokens.css` เป็น theme ไม่สร้างสีใหม่), primitive ที่เข้าถึงได้ชุดเดียว (Base UI หรือ Radix — เลือกอันเดียว ห้ามปน), TanStack Table สำหรับตารางหนัก, `cn()` (clsx + tailwind-merge) สำหรับ class logic

- ปุ่มที่มีแต่ไอคอน **ต้องมี `aria-label`**
- การกระทำที่ย้อนไม่ได้ (Reject ใบเสนอราคา, ลบ rate) **ต้องผ่าน AlertDialog**
- error แสดง **ติดกับฟิลด์ที่ผิด** ไม่ใช่รวมบนหัวฟอร์มอย่างเดียว
- ห้าม rebuild keyboard/focus เอง, ห้ามลบ focus ring
- ใช้ `h-dvh` ไม่ใช่ `h-screen`, z-index ใช้สเกลคงที่
- ตัวเลขทุกจุด `tabular-nums`; หัวข้อ `text-balance`, ย่อหน้า `text-pretty`
- loading ใช้ skeleton ตามโครงจริง; empty state ต้องมี action เดียวที่ชัด (เช่น "เพิ่มอัตราแรกของเส้นทางนี้")
- animate เฉพาะ `transform`/`opacity` ≤ 200ms ห้าม animate `width`/`height`/`margin`
- responsive ตรวจที่ 375 / 768 / 1024 / 1440 และห้าม horizontal scroll (ตารางกว้างให้ scroll เฉพาะตัวตาราง)

## 6. ลำดับงาน UI

1. ~~**UI-1** แก้ contrast 7 คู่ใน `tokens.json` → regenerate `tokens.css` → regenerate `style-guide.html`~~ **เสร็จแล้ว** (T2) — ดู §7
2. ~~**UI-2** เติม component token ที่ขาด (ข้อ 2) + ตรวจ contrast โหมดมืด~~ **เสร็จแล้ว** (T2)
3. **UI-3** ตั้ง React + Vite + Tailwind ให้ใช้ token จาก `tokens.css` ได้ตรง + ชุด primitive + `cn()`
4. **UI-4** สร้าง component หลักตาม style guide: Button / Input / Select / Badge / Table / SegmentedControl / Dialog / Toast / Skeleton / EmptyState
5. **UI-5** ประกอบหน้า 1–2 (public quote) → ต่อ API ของ M2
6. **UI-6** ประกอบหน้า 3–4 (Sale) → ต่อ API ของ M3
7. **UI-7** ประกอบหน้า 5–7 (Operation/Admin) → ต่อ API ของ M1
8. **UI-8** a11y + responsive pass ทั้งระบบ (keyboard, contrast, reduced motion, 4 breakpoints)

## 7. T2 — สิ่งที่ทำจริง (ตรวจแล้ว)

- แก้สี foreground 9 จุดใน `tokens.json` (semantic.color: success/warning/destructive, semantic.freight: export/mode-lcl/mode-air/status-delayed/status-arrived/status-pending-do, semantic.color.accent) ให้ใช้ shade 700 แทน 500/600
- เติม primitive color shade 300/900 ให้ 6 ตระกูล (teal/sky/orange/green/amber/red) สำหรับใช้เป็น dark-mode foreground/background
- เติม `primitive.zIndex` (7 ระดับ) และ `primitive.breakpoint` (4 ค่า) ตามที่ระบุว่าขาดใน §2
- เติม component token 15 กลุ่มใหม่: `focus`, `select`, `checkbox`, `radio`, `segmented`, `field`, `disabled`, `dialog`, `toast`, `tooltip`, `dropdown`, `pagination`, `skeleton`, `empty-state`, `sidebar`, `topbar`
- เขียน `scripts/generate-tokens.cjs` ใหม่ (แทนตัวเดิมจาก skill) ให้ component/semantic ชี้เป็น `var(--x)` ไม่ inline ค่า เพื่อให้ dark mode override ที่ semantic ชั้นเดียวไหลไปถึง component tokens อัตโนมัติ — สคริปต์เดิม flatten เป็นค่า literal ทำให้ต้อง override ซ้ำทุกจุด และ validate ก่อน generate (fail ถ้ามี reference ที่ resolve ไม่ได้)
- เขียน `scripts/check-contrast.py` ตรวจ 20 คู่สี × 2 โหมด (light/dark) อัตโนมัติ — ผลล่าสุด: **ผ่านทั้ง 40 คู่ที่ 4.5:1**
- เติม dark-mode override ให้ครบทุก semantic/freight token ที่ใช้จริง (เดิมมีแค่ 7 ตัว) + เพิ่ม `@media (prefers-color-scheme: dark)` คู่กับ `.dark` class
- Regenerate `tokens.css` และ `style-guide.html`

**เหตุการณ์ระหว่างทำ**: รันตัว generator `build-styleguide.cjs` ของ skill ไปทับ `style-guide.html` เดิมโดยไม่ได้เช็คก่อนว่ามันสร้างแค่ section พื้นฐาน (primitive/semantic/component) — ทำให้ section freight-specific ที่เขียนมือไว้เดิม (badges, Incoterms table, FCL vs LCL card, Instant Quote Form + Sale Approval, Shipment Tracking Card) หายไป ไฟล์นี้ไม่เคยถูก commit จึงกู้จาก git ไม่ได้ ต้องเขียนใหม่จาก [design-system-spec.md](design-system-spec.md) แทน (เนื้อหา/โครงสร้างตรงตามสเปกเดิม แต่ไม่ใช่ markup ต้นฉบับตัวต่อตัว) — ตรวจแล้วด้วย browser ว่า render ถูกต้องครบทุกส่วน
## 8. Dark-mode toggle + axe audit (ทำเพิ่มตามที่ผู้ใช้ขอ)

- เพิ่มปุ่ม **Dark mode / Light mode** จริงในหน้า `style-guide.html` (มุมขวาบน) — toggle class `.dark` บน `<html>`, จำค่าไว้ด้วย `localStorage` (wrap try/catch), fallback ตาม `prefers-color-scheme` ถ้ายังไม่เคยกด, transition สี ≤150ms และเคารพ `prefers-reduced-motion`
- **เจอบั๊กจริงระหว่างทดสอบ**: ปุ่ม "Approve & send to customer" ใช้ `var(--color-success)` เป็นพื้นหลัง — โทเค็นนี้ถูกออกแบบไว้เป็น "สีตัวอักษรบนพื้นอ่อน" (badge) ไม่ใช่ "พื้นหลังปุ่มทึบ" พอสลับ dark mode สีเปลี่ยนเป็นเขียวอ่อนพาสเทลแล้วตัวอักษรขาวจมหายไป (contrast 1.4:1) — แก้โดยเพิ่ม token ใหม่ `component.button.success-bg` / `success-fg` / `success-hover-bg` (อิง `primitive.color.green.700/800` ตรง ไม่ผูกกับ semantic token ที่สลับตามธีม) เพราะปุ่ม CTA สีทึบกับ badge สีอ่อนเป็นบริบทการใช้งานคนละแบบ ไม่ควรใช้โทเค็นร่วมกัน
- รัน **axe-core 4.13** จริงผ่าน headless browser (เปิด local static server ชั่วคราวเพื่อให้ axe โหลดจาก `/node_modules/` ได้ — ไฟล์ที่เปิดตรงจาก `file://` ในพรีวิวถูกเสิร์ฟเป็น data-URL snapshot ทำให้ inject สคริปต์ข้ามไฟล์ไม่ได้) เจอ 4 ปัญหาจริงในรอบแรก แก้ครบแล้วตรวจซ้ำเหลือ **0 violations ทั้ง light และ dark mode**:
  1. `color-contrast` (serious, 9 nodes) — label ใต้กล่อง Radius/Shadow ใช้พื้นหลัง `#fff` ฮาร์ดโค้ดคู่กับสี `muted-foreground` ที่สลับตามธีม → เปลี่ยนพื้นเป็น `var(--card-bg)` และตัวอักษรเป็น `var(--color-foreground)`
  2. `landmark-one-main` (moderate) — หน้าไม่มี `<main>` → ห่อเนื้อหาทั้งหมดด้วย `<main>`, ส่วนหัวเปลี่ยนเป็น `<header>`
  3. `region` (moderate, 27 nodes) — เนื้อหานอก landmark → แก้พร้อมข้อ 2
  4. `select-name` (critical) — `<select>` ตัวอย่างในหมวด Components ไม่มีชื่อที่เข้าถึงได้ → เพิ่ม `<label for="demo-select">`

## 9. CI จริงสำหรับ design system (ปิดรายการค้างจาก §8)

ตั้งเป็น automated check ถาวรแล้ว ไม่ใช่รันมือ:

- [package.json](package.json) — `npm run check` รัน 3 ขั้นตอนเรียงกัน: `tokens:check` → `contrast:check` → `a11y:check`
- [scripts/check-tokens-fresh.mjs](scripts/check-tokens-fresh.mjs) — regenerate `tokens.css` จาก `tokens.json` ในหน่วยความจำแล้ว diff กับไฟล์ที่ commit ไว้ ถ้าไม่ตรง fail (กันเคสแก้ `tokens.json` แล้วลืมรัน build)
- [scripts/a11y-check.mjs](scripts/a11y-check.mjs) — เปิด static server ชั่วคราว + Puppeteer (Chromium headless) รัน axe-core จริงทั้ง light/dark แล้ว exit code ตามผล ไม่ใช่แค่ print
- [.github/workflows/design-system-checks.yml](.github/workflows/design-system-checks.yml) — รันทั้ง 3 เช็คบน GitHub Actions ทุก push/PR ที่แตะ `tokens.json`, `tokens.css`, `style-guide.html`, `scripts/**`

**ตรวจแล้วว่าเช็คจับ regression ได้จริง** (ไม่ใช่แค่ happy path): ทดลองแก้ `semantic.color.success` ให้กลับไปใช้ shade 500 (ค่าที่เคยตก AA) แล้วรัน `npm run tokens:check` และ `python scripts/check-contrast.py` — **ทั้งคู่ fail จริงพร้อม exit code 1** (`success / success-bg` วัดได้ 2.18 ตกจาก 4.5) แล้ว revert กลับและรัน `npm run check` ซ้ำ ผ่านครบทั้ง 3 ขั้นตอนอีกครั้ง

**dependency**: เพิ่ม `puppeteer` (^25, ตรวจแล้ว 0 vulnerabilities จาก `npm audit` — เวอร์ชัน 23.x ที่ลองก่อนมี high-severity ผ่าน `@puppeteer/browsers`/`extract-zip` จึงไม่ใช้) และ `axe-core` เป็น devDependencies, `node_modules/` ใส่ใน `.gitignore` แล้ว

**ยังไม่ได้ทำ**: ยังไม่เคย push ขึ้น GitHub จริงเพื่อดู workflow รันบน Actions runner จริง (ตรวจแค่ในเครื่องด้วยคำสั่งเดียวกับที่ workflow เรียก) — ต้องรอ repo มี remote ก่อน
