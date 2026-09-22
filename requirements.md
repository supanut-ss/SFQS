# Freito — Smart Freight Quotation System (SFQS) — Requirements

## 1. เป้าหมายระบบ (Business Goal)

ให้ลูกค้า/เซลส์กรอกข้อมูลขนส่งให้ครบ แล้วระบบ**คำนวณร่างใบเสนอราคาอัตโนมัติ**ทันทีจากราคาที่มีอยู่ในระบบ เพื่อ:
- ลดเวลาที่เซลส์ต้องคำนวณราคาเอง (ระบบร่างให้ก่อน ไม่ต้องเริ่มจากศูนย์)
- ลดขั้นตอนที่ลูกค้าต้องติดต่อโดยตรงเพื่อขอราคาเบื้องต้น
- ให้ Operation อัพเดทค่าระวางเรือ/ค่าระวางทั่วโลกได้ตลอดเวลา โดยราคาที่ระบบใช้คำนวณคือ **ราคาปัจจุบันที่บันทึกไว้ในระบบ** (ไม่ใช่การดึงสดทุกครั้ง)
- **ใบเสนอราคาทุกใบต้องผ่านการตรวจสอบและอนุมัติจากฝ่ายขาย (Sale) ก่อนส่งให้ลูกค้าเสมอ** — ระบบช่วยร่างและคำนวณให้เร็วขึ้น แต่ไม่ส่งราคาให้ลูกค้าโดยอัตโนมัติโดยไม่มีคนตรวจ

## 2. ผู้ใช้งาน (User Roles)

| Role | หน้าที่หลัก |
|---|---|
| **ลูกค้า (Customer)** | กรอกข้อมูลขนส่งเพื่อขอใบเสนอราคา ดูราคาที่คำนวณอัตโนมัติ |
| **ฝ่ายขาย (Sale)** | กรอกข้อมูลแทนลูกค้า/ติดตามใบเสนอราคา, ปรับราคา/ส่วนลดก่อนส่งลูกค้า |
| **ฝ่าย Operation** | อัพเดทค่าระวาง (Freight Rate) และ Local Charge รายเส้นทาง/รายสายเรือ/สายการบินทั่วโลก |
| **Admin** | จัดการ Master Data: เมือง/ท่าเรือ/สนามบิน, สายเรือ/สายการบิน, Incoterms, สิทธิ์ผู้ใช้ |

## 3. ข้อมูลนำเข้า (Input Data) ที่ต้องเก็บต่อ 1 คำขอราคา

| ฟิลด์ | รายละเอียด | บังคับ |
|---|---|---|
| Origin | ต้นทาง (เมือง/ท่าเรือ/สนามบิน) | ✅ |
| Destination | ปลายทาง | ✅ |
| Direction | นำเข้า (Import) / ส่งออก (Export) | ✅ |
| Mode | FCL / LCL / Air | ✅ |
| Container Size (ถ้า FCL) | 20' / 40' / 40'HQ | เมื่อ Mode = FCL |
| ปริมาตร CBM (ถ้า LCL) | ตัวเลข CBM | เมื่อ Mode = LCL |
| น้ำหนัก KG (ถ้า Air) | ตัวเลข KG (+ chargeable weight ถ้าต่างจาก actual) | เมื่อ Mode = Air |
| ประเภทสินค้า | ชื่อ/หมวดสินค้า (ใช้เช็คสินค้าอันตราย/ห้ามส่ง) | ✅ |
| จำนวน | จำนวนหน่วยสินค้า | ✅ |
| Incoterm | 1 ใน 11 เทอม (EXW, FCA, FAS, FOB, CFR, CIF, CPT, CIP, DPU, DAP, DDP) | ✅ |
| Ready Date | วันที่สินค้าพร้อมส่ง | ✅ |
| ข้อมูลติดต่อ | ชื่อ/บริษัท/อีเมล/เบอร์โทร (สำหรับส่งใบเสนอราคา) | ✅ |

## 4. Logic การคำนวณราคา (Quote Calculation)

1. ระบบค้นหา**ราคาปัจจุบันที่บันทึกไว้ในระบบ** (current stored rate) ที่ตรงกับ **Origin–Destination–Mode–Direction** เป็นค่าเริ่มต้นในการคำนวณ — ไม่ดึงอัตราสดจากภายนอกทุกครั้งโดยอัตโนมัติ
2. มีปุ่ม **"ดึงอัตราล่าสุด" (Refresh Rate — optional)** ให้ผู้ใช้กดเทียบราคาปัจจุบันในระบบกับอัตราล่าสุดที่ Operation เพิ่งอัพเดท ถ้าต่างกันระบบแสดง **ส่วนต่าง (delta)** ให้เห็นชัด (เช่น ↑/↓ พร้อมจำนวนเงิน) เพื่อให้ Sale ตัดสินใจว่าจะใช้ราคาไหน
3. คำนวณ **Freight Cost** จากราคาที่เลือก (ราคาในระบบ หรือราคาล่าสุดที่กดดึงมา):
   - FCL → ราคาต่อตู้ (flat) ตามขนาดตู้
   - LCL → ราคา/CBM หรือ /ตัน (น้ำหนัก) **เลือกค่าที่สูงกว่า** (revenue ton)
   - Air → ราคา/KG ตาม weight break (เช่น <45kg, 45-100kg, 100-300kg, 300kg+)
4. บวก **Local Charge** (ปลายทาง/ต้นทาง ตาม Incoterm ว่าใครจ่ายฝั่งไหน)
5. ถ้า Mode = LCL: ระบบคำนวณเทียบ **Breakeven กับ FCL** อัตโนมัติ ((Freight_LCL × CBM) + Local_LCL เทียบกับ Freight_FCL + Local_FCL) และ**แนะนำโหมดที่ประหยัดกว่า**ให้เห็นควบคู่กัน (ดู FCL vs LCL Comparison Card ใน [style-guide.html](style-guide.html))
6. ระบบสร้าง**ร่างใบเสนอราคา (Draft Quotation)** พร้อมเลขที่อ้างอิง + วันหมดอายุราคา แล้วส่งเข้าคิวรอ **Sale ตรวจสอบและอนุมัติ** — ยังไม่ส่งให้ลูกค้าในขั้นนี้ (ดูสถานะในข้อ 6)

## 5. Data Model หลัก (สรุปเป็นตาราง)

- **FreightRate**: origin, destination, mode, container_size/weight_break, direction, price, currency, valid_from, valid_to, carrier
- **LocalCharge**: port/airport, direction, mode, charge_type, amount
- **Quotation**: customer_info, origin, destination, direction, mode, cargo_type, qty, incoterm, ready_date, rate_source (`system` / `refreshed`), freight_cost, local_charge, total, status, created_by, **approved_by**, **approved_at**, sent_at, expires_at
- **Incoterm** (master, 11 รายการคงที่): code, name, risk_transfer_point, who_pays_freight

## 6. สถานะใบเสนอราคา (Quotation Status)

`Draft` (ระบบร่างให้อัตโนมัติ) → `Pending Sale Approval` (รอ Sale ตรวจสอบ/แก้ไข/อนุมัติ — **บังคับทุกใบ**) → `Approved & Sent` (Sale อนุมัติแล้ว ส่งให้ลูกค้า) → `Confirmed` (ลูกค้าตอบรับ) / `Rejected` (Sale ตีกลับให้แก้ หรือลูกค้าปฏิเสธ) / `Expired` (เกินวันหมดอายุ)

ใบเสนอราคาจะ**ข้ามสถานะ `Pending Sale Approval` ไม่ได้** — เป็น gate บังคับก่อนถึง `Approved & Sent` เสมอ ไม่ว่าราคาจะคำนวณจากอัตราในระบบหรืออัตราที่เพิ่งดึงมาล่าสุดก็ตาม ใช้ badge สี success/warning/destructive/info ตามที่กำหนดใน design system (เช่น `Pending Sale Approval` = warning/amber)

## 7. Non-functional requirements

- **ความสดของราคา**: ค่าระวางต้องอัพเดทได้แบบ real-time โดย Operation โดยไม่ต้อง deploy ใหม่ (เช่นผ่านหน้า admin หรือ import Excel/CSV รายวัน)
- **ความแม่นยำของตัวเลข**: ใช้ทศนิยมที่ถูกต้องสำหรับ currency/CBM/KG ป้องกัน rounding error สะสม
- **Multi-currency**: รองรับหลายสกุลเงิน (ค่าระวางจากต่างประเทศ) พร้อม exchange rate ที่อัพเดทได้
- **Audit trail**: บันทึกว่าใครแก้อัตราค่าระวางเมื่อไหร่ (Operation อัพเดทบ่อย ต้องตรวจสอบย้อนหลังได้)
- **Performance**: การคำนวณใบเสนอราคาต้องเป็น real-time (< 1-2 วินาที) เพราะเป็นจุดขายหลักของระบบ

## 8. ขอบเขตที่ยังไม่รวม (Out of scope เบื้องต้น)

- ระบบ booking/tracking การขนส่งจริง (เป็นเฟสถัดไปหลังจากลูกค้ายืนยันใบเสนอราคา)
- การชำระเงิน/invoice
- การจัดการเอกสารศุลกากร

---
เอกสารนี้อิงจากข้อมูล business requirement และตัวอย่างการคำนวณ FCL vs LCL ที่ให้มา ใช้คู่กับ [design-system-spec.md](design-system-spec.md) และ [style-guide.html](style-guide.html) ที่ส่งไปก่อนหน้านี้
