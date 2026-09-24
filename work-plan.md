# Freito (SFQS) — Master Work Plan

แผนกลางที่ session นี้เป็นเจ้าของ รวมงานฝั่ง backend ([technical-plan.md](technical-plan.md)) และ UI ([ui-plan.md](ui-plan.md)) ไว้ในลำดับเดียว
Leader = Claude Code session นี้ (ถือ architecture, schema, quote engine, approval gate, integration, review ทั้งหมด)

## A. Acceptance criteria → งานที่รองรับ

| # | เกณฑ์รับงาน (จาก requirements.md) | งานที่รองรับ | วิธีพิสูจน์ |
|---|---|---|---|
| AC1 | กรอกครบแล้วได้ร่างราคาใน < 2 วินาที | T5, T6 | integration test จับเวลา + test ตามเคสจริงของ Operation |
| AC2 | ราคาคำนวณตรงสูตรทุกโหมด (FCL/LCL/Air) | T5 | unit test ครอบทุกโหมด + เคส revenue ton / weight break / breakeven |
| AC3 | ส่งใบให้ลูกค้าไม่ได้ถ้าไม่ผ่าน Sale approve | T8 | integration test ยิง API ตรงโดยข้าม UI แล้วต้องถูกปฏิเสธ |
| AC4 | Operation แก้อัตราเองได้ ไม่ต้องแตะ DB + ย้อนดูได้ว่าใครแก้ | T4, T12 | ทดลองแก้ผ่านเว็บ + อ่าน audit log |
| AC5 | รองรับหลายสกุลเงิน | T3, T5 | test คำนวณข้ามสกุลด้วย FX ย้อนหลัง |
| AC6 | ใบเก่าไม่เปลี่ยนตามเมื่อ rate ถูกแก้ | T5, T7 | test: สร้าง draft → แก้ rate → ยอดในใบเดิมต้องเท่าเดิม |
| AC7 | เข้าถึงได้ตามมาตรฐาน (contrast, keyboard, reduced motion) | T2, T15 | contrast script + keyboard pass + 4 breakpoints |

## B. ตารางงาน

| ID | งาน | ผู้ทำ | ขอบเขตไฟล์ | ขึ้นกับ | สถานะ |
|---|---|---|---|---|---|
| T0 | ยืนยันเวอร์ชัน .NET บน Plesk, สร้าง DB, deploy hello world | leader + ผู้ใช้ | infra | — | 🔶 **Dev สภาพแวดล้อมพร้อมแล้ว**: .NET เปลี่ยนเป็น net10.0 (ตรงกับเครื่อง dev, EF Core 9 เพราะ Pomelo ยังไม่รองรับ EFCore10) + MySQL dev รันผ่าน Docker (`docker-compose.yml`) — migration apply สำเร็จ, 14 ตารางครบ; **ที่ยังเหลือ**: ยืนยันเวอร์ชัน .NET ที่ Plesk hosting จริงรองรับ + สร้าง DB บน Plesk จริง ยังรอผู้ใช้ (Plesk access) |
| T1 | ยืนยันสูตรกับ Operation (local charge × Incoterm, weight break, ตัวอย่างจริง) | ผู้ใช้/business | เอกสาร | — | ✅ **ปิดแล้ว 100%** — ดู [operation-worksheet.md](operation-worksheet.md) |
| T2 | แก้ contrast 7 คู่ + เติม component token + regenerate css/style guide | subagent (sonnet) | `tokens.json`, `tokens.css`, `style-guide.html` | — | ✅ เสร็จ |
| T3 | Solution skeleton + EF Core + MySQL + migration แรก | leader | `src/**` | T0 | ✅ skeleton เสร็จ; T4 สร้าง initial migration แบบ provisional แล้ว — ยังไม่ได้ apply/ยืนยันกับ DB จริง รอ T0 |
| **T3b** | สร้าง core domain entities ทั้งหมดใน §2 (`Port`, `Carrier`, `Currency`, `ExchangeRate`, `Incoterm`, `IncotermChargeRule`, `CargoType`, `User`, `FreightRate`, `LocalCharge`, `Quotation`, `QuotationLine`, `QuotationStatusHistory`, `AuditLog`) — **leader ทำเอง ไม่ delegate เพราะ T4 กับ T5 ใช้ type เดียวกันพร้อมกัน ถ้าแยกให้ subagent คนละคนทำจะชนไฟล์กัน** | leader | `src/Freito.Domain/Entities/**` | T3 | ✅ **เสร็จ** — 14 entity + `Enums.cs` ครบตาม §2, `dotnet build` 0 errors, `dotnet test` 2/2 ผ่าน — ยังเป็น POCO ล้วน ไม่มี EF config (เป็นงาน T4) |
| T4 | Master data + rate/local charge CRUD + audit log + CSV import | leader | `src/Freito.Api/Controllers/**`, `Infrastructure/**` | **T3b** | 🔶 Backend implement แล้ว: schema + initial migration, seed Incoterms/charge rules/currencies, CRUD, audit log, all-or-nothing CSV; mutations ตรวจ role/actor ID แต่จะตอบ 401 จน T8 เพิ่ม authentication — migration apply กับ dev DB (Docker) สำเร็จแล้ว, UI เป็น T12 |
| T5 | **Quote engine** (rate resolution, 3 โหมด, breakeven, FX, snapshot) | **leader** | `src/Freito.Domain/Quoting/**` | T1, **T3b** | ✅ **เสร็จ (แกนหลัก)** — rate resolution + FX normalize + 3 สูตร (FCL/LCL/Air) + local charge scoping ตาม incoterm + breakeven + NotQuotable — `dotnet test` 13/13 ผ่าน รวมเทียบกับตัวเลขจริง Barcelona quote ตรงเป๊ะ; **snapshot (สร้าง `QuotationLine` จริง) เป็นงาน T7** เพราะต้องแตะ DB |
| T6 | Unit test quote engine ตามเคสจริง | subagent (sonnet) | `src/Freito.Tests/**` | T5 | 🔶 มี regression test พื้นฐาน 11 เคสจาก T5 แล้ว (รวมเทียบเคส Air จริง) — ยังขาด: multi-currency FX เต็มรูปแบบ, alternative-carrier list, LCL/FCL edge case เพิ่มเติมตามเคสจริงที่ Operation ให้เพิ่มทีหลัง |
| T7 | Quotation API (guest calculate/submit, list, detail, refresh-rate) | leader | `src/Freito.Api/Controllers/Quotes*` | T5 | ✅ **เสร็จ** — `QuotationService` เชื่อม quote engine (T5) กับ DB จริง: normalize ราคาเป็นสกุลเดียว (`quote_currency` = สกุลของ freight rate, local charge อื่นสกุลถูกแปลงด้วย exchange rate) ก่อนรวม subtotal; guest calculate/submit ไม่ต้อง auth, submit สร้าง Draft แล้วเลื่อนเข้า PendingSaleApproval ทันทีพร้อม status history 2 แถว (ตาม requirements.md §6); list/detail/refresh-rate gate ด้วย RequireRoles (ตอบ 401 จน T8); `dotnet test` 19/19 ผ่าน (เพิ่ม `QuotationServiceTests` 6 เคสรวม multi-currency + quote-no + refresh delta) + ทดสอบจริงผ่าน `dotnet run` ยิง curl เทียบตัวเลขกับ DB ตรง — ระหว่างทางเจอบั๊กจริง: `yyyyMMdd` ใน quote_no ไปอ่าน Thai Buddhist calendar จาก server locale (ได้ปี 2569 แทน 2026) แก้ด้วย `CultureInfo.InvariantCulture` แล้ว |
| T8 | Auth + role + approval state machine + gate ใน service layer | **leader** | `src/Freito.Api/Auth/**`, `Domain/Workflow/**` | T7 | ✅ **เสร็จ** — JWT cookie-based auth (`PasswordHasher` เอง PBKDF2-HMACSHA256, ไม่ใช้ ASP.NET Identity ตามที่ตัดสินไว้ใน technical-plan.md §2), `POST /api/auth/{login,logout}` + `GET /api/auth/me`; เพิ่ม Users CRUD ใน `MasterDataController` (Admin only, จำเป็นสำหรับสร้าง user คนแรกๆ) พร้อม bootstrap Admin seed ผ่าน migration (`admin@freito.local` — ต้องเปลี่ยนรหัสทันที ดู README.md); `QuotationWorkflow` (Domain layer, pure) บังคับ gate ว่า approve ได้เฉพาะจาก `PendingSaleApproval` และเป็น role **Sale เท่านั้น** (ไม่ใช่ Admin ตามที่ technical-plan.md §3 ระบุ); `/api/quotes/{id}/approve` + `/reject` เพิ่มใน T7's `QuotesController`; ทดสอบจริงผ่าน `dotnet run` + curl ครบ: login ผิดรหัส 401, ถูกรหัสได้ cookie, Admin กด approve โดนบล็อก 403, Sale กด approve สำเร็จ + audit log + status history ครบ, approve ซ้ำโดน 409; `dotnet test` 35/35 ผ่าน |
| T9 | Generate PDF ใบเสนอราคาให้ดาวน์โหลด (Sale ส่งเอง — ไม่ต้อง SMTP ตามคำตอบ Operation) | subagent (sonnet) | `src/Freito.Api/Quotes/PdfExport/**` | T8 | ✅ **เสร็จ** — ใช้ **QuestPDF** (Community license, เลือกโดยผู้ใช้ — ฟรีสำหรับองค์กรรายได้ < 1 ล้าน USD/ปีหรือ open source, ต้องเช็คก่อน deploy จริงถ้าเงื่อนไขเปลี่ยน ดูคอมเมนต์ใน Program.cs); `QuotationPdfDocument` render โลโก้+ข้อมูลลูกค้า/shipment/line items/total+หมายเหตุ; `GET /api/quotes/{id}/pdf` (Sale/Admin) — **gate เพิ่มเติมที่ตั้งใจ**: ดาวน์โหลดได้เฉพาะสถานะ ApprovedAndSent/Confirmed เท่านั้น (ไม่งั้น Sale จะส่ง PDF ข้าม approval gate ได้) ตอบ 409 ถ้ายังไม่อนุมัติ; ทดสอบจริง: submit→login Sale→approve→โหลด PDF ได้ไฟล์จริง 1 หน้า ตัวเลขตรง (200+100=300); **เจอบั๊กจริง 2 อย่างระหว่างทดสอบ**: (1) `FontFamily("Arial")` throw เพราะ QuestPDF bundle มาแค่ font "Lato" — แก้เป็น "Lato"; (2) date format `:yyyy-MM-dd` อ่าน Thai Buddhist calendar จาก server locale ซ้ำปัญหาเดิมจาก T7 (ได้ 2569 แทน 2026) — แก้ด้วย `CultureInfo.InvariantCulture` ทุกจุด |
| T10 | React + Vite + Tailwind + token wiring + primitive set | subagent (sonnet) | `web/**` (config, theme) | T2 | ✅ **เสร็จ** — primitive set ทำเสร็จพร้อม T11 (ดูรายละเอียดที่ T11) |
| T11 | Component library ตาม style guide | subagent (sonnet) | `web/src/components/**` | T10 | ✅ **เสร็จ** — 10 component ตาม ui-plan.md §UI-4 ครบ: Button, Input/Select (+ Field wrapper), Badge, Table (+ EmptyState in-built), SegmentedControl (roving tabindex, arrow keys), Dialog (focus trap + Escape + focus restore, WAI-ARIA APG pattern), Toast (provider/hook, aria-live), Skeleton (respects `prefers-reduced-motion`), EmptyState — ทุกตัวอ่าน `--component-*` token จาก `tokens.css` ตรง ไม่ hardcode สี/ขนาด (เพิ่ม `web/src/styles/components.css` เป็น CSS class เดียวกับที่ style-guide.html ใช้); `web/src/ComponentGallery.tsx` เป็นหน้าโชว์ทุก component ไว้ใช้ dev-reference (แทนที่หน้าจริงใน T12-14); ตรวจจริงผ่าน browser ทั้ง light/dark: dialog focus trap ทำงาน (focus ไป Cancel อัตโนมัติ, Escape ปิด+คืน focus ให้ trigger), toast แสดงผลถูกต้อง, table/badge/segmented เรนเดอร์ตรง token; `npm run build` + `npm run lint` ผ่านไม่มี warning; **เจอบั๊กจริงระหว่างทดสอบ**: gallery section ไม่มี `w-full` ทำให้ parent `flex items-center` shrink-to-fit จน flex-wrap ไม่ทำงานและปุ่มล้นขอบจอ แก้แล้ว |
| T12 | หน้า Operation/Admin (rate, local charge, master data) | subagent (sonnet) | `web/src/pages/ops/**` | T4, T11 |
| T13 | หน้า public quote + FCL/LCL compare | subagent (sonnet) | `web/src/pages/quote/**` | T7, T11 | ✅ **เสร็จ** — `InstantQuotePage` เป็น flow เดียว (ยังไม่มี router): ฟอร์ม (segmented FCL/LCL/Air + field ตามโหมด) → ผลราคา (+ FCL vs LCL compare เมื่อเลือก LCL เทียบกับตู้ 20' เดียวตามสูตร Breakeven) → ฟอร์มติดต่อลูกค้า → หน้ายืนยันพร้อมเลขที่ใบ+วันหมดอายุ; ต่อ API จริงจาก T7 (`/api/public/quotes/{calculate,}`) และ master data จาก T4 (ports/incoterms/cargo-types ซึ่งเป็น public GET); ปุ่ม "Calculate" คำนวณอย่างเดียวไม่ส่งลูกค้าตามที่ design-system-spec.md กำหนด — ต้องผ่าน "Request this quote" + กรอกข้อมูลติดต่อก่อนถึง submit จริง; ตรวจจริงผ่าน browser: ยิง FCL จริง (200+100=300 ตรง), ยิง LCL จริง (125+50=175 ตรง, compare card แสดง FCL 250 vs LCL 175 ไฮไลต์ตัวถูกกว่าถูกต้อง), submit จริงได้ quoteNo กลับมา, ทดสอบ mobile viewport ไม่มี horizontal scroll; **เจอบั๊กจริงระหว่างทดสอบ**: `Select` component (T11) ส่ง `defaultValue` กับ `value` พร้อมกันทำให้ React เตือน controlled/uncontrolled conflict — แก้แล้วให้ `defaultValue` ใส่เฉพาะตอนไม่มี `value`/`defaultValue` จาก caller |
| T14 | หน้า Sale inbox + approval | **leader** | `web/src/pages/sale/**` | T8, T11 |
| T15 | a11y + responsive pass + baseline-ui audit ทั้งระบบ | leader | ทั้ง `web/**` | T12–T14 |
| T16 | Deploy pipeline ขึ้น Plesk + backup + UAT | leader + ผู้ใช้ | infra | T15 |

**งานที่ไม่ delegate**: T5 (สูตรราคา = หัวใจธุรกิจ), T8 (ประตูอนุมัติ = ความเสี่ยงสูงสุด), T14 (หน้าที่ใช้ตัดสินใจเงิน), T15/T16 (รวมงาน + ตรวจรับ)

## C. ลำดับการรวมงาน

```
T0 ─┬─> T3 ─> T3b ─┬─> T4 ─────────> T12 ─┐
    │              └─> T5 ─> T6           │
T1 ─┘                     └> T7 ─> T8 ─┬─> T13 ─┼─> T15 ─> T16
T2 ─> T10 ─> T11 ───────────────────────┴─> T14 ─┘
                                        └> T9
```

ทำ T2 และ T10/T11 คู่ขนานกับ backend ได้ เพราะคนละไฟล์ ส่วน T5 ต้องจบก่อน T7 เสมอ — **T3b เป็นจุดคอขวดที่ต้องทำให้เสร็จก่อนแยก T4/T5 ออกไปคู่ขนาน** (ทั้งคู่แก้ entity เดียวกัน ถ้าเริ่มพร้อมกันก่อน T3b เสร็จจะชนไฟล์)

## D. คำสั่งตรวจ (ใช้ทุก integration point)

```bash
dotnet build && dotnet test
cd web && npm run lint && npm run build
git status && git diff --stat
```
บวก contrast check ของ token หลังแก้ `tokens.json` ทุกครั้ง

## E. ความเสี่ยงที่ยังเปิดอยู่

| ความเสี่ยง | ผลกระทบ | ทางรับมือ |
|---|---|---|
| Plesk ไม่รองรับ .NET เวอร์ชันที่เลือก | บล็อกทั้งโปรเจกต์ | T0 ทำก่อนทุกอย่าง |
| ~~สูตร local charge × Incoterm ยังไม่ confirm~~ | — | ✅ ปิดแล้ว — T1 ตอบครบ, ดู technical-plan.md |
| Guest endpoint ถูกยิงสแปม | ข้อมูลขยะ + โหลด DB | rate limit + honeypot ตั้งแต่ T7 |
| MySQL decimal/rounding | ราคาเพี้ยนสะสม | ล็อก `decimal(18,4)` + test ปัดเศษใน T6 |
| ~~ยังไม่ตัดสิน: base currency, ลำดับชั้นอนุมัติ, อายุใบ, SMTP~~ | — | ✅ ตอบครบแล้ว (USD, ไม่มีลำดับชั้น, 30 วัน, Manual/Outlook) — ดู technical-plan.md §7 |
| ~~Air weight break เป็นตารางหรือ minimum weight~~ | — | ✅ ตอบแล้ว: minimum 50kg + table ถึง 500kg, เกินนั้น manual — ดู technical-plan.md §3 |
| ~~DDP charge แบบ %/at-cost/ตามเวลารวมเข้ายอด instant quote ไหม~~ | — | ✅ ปิดแล้ว: ไม่เข้ายอด, แสดงเป็นหมายเหตุแทน (`calc_basis = NotQuotable`) |
