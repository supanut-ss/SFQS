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
- `incoterms` — 11 เทอมคงที่ + `incoterm_charge_rules` (incoterm, charge_side `Origin`/`Destination`, payer `Seller`/`Buyer`)
  - **✅ ยืนยันแล้วกับ Operation**: ใบเสนอราคา 1 ใบออกให้ลูกค้าฝั่งเดียวเสมอ (ตาม `direction` Import/Export) และลูกค้าฝั่งนั้นจ่าย 100% ของทุกรายการในใบ — ไม่ใช่แบ่งจ่ายข้ามฝั่งในเอกสารเดียว ดังนั้น logic คือ: ถ้า `direction = Import` → ลูกค้าคือผู้ซื้อ ระบบรวมเฉพาะ local charge ฝั่งที่ `incoterm_charge_rules.payer = Buyer`; ถ้า `direction = Export` → ลูกค้าคือผู้ขาย ระบบรวมเฉพาะฝั่งที่ `payer = Seller` — ตรวจตรงกับตัวอย่างจริงแล้วทั้ง EXW (ผู้ซื้อรับทั้งสองฝั่ง), DDP (ผู้ขายรับทั้งสองฝั่ง), CIF (ผู้ขายรับแค่ต้นทาง ไม่มีปลายทางในใบเลย) — ตารางมาตรฐานใช้ได้จริง ไม่ต้องมี override เป็น freeform ต่อดีล
- `cargo_types` — name, is_dangerous, is_prohibited
- `users` — email, password_hash, role (`Sale`/`Operation`/`Admin`), is_active, created_at — **ไม่เคยระบุไว้ก่อนหน้านี้แม้จะอ้างถึงใน API §4 (`/api/master/users`) และ JWT auth ใน §1** — ต้องตัดสินใจก่อน T8 ว่าจะใช้ ASP.NET Core Identity เต็มรูปแบบ (สร้างตาราง Identity เอง, รองรับ password reset ในตัว) หรือทำตารางเรียบง่ายเองแล้ว hash รหัสผ่านเอง (เร็วกว่าสำหรับแค่ 3 role ภายใน ไม่มี self-signup)

**Rate**
- `freight_rates` — origin_port_id, destination_port_id, mode, direction, container_size (FCL), weight_break_min/max (Air), carrier_id, **price_min**, **price_max** (เท่ากันถ้าเป็นราคาตายตัวอย่าง FCL), currency, valid_from, valid_to, is_active
  - **แก้ตามหลักฐานจริง**: เดิมมีแค่ `price` เดียว แต่ Air rate จริงให้เป็นช่วง ($2.95–$3.10/kg) ไม่ใช่ตัวเลขเดียว — เปลี่ยนเป็น min/max แบบเดียวกับ `local_charges` (ดูด้านล่าง)
  - **weight break ของ Air ต้องมี row สำหรับ "minimum chargeable weight"** ด้วย เช่น `weight_break_min=0, weight_break_max=50` แทนช่วง "ต่ำกว่า 50kg คิดเป็น 50kg" และ `weight_break_min=50, weight_break_max=500` เป็นช่วงที่มี rate จริง โดยนับค่าขอบล่างรวมแต่ไม่รวมขอบบน ยกเว้น 500kg ที่รวมอยู่ในช่วงสุดท้าย — **เกิน 500kg ไม่ seed row ไว้เลย** เพื่อให้ rate resolution คืน `NoRateFound` แล้วบังคับ Sale กรอกเอง (`rate_source = Manual`) ตามที่ยืนยันกับ Operation แล้ว (ดู §3)
- `local_charges` — port_id, direction, mode, charge_type, **calc_basis** (`PerShipment` / `PerContainer` / `PerRevenueTon` / `PerCBM` / `PerKG` / `NotQuotable`), **amount_min**, **amount_max** (เท่ากันถ้าเป็นราคาตายตัว), currency, charge_side (`Origin`/`Destination`)
  - **แก้จากตัวเลขจริง** (ดู [operation-worksheet.md](operation-worksheet.md) §4): local charge ของจริงมักมี **ราคาเป็นช่วง** ไม่ใช่ตัวเลขตายตัว (เช่น "TRANSPORT: 5,500-6,500 Baht", "PICK UP: THB 5,800-10,000") จึงต้องมี min/max ไม่ใช่ `amount` เดียว
  - **ทั้ง `PerRevenueTon` และ `PerCBM` มีใช้จริง คนละเส้นทาง/คนละรายการ** — ใบ Ningbo (CFS/THC/STS/FAC) คิดต่อ **revenue ton** (หน่วย "/RT") แต่ใบ Jakarta (THC/CFS) คิดต่อ **CBM ดิบ** ตรงๆ (หน่วย "THB/CBM") จึงต้องเก็บ**ทั้งสองแบบแยกกัน** ไม่ใช่รวมเป็นแบบเดียว ต้องระบุ `calc_basis` ต่อรายการเวลา seed ข้อมูลจริง ไม่ใช่ตั้งสมมติฐานตายตัวว่าทุก local charge ของ LCL ใช้ revenue ton หมด
  - บาง local charge (เช่น air surcharge อย่าง SAF, X-ray, THC) คิดจาก **chargeable weight คูณเรทต่อ kg แต่มีขั้นต่ำ** (เช่น "Min: 65.00 €") —ต้องเพิ่ม **`minimum_charge`** แยกจาก `amount_min/max` (คนละความหมาย: `amount_min/max` = ช่วงราคาที่ยังไม่ได้เลือก, `minimum_charge` = พื้นราคาต่ำสุดเมื่อคำนวณจาก per-unit แล้วต่ำกว่านี้)
  - **✅ ตอบแล้ว — DDP charge ที่คำนวณล่วงหน้าไม่ได้ (Customs Duty % ของมูลค่าสินค้า, at-cost, ตามเวลาใช้จริงเช่น detention/storage) จาก LCB→Memphis: "ไม่ต้องเข้ายอด"** — ใช้ `calc_basis = NotQuotable` ให้เก็บเป็น**บรรทัดข้อมูล/หมายเหตุเท่านั้น** ไม่รวมเข้า `subtotal`/`total` ของใบเสนอราคา (แสดงเป็น "อาจมีค่าใช้จ่ายเพิ่มเติมตามจริง เช่น ภาษีศุลกากร/ค่าปรับตามเวลา — แจ้งราคาสุดท้ายอีกครั้งหลังยืนยันออเดอร์")
    - **ข้อดีของคำตอบนี้**: ไม่ต้องเพิ่ม `cargo_value` เข้า `Quotation` เลย เพราะไม่มีการคำนวณ % จริงในระบบ — แค่โชว์ข้อความ ทำให้ M2/T5 เบาลง ไม่ต้องรองรับ percent-of-value หรือ usage-based calculation จริงๆ

**Quotation** (เพิ่มจาก requirements)
- `quotations` — `quote_no` (running, unique), customer_name/company/email/phone, origin/destination, direction, mode, cargo_type, qty, container_size/cbm/weight, incoterm, ready_date, **quote_currency**, **fx_rate_used**, `rate_source` (`System`/`Refreshed`/`Manual`), freight_cost, local_charge_total, subtotal, discount_amount, **final_price**, status, version, created_by (nullable = guest), approved_by, approved_at, sent_at, expires_at
- `quotation_lines` — **snapshot ของทุกบรรทัดราคา** (description, basis, unit_price, qty, amount, currency, source_rate_id) → rate เปลี่ยนทีหลัง ใบเก่าไม่เปลี่ยนตาม
- `quotation_status_history` — from_status, to_status, actor, note, at
- `audit_logs` — entity, entity_id, action, changed_by, changed_at, before/after (JSON) ใช้กับ rate/charge ทั้งหมด

**หลักเลข**: เงินใช้ `decimal(18,4)`, CBM/KG ใช้ `decimal(12,3)`, ปัดเศษเฉพาะตอนแสดงผลและตอนบันทึก final_price เท่านั้น

## 3. Quote engine — กฎที่ต้องชัดก่อนเขียน

**✅ VAT: ไม่ต้องคิดในระบบ** — ยืนยันจาก Operation ว่าราคาทุกใบเสนอราคาไม่มีการคิด VAT เลย (`subtotal` = `total` = `final_price` ไม่มีชั้นภาษีเพิ่ม) — noted ไว้กันสับสนตอนเขียน T5

**Rate resolution** (เรียงตามลำดับ):
1. กรอง `freight_rates` ที่ตรง origin + destination + mode + direction + `is_active` + `ready_date` อยู่ในช่วง valid_from..valid_to
2. กรองตามขนาดตู้ (FCL) หรือ weight break ที่ครอบ chargeable weight (Air)
3. เจอหลายรายการ → normalize เป็นสกุลฐานด้วย exchange rate ล่าสุด แล้ว **เลือกราคาต่ำสุด** และคืนรายการที่เหลือให้ Sale เลือกเปลี่ยน carrier ได้
4. ไม่เจอเลย → ไม่บล็อก แต่ตอบ `NoRateFound` + ให้ Sale กรอกราคาเอง (`rate_source = Manual`) และ flag ให้ Operation รู้ว่าเส้นทางนี้ยังไม่มี rate
   - **สำหรับ Air**: path นี้ไม่ใช่ edge case แต่เป็น**เส้นทางหลักที่คาดว่าจะเกิดบ่อย** — ยืนยันจาก Operation ว่า chargeable weight เกิน 500 kg ไม่มี rate table ตายตัว ต้องให้ Sale กรอกเองเสมอ (ดู §3 ด้านล่าง)

**สูตร**
- FCL: `freight = rate_per_container × qty_containers`
- LCL: `revenue_ton = max(CBM, weight_kg / 1000)` → `freight = rate × revenue_ton`
- Air: ใช้ `max(actual_kg, volume_cm³ / 6000)` เลือก weight break ก่อน floor น้ำหนัก จากนั้น `billable_weight = max(weight_for_rate_lookup, 50 kg)` → `freight = rate_per_kg × billable_weight`
  - **ตัวหาร 6000 ยืนยันแล้วด้วยตัวเลขจริง** — ใบเสนอราคา Air Barcelona→BKK จริง (ดู [operation-worksheet.md](operation-worksheet.md) §4) คำนวณ volumetric weight ตรงกับสูตรนี้เป๊ะ (53.5 cbm → 8,916.4 kg, freight 1.85×8,916.4 = 16,495.34 ตรงทุกบาท)
  - **weight break ยืนยันแล้ว — ไม่ใช่ตารางหลายชั้นแบบที่สมมติไว้เดิม (<45/45-100/100-300/300+)**: มี **minimum chargeable weight = 50 kg** (ต่ำกว่านี้คิดเป็น 50 kg) + เรทช่วง 50–500 kg ตาม rate table ปกติ โดยช่วงรวมขอบล่างและไม่รวมขอบบน ยกเว้น 500 kg ซึ่งรวมอยู่ในช่วงสุดท้าย (เก็บเป็นช่วงราคา `price_min`/`price_max` เพราะของจริงให้เป็นช่วงเช่น $2.95–$3.10/kg ไม่ใช่ราคาเดียว) — **เกิน 500 kg ไม่มี rate table เลย ให้ Sale กรอกราคาเอง (`rate_source = Manual`) ทุกครั้ง** ตามที่ยืนยันแล้วข้างบน
- Local charge: รวมตาม `calc_basis` ของแต่ละรายการ และเก็บเฉพาะฝั่งที่ลูกค้าต้องจ่ายตาม `incoterm_charge_rules` — **ยกเว้นรายการที่ `calc_basis = NotQuotable`** (เช่น DDP customs duty/at-cost/detention) **ไม่รวมเข้า subtotal เลย** แสดงแยกเป็นหมายเหตุใต้ยอดรวมแทน
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
- ~~สร้าง solution skeleton + React app + ต่อ `tokens.css` เข้า build~~ **เสร็จแล้ว** — ดู §6
- Deploy "hello world" ขึ้น Plesk จริงให้ผ่านก่อน (กัน surprise เรื่อง hosting bundle / connection string / HTTPS)
- Confirm กับ Operation: mapping local charge ต่อ 11 Incoterms, weight break ของ Air, ตัวอย่างสูตรจริง
- **Exit**: URL จริงเปิดได้ + คุยสูตรกับ Operation จบ — **ยังไม่จบ** เพราะ 2 ข้อบนต้องใช้สิทธิ์ Plesk/คุยกับทีม Operation ซึ่งผมทำแทนไม่ได้

### M1 — Data Foundation (สัปดาห์ 1-2)
Schema + migrations, seed Incoterms/**incoterm_charge_rules**/currencies/ports, หน้า Operation จัดการ rate/local charge + audit log + import CSV
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

## 6. M0 — สิ่งที่ทำจริงแล้ว (ตรวจแล้ว)

**Backend** (`src/`, .NET 8 — SDK ใหม่สุดที่มีคือ .NET 10 แต่เลือก 8 เพราะเป็น LTS และมีโอกาสสูงสุดที่ Plesk hosting bundle จะรองรับ — **ต้องยืนยันกับ Plesk จริงใน T0**):
- Solution `Freito.slnx` + 4 โปรเจกต์ตามผังใน §1 (`Freito.Api/Domain/Infrastructure/Tests`) พร้อม project reference ครบ
- EF Core 8 + Pomelo MySQL provider ผูกกับ `FreitoDbContext` (มี entity เดียวคือ `Incoterm` เป็น proof-of-pipeline — schema เต็มเป็นงาน M1)
- ตั้งใจใช้ **pinned `MySqlServerVersion`** แทน `ServerVersion.AutoDetect` เพราะ AutoDetect เชื่อมต่อ DB แบบ synchronous ตอน startup — ถ้า Plesk MySQL หลุดชั่วคราวตอนบูตแอปจะ crash-loop ทั้งแอป ปักหมุดเวอร์ชันแทนเพื่อให้แอปเริ่มได้แม้ DB ยังต่อไม่ได้
- `GET /api/health` (liveness, ไม่ต้องมี DB) และ `GET /api/health/db` (readiness, คืน 503 ถ้าต่อ DB ไม่ได้ — ไม่ throw) — **ทดสอบจริงแล้ว**: รันแอปโดยไม่มี MySQL อยู่เลย ยัง boot ได้ `/api/health` ตอบ 200, `/api/health/db` ตอบ 503 ตามที่ออกแบบ
- Unit test 2 เคสผ่าน EF Core InMemory provider — `dotnet test` **ผ่านจริง 2/2**
- MSBuild target `BuildWebApp` ใน `Freito.Api.csproj`: `dotnet publish` จะ build React app และ copy เข้า `wwwroot` อัตโนมัติ ให้ deploy ขึ้น Plesk เป็น artifact เดียว

**Frontend** (`web/`, React 19 + Vite + TypeScript + Tailwind):
- `npm run tokens:sync` (auto-run ก่อน dev/build) copy `../tokens.css` เข้า `web/src/styles/` — root `tokens.json`/`tokens.css` ยังเป็น source of truth เดียว ไม่มีการ fork
- `tailwind.config.js` map สีทุกตัวเป็น `var(--...)` ชี้กลับไปที่ token จริง ไม่มีสีใหม่ถูกสร้างขึ้น (ตรวจแล้วด้วยการอ่านไฟล์)
- หน้า placeholder เดียว: โลโก้ + เช็คสถานะ API — **ตรวจผ่านเบราว์เซอร์จริงแล้ว**: build ผ่าน, dark mode ตรงกับ `style-guide.html` (ผ่าน `prefers-color-scheme` เดียวกัน), เรียก `/api/health` สำเร็จ

**Single-domain serving** — ตรวจจริงโดยรัน `dotnet run` แล้ว build React ใส่ `wwwroot` เอง (ก่อนที่ publish target จะทำอัตโนมัติ): `GET http://localhost:5025/` คืน 200 (SPA), `GET http://localhost:5025/api/health` คืน 200 (API) จาก origin เดียวกัน — ตรงกับสถาปัตยกรรมใน §1 เป๊ะ

**ยังไม่ได้ทำ / บล็อกอยู่ที่ผู้ใช้**:
- ยืนยันเวอร์ชัน .NET ที่ Plesk รองรับจริง (ผมสมมติ .NET 8 LTS ไว้ก่อน — ถ้า Plesk รองรับแค่เก่ากว่านี้ต้อง downgrade)
- สร้าง MySQL database บน Plesk host + connection string จริง
- Deploy ขึ้น Plesk จริงเพื่อปิด exit criteria ของ M0
- คุยสูตรกับทีม Operation (T1 ในตาราง work-plan.md)

## 7. Decisions ที่เคยค้าง

เกือบทั้งหมดตอบครบแล้ว (ประวัติด้านล่าง) — **มี 1 ข้อใหม่ที่เจอตอนเขียน T5** ยังไม่ได้ถาม Operation:

| ประเด็น | ทำไมต้องรู้ |
|---|---|
| **[ใหม่]** เมื่อ rate/local charge เก็บเป็นช่วงราคา (`PriceMin`/`PriceMax` หรือ `AmountMin`/`AmountMax`) instant quote ควรใช้ปลายไหนคำนวณ? ตอนนี้โค้ด (`RateResolver`, `LocalChargeCalculator`) ใช้ **`Max` ไปก่อนเป็นสมมติฐาน** (กันการเสนอราคาต่ำเกินจริง) — มี comment `ASSUMPTION` กำกับไว้ในโค้ดทั้งสองจุด | กระทบราคาที่ลูกค้าเห็นตรงๆ ทุกเคสที่เจอ range (Air ทุกใบ, local charge ที่เป็นช่วงหลายรายการ) — ควรถาม Operation ยืนยันก่อนขึ้น production จริง |

รายละเอียด/หลักฐานของข้อที่ตอบไปแล้วอยู่ที่ [operation-worksheet.md](operation-worksheet.md) (ใช้ปิด **T1** ใน [work-plan.md](work-plan.md) แล้ว) — ประวัติด้านล่างนี้:

| ประเด็น | ทำไมต้องรู้ |
|---|---|
| ~~ส่วนลดเกิน X% ต้องหัวหน้าอนุมัติไหม~~ | ✅ **ตอบแล้ว**: ไม่มี ระดับชั้นอนุมัติเดียว |
| ~~สกุลฐาน (base currency) ของระบบคือ THB หรือ USD~~ | ✅ **ตอบแล้ว**: **USD** |
| ~~Rate หมดอายุ → เตือน หรือบล็อกไม่ให้ออกใบ~~ | ✅ **ตอบแล้ว**: **เตือนให้อัพเดท** ไม่บล็อก |
| ~~อายุใบเสนอราคา (expires_at) กี่วัน~~ | ✅ **ตอบแล้ว**: **30 วัน** |
| ~~ส่งใบเสนอราคาทางอีเมลผ่าน SMTP ตัวไหน~~ | ✅ **ตอบแล้ว**: Manual ผ่าน Outlook — Sale โหลด PDF ไปส่งเอง ไม่ต้องมี SMTP integration ในระบบ (ลดงาน M3) |
| ~~Air weight break เป็นตารางตายตัว หรือมีแค่ minimum chargeable weight~~ | ✅ **ตอบแล้ว**: minimum 50kg + rate table ถึง 500kg, เกินนั้น**กรอกเองได้ (manual)** — ดู §3 |
| ~~DDP quote มีค่าใช้จ่าย % ของมูลค่าสินค้า / at-cost / ตามเวลาใช้จริง — รวมเข้ายอดไหม~~ | ✅ **ตอบแล้ว**: **ไม่ต้องเข้ายอด** — เก็บเป็นหมายเหตุ (`calc_basis = NotQuotable`) เท่านั้น |
