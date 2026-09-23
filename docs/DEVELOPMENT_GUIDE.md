# SOLTIUS Integration Hub API — Development Guide & Technical Specification

Dokumen ini merupakan panduan resmi pengembangan, arsitektur, dan integrasi untuk **SOLTIUS - Web API Add-On (Integration Hub)** dalam ekosistem pengadaan barang armada kapal **IBT ONE Web App**.

---

## 1. Arsitektur Integrasi & Domain Rules

### 1.1 Pola Integrasi: Staging Database Gateway
Integration Hub bertindak sebagai middleware penengah antara **IBT ONE Web App** dan **SAP Business One (10.0 FP / SQL Server / HANA)**:
1. **Transaksi Pengadaan (Push ke SAP)**:
   - Web App mengirim payload JSON transaksi ke endpoint staging Integration Hub (`/api/PurchaseOrder`, `/api/GoodsReceiptPO`, `/api/StockTransfer`).
   - Hub memvalidasi dan menyimpan transaksi ke **User-Defined Table (UDT) Staging SAP**:
     - `@SOL_PO_H` & `@SOL_PO_D` (Purchase Order)
     - `@SOL_GRPO_H` & `@SOL_GRPO_D` (Goods Receipt PO)
     - `@SOL_IT_H` & `@SOL_IT_D` (Inventory Transfer / Packing List)
   - SAP B1 Add-On Background Worker (DI-API) membaca tabel staging dan membuat dokumen SAP resmi secara transaksional (`U_SOL_ProcessStatus`: 0 = Staged, 1 = Success, 2 = Error).
2. **Master Data (Tarik dari SAP)**:
   - Hub menyediakan endpoint REST cepat via **Dapper Direct DB Connection** (dual support: SQL Server & SAP HANA).
   - Memiliki **Fallback Staging Mode** otomatis: jika database staging belum tersambung atau tabel belum didaftarkan, endpoint tetap merespons status `200 OK` dengan mock payload yang valid (`isFallback: true`), menjamin Web App tidak mengalami kegagalan 500 saat lingkungan lokal/offline.

---

### 1.2 Ketetapan Domain Web App vs SAP B1 (Authoritative Scope)

Berdasarkan keselarasan proses bisnis pengadaan suku cadang kapal IBT ONE:
1. **Requisition Letter (RL) / Purchase Request**:
   - RL murni proses internal persetujuan operasional armada kapal di Web App.
   - **RL TIDAK ADA di SAP B1**, sehingga **tidak membutuhkan** tabel staging atau endpoint di Integration Hub.
2. **Goods Receipt PO (GRPO) vs Issue/Receipt Mandiri**:
   - Alur penerimaan suku cadang kapal dari vendor di pelabuhan/gudang adalah **GRPO (Goods Receipt PO)** berbasis referensi PO.
   - **TIDAK ADA Goods Issue mandiri atau Goods Receipt mandiri** di alur pengadaan suku cadang web app. Penyesuaian antar gudang kapal/dermaga menggunakan **Stock Transfer (IT)**.
3. **A/P Down Payment & A/P Invoice**:
   - Pembayaran uang muka dan faktur A/P **TIDAK DIKELOLA di Web App**. Seluruh pencatatan keuangan dan penagihan vendor dilakukan langsung oleh divisi Finance & Accounting di SAP B1 Client.
4. **4 Modul yang DITAKEOUT Total dari Web App (Jangan Di-Maintain)**:
   - **Withholding Tax Codes** (`/api/MasterData/withholding-taxes`) $\to$ Dihapus total dari alur web.
   - **Chart of Accounts** (`/api/MasterData/chart-of-accounts`) $\to$ Transaksi barang tidak lagi memilih akun COA. Untuk Freight, kode akun dibaca langsung secara mentah (*raw string*) dari `OEXD`.
   - **Dimensions** (`/api/MasterData/dimensions`) $\to$ Dimensi 1–5 sudah melekat langsung pada atribut master Cost Center.
   - **Departments** (`/api/MasterData/departments`) $\to$ Departemen di web app kini ditarik langsung dari master Cost Center / Profit Center Dimensi 1 (`OPRC` dengan `DimCode = 1`).
5. **Jalur Push Item Groups Katalog (P7) Tetap Dipertahankan**:
   - Pendaftaran running number kelompok barang suku cadang kapal (`ItmsGrpCod`) via `ItemGroups` tetap aktif untuk sinkronisasi katalog.

---

## 2. Peta Endpoint Integration Hub (MasterDataController)

Semua endpoint master data mengembalikan struktur respons dual key (`data` dan `value`) agar kompatibel dengan OData parser Laravel:
```json
{
  "success": true,
  "count": 2,
  "data": [ ... ],
  "value": [ ... ]
}
```

### 2.1 Ringkasan Seluruh Endpoint Master Data

| Modul | Endpoint | Tabel SAP B1 | Parameter Query | Deskripsi |
| :--- | :--- | :---: | :--- | :--- |
| **Item Hierarchy** | `GET /api/MasterData/item-hierarchy` | `@SOL_ITEMHIER` | `search`, `top=100`, `skip=0` | Struktur pohon kelompok barang suku cadang kapal |
| **Items (Sparepart)** | `GET /api/MasterData/items` | `OITM` | `search`, `since`, `top=100`, `skip=0` | Master barang katalog dan suku cadang kapal |
| **Business Partners** | `GET /api/MasterData/business-partners` | `OCRD` | `cardType=S`, `search` | Master vendor armada dan supplier suku cadang |
| **Warehouses** | `GET /api/MasterData/warehouses` | `OWHS` | - | Master gudang darat, dermaga, dan armada |
| **Taxes / PPN** | `GET /api/MasterData/taxes`<br>`GET /api/MasterData/vat-groups` | `OVTG` | - | Kode pajak transaksi (PPN 11%, Bebas PPN, dsb.) |
| **Unit of Measure** | `GET /api/MasterData/uoms`<br>`GET /api/MasterData/unit-of-measurements` | `OUOM` | - | Satuan dasar barang (PCS, SET, CAN, LTR, dsb.) |
| **UoM Groups** | `GET /api/MasterData/uom-groups`<br>`GET /api/MasterData/unit-of-measurement-groups` | `OUGP` | - | Grup satuan konversi kemasan |
| **Bin Locations** | `GET /api/MasterData/bin-locations` | `OBIN` | `warehouse` (optional) | Lokasi rak / bin penyimpanan per gudang |
| **Cost Centers** | `GET /api/MasterData/cost-centers`<br>`GET /api/MasterData/profit-centers` | `OPRC` | `dimension` (1: Dept, 2: Armada) | Pusat biaya operasional kapal & departemen |
| **Payment Terms** | `GET /api/MasterData/payment-terms`<br>`GET /api/MasterData/payment-terms-types` | `OCTG` | - | Termin pembayaran vendor (COD, Net 30, dsb.) |
| **Freight** | `GET /api/MasterData/freight`<br>`GET /api/MasterData/additional-expenses` | `OEXD` | - | Biaya ekspedisi darat, trucking, dan pelayaran kapal |
| **Projects** | `GET /api/MasterData/projects` | `OPRJ` | `search` | Kode proyek docking kapal / overhaul armada |
| **Item Groups** | `GET /api/MasterData/item-groups` | `OITB` | - | Daftar kelompok barang suku cadang katalog |
| **HS Codes** | `GET /api/MasterData/hs-codes` | `OCHS` | `search` | Klasifikasi HS Code bea cukai & status lartas |
| **Part Numbers** | `GET /api/MasterData/part-numbers` | `@SOL_PNUM_H`<br>`@SOL_PNUM_D` | `search`, `top=100`, `skip=0` | Master part number suku cadang mesin kapal (UDO) |

---

## 3. Peta Endpoint Dokumen Transaksi (Staging Gateway)

| Modul | Endpoint | HTTP Method | Tabel Staging SAP |
| :--- | :--- | :---: | :---: |
| **Purchase Order** | `/api/PurchaseOrder` | `POST` | `@SOL_PO_H`, `@SOL_PO_D` |
| **PO Status Check** | `/api/PurchaseOrder/status/{webTxNumber}` | `GET` | `@SOL_PO_H` |
| **PO Batch Sync Status** | `/api/PurchaseOrder/sync-status?since={date}` | `GET` | `@SOL_PO_H` |
| **PO Cancel Document** | `/api/PurchaseOrder/{webTxNumber}` / `cancel` | `DELETE` / `POST` | `@SOL_PO_H` |
| **Goods Receipt PO** | `/api/GoodsReceiptPO` | `POST` | `@SOL_GRPO_H`, `@SOL_GRPO_D` |
| **GRPO Status Check** | `/api/GoodsReceiptPO/status/{webTxNumber}` | `GET` | `@SOL_GRPO_H` |
| **Stock Transfer** | `/api/StockTransfer` | `POST` | `@SOL_IT_H`, `@SOL_IT_D` |
| **Stock Transfer Status** | `/api/StockTransfer/status/{webTxNumber}` | `GET` | `@SOL_IT_H` |

---

## 4. Standar Kode & Konvensi Dual Database

Untuk memastikan query Dapper kompatibel baik di Microsoft SQL Server maupun SAP HANA:
1. **Identifier Quoting**:
   - HANA: Selalu gunakan double quotes pada nama tabel dan kolom (`"@SOL_ITEMHIER"`, `"U_SOL_Code"`).
   - SQL Server: Gunakan bracket (`[@SOL_ITEMHIER]`, `[U_SOL_Code]`).
2. **Pagination**:
   - HANA / MySQL: `LIMIT {top} OFFSET {skip}`.
   - SQL Server: `SELECT TOP ({top + skip}) ...` dipadukan dengan LINQ `.Skip(skip).Take(top)` atau syntax `OFFSET @skip ROWS FETCH NEXT @top ROWS ONLY`.
3. **Pembersihan String Null**:
   - Gunakan `NULLIF(NULLIF(col, '-'), '')` di level query SQL untuk mengeliminasi tanda strip dan string hampa.
   - Gunakan `string.IsNullOrWhiteSpace(...)` dan `.Trim()` di level C# sebelum serialisasi JSON.

---

## 5. Standar Git Tracking, Versioning & Alur Rilis Wajib

### 5.1 Aturan Wajib Git Tracking (Mandatory Push Protocol)
Setiap pengembang atau konsultan yang melakukan pemutakhiran, penambahan endpoint, perbaikan bug, atau perubahan skema tabel pada proyek Integration Hub **WAJIB melakukan commit dan push ke remote repository Git resmi** demi menjamin transparansi, audit trail, kemudahan rollback, serta sinkronisasi dengan tim Web App:
- **Repository Remote**: `https://github.com/nyanky0/IntegrationHubGuiSwag`
- **Branch Utama**: `main`

### 5.2 Standar Versioning (Semantic Versioning)
Proyek ini mengadopsi format versioning **SemVer (`v{MAJOR}.{MINOR}.{PATCH}`)**:
- **MAJOR**: Perubahan besar arsitektur atau breaking changes pada kontrak payload staging.
- **MINOR**: Penambahan fitur baru, endpoint staging baru, atau modul master data baru yang backward-compatible.
- **PATCH**: Perbaikan bug, optimasi query Dapper, penyesuaian logging, atau update dokumentasi.

#### Riwayat Versi Resmi:
- **`v1.0.0`**: Rilis awal Web API .NET 8, OAuth2 JWT Bearer, Swagger UI, Purchase Order Staging, dan Scheduler WinForms GUI (DI API).
- **`v1.1.0`**: Penambahan Debug tab, total transaction purge, row delete context menu, staging DB health indicator, auto-resolution [-5006], serta selective sync queue.
- **`v1.2.0`**:
  - Penambahan endpoint `GET /api/MasterData/item-hierarchy` (`@SOL_ITEMHIER`).
  - Penambahan seluruh master data pengadaan: `taxes`, `uoms`, `uom-groups`, `bin-locations`, `cost-centers`, `payment-terms`, `freight`, `projects`, `item-groups`, `hs-codes`, dan `part-numbers`.
  - Penegasan Authoritative Domain Scope (RL tidak ada di SAP, tidak ada GI/GR mandiri, tidak ada AP DP/Invoice di web).
  - Pembentukan struktur dokumentasi resmi pada folder `docs/`.

### 5.3 Prosedur Eksekusi Git Wajib
Sebelum menandai pekerjaan selesai:
1. Pastikan solusi dapat dibuild bersih (`dotnet build` $\to$ 0 error).
2. Periksa status perubahan:
   ```bash
   git status
   ```
3. Stage semua perubahan kode dan dokumentasi:
   ```bash
   git add .
   ```
4. Buat commit terstruktur menggunakan *Conventional Commits*:
   ```bash
   git commit -m "feat(master-data): implement item-hierarchy and all procurement master data endpoints (v1.2.0)"
   ```
5. Buat tag rilis git:
   ```bash
   git tag -a v1.2.0 -m "Release v1.2.0: Item Hierarchy and Full Master Data Suite"
   ```
6. Push commit beserta tags ke remote repository:
   ```bash
   git push origin main --tags
   ```

