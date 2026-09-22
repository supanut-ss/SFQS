# Freito — Smart Freight Quotation System (SFQS) — Project Plan

อ้างอิงจาก [requirements.md](requirements.md) (business/functional requirement) และ [design-system-spec.md](design-system-spec.md) + [style-guide.html](style-guide.html) (UI/design tokens) ที่ทำไว้ก่อนหน้านี้ เอกสารนี้แปลงเป็นแผนงานที่เริ่มโปรเจกต์ได้จริง

## 1. เป้าหมายโปรเจกต์

สร้างระบบให้ลูกค้า/เซลส์กรอกข้อมูลขนส่งแล้วได้**ร่างใบเสนอราคาอัตโนมัติทันที** โดยยังคงมี **Sale ตรวจสอบและอนุมัติก่อนส่งลูกค้าเสมอ** และให้ **Operation อัพเดทค่าระวางทั่วโลกได้ real-time**

**Success metric เบื้องต้น** (ปรับได้ตามข้อมูลจริงของทีม):
- เวลาเฉลี่ยจากลูกค้ากรอกข้อมูล → Sale อนุมัติส่งราคา ลดลงจากเดิม (baseline ต้องวัดจาก process ปัจจุบันก่อน)
- % ใบเสนอราคาที่ใช้ราคาจากระบบตรงกับราคาที่ Sale อนุมัติ (วัด "ความแม่นของ default price")

## 2. ขอบเขต MVP (Phase 1) vs เฟสถัดไป

| อยู่ใน MVP | ไม่อยู่ใน MVP (เฟสถัดไป) |
|---|---|
| Quote engine + Sale approval gate | Shipment booking/tracking แบบเต็มระบบ (real-time integration กับสายเรือ/สายการบิน) |
| Operation rate management (CRUD) | Payment / invoice |
| Incoterms reference data | เอกสารศุลกากร |
| Instant quote form + FCL vs LCL comparison | Auto sync exchange rate จาก external API (MVP ใช้ multi-currency แบบ Admin อัพเดท rate เอง) |
| Quotation status tracking (Draft → Approved & Sent → Confirmed/Expired) | Analytics/BI dashboard เชิงลึก |

## 3. Milestones

### Milestone 1 — Data Foundation (สัปดาห์ 1-2)
- ออกแบบ+สร้าง schema: `FreightRate`, `LocalCharge`, `Quotation`, `Incoterm` (master data ตาม [requirements.md §5](requirements.md))
- Seed ข้อมูล Incoterms 2020 ทั้ง 11 เทอมเป็น master data คงที่
- หน้า Admin สำหรับ Operation จัดการ `FreightRate`/`LocalCharge` (CRUD + audit trail ว่าใครแก้เมื่อไหร่)
- **Exit criteria**: Operation เพิ่ม/แก้อัตราได้จริงผ่านหน้าเว็บ ไม่ต้องแตะ database ตรง

### Milestone 2 — Quote Engine (สัปดาห์ 3-4)
- Instant Quote Form (ตาม [style-guide.html](style-guide.html) หัวข้อ "Instant Quote Form")
- Logic คำนวณราคา: ค้นหาราคาปัจจุบันในระบบ → คำนวณ Freight + Local Charge ตามโหมด (FCL/LCL/Air) ตาม [requirements.md §4](requirements.md)
- FCL vs LCL Breakeven comparison แสดงผลคู่กัน
- ปุ่ม "ดึงอัตราล่าสุด" (optional) + แสดง delta เทียบราคาในระบบ
- **Exit criteria**: กรอกข้อมูลครบแล้วได้ราคาร่างถูกต้องตามสูตร ภายใน < 2 วินาที

### Milestone 3 — Sale Approval Workflow (สัปดาห์ 5)
- สถานะ Quotation ครบวงจร: `Draft` → `Pending Sale Approval` → `Approved & Sent` → `Confirmed`/`Rejected`/`Expired`
- หน้า/ส่วน Approval: อนุมัติ/ตีกลับ/แก้ราคาสุดท้าย/บันทึกหมายเหตุ (`approved_by`, `approved_at`)
- Notification ให้ Sale รู้เมื่อมีใบเสนอราคาใหม่รอตรวจ
- **Exit criteria**: ใบเสนอราคาส่งถึงลูกค้าไม่ได้เลยถ้าไม่ผ่านการอนุมัติจาก Sale (บังคับใน backend ไม่ใช่แค่ UI)

### Milestone 4 — Polish + UAT (สัปดาห์ 6)
- ใช้ design tokens จาก [tokens.json](tokens.json)/[tokens.css](tokens.css) ให้ครบทุกหน้า (สี/ฟอนต์/spacing ตรงกับ style guide)
- Responsive + accessibility check (โดยเฉพาะฟอร์มที่มีฟิลด์เยอะ)
- User Acceptance Testing กับฝ่ายขายและ Operation จริง
- **Exit criteria**: ทีมขายและ Operation ใช้งานจริงได้ 1 สัปดาห์โดยไม่มี blocking bug

### Milestone 5 — Shipment Tracking (เฟสถัดไป, หลัง MVP)
- Shipment Tracking Card ผูกกับข้อมูลจริงจาก OP (manual update ก่อน, ค่อยพิจารณา integration กับสายเรือ/สายการบินทีหลัง)
- สถานะ In Transit → Delayed/Arrived → Pending D/O

## 4. ทีมที่ต้องใช้ (บทบาท ไม่ใช่จำนวนคน)

- **Backend**: schema, quote calculation logic, approval workflow, audit trail
- **Frontend**: ประกอบ component จาก design system ที่มีอยู่แล้ว (tokens + style guide เป็น reference ตรง ลดเวลา design)
- **Operation (business)**: ยืนยันสูตรคำนวณราคาจริง (LCL revenue-ton, weight break ของ Air) และ local charge ต่อท่า/สนามบินจริงก่อนเริ่ม Milestone 2
- **Sale (business)**: ให้ feedback ระหว่าง Milestone 3 ว่า approval flow ตรงกับการทำงานจริงหรือไม่

## 5. ความเสี่ยง/สิ่งที่ต้องตัดสินใจก่อนเริ่ม

| ประเด็น | ต้องตัดสินใจ |
|---|---|
| Currency | เริ่มด้วยสกุลเดียว (เช่น USD) หรือ multi-currency ตั้งแต่ MVP? กระทบ schema `FreightRate.currency` |
| Rate ผิดพลาด/ล้าสมัย | ถ้า Operation ลืมอัพเดทอัตรา ระบบควรเตือนหรือบล็อกการสร้างใบเสนอราคาหรือไม่ |
| สิทธิ์อนุมัติ | Sale ทุกคนอนุมัติได้ หรือมีลำดับชั้น (เช่น ส่วนลดเกิน X% ต้องหัวหน้าอนุมัติ) |
| Local Charge ตาม Incoterm | mapping "ใครจ่ายฝั่งไหน" ต่อ 11 Incoterms ต้อง confirm กับ Operation ให้ครบก่อนเขียน logic |

## 6. ไฟล์อ้างอิงที่มีอยู่แล้ว (ใช้เริ่มงานได้ทันที)

- [requirements.md](requirements.md) — business/functional requirement + data model
- [design-system-spec.md](design-system-spec.md) — เหตุผลการออกแบบ + component spec
- [tokens.json](tokens.json) / [tokens.css](tokens.css) — design tokens พร้อมใช้กับโค้ดจริง
- [style-guide.html](style-guide.html) — ตัวอย่าง component ที่ frontend ใช้อ้างอิงตรงได้เลย (badge, incoterms table, FCL vs LCL card, instant quote form + approval, shipment tracking card)
