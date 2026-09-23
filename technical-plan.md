# Freito (SFQS) — Technical Plan (Implementation)

เอกสารนี้ต่อจาก [requirements.md](requirements.md) และ [project-plan.md](project-plan.md) โดยแปลงเป็นแผนทางเทคนิคที่ลงมือเขียนโค้ดได้

## 0. Decisions ที่ยืนยันแล้ว

| หัวข้อ | มติ |
|---|---|
| Stack | React (Vite + TypeScript) + ASP.NET Core Web API + MySQL |
| ORM | EF Core + Pomelo.EntityFrameworkCore.MySql, migrations-first |
| Deployment | Plesk, **single domain** — API ที่ `/api/*`, React static ที่ `/` (SPA fallback) |
| Customer access | **Guest form** ไม่ต้องสมัครสมาชิก; login มีเฉพาะ Sale / Operation / Admin |
| Currency | **Multi-currency ตั้งแต่ MVP** (Admin อัพเดท exchange rate เอง, ยังไม่ sync external API) |
| .NET version | เลือกเวอร์ชัน LTS ที่ Plesk host ติดตั้ง ASP.NET Core Hosting Bundle ไว้ — **ต้องเช็คกับ host ก่อนเริ่ม M0** |

## 1. Architecture

```
Browser ──> https://<domain>/            React SPA (static, served by ASP.NET Core)
            https://<domain>/api/...     ASP.NET Core Web API
                                          └─> EF Core ─> MySQL
```

- Deploy เป็น **artifact เดียว**: `dotnet publish` แล้ววาง React build ลง `wwwroot/` → อัปโหลดขึ้น Plesk (ไม่มี CORS, ไม่มี subdomain แยก)
- Guest ใช้ endpoint สาธารณะ (`POST /api/public/quotes/calculate`, `POST /api/public/quotes`) มี rate limit + honeypot/captcha กัน spam
- Internal ใช้ JWT cookie-based auth + role claim (`Sale` / `Operation` / `Admin`)

### Solution layout
```
src/
  Freito.Api/            controllers, auth, DI, wwwroot (React build)
  Freito.Domain/         entities, enums, business rules (quote engine อยู่ที่นี่)
  Freito.Infrastructure/ EF Core DbContext, migrations, repositories
  Freito.Tests/          unit tests (เน้น quote engine) + integration tests
web/                   React + Vite + TypeScript (import tokens.css ตรง)
```

## 2. Data model (ปรับจาก requirements §5 — เติมช่องที่ขาด)

**Master data**
- `ports` — code (UN/LOCODE), name, city, country, type (`Sea` / `Air`)
- `carriers` — code, name, type (`Shipping Line` / `Airline`)
- `currencies` — code, name, decimals; `exchange_rates` — currency, rate_to_base, effective_date (เก็บย้อนหลัง ไม่ overwrite)
- `incoterms` — 11 เทอมคงที่ + `incoterm_charge_rules` (incoterm, charge_side `Origin`/`Destination`, payer `Seller`/`Buyer`) ← ต้อง confirm กับ Operation ก่อนเขียน logic
- `cargo_types` — name, is_dangerous, is_prohibited

**Rate**
- `freight_rates` — origin_port_id, destination_port_id, mode, direction, container_size (FCL), weight_break_min/max (Air), carrier_id, price, currency, valid_from, valid_to, is_active
- `local_charges` — port_id, direction, mode, charge_type, **calc_basis** (`PerShipment` / `PerContainer` / `PerCBM` / `PerKG`), amount, currency, charge_side (`Origin`/`Destination`)

**Quotation** (เพิ่มจาก requirements)
- `quotations` — `quote_no` (running, unique), customer_name/company/email/phone, origin/destination, direction, mode, cargo_type, qty, container_size/cbm/weight, incoterm, ready_date, **quote_currency**, **fx_rate_used**, `rate_source` (`System`/`Refreshed`/`Manual`), freight_cost, local_charge_total, subtotal, discount_amount, **final_price**, status, version, created_by (nullable = guest), approved_by, approved_at, sent_at, expires_at
- `quotation_lines` — **snapshot ของทุกบรรทัดราคา** (description, basis, unit_price, qty, amount, currency, source_rate_id) → rate เปลี่ยนทีหลัง ใบเก่าไม่เปลี่ยนตาม
- `quotation_status_history` — from_status, to_status, actor, note, at
- `audit_logs` — entity, entity_id, action, changed_by, changed_at, before/after (JSON) ใช้กับ rate/charge ทั้งหมด

**หลักเลข**: เงินใช้ `decimal(18,4)`, CBM/KG ใช้ `decimal(12,3)`, ปัดเศษเฉพาะตอนแสดงผลและตอนบันทึก final_price เท่านั้น

## 3. Quote engine — กฎที่ต้องชัดก่อนเขียน

**Rate resolution** (เรียงตามลำดับ):
1. กรอง `freight_rates` ที่ตรง origin + destination + mode + direction + `is_active` + `ready_date` อยู่ในช่วง valid_from..valid_to
2. กรองตามขนาดตู้ (FCL) หรือ weight break ที่ครอบ chargeable weight (Air)
3. เจอหลายรายการ → normalize เป็นสกุลฐานด้วย exchange rate ล่าสุด แล้ว **เลือกราคาต่ำสุด** และคืนรายการที่เหลือให้ Sale เลือกเปลี่ยน carrier ได้
4. ไม่เจอเลย → ไม่บล็อก แต่ตอบ `NoRateFound` + ให้ Sale กรอกราคาเอง (`rate_source = Manual`) และ flag ให้ Operation รู้ว่าเส้นทางนี้ยังไม่มี rate

**สูตร**
- FCL: `freight = rate_per_container × qty_containers`
- LCL: `revenue_ton = max(CBM, weight_kg / 1000)` → `freight = rate × revenue_ton`
- Air: `chargeable_weight = max(actual_kg, volume_cm³ / 6000)` → เลือก weight break → `freight = rate_per_kg × chargeable_weight`
- Local charge: รวมตาม `calc_basis` ของแต่ละรายการ และเก็บเฉพาะฝั่งที่ลูกค้าต้องจ่ายตาม `incoterm_charge_rules`
- Breakeven LCL vs FCL: `(rate_LCL × CBM) + local_LCL` เทียบกับ `rate_FCL + local_FCL` ← **แก้จากสูตรเดิมในเอกสารแล้ว**

**"ดึงอัตราล่าสุด"** = เทียบ `quotation_lines` (snapshot ตอนสร้าง draft) กับค่าปัจจุบันใน `freight_rates` แล้วแสดง delta — กดเองเท่านั้น ไม่ auto refresh

**Approval gate**: ตรวจใน service layer ว่า `status == PendingSaleApproval` และผู้กดมี role `Sale` เท่านั้นถึงเปลี่ยนเป็น `ApprovedAndSent` ได้ — บังคับใน backend + unit test ครอบ

## 4. API (ร่าง)

| Method | Path | ใคร |
|---|---|---|
| POST | `/api/public/quotes/calculate` | Guest — คำนวณอย่างเดียว ไม่บันทึก |
| POST | `/api/public/quotes` | Guest — สร้าง draft เข้าคิว Sale |
| GET | `/api/quotes?status=&page=` | Sale |
| GET | `/api/quotes/{id}` | Sale |
| POST | `/api/quotes/{id}/refresh-rate` | Sale — คืน delta เทียบ snapshot |
| POST | `/api/quotes/{id}/approve` | Sale — final_price + note |
| POST | `/api/quotes/{id}/reject` | Sale |
| GET/POST/PUT/DELETE | `/api/rates`, `/api/local-charges` | Operation |
| POST | `/api/rates/import` | Operation — Excel/CSV |
| CRUD | `/api/master/{ports,carriers,currencies,users}` | Admin |

## 5. Milestones (ปรับจาก project-plan.md)

### M0 — Foundation (สัปดาห์ 0, ~3-5 วัน) ← เพิ่มใหม่
- ยืนยันเวอร์ชัน .NET ที่ Plesk รองรับ + สร้าง MySQL database บน host
- สร้าง solution skeleton + React app + ต่อ `tokens.css` เข้า build
- Deploy "hello world" ขึ้น Plesk จริงให้ผ่านก่อน (กัน surprise เรื่อง hosting bundle / connection string / HTTPS)
- Confirm กับ Operation: mapping local charge ต่อ 11 Incoterms, weight break ของ Air, ตัวอย่างสูตรจริง
- **Exit**: URL จริงเปิดได้ + คุยสูตรกับ Operation จบ

### M1 — Data Foundation (สัปดาห์ 1-2)
Schema + migrations, seed Incoterms/currencies/ports, หน้า Operation จัดการ rate/local charge + audit log + import CSV
**Exit**: Operation เพิ่ม/แก้ rate ผ่านเว็บได้จริง และย้อนดูได้ว่าใครแก้อะไร

### M2 — Quote Engine (สัปดาห์ 3-4)
Quote engine + unit tests ครอบทุกโหมด, Instant Quote Form (guest), FCL vs LCL comparison, refresh rate + delta
**Exit**: เคสทดสอบจาก Operation คำนวณตรงทุกเคส, ตอบใน < 2 วินาที

### M3 — Approval Workflow (สัปดาห์ 5)
Auth + role, quotation list/detail ของ Sale, approve/reject + versioning, email ใบเสนอราคา (PDF), notification
**Exit**: ส่งถึงลูกค้าไม่ได้เลยถ้าไม่ผ่าน approve — พิสูจน์ด้วย integration test ที่ยิง API ตรง

### M4 — Polish + UAT (สัปดาห์ 6-7)
Design token ครบทุกหน้า, responsive + a11y, backup/monitoring บน Plesk, UAT กับ Sale/Operation
**Exit**: ใช้งานจริง 1 สัปดาห์ไม่มี blocking bug

> งาน QA ไม่แยกเป็น milestone — แต่ละ milestone ต้องมี test ของตัวเองก่อนถือว่าจบ

## 6. Decisions ที่ยังค้าง (ต้องเคลียร์ก่อน M2/M3)

| ประเด็น | ทำไมต้องรู้ |
|---|---|
| ส่วนลดเกิน X% ต้องหัวหน้าอนุมัติไหม | กระทบ state machine + role model ใน M3 |
| สกุลฐาน (base currency) ของระบบคือ THB หรือ USD | กระทบทุกการเปรียบเทียบราคาใน M2 |
| Rate หมดอายุ → เตือน หรือบล็อกไม่ให้ออกใบ | กระทบ rate resolution ใน M2 |
| อายุใบเสนอราคา (expires_at) กี่วัน | ตั้งเป็น config ได้ แต่ต้องมีค่าเริ่มต้น |
| ส่งใบเสนอราคาทางอีเมลผ่าน SMTP ตัวไหน | Plesk mail หรือ service ภายนอก — กระทบ M3 |
