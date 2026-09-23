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
| T0 | ยืนยันเวอร์ชัน .NET บน Plesk, สร้าง DB, deploy hello world | leader + ผู้ใช้ | infra | — | ⏳ รอผู้ใช้ (Plesk access) — solution ใช้ .NET 8 LTS ไปก่อนเป็น assumption |
| T1 | ยืนยันสูตรกับ Operation (local charge × Incoterm, weight break, ตัวอย่างจริง) | ผู้ใช้/business | เอกสาร | — | ⏳ รอผู้ใช้ |
| T2 | แก้ contrast 7 คู่ + เติม component token + regenerate css/style guide | subagent (sonnet) | `tokens.json`, `tokens.css`, `style-guide.html` | — | ✅ เสร็จ |
| T3 | Solution skeleton + EF Core + MySQL + migration แรก | leader | `src/**` | T0 | ✅ skeleton เสร็จ (ยังไม่มี migration จริงเพราะรอ T0/DB) |
| T4 | Master data + rate/local charge CRUD + audit log + CSV import | subagent (sonnet) | `src/Freito.Api/Controllers/Rates*`, `Infrastructure/**` | T3 | |
| T5 | **Quote engine** (rate resolution, 3 โหมด, breakeven, FX, snapshot) | **leader** | `src/Freito.Domain/Quoting/**` | T1, T3 |
| T6 | Unit test quote engine ตามเคสจริง | subagent (sonnet) | `src/Freito.Tests/**` | T5 |
| T7 | Quotation API (guest calculate/submit, list, detail, refresh-rate) | leader | `src/Freito.Api/Controllers/Quotes*` | T5 |
| T8 | Auth + role + approval state machine + gate ใน service layer | **leader** | `src/Freito.Api/Auth/**`, `Domain/Workflow/**` | T7 |
| T9 | ส่งอีเมลใบเสนอราคา + PDF | subagent (sonnet) | `src/Freito.Api/Notifications/**` | T8 |
| T10 | React + Vite + Tailwind + token wiring + primitive set | subagent (sonnet) | `web/**` (config, theme) | T2 | 🔶 scaffold+wiring เสร็จจาก M0, primitive component set ยังไม่ทำ |
| T11 | Component library ตาม style guide | subagent (sonnet) | `web/src/components/**` | T10 |
| T12 | หน้า Operation/Admin (rate, local charge, master data) | subagent (sonnet) | `web/src/pages/ops/**` | T4, T11 |
| T13 | หน้า public quote + FCL/LCL compare | subagent (sonnet) | `web/src/pages/quote/**` | T7, T11 |
| T14 | หน้า Sale inbox + approval | **leader** | `web/src/pages/sale/**` | T8, T11 |
| T15 | a11y + responsive pass + baseline-ui audit ทั้งระบบ | leader | ทั้ง `web/**` | T12–T14 |
| T16 | Deploy pipeline ขึ้น Plesk + backup + UAT | leader + ผู้ใช้ | infra | T15 |

**งานที่ไม่ delegate**: T5 (สูตรราคา = หัวใจธุรกิจ), T8 (ประตูอนุมัติ = ความเสี่ยงสูงสุด), T14 (หน้าที่ใช้ตัดสินใจเงิน), T15/T16 (รวมงาน + ตรวจรับ)

## C. ลำดับการรวมงาน

```
T0 ─┬─> T3 ─> T4 ─────────────> T12 ─┐
    │        └> T5 ─> T6            │
T1 ─┘              └> T7 ─> T8 ─┬─> T13 ─┼─> T15 ─> T16
T2 ─> T10 ─> T11 ───────────────┴─> T14 ─┘
                                 └> T9
```

ทำ T2 และ T10/T11 คู่ขนานกับ backend ได้ เพราะคนละไฟล์ ส่วน T5 ต้องจบก่อน T7 เสมอ

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
| สูตร local charge × Incoterm ยังไม่ confirm | เขียน T5 ซ้ำสองรอบ | T1 ต้องจบก่อน T5 |
| Guest endpoint ถูกยิงสแปม | ข้อมูลขยะ + โหลด DB | rate limit + honeypot ตั้งแต่ T7 |
| MySQL decimal/rounding | ราคาเพี้ยนสะสม | ล็อก `decimal(18,4)` + test ปัดเศษใน T6 |
| ยังไม่ตัดสิน: base currency, ลำดับชั้นอนุมัติ, อายุใบ, SMTP | บล็อก T5/T8/T9 | เคลียร์ก่อนถึงงานนั้น (ดู technical-plan.md §6) |
