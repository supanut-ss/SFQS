# Freito — Smart Freight Quotation System (SFQS) — Design System Spec

## Brand brief

**Freito (Smart Freight Quotation System — SFQS)** — B2B sale freight logistics platform (นำเข้า-ส่งออกทางเรือ FCL/LCL และทางอากาศ) ที่ให้ฝ่ายขายกรอกข้อมูลลูกค้าแล้วคำนวณร่างใบเสนอราคาอัตโนมัติ (ผ่าน Sale approval ก่อนส่งลูกค้า) และฝ่าย Operation อัพเดทค่าระวางทั่วโลก โทนต้อง **น่าเชื่อถือ มืออาชีพ** และสื่อสารตัวเลข/ข้อมูลโลจิสติกส์ที่ซับซ้อนให้ดูง่าย เหมาะกับผู้ใช้งาน enterprise/B2B ที่ต้องดูข้อมูลหนาแน่น (dense dashboard) มากกว่าเว็บการตลาด

## Key decisions

| Decision | Choice | Why |
|---|---|---|
| Anchor hue | Navy blue (`primitive.color.navy`) | สื่อความน่าเชื่อถือ+เสถียรภาพ (ตาม heuristic trust→blue) และให้ฟีล maritime/ocean freight โดยไม่เลือก cyan ที่ดูเล่นเกินไป |
| Neutral scale | Cool slate gray | งาน B2B ที่ต้องอ่านตารางราคา/ข้อมูลจำนวนมาก โทนเทาเย็นให้ความรู้สึก technical/แม่นยำ เข้ากับ navy ได้ดีกว่าเทาอุ่น |
| Freight-domain accent | Teal = LCL, Sky = Air, Orange = Export | แยกโหมดขนส่งและทิศทางด้วยสีที่แยกแยะง่ายในตาราง/badge โดยไม่ชนกับสี semantic (success/warning/destructive) |
| Semantic colors | Green/Amber/Red/Navy ตามธรรมเนียม | ผู้ใช้คุ้นเคยกับความหมายสีเหล่านี้อยู่แล้ว ไม่ควรฝืน แม้แบรนด์จะเป็น navy |
| Typography | Inter (heading + body), IBM Plex Mono (ตัวเลข) | Inter อ่านง่ายที่ขนาดเล็กในตารางหนาแน่น; ใช้ monospace เฉพาะคอลัมน์ตัวเลข (ค่าระวาง, CBM, KG, ราคา) เพื่อให้ตัวเลขเรียงคอลัมน์ตรงกัน อ่านเทียบง่าย |
| Type scale | กระชับ (base 0.875rem, heading สูงสุด ~2.1rem) | เป็น dense web app/dashboard ไม่ใช่ marketing site ไม่ต้องการ hero ใหญ่ |
| Radius | 6px (md) ทั่วไป, badge pill เต็ม, การ์ด 8px | ระดับกลาง อ่านเป็นมืออาชีพ/enterprise ไม่แข็งเกินไป (0px) และไม่เล่นเกินไป (12px+) |
| Shadow | เบามาก เน้น border แทน | โทนเทคนิค/serious ตาม heuristic เอกสารจำนวนมากอ่านง่ายกว่าด้วยเส้นขอบมากกว่าเงา |
| Spacing | Base 4px, padding component 8–12px | หนาแน่น เหมาะกับฟอร์ม/ตารางที่มีฟิลด์เยอะ (route, cargo, incoterms, mode, ready date) |
| Motion | fast 150ms / normal 200ms / slow 300ms | โต้ตอบไว รู้สึกแม่นยำ เหมาะกับ toggle โหมด FCL/LCL/Air และ dropdown ฟอร์ม |

## Semantic tokens

| Token | ใช้ทำอะไร |
|---|---|
| `semantic.color.primary` / `primary-hover` | ปุ่มหลัก, ลิงก์, highlight ของการ์ดที่แนะนำ (เช่น FCL แนะนำ) |
| `semantic.color.accent` (teal) | ปุ่มรอง / แถบไฮไลต์รอง |
| `semantic.color.success` / `warning` / `destructive` / `info` | สถานะใบเสนอราคา/บุคกิ้ง: ยืนยันแล้ว, รอราคา, เกินกำหนด, ฉบับร่าง |
| `semantic.freight.import` / `export` | Badge ทิศทางนำเข้า/ส่งออก |
| `semantic.freight.mode-fcl` / `mode-lcl` / `mode-air` | Badge/แท็บโหมดขนส่ง |
| `semantic.freight.status-in-transit` / `status-delayed` / `status-arrived` / `status-pending-do` | Badge สถานะการเดินเรือ/ปล่อยสินค้า ใน Shipment Tracking |
| `semantic.color.highlight` / `highlight-bg` | ข้อความไฮไลต์สำคัญที่ OP ต้องการให้ลูกค้าสังเกตเห็นก่อน (เช่น ETA ที่ประมาณการ) |
| `semantic.font.numeric` | ฟอนต์ monospace สำหรับคอลัมน์ตัวเลข (ค่าระวาง, CBM, KG, เลขเที่ยวเรือ, timestamp) |

## Component specs

### Button
| State | bg | fg | border |
|---|---|---|---|
| Default | `primary` | `primary-foreground` | — |
| Hover | `primary-hover` | `primary-foreground` | — |
| Disabled | `primary` @ 50% opacity | `primary-foreground` | — |

### Badge (Direction / Mode / Status)
Pill shape (`radius.full`), `fontSize.xs`, bold 600. คู่สี bg-อ่อน/fg-เข้มของ token คู่นั้น เช่น `import` = `freight.import-bg` + `freight.import`.

### Incoterms table
- Header: `table.header-bg` + `table.header-fg`, uppercase, ตัวเล็ก
- Row border: `table.row-border`, hover: `table.row-hover-bg`
- คอลัมน์ตัวเลข/บ่งชี้ (เช่น ใครจ่ายค่าระวาง) จัดชิดขวา ใช้ `font.numeric`

### FCL vs LCL Comparison Card
- การ์ดคู่วางเทียบกัน, การ์ดที่แนะนำ (breakeven ชนะ) ใช้ `compare-card.highlight-border` (border หนา สี primary) เพื่อดึงสายตา
- ราคารวมใช้ `font.numeric` ตัวใหญ่เด่น ให้ลูกค้าเทียบง่ายในสายตาแรก

### Instant Quote Form
- Toggle โหมด FCL/LCL/Air เป็นปุ่มกลุ่ม (segmented control) ไม่ใช่ dropdown เพราะเป็นฟิลด์ตัดสินใจหลักที่ควบคุม field ที่เหลือ (ตู้/CBM/KG)
- Field คู่ต่อแถว (`form-row`) เพื่อความหนาแน่นแต่ยังอ่านง่าย: Origin/Destination, Direction/Incoterm, Container-or-CBM-or-KG / Ready Date
- ปุ่ม "Calculate Quote" คำนวณร่างราคาเท่านั้น — **ไม่ส่งให้ลูกค้าโดยตรง** เพราะทุกใบต้องผ่าน Sale Approval Gate ก่อน (ดู [requirements.md](requirements.md) หัวข้อ 6)

**ส่วน Sale Approval** (ต่อท้ายในฟอร์มเดียวกัน คั่นด้วย `quote-divider`):
- Badge `Pending Sale Approval` (warning/amber) แสดงสถานะปัจจุบันของใบเสนอราคานี้
- `rate-compare` แถบเทียบราคาในระบบกับอัตราล่าสุด — แสดง delta เป็นสีแดง (▲ แพงขึ้น) หรือเขียว (▼ ถูกลง) พร้อมปุ่ม "ดึงอัตราล่าสุด" (optional, กดเองเท่านั้น ไม่ auto-refresh)
- Field `Approved By` (เลือกชื่อ Sale) และ `Final Price to Customer` (แก้ไขราคาสุดท้ายได้ก่อนอนุมัติ)
- `Approval Note` (textarea) สำหรับบันทึกเหตุผลถ้ามีการปรับราคา
- ปุ่มคู่ 1:2 — `Reject / ตีกลับ` (outline สีแดง, น้ำหนักรอง) กับ `Approve & Send to Customer` (เต็ม สีเขียว/success, น้ำหนักหลัก เพราะเป็น action ที่ควรทำบ่อยกว่า) ไม่ใช้สี primary (navy) กับปุ่มนี้เพื่อไม่ให้ชนกับความหมาย "ปุ่มหลักทั่วไป" ของระบบ และให้ผู้ใช้เห็นชัดว่านี่คือ action ที่ "ปิดงาน" ได้จริง

### Shipment Tracking Card
ออกแบบจากตัวอย่างข้อความแจ้งสถานะจริงของ OP (เช่น "GPS XIN CHI WAN V.S102" จาก Shekou → Laem Chabang)

- **Header**: ชื่อผู้รับ (เรียนคุณ...) + badge สถานะขวาบน (`status-in-transit` เป็นค่าเริ่มต้น)
- **Vessel line**: ชื่อเรือ + เที่ยว (Voyage No.) ใช้ `font.numeric` ให้เลขอ่านง่าย
- **Route**: ต้นทาง–ปลายทาง วางแบบ 2 คอลัมน์ซ้าย/ขวา คั่นด้วยเส้น+จุด+ลูกศร (`tracking-card.route-line`, `route-dot`) สื่อทิศทางการเดินทางเหมือนภาพต้นฉบับ, รหัสท่า/เมืองใช้ `font.numeric` ตัวหนา
- **Meta row**: Actual time of departure (ซ้าย) / Reported ETA (ขวา) — timestamp ใช้ `font.numeric`
- **Highlight note**: กล่องข้อความสีเหลืองอ่อน (`tracking-card.note-bg` / `note-fg`) สำหรับข้อความที่ OP อยากให้ลูกค้าเห็นก่อนสิ่งอื่น เช่น ETA คร่าวๆ ที่ต้องเน้น (แทนการไฮไลต์เหลืองในแชทด้วยมือ)
- **Remark**: ข้อความอัปเดตอิสระจาก OP + badge สถานะเสริม (เช่น `status-pending-do`) ต่อท้ายได้

สถานะที่รองรับ: `In Transit` (navy) → `Delayed` (amber, ถ้าเรือ/เที่ยวบินล่าช้ากว่ากำหนด) → `Arrived` (green) และ `Pending D/O` (orange, รอปล่อยใบปล่อยสินค้า/ค่าใช้จ่ายปลายทาง) — ใช้แยกจากสถานะใบเสนอราคา (Quotation) เพราะเป็นคนละ lifecycle กัน (ใบเสนอราคา ก่อนขนส่ง / tracking หลังขนส่งเริ่มแล้ว)

## Files delivered
- `tokens.json` — three-layer design tokens (primitive → semantic → component) + dark mode overrides
- `tokens.css` — generated CSS custom properties
- `style-guide.html` — browsable style guide: primitive/semantic colors, type scale, spacing, radius, shadow, base components (button/input/card) + freight-specific components (badges, incoterms table, FCL vs LCL comparison, instant quote form, shipment tracking card)
- `design-system-spec.md` — this document
- `assets/logo.webp` — official Freito logo (full lockup: icon + wordmark + tagline)
- `assets/logo-icon.png` / `assets/logo-lockup.png` — icon-only and icon+wordmark crops, transparent background, cropped and verified from the full lockup for use as favicon and in compact headers (used in `style-guide.html`)

The logo's icon gradient (navy → blue → teal) already lines up with the existing `primitive.color.navy` and `primitive.color.teal` scales — no palette change needed to accommodate it.
