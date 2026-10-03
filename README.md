# Integration Hub: SOLTIUS Web API & Scheduler Add-On (GUI & Swagger)

Integration Hub adalah middleware enterprise yang menjembatani komunikasi transaksi antara aplikasi client eksternal (seperti Web App IBT Laravel/React) dengan **SAP Business One** menggunakan arsitektur **Staging Database**, **Web API (.NET 8)** berbasis **OAuth2 & Swagger**, dan **Scheduler Add-On (.NET Framework 4.8 / WinForms GUI)** dengan integrasi penuh **SAP Business One Service Layer (OData REST)**.

---

## 🏗️ Arsitektur Sistem

```
┌──────────────────────────────────────────────┐
│         Client Web App (IBT Web)             │
│            (Laravel + React)                 │
└──────────────────────┬───────────────────────┘
                       │ 1. REST Calls / OAuth2 Bearer Token
                       ▼
┌──────────────────────────────────────────────┐
│             SOLTIUS Web API Hub              │
│       (.NET 8 - OAuth2, Swagger UI)          │
│ ──────────────────────────────────────────── │
│ • Master Data CRUD via SAP Service Layer     │
│ • Staging Transaction Queue (PO, GRPO,       │
│   Goods Return, Stock Transfer)              │
│ • Vendor Mapping (TBL_VENDOR_MAPPING)        │
└──────────────────────┬───────────────────────┘
                       │ 2. Enqueue Transaction / Readback
                       ▼
┌──────────────────────────────────────────────┐
│               Staging Database               │
│             (SQL Server / NYANKYO)           │
│   SOL_PURCHASE_ORDER_*, SOL_STOCK_TRANSFER_* │
│   SOL_GOODS_RECEIPT_*, TBL_VENDOR_MAPPING    │
└──────────────────────┬───────────────────────┘
                       │ 3. Periodic / Real-Time Polling
                       ▼
┌──────────────────────────────────────────────┐
│           SOLTIUS Scheduler Add-On           │
│         (Headless Engine & WinForms)         │
│ ──────────────────────────────────────────── │
│ • Dynamic Timer Adjustment (No Restart)      │
│ • Branch 3 ↔ WH-IBT Validation               │
│ • Auto-Skip / Protection PO DocEntry 2       │
└──────────────────────┬───────────────────────┘
                       │ 4. OData REST (POST / GET / Readback)
                       ▼
┌──────────────────────────────────────────────┐
│        SAP Business One Service Layer        │
│           (b1s/v2 - HTTPS / HTTP)            │
│  PurchaseOrders, PurchaseDeliveryNotes,      │
│  PurchaseReturns, StockTransfers, Items,     │
│  BusinessPartners, Warehouses                │
└──────────────────────────────────────────────┘
```

---

## 📦 Komponen Project

| Project | Target Runtime | Deskripsi & Tanggung Jawab |
|---|---|---|
| **`SOLTIUS - Web API Add-On`** | .NET 8 (ASP.NET Core) | REST API endpoints, OAuth2 Client Credentials & JWT Bearer token, Swagger UI interaktif, validasi transaksi idempotent, operasi Master Data 100% via SAP Service Layer dengan readback verifikasi. |
| **`SOLTIUS - Scheduler Add-On`** | .NET Framework 4.8 (WinForms) | Antarmuka desktop GUI dan background headless engine (`SyncSchedulerEngine`), sinkronisasi dokumen procurement & logistik langsung via SAP Service Layer OData REST, validasi pasangan cabang & gudang, log viewer interaktif, dan ekspor Excel. |
| **`SOLTIUS - Web API Add-On.Tests`** | .NET 9 (xUnit) | Test suite otomasi: unit tests untuk error propagation, duplicate rejection, vendor code mapping, validasi pasangan BPL-gudang, timer dynamic restart, dan opt-in live SAP readback integration tests. |

---

## 🚀 Fitur Utama

### 1. 100% Service Layer Only (Zero Direct SAP SQL, Zero DI API)
- Semua operasi baca dan tulis data SAP B1 kini berjalan eksklusif melalui **SAP Business One Service Layer** (`/b1s/v2`).
- Tidak ada direct SQL ke tabel SAP (`OITM`, `OCRD`, `OWHS`, dsb.).
- Penanganan sesi otomatis: `B1SESSION` dan `ROUTEID` cookie management dengan auto-relogin saat token kedaluwarsa.

### 2. Penanganan Master Data & Pemetaan Vendor (Series 73)
- **Vendor Mapping Persistent**: Web vendor code (misal `V-MARINDO-01`) dipetakan secara persisten ke SAP CardCode otomatis Series 73 (`VL-00xxx`) dalam tabel `TBL_VENDOR_MAPPING`.
- **Readback Verification**: Setiap penambahan/modifikasi master data langsung membaca ulang entity SAP sebelum mengembalikan status sukses.
- **Propagation Error Akurat**: Pesan error dan kode kesalahan asli dari Service Layer diteruskan langsung ke client HTTP tanpa fallback data diam-diam.

### 3. Validasi Pasangan Cabang & Gudang (Anti-Cross Branch Fallback)
- Sistem memberlakukan aturan validasi ketat: **Cabang BPLID 3 (PT Indobaruna Bulk Transport)** wajib berpasangan dengan gudang **`WH-IBT`**.
- Dilarang keras melakukan fallback otomatis ke gudang cabang lain (`DC`, `JK-MAIN`, `WH-ISL`, dll.). Transaksi dengan pasangan cabang-gudang yang tidak cocok akan ditolak seketika.

### 4. Proteksi Transaksi & Anti-Reproses
- Transaksi uji awal dengan nomor referensi `PO\IBT\HOF\269901` (DocEntry 2) dilindungi secara permanen dari pemrosesan ulang (*idempotent protection*).

### 5. Engine Scheduler Headless dengan Timer Dinamis
- Scheduler Add-On mendukung in-app engine dan Windows Service headless.
- Pengaturan interval atau mode waktu (Interval vs Real-time) dapat diubah langsung dari UI tanpa perlu me-restart aplikasi (`SyncSchedulerEngine.RestartIfRunning`).
- Mendukung sinkronisasi 4 dokumen procurement & logistik:
  1. **Purchase Order** (`PurchaseOrders` / OPOR)
  2. **Goods Receipt PO** (`PurchaseDeliveryNotes` / OPDN)
  3. **Goods Return** (`PurchaseReturns` / ORPD)
  4. **Stock Transfer** (`StockTransfers` / OWTR)
- Two-way status reconciliation otomatis mendeteksi dokumen yang di-close atau di-cancel di SAP dan mengirimkan callback webhook ke Web App.

---

## 📡 Ringkasan Endpoint API

### 1. Autentikasi OAuth2
- `POST /oauth2/token`: Meminta Bearer JWT token menggunakan `client_credentials`.

### 2. Dokumen Transaksi Staging
- `POST /api/PurchaseOrder`: Mengirim Purchase Order baru ke staging.
- `GET /api/PurchaseOrder/status/{webTxNumber}`: Cek status PO di staging/SAP.
- `POST /api/GoodsReceiptPO`: Mengirim Goods Receipt PO ke staging.
- `POST /api/GoodsReturn`: Mengirim Goods Return ke staging.
- `POST /api/StockTransfer`: Mengirim Stock Transfer logistik ke staging.

### 3. Master Data (via Service Layer)
- `GET /api/MasterData/items`: Mengambil katalog suku cadang dari Service Layer `Items`.
- `POST /api/MasterData/items`: Membuat master item baru di SAP dengan readback verifikasi.
- `GET /api/MasterData/business-partners`: Mengambil vendor/customer dari Service Layer `BusinessPartners`.
- `POST /api/MasterData/business-partners`: Mendaftarkan vendor baru dengan Series 73 dan pemetaan kode sumber.
- `GET /api/MasterData/warehouses`: Mengambil daftar gudang dari Service Layer `Warehouses`.
- `POST /api/MasterData/warehouses`: Menambahkan gudang baru.
- Endpoint pendukung lainnya: `taxes`, `uoms`, `uom-groups`, `bin-locations`, `cost-centers`, `payment-terms`, `freight`, `projects`, `item-groups`, `hs-codes`, dan `part-numbers`.

---

## 🧪 Pengujian Otomatis (Automated Testing)

Jalankan test suite menggunakan .NET CLI:
```bash
# Menjalankan unit tests
dotnet test "SOLTIUS - Web API Add-On.Tests/SOLTIUS - Web API Add-On.Tests.csproj"

# Menjalankan integration test terhadap live SAP Service Layer (opt-in)
set RUN_SAP_INTEGRATION_TESTS=true
dotnet test "SOLTIUS - Web API Add-On.Tests/SOLTIUS - Web API Add-On.Tests.csproj" --filter "LiveSapServiceLayer_Readback_WhenEnabled"
```

---

## ⚙️ Panduan Menjalankan

### A. Prasyarat
- Windows OS (mendukung GUI Scheduler Add-On & WinForms).
- .NET 8 SDK (untuk Web API) dan .NET Framework 4.8 Runtime (untuk Scheduler).
- Akses ke SAP Business One Service Layer (default: `http://localhost:50001/b1s/v2`).
- SQL Server (database staging `NYANKYO`).

### B. Menjalankan Web API Add-On
```bash
cd "SOLTIUS - Web API Add-On"
dotnet run --urls http://localhost:5006
```
Akses Swagger UI: `http://localhost:5006/swagger`

### C. Menjalankan Scheduler Add-On
Build solusi menggunakan Visual Studio 2022 atau MSBuild:
```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'SOLTIUS - Scheduler Add-On\SOLTIUS - Scheduler Add-On.csproj' /p:Configuration=Debug
```
Jalankan file executable dari folder `SOLTIUS - Scheduler Add-On/bin/Debug/SOLTIUS - Scheduler Add-On.exe`.

---

## 🛡️ Keamanan & Kredensial
- Hardcoded fallback password telah dihilangkan seluruhnya dari kode sumber.
- Konfigurasikan kredensial SAP dan staging melalui Environment Variables (`SAP_B1_PASSWORD`, `LOG_DB_PASS`, `JWT_SIGNING_KEY`) atau file konfigurasi lokal yang aman.

---

## 📄 Lisensi
Hak Cipta © 2026 SOLTIUS / PT Metrodata Electronics Tbk. Seluruh hak cipta dilindungi undang-undang.
